using System.Collections.Generic;
using Waffle.Combat;

namespace Waffle.Rules.Encounters
{
    // ⚠ 临时类型（PROVISIONAL）：在 GP00 共同契约冻结之前写的模块内最小输入/输出。
    // GP00 落地后由 Codex 直接映射或替换为 GameplayNext 下的正式类型；字段一一对应 CONTRACTS.md。

    public static class EncounterArchetypes
    {
        public const string Chaser = "chaser";   // 既有追击敌人：本模块不给移动意图，沿用旧路径
        public const string Charger = "charger";
        public const string Dropper = "dropper";
    }

    /// <summary>≈ RunContext。宿主每个 Playing tick 提供一次；暂停/升级时宿主不调用 Tick。</summary>
    public struct EncounterContext
    {
        public int RunId;
        public double Now;
        public float Dt;
        public Vec2 PlayerPos;      // T2 之后的实际位置
        public Vec2 PlayerPrevPos;
        public float PlayerRadius;
        public Vec2 ArenaMin, ArenaMax;
        public int NormalAlive;     // 宿主当前普通敌人数
        public int PoolUsed;        // 宿主敌人池占用
        public int ActiveAttacks;   // 宿主当前全部未结束敌方攻击（含其他模块）
        public int ActiveNormalAttacks;
    }

    /// <summary>≈ EnemySnapshot。</summary>
    public struct EncounterEnemyView
    {
        public int Serial, Generation;
        public string ArchetypeId;
        public Vec2 Pos, PrevPos;
        public float Radius;
        public bool Alive, Elite, Boss;
        /// <summary>宿主本 tick 对其做过远离挪位（可选；模块另有跳变距离兜底）。</summary>
        public bool Relocated;
    }

    /// <summary>≈ SpawnIntent。</summary>
    public struct EncounterSpawnRequest
    {
        public long RequestId;
        public string ArchetypeId;
        public Vec2 ZoneCenter;     // 出生环中心 = 玩家位置
        public float ZoneMinRadius, ZoneMaxRadius;
        public float WarningTime;
        public int StageIndex;
    }

    /// <summary>≈ MovementIntent。Speed=0 表示原地；Locked=true 时宿主不得改向、不做远离挪位以外的转向。</summary>
    public struct EncounterMoveRequest
    {
        public int Serial, Generation;
        public Vec2 Direction;
        public float Speed;
        public bool Locked;
        public bool AllowRelocate;
    }

    public enum EncounterShape { Circle, Capsule }

    /// <summary>≈ HostileAttackIntent。宿主做几何检测；每个 AttackId 最多一次有效伤害候选（经 TryClaimHit）。</summary>
    public struct EncounterAttackRequest
    {
        public long AttackId;
        public int OwnerSerial, OwnerGeneration;
        public EncounterShape Shape;
        public Vec2 Start, End;     // Capsule: 扫掠起终点；Circle: Start=End=圆心
        public float Radius;        // Capsule 半宽（不含玩家半径）/ Circle 半径
        public Vec2 LockedDir;
        public double StartTime, ActiveStart, ActiveEnd;
        public int Damage;
        public bool Normal;
    }

    /// <summary>一个 tick 的全部输出；列表在下一次 Tick 时被清空复用。</summary>
    public sealed class EncounterOutput
    {
        public readonly List<EncounterSpawnRequest> Spawns = new List<EncounterSpawnRequest>();
        public readonly List<EncounterMoveRequest> Moves = new List<EncounterMoveRequest>();
        public readonly List<EncounterAttackRequest> NewAttacks = new List<EncounterAttackRequest>();
        public readonly List<long> CancelledAttacks = new List<long>();
        public int SpawnsSkippedByBudget, AttacksDeferredByBudget;
        internal void Clear()
        {
            Spawns.Clear(); Moves.Clear(); NewAttacks.Clear(); CancelledAttacks.Clear();
            SpawnsSkippedByBudget = 0; AttacksDeferredByBudget = 0;
        }
    }

    /// <summary>模块私有确定性随机（xorshift32），种子 = 共同种子 ^ 模块盐；不消耗其他随机流。</summary>
    public sealed class EncounterRng
    {
        public const uint ModuleSalt = 0xA11CE5E7u;
        private uint s;
        public EncounterRng(int runSeed)
        {
            s = ((uint)runSeed ^ ModuleSalt) * 0x9E3779B1u;
            if (s == 0) s = 0x6C8E9CF5u;
            for (int i = 0; i < 4; i++) Next();
        }
        public uint Next() { uint x = s; x ^= x << 13; x ^= x >> 17; x ^= x << 5; s = x; return x; }
        public float Value01() { return (Next() >> 8) * (1f / 16777216f); }
        public float Range(float a, float b) { return a + (b - a) * Value01(); }
    }
}
