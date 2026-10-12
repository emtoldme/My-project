using System;
using System.Collections.Generic;
using Waffle.Combat;

namespace Waffle.RunContent.Components
{
    /// <summary>敌人只读快照：宿主每次 Advance 前从唯一 CombatModule 拷贝存活敌人。组件不存 HP/XP。</summary>
    public readonly struct EnemySnapshot
    {
        public readonly int Serial, Generation;
        public readonly Vec2 Position;
        public readonly float Radius;
        public readonly bool IsElite;
        public EnemySnapshot(int serial, int generation, Vec2 position, float radius, bool isElite)
        { Serial = serial; Generation = generation; Position = position; Radius = radius; IsElite = isElite; }
        public long Key => RuleKeys.Of(Serial, Generation);
    }

    public static class RuleKeys
    {
        /// <summary>与 CombatModule.Key 相同的 serial+generation 去重键。</summary>
        public static long Of(int serial, int generation) => ((long)serial << 32) | (uint)generation;
    }

    /// <summary>命令种类：宿主据此选择现有结算路径。</summary>
    public enum RuleCommandKind
    {
        /// <summary>对单个 serial+generation 目标结算一次伤害（W05 每一跳、W06 接触）。</summary>
        TargetDamage = 0,
        /// <summary>以 Position 为圆心、Radius 为半径的一次范围伤害，最多 Budget 个目标（P17、W06 引爆）。</summary>
        AreaDamage = 1,
        /// <summary>按旧武器弹体路径发射（W01 适配样板；仅在宿主把 W01 迁到组件入口后产生）。</summary>
        FireProjectile = 2,
        /// <summary>纯表现：残留点布置/消失（不结算伤害）。</summary>
        ZoneSpawned = 3,
    }

    /// <summary>
    /// 组件输出命令。伤害/半径/持续均已乘过 GrowthStats（宿主不得再乘一次）。
    /// 宿主在 T6 之后、T7 之前按顺序执行；目标失效（代号不符/已死）时丢弃该条。
    /// </summary>
    public struct RuleCommand
    {
        public RuleCommandKind Kind;
        public string ContentId;     // W05 / W06 / P17 / W01
        public int Slot;             // 武器槽；被动为 -1
        public string RootId;        // 派生根（链的起点内容ID）
        public int ChainId;          // 同一次释放的链编号（每局递增）
        public int Serial, Generation; // TargetDamage 的目标；其他为 0
        public Vec2 Position;        // 命中点/圆心/发射点
        public Vec2 From;            // 链的上一跳位置（表现画电弧用）；FireProjectile 时为方向
        public float Damage, Radius, Duration, Speed;
        public int Depth;            // 0 = 直接命中（可触发 P12/P13）；>0 = 派生（不得再触发回声/传播/链）
        public int BudgetLeft;       // 本链剩余目标预算 / 范围最多目标数
        public int Count, Pierce;    // FireProjectile 用
        /// <summary>true 时宿主可把这次命中当作直接命中交给 P12/P13（一次释放最多一次）。</summary>
        public bool AllowsMechanism;
    }

    /// <summary>每次 Advance 的输入（全部来自规则宿主；不读 UnityEngine.Time）。</summary>
    public struct ComponentAdvanceInput
    {
        public float Now;            // 规则局内时钟（Playing 时钟）
        public float Dt;             // 本 tick 规则 dt（暂停时不调用 Advance）
        public Vec2 PlayerPos;       // T2 移动之后的玩家位置
        /// <summary>本 tick 合法最大位移（建议 max(moveSpeed,dodgeSpeed)×dt×1.25）；超过视为传送，不累计。</summary>
        public float MaxLegalStep;
    }
}
