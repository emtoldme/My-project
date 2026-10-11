using System;
using System.Collections.Generic;

namespace Waffle
{
    /// <summary>升级选项种类（§1.6 表 U-1）。None 表示面板上这一格留空。</summary>
    public enum UpgradeKind
    {
        None = -1,
        Damage = 0,
        FireRate = 1,
        Pierce = 2,
        MoveSpeed = 3,
        MaxHp = 4,
        PickupRange = 5,
        DodgeCooldown = 6,
        Heal = 7
    }

    /// <summary>
    /// 升级规则（§1.6）。所有数值都来自 RunConfig：数值 = 基础值 + 前 level 个每级增量之和。
    /// 这里不保存任何数值表（RunConfig 是唯一来源）。
    /// </summary>
    public static class UpgradeRules
    {
        /// <summary>可升级的选项数（不含兜底的"回复"）。</summary>
        public const int RealKindCount = 7;

        private static float BaseValue(UpgradeKind kind, RunConfig c)
        {
            switch (kind)
            {
                case UpgradeKind.Damage: return c.damage;
                case UpgradeKind.FireRate: return c.fireInterval;
                case UpgradeKind.Pierce: return c.pierce;
                case UpgradeKind.MoveSpeed: return c.moveSpeed;
                case UpgradeKind.MaxHp: return c.maxHp;
                case UpgradeKind.PickupRange: return c.magnetRadius;
                case UpgradeKind.DodgeCooldown: return c.dodgeCooldown;
                default: return 0f;
            }
        }

        private static float[] Steps(UpgradeKind kind, RunConfig c)
        {
            UpgradeConfig u = c.upgrades;
            if (u == null) return null;
            switch (kind)
            {
                case UpgradeKind.Damage: return u.damageSteps;
                case UpgradeKind.FireRate: return u.fireIntervalSteps;
                case UpgradeKind.Pierce: return u.pierceSteps;
                case UpgradeKind.MoveSpeed: return u.moveSpeedSteps;
                case UpgradeKind.MaxHp: return u.maxHpSteps;
                case UpgradeKind.PickupRange: return u.magnetRadiusSteps;
                case UpgradeKind.DodgeCooldown: return u.dodgeCooldownSteps;
                default: return null;
            }
        }

        /// <summary>某个选项的最高等级（= 每级增量数组的长度）；回复为无限。</summary>
        public static int MaxLevel(UpgradeKind kind, RunConfig config)
        {
            if (kind == UpgradeKind.Heal) return int.MaxValue;
            float[] steps = Steps(kind, config);
            return steps == null ? 0 : steps.Length;
        }

        /// <summary>某选项在某等级时的数值（默认配置下 Value(MaxHp, 1) = 125）。</summary>
        public static float Value(UpgradeKind kind, int level, RunConfig config)
        {
            float v = BaseValue(kind, config);
            float[] steps = Steps(kind, config);
            if (steps == null) return v;
            int n = Math.Min(Math.Max(level, 0), steps.Length);
            for (int i = 0; i < n; i++) v += steps[i];
            return v;
        }

        public static string DisplayName(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.Damage: return "伤害";
                case UpgradeKind.FireRate: return "射速";
                case UpgradeKind.Pierce: return "穿透";
                case UpgradeKind.MoveSpeed: return "移速";
                case UpgradeKind.MaxHp: return "生命强化";
                case UpgradeKind.PickupRange: return "拾取范围";
                case UpgradeKind.DodgeCooldown: return "闪避冷却";
                case UpgradeKind.Heal: return "回复";
                default: return "（空）";
            }
        }

        /// <summary>
        /// 抽 3 个选项：从还没满级的选项里不放回随机抽；不够 3 个时用"回复"补 1 格，其余留空（None）。
        /// levels 下标对应 UpgradeKind 0..6。
        /// </summary>
        public static UpgradeKind[] Draw(int[] levels, RunConfig config, Random random)
        {
            var candidates = new List<UpgradeKind>(RealKindCount);
            for (int i = 0; i < RealKindCount; i++)
            {
                var kind = (UpgradeKind)i;
                if (levels[i] < MaxLevel(kind, config)) candidates.Add(kind);
            }

            var result = new UpgradeKind[3];
            bool healUsed = false;
            for (int slot = 0; slot < 3; slot++)
            {
                if (candidates.Count > 0)
                {
                    int pick = random.Next(candidates.Count);
                    result[slot] = candidates[pick];
                    candidates.RemoveAt(pick);
                }
                else if (!healUsed)
                {
                    result[slot] = UpgradeKind.Heal;
                    healUsed = true;
                }
                else
                {
                    result[slot] = UpgradeKind.None;
                }
            }
            return result;
        }
    }
}
