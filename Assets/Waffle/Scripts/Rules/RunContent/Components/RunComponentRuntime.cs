using System;
using System.Collections.Generic;
using Waffle.Combat;

namespace Waffle.RunContent.Components
{
    /// <summary>
    /// RC02 规则组件运行时（纯 C#，每局 new 一个）。不自带循环、不读时间、不存敌人 HP/XP。
    /// 宿主调用顺序（详见 Docs/Design/RC02/Growth/API.md）：
    ///   开局 new RunComponentRuntime(growth) → 选择后 Refresh() →
    ///   T1 实际闪避开始时 NotifyDodge(pos) → T6 之后 T7 之前 Advance(input, enemies, commands) → 宿主执行 commands →
    ///   暂停不调用；调试传送/复活后 RebaseMovement(pos)；终局/重开丢弃实例或 Clear()。
    /// 只处理 ComponentOwnedIds 中的内容；其余继续走旧入口（迁移表见 API.md）。
    /// </summary>
    public sealed class RunComponentRuntime
    {
        /// <summary>默认由组件入口负责的内容。W01 默认仍在旧入口，宿主须显式 MigrateLegacy("W01") 并同时跳过旧 W01。</summary>
        public static readonly string[] DefaultOwnedIds = { "W05", "W06", "P17" };

        readonly RunGrowthState growth;
        readonly HashSet<string> owned = new HashSet<string>(DefaultOwnedIds);
        readonly CooldownTrigger[] cooldowns = { new CooldownTrigger(), new CooldownTrigger(), new CooldownTrigger(), new CooldownTrigger() };
        readonly DistanceAccumulator distance = new DistanceAccumulator();
        readonly DodgeTrigger dodge = new DodgeTrigger();
        readonly ChainArcCarrier chain = new ChainArcCarrier();
        readonly ResidualZoneField trail = new ResidualZoneField();
        GrowthStats stats;
        int nextChainId = 1;
        bool dodgePending; Vec2 dodgePos;

        public IReadOnlyCollection<string> ComponentOwnedIds => owned;
        public IReadOnlyList<ResidualZoneField.Point> TrailPoints => trail.Points;
        public GrowthStats Stats => stats;

        public RunComponentRuntime(RunGrowthState growth)
        {
            this.growth = growth ?? throw new ArgumentNullException(nameof(growth));
            Refresh();
        }

        /// <summary>把旧入口武器迁到组件入口（目前只支持 W01 样板）。调用后宿主必须在 RunWeaponStep 跳过该 ID。</summary>
        public bool MigrateLegacy(string id) { if (id != "W01") return false; owned.Add(id); return true; }
        public bool Owns(string id) => owned.Contains(id);

        /// <summary>每次成长选择被接受后调用一次（与 CombatModule.RefreshRunContent 同时）。</summary>
        public void Refresh() { stats = GrowthStats.From(growth); }

        /// <summary>T1：仅在闪避真正发动（RunSimulation 发 Dodge 事件的同一分支）时调用。</summary>
        public void NotifyDodge(Vec2 playerPos) { dodgePending = true; dodgePos = playerPos; }
        /// <summary>调试传送、复活或任何非运动位置改变后调用，避免累计爆发。</summary>
        public void RebaseMovement(Vec2 playerPos) { distance.Rebase(playerPos); }
        public void Clear()
        {
            foreach (var c in cooldowns) c.Reset();
            distance.Clear(); dodge.Clear(); trail.Clear(); dodgePending = false;
        }

        /// <summary>T6 之后、T7 之前调用一次。enemies 只含存活敌人快照。命令追加到 output（调用方负责 Clear）。</summary>
        public void Advance(in ComponentAdvanceInput input, IReadOnlyList<EnemySnapshot> enemies, List<RuleCommand> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            enemies = enemies ?? Array.Empty<EnemySnapshot>();
            bool dodged = dodgePending; dodgePending = false;
            var ws = growth.WeaponSlots;
            ContentRank w06 = null; int w06Slot = -1;
            for (int slot = 0; slot < ws.Count && slot < cooldowns.Length; slot++)
            {
                var o = ws[slot]; if (o == null || !owned.Contains(o.Def.Id)) continue;
                var r = o.Def.RankAt(o.Rank); if (r == null) continue;
                switch (o.Def.Id)
                {
                    case "W01": AdvanceW01(input, slot, r, enemies, output); break;
                    case "W05": AdvanceW05(input, slot, r, enemies, output); break;
                    case "W06": w06 = r; w06Slot = slot; break;
                }
            }
            if (w06 != null) AdvanceW06(input, w06Slot, w06, enemies, dodged, output);
            else distance.Rebase(input.PlayerPos); // 未持有时不积攒距离
            if (dodged) AdvanceP17(input, output);
        }

        // ---- W01 旧行为适配样板：冷却 → 最近目标 → 弹体命令（数值与 CombatModule.RunWeaponStep 一致） ----
        void AdvanceW01(in ComponentAdvanceInput input, int slot, ContentRank r, IReadOnlyList<EnemySnapshot> enemies, List<RuleCommand> output)
        {
            int target = NearestIndex(enemies, input.PlayerPos);
            if (!cooldowns[slot].Advance(input.Dt, StatModifiers.Cooldown(stats, r.Get("cd")), target >= 0)) return;
            var t = enemies[target];
            output.Add(new RuleCommand { Kind = RuleCommandKind.FireProjectile, ContentId = "W01", Slot = slot, RootId = "W01",
                Serial = t.Serial, Generation = t.Generation, Position = input.PlayerPos, From = (t.Position - input.PlayerPos).Normalized,
                Damage = StatModifiers.Damage(stats, r.Get("dmg")), Speed = StatModifiers.ProjectileSpeed(stats, r.Get("speed", 14)),
                Count = (int)r.Get("count", 1), Pierce = (int)r.Get("pierce", 1), BudgetLeft = (int)r.Get("budget", 1), AllowsMechanism = true });
        }

        // ---- W05 弹跳电弧：冷却 → 最近目标 → 有限链击 → 衰减/分叉 ----
        void AdvanceW05(in ComponentAdvanceInput input, int slot, ContentRank r, IReadOnlyList<EnemySnapshot> enemies, List<RuleCommand> output)
        {
            float first = StatModifiers.Radius(stats, r.Get("range", 7f));
            bool any = NearestIndex(enemies, input.PlayerPos, first) >= 0;
            if (!cooldowns[slot].Advance(input.Dt, StatModifiers.Cooldown(stats, r.Get("cd")), any)) return;
            var spec = new ChainArcCarrier.Spec { ContentId = "W05", Slot = slot, ChainId = nextChainId++,
                Damage = StatModifiers.Damage(stats, r.Get("dmg")), Falloff = r.Get("falloff", 1f), FirstRange = first,
                Jump = StatModifiers.Radius(stats, r.Get("jump")), Bounces = (int)r.Get("bounces"), Fork = (int)r.Get("fork", 1),
                Budget = (int)r.Get("budget") };
            chain.Cast(spec, input.PlayerPos, enemies, output);
        }

        // ---- W06 移动残响：实际移动累计 → 布置残留 → 接触节流；阶5 实际闪避引爆最近N点 ----
        void AdvanceW06(in ComponentAdvanceInput input, int slot, ContentRank r, IReadOnlyList<EnemySnapshot> enemies, bool dodged, List<RuleCommand> output)
        {
            float life = StatModifiers.Duration(stats, r.Get("life", 3));
            float radius = StatModifiers.Radius(stats, r.Get("radius", 0.8f));
            int max = (int)r.Get("max", 6);
            trail.Expire(input.Now);
            int drops = distance.Feed(input.PlayerPos, input.MaxLegalStep, r.Get("spacing", 2.5f), 2);
            for (int i = 0; i < drops; i++) trail.Place(input.PlayerPos, input.Now, life, max, "W06", slot, radius, output);
            if (dodged && r.Get("detonate") > 0)
                trail.Detonate(input.PlayerPos, (int)r.Get("detonate"), StatModifiers.Damage(stats, r.Get("detDmg")),
                    StatModifiers.Radius(stats, r.Get("detRadius", 1.5f)), "W06", slot, output);
            trail.Contact(input.Now, enemies, radius, r.Get("link") > 0, StatModifiers.HitInterval(stats, r.Get("hitCd", 0.6f)),
                StatModifiers.Damage(stats, r.Get("dmg")), "W06", slot, output);
        }

        // ---- P17 闪避放电：实际闪避 → 独立节流 → 玩家位置范围伤害 ----
        void AdvanceP17(in ComponentAdvanceInput input, List<RuleCommand> output)
        {
            int rank = growth.RankOf("P17"); if (rank <= 0 || !owned.Contains("P17")) return;
            var r = growth.Catalog.Get("P17").RankAt(rank);
            dodge.Notify(dodgePos);
            if (!dodge.Consume(input.Now, r.Get("dodgeCd", 1.5f), out var at)) return;
            output.Add(new RuleCommand { Kind = RuleCommandKind.AreaDamage, ContentId = "P17", Slot = -1, RootId = "P17",
                Position = at, From = at, Damage = StatModifiers.Damage(stats, r.Get("dodgeDmg")),
                Radius = StatModifiers.Radius(stats, r.Get("dodgeRadius")), Depth = 1, BudgetLeft = (int)r.Get("budget", 16) });
        }

        static int NearestIndex(IReadOnlyList<EnemySnapshot> enemies, Vec2 from, float range = float.MaxValue)
        {
            int best = -1; float bd = range >= float.MaxValue ? float.MaxValue : range * range;
            for (int i = 0; i < enemies.Count; i++)
            {
                float d = Vec2.SqrDistance(from, enemies[i].Position);
                if (d < bd || (d == bd && best >= 0 && enemies[i].Serial < enemies[best].Serial)) { bd = d; best = i; }
            }
            return best;
        }
    }
}
