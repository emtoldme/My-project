// TEST_ONLY：M01 回声 + 传播规则适配。每局一个实例；不持有 HP，只排队并经宿主申请派生伤害。
using System;
using System.Collections.Generic;

namespace Reverb.Experiments.Mechanism.Rules
{
    public sealed class MechanismRules
    {
        // 一次直接命中的预算（触发链）。
        private sealed class Chain
        {
            public MechanismTarget Origin;
            public MechanismVec2 HitPosition;
            public int InitialDamage;
            public int PendingChildren;
            public MechanismTarget PropagatedA, PropagatedB;
            public int PropagatedCount;
        }

        private struct Pending
        {
            public double Time;
            public long Seq;
            public DerivedKind Kind;
            public Chain Chain;
            public MechanismVec2 Center;  // 传播中心
        }

        private readonly IMechanismHost host;
        private readonly MechanismRulesConfig cfg;
        private readonly List<Pending> pending = new List<Pending>();
        private readonly Stack<Chain> chainPool = new Stack<Chain>();
        private readonly List<MechanismLiveTarget> liveBuffer = new List<MechanismLiveTarget>();
        private readonly List<DerivedDamageEvent> events = new List<DerivedDamageEvent>();
        private long nextSeq;
        private int activeChains;

        public MechanismMode Mode { get; }
        public int EchoHits { get; private set; }
        public int PropagationHits { get; private set; }
        public int PendingCount => pending.Count;
        public int ActiveChainCount => activeChains;
        /// <summary>最近一次 Tick 结算的派生事件；下一次 Tick 开头清空。</summary>
        public IReadOnlyList<DerivedDamageEvent> Events => events;

        public bool EchoEnabled => Mode == MechanismMode.Echo || Mode == MechanismMode.Combined;
        public bool PropagationEnabled => Mode == MechanismMode.Propagation || Mode == MechanismMode.Combined;

        public MechanismRules(MechanismMode mode, MechanismRulesConfig config, IMechanismHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            cfg = (config ?? new MechanismRulesConfig()).Clone();
            Mode = mode;
        }

        /// <summary>T6 收集到的真实直接命中。返回是否产生了排队。</summary>
        public bool ReportDirectHit(in DirectHitRecord hit)
        {
            if (Mode == MechanismMode.None || hit.Damage <= 0) return false;
            Chain c = chainPool.Count > 0 ? chainPool.Pop() : new Chain();
            c.Origin = hit.Target; c.HitPosition = hit.Position; c.InitialDamage = hit.Damage;
            c.PendingChildren = 0; c.PropagatedCount = 0;
            activeChains++;
            if (PropagationEnabled) Enqueue(hit.Time + cfg.PropagationDelay, DerivedKind.Propagation, c, hit.Position);
            if (EchoEnabled) Enqueue(hit.Time + cfg.EchoDelay, DerivedKind.Echo, c, hit.Position);
            return true;
        }

        /// <summary>处理计划时间 ≤ now 的事件。暂停/升级/终局时宿主不调用。</summary>
        public void Tick(double now)
        {
            events.Clear();
            while (pending.Count > 0 && pending[0].Time <= now)
            {
                Pending p = pending[0];
                pending.RemoveAt(0);
                Resolve(p);
                if (--p.Chain.PendingChildren == 0) Recycle(p.Chain);
            }
        }

        /// <summary>局间清理（正常做法是重开时直接新建实例）。</summary>
        public void Clear()
        {
            foreach (var p in pending) if (--p.Chain.PendingChildren == 0) Recycle(p.Chain);
            pending.Clear(); events.Clear();
            EchoHits = 0; PropagationHits = 0;
        }

        private void Resolve(in Pending p)
        {
            Chain c = p.Chain;
            if (p.Kind == DerivedKind.Echo)
            {
                if (!host.TryGetLiveTarget(c.Origin, out var pos)) return;
                var req = new DerivedDamageRequest(DerivedKind.Echo, c.Origin, Scale(c.InitialDamage, cfg.EchoMultiplier), p.Time, pos, c.Origin);
                var res = Apply(req, pos);
                if (res == DerivedDamageResult.Applied || res == DerivedDamageResult.Killed)
                {
                    EchoHits++;
                    if (PropagationEnabled) Enqueue(p.Time + cfg.PropagationDelay, DerivedKind.EchoPropagation, c, pos);
                }
                return;
            }
            // 传播：在结算时刻选目标（上一事件已应用，不会重复击杀）。
            if (!SelectNearest(c, p.Center, out var target, out var tpos)) return;
            var preq = new DerivedDamageRequest(p.Kind, target, Scale(c.InitialDamage, cfg.PropagationMultiplier), p.Time, p.Center, c.Origin);
            var pres = Apply(preq, tpos);
            if (pres == DerivedDamageResult.Applied || pres == DerivedDamageResult.Killed)
            {
                PropagationHits++;
                if (c.PropagatedCount == 0) c.PropagatedA = target; else c.PropagatedB = target;
                c.PropagatedCount++;
            }
        }

        private DerivedDamageResult Apply(in DerivedDamageRequest req, MechanismVec2 pos)
        {
            if (req.Damage <= 0) return DerivedDamageResult.Missed;
            var res = host.ApplyDerivedDamage(req);
            events.Add(new DerivedDamageEvent(req, res, pos));
            return res;
        }

        private bool SelectNearest(Chain c, MechanismVec2 center, out MechanismTarget best, out MechanismVec2 bestPos)
        {
            liveBuffer.Clear();
            host.CollectLiveTargets(liveBuffer);
            float r2 = cfg.PropagationRadius * cfg.PropagationRadius;
            best = default; bestPos = default;
            bool found = false; float bestD = float.MaxValue;
            for (int i = 0; i < liveBuffer.Count; i++)
            {
                var t = liveBuffer[i];
                if (t.Target.Serial == c.Origin.Serial) continue;
                if (c.PropagatedCount > 0 && t.Target.Serial == c.PropagatedA.Serial) continue;
                if (c.PropagatedCount > 1 && t.Target.Serial == c.PropagatedB.Serial) continue;
                float d = t.Position.SqrDistance(center);
                if (d > r2) continue;
                if (!found || d < bestD || (d == bestD && t.Target.Serial < best.Serial))
                { found = true; bestD = d; best = t.Target; bestPos = t.Position; }
            }
            return found;
        }

        private static int Scale(int dmg, float m) => (int)Math.Round(dmg * (double)m, MidpointRounding.AwayFromZero);

        private void Enqueue(double time, DerivedKind kind, Chain c, MechanismVec2 center)
        {
            var p = new Pending { Time = time, Seq = nextSeq++, Kind = kind, Chain = c, Center = center };
            c.PendingChildren++;
            int i = pending.Count;
            while (i > 0 && (pending[i - 1].Time > time)) i--; // 同时间按 Seq（插入序）稳定
            pending.Insert(i, p);
        }

        private void Recycle(Chain c) { activeChains--; chainPool.Push(c); }
    }
}
