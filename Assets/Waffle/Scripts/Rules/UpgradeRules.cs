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
    /// 升级表与抽卡规则（§1.6）。本阶段只记录等级，伤害/射速等数值由以后的系统读取。
    /// </summary>
    public static class UpgradeRules
    {
        /// <summary>可升级的选项数（不含兜底的"回复"）。</summary>
        public const int RealKindCount = 7;

        // 每个选项从 Lv0 开始的数值表（设计初值）。设为 private，外部只能通过 Value() 读取，保证没有可被修改的 static 数据。
        private static readonly float[] Damage = { 10, 15, 20, 25, 30 };
        private static readonly float[] FireInterval = { 0.60f, 0.48f, 0.40f, 0.34f, 0.30f };
        private static readonly float[] Pierce = { 1, 2, 3 };
        private static readonly float[] MoveSpeed = { 5.0f, 5.4f, 5.8f, 6.2f };
        private static readonly float[] MaxHp = { 100, 125, 150, 175, 200 };
        private static readonly float[] MagnetRadius = { 3.0f, 4.5f, 6.0f, 8.0f };
        private static readonly float[] DodgeCooldown = { 1.5f, 1.25f, 1.0f, 0.8f };

        public const int MaxHpUpgradeHeal = 25;
        public const float HealFraction = 0.30f;

        /// <summary>某个选项的最高等级（Lv 上限）。</summary>
        public static int MaxLevel(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.Damage: return Damage.Length - 1;
                case UpgradeKind.FireRate: return FireInterval.Length - 1;
                case UpgradeKind.Pierce: return Pierce.Length - 1;
                case UpgradeKind.MoveSpeed: return MoveSpeed.Length - 1;
                case UpgradeKind.MaxHp: return MaxHp.Length - 1;
                case UpgradeKind.PickupRange: return MagnetRadius.Length - 1;
                case UpgradeKind.DodgeCooldown: return DodgeCooldown.Length - 1;
                default: return int.MaxValue; // 回复：无限
            }
        }

        /// <summary>某选项在某等级时的数值（例如 Value(MaxHp, 1) = 125）。</summary>
        public static float Value(UpgradeKind kind, int level)
        {
            float[] table;
            switch (kind)
            {
                case UpgradeKind.Damage: table = Damage; break;
                case UpgradeKind.FireRate: table = FireInterval; break;
                case UpgradeKind.Pierce: table = Pierce; break;
                case UpgradeKind.MoveSpeed: table = MoveSpeed; break;
                case UpgradeKind.MaxHp: table = MaxHp; break;
                case UpgradeKind.PickupRange: table = MagnetRadius; break;
                case UpgradeKind.DodgeCooldown: table = DodgeCooldown; break;
                default: return 0f;
            }
            if (level < 0) level = 0;
            if (level >= table.Length) level = table.Length - 1;
            return table[level];
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
                case UpgradeKind.Heal: return "回复 30%";
                default: return "（空）";
            }
        }

        /// <summary>
        /// 抽 3 个选项：从还没满级的选项里不放回随机抽；不够 3 个时用"回复"补 1 格，其余留空（None）。
        /// levels 下标对应 UpgradeKind 0..6。
        /// </summary>
        public static UpgradeKind[] Draw(int[] levels, Random random)
        {
            var candidates = new List<UpgradeKind>(RealKindCount);
            for (int i = 0; i < RealKindCount; i++)
            {
                var kind = (UpgradeKind)i;
                if (levels[i] < MaxLevel(kind)) candidates.Add(kind);
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
