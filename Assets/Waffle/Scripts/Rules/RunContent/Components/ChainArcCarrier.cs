using System;
using System.Collections.Generic;
using Waffle.Combat;

namespace Waffle.RunContent.Components
{
    /// <summary>
    /// 选目标＋载体：有限链击（W05）。同链不重复目标（serial+generation），深度与总预算封顶，可分叉。
    /// 每局随运行时新建（内部只有复用缓冲）。只产生 TargetDamage 命令；第一跳 AllowsMechanism=true（可交给 P12/P13 一次），后续跳为派生。
    /// </summary>
    public sealed class ChainArcCarrier
    {
        public const int HardBudgetCap = 8, HardDepthCap = 6;

        public struct Spec
        {
            public string ContentId; public int Slot, ChainId;
            public float Damage, Falloff, FirstRange, Jump;
            public int Bounces, Fork, Budget;
        }

        readonly HashSet<long> visited = new HashSet<long>();
        readonly List<int> frontier = new List<int>(8), next = new List<int>(8);
        readonly List<Vec2> frontierPos = new List<Vec2>(8), nextPos = new List<Vec2>(8);

        /// <summary>返回本次链击写入的命令数。enemies 为只读快照；不修改任何敌人。</summary>
        public int Cast(in Spec s, Vec2 origin, IReadOnlyList<EnemySnapshot> enemies, List<RuleCommand> output)
        {
            visited.Clear(); frontier.Clear(); frontierPos.Clear();
            int budget = Math.Min(HardBudgetCap, s.Budget > 0 ? s.Budget : s.Bounces + 1);
            int maxDepth = Math.Min(HardDepthCap, Math.Max(0, s.Bounces));
            int first = Nearest(enemies, origin, s.FirstRange);
            if (first < 0 || budget <= 0) return 0;
            int written = 0; float dmg = s.Damage;
            Emit(s, enemies[first], origin, dmg, 0, --budget, output); written++;
            visited.Add(enemies[first].Key); frontier.Add(first); frontierPos.Add(enemies[first].Position);
            for (int depth = 1; depth <= maxDepth && budget > 0 && frontier.Count > 0; depth++)
            {
                dmg *= s.Falloff > 0 ? s.Falloff : 1f;
                int branches = s.Fork > 1 && depth == 2 ? s.Fork : 1; // 阶5：第2跳分叉
                next.Clear(); nextPos.Clear();
                for (int f = 0; f < frontier.Count && budget > 0; f++)
                    for (int b = 0; b < branches && budget > 0; b++)
                    {
                        int hit = Nearest(enemies, frontierPos[f], s.Jump);
                        if (hit < 0) break;
                        visited.Add(enemies[hit].Key);
                        Emit(s, enemies[hit], frontierPos[f], dmg, depth, --budget, output); written++;
                        next.Add(hit); nextPos.Add(enemies[hit].Position);
                    }
                frontier.Clear(); frontier.AddRange(next); frontierPos.Clear(); frontierPos.AddRange(nextPos);
            }
            return written;
        }

        static void Emit(in Spec s, in EnemySnapshot e, Vec2 from, float dmg, int depth, int budgetLeft, List<RuleCommand> output)
        {
            output.Add(new RuleCommand { Kind = RuleCommandKind.TargetDamage, ContentId = s.ContentId, Slot = s.Slot,
                RootId = s.ContentId, ChainId = s.ChainId, Serial = e.Serial, Generation = e.Generation,
                Position = e.Position, From = from, Damage = dmg, Depth = depth, BudgetLeft = budgetLeft,
                AllowsMechanism = depth == 0 });
        }

        /// <summary>最近未访问目标（边界含），等距按 serial 小者优先，保证确定性。</summary>
        int Nearest(IReadOnlyList<EnemySnapshot> enemies, Vec2 from, float range)
        {
            int best = -1; float bestD = range * range;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (visited.Contains(e.Key)) continue;
                float d = Vec2.SqrDistance(from, e.Position);
                if (d < bestD || (d == bestD && (best < 0 || e.Serial < enemies[best].Serial))) { bestD = d; best = i; }
            }
            return best;
        }
    }
}
