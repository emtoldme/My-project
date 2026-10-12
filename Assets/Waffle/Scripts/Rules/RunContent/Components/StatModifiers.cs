using System;

namespace Waffle.RunContent.Components
{
    /// <summary>
    /// 修饰：唯一读取 GrowthStats 的地方（组件侧）。宿主已有的 P03/P05/P08 消费不在这里重复：
    /// P03 弹速 → CombatModule 弹体 Speed×ProjSpeedMul（W01/W02/E01/E02）；FireProjectile 命令的 Speed 已乘，宿主照用不再乘。
    /// P05 持续 → W04 区域 EndsAt×DurationMul（宿主）；W06 残留寿命×DurationMul（本组件）。不延长无敌/回声延迟/闪避。
    /// P08 减伤 → RunSimulation.ApplyDamage 调 GrowthStats.FinalIncoming（宿主唯一一次，≤50%，≥1）；组件不碰受伤链。
    /// </summary>
    public static class StatModifiers
    {
        public static float Damage(in GrowthStats s, float rank) => s.FinalDamage(rank);
        public static float Radius(in GrowthStats s, float rank) => s.FinalRadius(rank);
        public static float Cooldown(in GrowthStats s, float rank) => s.FinalCooldown(rank);
        public static float Duration(in GrowthStats s, float rank) => Math.Max(0f, rank) * s.DurationMul;
        public static float ProjectileSpeed(in GrowthStats s, float rank) => Math.Max(0f, rank) * s.ProjSpeedMul;
        /// <summary>接触/命中节流按频率缩短，与 W03 hitCd/RateMul 口径一致，下限 0.15s。</summary>
        public static float HitInterval(in GrowthStats s, float rank) => Math.Max(GrowthStats.MinCooldown, rank / s.RateMul);
    }
}
