using System;
using System.Collections.Generic;

namespace Waffle.RunContent
{
    /// <summary>
    /// 被动属性聚合（可执行，含上限）。同组加算；不同组相乘（武器阶值 × (1+同组和)）。
    /// 负值/缺失按0处理。尚未被 CombatModule 消费——待 Codex 接线。
    /// </summary>
    public readonly struct GrowthStats
    {
        public const float DamagePctCap = 200f, RatePctCap = 150f, AreaPctCap = 100f, DurationPctCap = 100f,
            MoveSpeedPctCap = 40f, MaxHpPctCap = 100f, DamageReductionCap = 50f, PickupPctCap = 150f,
            CritPctCap = 50f, ProjSpeedPctCap = 100f, MinCooldown = 0.15f;

        public readonly float DamageMul, RateMul, AreaMul, DurationMul, ProjSpeedMul, MoveSpeedMul, MaxHpMul, PickupMul;
        public readonly float DamageReduction; // 0..0.5
        public readonly float CritChance, CritMul; // 仅直接命中
        public readonly float EchoCoef, PropCoef, PropRadius; // 0=未取得
        public readonly int PropBudget;

        private GrowthStats(Dictionary<string, float> s)
        {
            float G(string k) { return s.TryGetValue(k, out var v) && v > 0 && !float.IsNaN(v) ? v : 0f; }
            DamageMul = 1f + Math.Min(G("damagePct"), DamagePctCap) / 100f;
            RateMul = 1f + Math.Min(G("ratePct"), RatePctCap) / 100f;
            AreaMul = 1f + Math.Min(G("areaPct"), AreaPctCap) / 100f;
            DurationMul = 1f + Math.Min(G("durationPct"), DurationPctCap) / 100f;
            ProjSpeedMul = 1f + Math.Min(G("projSpeedPct"), ProjSpeedPctCap) / 100f;
            MoveSpeedMul = 1f + Math.Min(G("moveSpeedPct"), MoveSpeedPctCap) / 100f;
            MaxHpMul = 1f + Math.Min(G("maxHpPct"), MaxHpPctCap) / 100f;
            PickupMul = 1f + Math.Min(G("pickupPct"), PickupPctCap) / 100f;
            DamageReduction = Math.Min(G("damageReductionPct"), DamageReductionCap) / 100f;
            CritChance = Math.Min(G("critPct"), CritPctCap) / 100f;
            CritMul = G("critMul") > 0 ? G("critMul") : 1f;
            EchoCoef = Math.Min(G("echoCoef"), 1f);
            PropCoef = Math.Min(G("propCoef"), 1f);
            PropRadius = Math.Min(G("propRadius"), 8f);
            PropBudget = (int)Math.Min(G("propBudget"), 3f);
        }

        /// <summary>按当前被动阶数聚合（每次选择后调用一次即可，不需每帧）。</summary>
        public static GrowthStats From(RunGrowthState state)
        {
            var sum = new Dictionary<string, float>();
            foreach (var o in state.PassiveSlots)
            {
                if (o == null) continue;
                var r = o.Def.RankAt(o.Rank); if (r == null) continue;
                foreach (var p in r.Params)
                {
                    if (p.Key == "critMul" || p.Key.EndsWith("Coef") || p.Key == "propRadius" || p.Key == "propBudget" || p.Key.EndsWith("Delay"))
                        sum[p.Key] = Math.Max(sum.TryGetValue(p.Key, out var m) ? m : 0f, p.Value); // 非加算键取最大
                    else
                        sum[p.Key] = (sum.TryGetValue(p.Key, out var v) ? v : 0f) + p.Value;
                }
            }
            return new GrowthStats(sum);
        }

        /// <summary>最终冷却 = 阶值 / 频率倍率，下限 0.15s；阶值&lt;=0 视为不按冷却调度，返回0。</summary>
        public float FinalCooldown(float rankCooldown) => rankCooldown <= 0 ? 0f : Math.Max(MinCooldown, rankCooldown / RateMul);
        public float FinalDamage(float rankDamage) => Math.Max(0f, rankDamage) * DamageMul;
        public float FinalRadius(float rankRadius) => Math.Max(0f, rankRadius) * AreaMul;
        /// <summary>受伤：减免后至少1（输入&lt;=0返回0）。</summary>
        public float FinalIncoming(float raw) => raw <= 0 ? 0f : Math.Max(1f, raw * (1f - DamageReduction));
    }

    /// <summary>某武器槽当前参数（阶表 × 聚合属性）。供 Codex 多武器调度读取。</summary>
    public readonly struct WeaponRuntimeParams
    {
        public readonly string Id; public readonly int Slot, Rank;
        public readonly float Damage, Cooldown, Radius;
        public readonly ContentRank RankData;
        public WeaponRuntimeParams(string id, int slot, int rank, float damage, float cooldown, float radius, ContentRank data)
        { Id = id; Slot = slot; Rank = rank; Damage = damage; Cooldown = cooldown; Radius = radius; RankData = data; }

        public static List<WeaponRuntimeParams> From(RunGrowthState state)
        {
            var stats = GrowthStats.From(state);
            var list = new List<WeaponRuntimeParams>();
            var ws = state.WeaponSlots;
            for (int i = 0; i < ws.Count; i++)
            {
                var o = ws[i]; if (o == null) continue;
                var r = o.Def.RankAt(o.Rank);
                list.Add(new WeaponRuntimeParams(o.Def.Id, i, o.Rank, stats.FinalDamage(r.Get("dmg")), stats.FinalCooldown(r.Get("cd")),
                    stats.FinalRadius(r.Get("radius", r.Get("range"))), r));
            }
            return list;
        }
    }
}
