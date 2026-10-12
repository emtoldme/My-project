// TEST_ONLY 技术夹具：E-02 装备组合实验（延迟回声 A / 单次传播 B）。纯 C#，不引用 UnityEngine，不接正式规则。
using System;
using System.Collections.Generic;
using System.Text;

namespace Waffle.Experiments.Equipment
{
    public enum ComboEventKind { Direct, Echo, Propagation }

    public enum ComboOutcome { Applied, Killed, SkippedStale, SkippedDead }

    /// <summary>目标句柄：Id + Generation。回池/重生会使 Generation 递增，旧句柄失效。</summary>
    public readonly struct TargetHandle : IEquatable<TargetHandle>
    {
        public readonly int Id;
        public readonly int Generation;
        public TargetHandle(int id, int generation) { Id = id; Generation = generation; }
        public bool Equals(TargetHandle o) => Id == o.Id && Generation == o.Generation;
        public override bool Equals(object obj) => obj is TargetHandle h && Equals(h);
        public override int GetHashCode() => Id * 397 ^ Generation;
        public override string ToString() => "T" + Id + "g" + Generation;
    }

    /// <summary>最小配置（只读值）。A=延迟回声，B=单次传播；A+B 时回声可再引发一次受限传播。</summary>
    public sealed class ComboConfig
    {
        public int DirectDamage = 10;
        public bool EchoEnabled;            // 模块 A
        public float EchoDelay = 0.5f;
        public int EchoDamage = 5;
        public bool PropagationEnabled;     // 模块 B
        public float PropagationDelay = 0.1f;
        public int PropagationDamage = 4;
        public float PropagationRange = 5f;
        public int MaxEventsPerTrigger = 4; // 每次直接命中派生事件总数上限（不含直接命中本身）
        public int MaxDepth = 2;            // 直接命中深度0；回声/传播 = 父深度+1

        public static ComboConfig None() => new ComboConfig();
        public static ComboConfig A() => new ComboConfig { EchoEnabled = true };
        public static ComboConfig B() => new ComboConfig { PropagationEnabled = true };
        public static ComboConfig AB() => new ComboConfig { EchoEnabled = true, PropagationEnabled = true };
    }

    public sealed class ComboTargetSpec
    {
        public int Id; public float X; public int Hp;
        public ComboTargetSpec(int id, float x, int hp) { Id = id; X = x; Hp = hp; }
    }

    /// <summary>已结算记录（供表现层只读消费，不可回写）。</summary>
    public readonly struct ComboRecord
    {
        public readonly long Seq; public readonly long ParentSeq; public readonly int TriggerId;
        public readonly float Time; public readonly ComboEventKind Kind; public readonly int Depth;
        public readonly TargetHandle Source; public readonly TargetHandle Target;
        public readonly int Damage; public readonly int HpAfter; public readonly ComboOutcome Outcome;
        public ComboRecord(long seq, long parent, int trig, float time, ComboEventKind kind, int depth,
            TargetHandle src, TargetHandle tgt, int dmg, int hpAfter, ComboOutcome outcome)
        { Seq = seq; ParentSeq = parent; TriggerId = trig; Time = time; Kind = kind; Depth = depth;
          Source = src; Target = tgt; Damage = dmg; HpAfter = hpAfter; Outcome = outcome; }
        public override string ToString() =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "t={0:0.00} #{1} trig={2} {3} d{4} {5}->{6} dmg={7} hp={8} {9} parent={10}",
                Time, Seq, TriggerId, Kind, Depth, Source, Target, Damage, HpAfter, Outcome, ParentSeq);
    }

    /// <summary>每局独立状态：目标、时钟、确定性队列（按 时间→序号 排序）。</summary>
    public sealed class ComboRun
    {
        sealed class Target { public int Id; public float X; public int Hp; public int Gen; public bool Alive; }
        struct Pending
        {
            public long Seq, ParentSeq; public int TriggerId; public float Time; public ComboEventKind Kind;
            public int Depth; public TargetHandle Source, Target;
        }
        sealed class TriggerBudget { public int Spawned; public HashSet<int> PropagatedTo = new HashSet<int>(); }

        readonly ComboConfig config;
        readonly List<ComboTargetSpec> specs;
        readonly List<Target> targets = new List<Target>();
        readonly List<Pending> queue = new List<Pending>();
        readonly Dictionary<int, TriggerBudget> budgets = new Dictionary<int, TriggerBudget>();
        readonly List<ComboRecord> log = new List<ComboRecord>();
        long nextSeq; int nextTrigger;

        public float Time { get; private set; }
        public bool Paused { get; set; }
        public int PendingCount => queue.Count;
        public IReadOnlyList<ComboRecord> Log => log;

        public ComboRun(ComboConfig config, IEnumerable<ComboTargetSpec> targetSpecs)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            specs = new List<ComboTargetSpec>(targetSpecs);
            Restart();
        }

        /// <summary>重开：清队列、时钟、日志、序号，目标恢复初始（Generation 继续递增，旧句柄全部失效）。</summary>
        public void Restart()
        {
            queue.Clear(); budgets.Clear(); log.Clear();
            Time = 0f; nextSeq = 1; nextTrigger = 1; Paused = false;
            var oldGen = new Dictionary<int, int>();
            foreach (var t in targets) oldGen[t.Id] = t.Gen;
            targets.Clear();
            foreach (var s in specs)
            {
                oldGen.TryGetValue(s.Id, out var g);
                targets.Add(new Target { Id = s.Id, X = s.X, Hp = s.Hp, Gen = g + 1, Alive = true });
            }
        }

        public TargetHandle Handle(int id) { var t = Find(id); return new TargetHandle(id, t.Gen); }
        public int Hp(int id) => Find(id).Hp;
        public bool IsAlive(int id) => Find(id).Alive;

        /// <summary>目标回池：Generation+1，不再存活。</summary>
        public void Recycle(int id) { var t = Find(id); t.Gen++; t.Alive = false; }
        /// <summary>同 Id 重新出池：新 Generation、满血。</summary>
        public void Respawn(int id, int hp) { var t = Find(id); t.Gen++; t.Alive = true; t.Hp = hp; }

        /// <summary>一次直接命中，立即结算并处理所有已到期事件。返回触发代号。</summary>
        public int DirectHit(TargetHandle target)
        {
            int trig = nextTrigger++;
            budgets[trig] = new TriggerBudget();
            var p = new Pending { Seq = nextSeq++, ParentSeq = 0, TriggerId = trig, Time = Time,
                Kind = ComboEventKind.Direct, Depth = 0, Source = target, Target = target };
            Resolve(p);
            Drain();
            return trig;
        }

        /// <summary>推进时钟；暂停时完全不推进。</summary>
        public void Tick(float dt)
        {
            if (Paused || dt <= 0f) return;
            Time += dt;
            Drain();
        }

        void Drain()
        {
            while (queue.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < queue.Count; i++)
                {
                    var a = queue[i]; var b = queue[best];
                    if (a.Time < b.Time || (a.Time == b.Time && a.Seq < b.Seq)) best = i;
                }
                if (queue[best].Time > Time) return;
                var p = queue[best];
                queue.RemoveAt(best);
                Resolve(p);
            }
        }

        void Resolve(Pending p)
        {
            var t = Find(p.Target.Id);
            int dmg = p.Kind == ComboEventKind.Direct ? config.DirectDamage
                    : p.Kind == ComboEventKind.Echo ? config.EchoDamage : config.PropagationDamage;
            ComboOutcome outcome;
            if (t.Gen != p.Target.Generation) { outcome = ComboOutcome.SkippedStale; dmg = 0; }
            else if (!t.Alive) { outcome = ComboOutcome.SkippedDead; dmg = 0; }
            else
            {
                t.Hp = Math.Max(0, t.Hp - dmg);
                if (t.Hp == 0) { t.Alive = false; outcome = ComboOutcome.Killed; } else outcome = ComboOutcome.Applied;
            }
            log.Add(new ComboRecord(p.Seq, p.ParentSeq, p.TriggerId, p.Time, p.Kind, p.Depth,
                p.Source, p.Target, dmg, t.Hp, outcome));
            if (outcome == ComboOutcome.SkippedStale || outcome == ComboOutcome.SkippedDead) return;

            // 派生规则（固定、非递归）：
            // Direct -> A: 同目标回声；B: 传播到另一合法目标
            // Echo   -> 仅 A+B: 从回声目标再传播一次
            // Propagation -> 不派生
            if (p.Kind == ComboEventKind.Direct)
            {
                if (config.EchoEnabled && t.Alive)
                    Spawn(p, ComboEventKind.Echo, new TargetHandle(t.Id, t.Gen), config.EchoDelay);
                if (config.PropagationEnabled) TryPropagate(p, t);
            }
            else if (p.Kind == ComboEventKind.Echo && config.PropagationEnabled)
                TryPropagate(p, t);
        }

        void TryPropagate(Pending parent, Target from)
        {
            var budget = budgets[parent.TriggerId];
            Target pick = null; float bestD = float.MaxValue;
            foreach (var c in targets)
            {
                if (c.Id == from.Id || !c.Alive || budget.PropagatedTo.Contains(c.Id)) continue;
                float d = Math.Abs(c.X - from.X);
                if (d > config.PropagationRange) continue;
                if (d < bestD || (d == bestD && pick != null && c.Id < pick.Id)) { bestD = d; pick = c; }
            }
            if (pick == null) return;
            if (Spawn(parent, ComboEventKind.Propagation, new TargetHandle(pick.Id, pick.Gen), config.PropagationDelay))
                budget.PropagatedTo.Add(pick.Id);
        }

        bool Spawn(Pending parent, ComboEventKind kind, TargetHandle target, float delay)
        {
            var budget = budgets[parent.TriggerId];
            int depth = parent.Depth + 1;
            if (depth > config.MaxDepth || budget.Spawned >= config.MaxEventsPerTrigger) return false;
            budget.Spawned++;
            queue.Add(new Pending { Seq = nextSeq++, ParentSeq = parent.Seq, TriggerId = parent.TriggerId,
                Time = Time + Math.Max(0f, delay), Kind = kind, Depth = depth,
                Source = parent.Target, Target = target });
            return true;
        }

        Target Find(int id)
        {
            foreach (var t in targets) if (t.Id == id) return t;
            throw new ArgumentException("unknown target " + id);
        }

        public string FormatLog()
        {
            var sb = new StringBuilder();
            foreach (var r in log) sb.AppendLine(r.ToString());
            return sb.ToString();
        }
    }

    /// <summary>可打印的对比入口：none/A/B/A+B 同一场景。</summary>
    public static class ComboLabScenario
    {
        public static ComboTargetSpec[] DefaultTargets() => new[]
        {
            new ComboTargetSpec(1, 0f, 30), new ComboTargetSpec(2, 2f, 30),
            new ComboTargetSpec(3, 3f, 30), new ComboTargetSpec(4, 20f, 30),
        };

        public static ComboRun RunOne(ComboConfig cfg, float duration = 2f, float dt = 0.05f)
        {
            var run = new ComboRun(cfg, DefaultTargets());
            run.DirectHit(run.Handle(1));
            int steps = (int)Math.Round(duration / dt);
            for (int i = 0; i < steps; i++) run.Tick(dt);
            return run;
        }

        public static string CompareAll()
        {
            var sb = new StringBuilder();
            foreach (var (name, cfg) in new[] { ("none", ComboConfig.None()), ("A", ComboConfig.A()),
                         ("B", ComboConfig.B()), ("A+B", ComboConfig.AB()) })
            {
                var run = RunOne(cfg);
                int total = 0; foreach (var r in run.Log) total += r.Damage;
                sb.AppendLine("== " + name + " : events=" + run.Log.Count + " totalDamage=" + total + " pending=" + run.PendingCount);
                sb.Append(run.FormatLog());
            }
            return sb.ToString();
        }
    }
}
