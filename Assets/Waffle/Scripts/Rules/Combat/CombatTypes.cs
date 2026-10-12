using System.Collections.Generic;

namespace Waffle.Combat
{
    public enum EnemyLife { Alive, Dead }
    public enum GemLife { Free, Magnet, Collected, Expired }

    /// <summary>
    /// 框架每 tick 传入的玩家属性（已按升级计算好的最终值）。
    /// 例：Damage = RunSimulation.CurrentValue(UpgradeKind.Damage)。
    /// </summary>
    public struct CombatPlayerStats
    {
        public float Damage;
        /// <summary>开火间隔（s）。</summary>
        public float FireInterval;
        /// <summary>穿透（一颗子弹最多命中的敌人数，≥ 1）。</summary>
        public int Pierce;
        /// <summary>磁吸半径（m）。</summary>
        public float MagnetRadius;
    }

    /// <summary>每 tick 的输入。PlayerPos 必须是 T2 之后的位置。</summary>
    public struct CombatInput
    {
        /// <summary>本 tick 的逻辑 dt（已夹到 maxDt、乘 slowmo）。</summary>
        public float Dt;
        /// <summary>本 tick 开始时的游戏时钟 now（T11 之前的值）。</summary>
        public float RunTime;
        /// <summary>玩家 XZ 位置（X = x，Y = z）。</summary>
        public Vec2 PlayerPos;
        /// <summary>视野半径 R_view（m）；框架按相机计算，未提供（≤0）时用 14。</summary>
        public float ViewRadius;
        public float MaxLegalMovementStep;
        public CombatPlayerStats Stats;
    }

    /// <summary>敌人只读视图。字段只有本程序集能写，视图层只能读。</summary>
    public sealed class EnemyState
    {
        /// <summary>生成序号：每次出池递增，一局内不重复。</summary>
        public int Serial { get; internal set; }
        /// <summary>代号：回池时 +1；保存引用时与 Serial 一起保存。</summary>
        public int Generation { get; internal set; }
        public int PoolIndex { get; internal set; }
        public EnemyLife Life { get; internal set; }
        public Vec2 Position { get; internal set; }
        public Vec2 PrevPosition { get; internal set; }
        public float Hp { get; internal set; }
        public float MaxHp { get; internal set; }
        public float Speed { get; internal set; }
        public float Radius { get; internal set; }
        public int ContactDamage { get; internal set; }
        public int GemValue { get; internal set; }
        public bool IsElite { get; internal set; }
        internal string LastSourceId; internal int LastSlot;
        /// <summary>生成时的阶段（0..3），视图可用来换颜色档位。</summary>
        public int Phase { get; internal set; }
        /// <summary>事件编号（调试用）：0 = 普通波次，1 = 方向推进，2 = 包围圈，3 = 精英，9 = 调试生成。</summary>
        public int SpawnSource { get; internal set; }
        public bool IsAlive { get { return Life == EnemyLife.Alive; } }
    }

    /// <summary>子弹只读视图。</summary>
    public sealed class ProjectileState
    {
        public int Serial { get; internal set; }
        public int Generation { get; internal set; }
        public int PoolIndex { get; internal set; }
        public Vec2 Position { get; internal set; }
        public Vec2 Direction { get; internal set; }
        public float Age { get; internal set; }
        public int PierceLeft { get; internal set; }
        public float Damage { get; internal set; }
        /// <summary>已标记回收（T13 回池），视图应隐藏。</summary>
        public bool Finished { get; internal set; }
        public string SourceId { get; internal set; }
        public int Slot { get; internal set; }
        internal float Speed, Lifetime, MaxDistance, Travelled;
        internal bool Returning, ReturnAllowed;
        internal RunShotGroup Group;
        internal bool BornThisTick;
        /// <summary>已命中列表：敌人 (Serial, Generation)。</summary>
        internal readonly List<long> HitKeys = new List<long>(4);
    }

    /// <summary>经验宝石只读视图。</summary>
    public sealed class GemState
    {
        public int Serial { get; internal set; }
        public int Generation { get; internal set; }
        public int PoolIndex { get; internal set; }
        public GemLife Life { get; internal set; }
        public Vec2 Position { get; internal set; }
        public int Value { get; internal set; }
        public float Age { get; internal set; }
        public bool IsBig { get; internal set; }
        internal float MagnetSpeed;
        /// <summary>最后 3 s 闪烁提示（视图用）。</summary>
        public bool IsBlinking(float lifetime) { return !IsBig && Life == GemLife.Free && lifetime > 0f && Age >= lifetime - 3f; }
    }

    /// <summary>
    /// 一个步骤的输出。模块内部复用同一个对象，调用下一个步骤前读取。
    /// Events 使用框架已有的 PresentationEvent（Frame 由框架填写）。
    /// </summary>
    public sealed class CombatStepResult
    {
        /// <summary>T8：本 tick 接触玩家的 Alive 敌人中最大的接触伤害；0 = 无接触。未考虑无敌，由框架决定是否结算。</summary>
        public int ContactDamage;
        /// <summary>T8：接触伤害之和（只在框架选择"叠加"时用，默认不用）。</summary>
        public int ContactDamageSum;
        /// <summary>T8：造成最大伤害的敌人 Serial；-1 = 无。</summary>
        public int ContactSourceSerial = -1;
        /// <summary>T8：接触玩家的 Alive 敌人数。</summary>
        public int ContactCount;
        /// <summary>T7：本 tick 击杀数。</summary>
        public int Kills;
        /// <summary>T10：本 tick 拾取的 XP（含满上限时直接计入的部分）。</summary>
        public int XpCollected;
        public float HealFraction;
        public readonly List<long> EliteRewards = new List<long>();
        /// <summary>本步骤产生的表现事件（按发生顺序）。</summary>
        public readonly List<PresentationEvent> Events = new List<PresentationEvent>(32);

        internal void Clear()
        {
            ContactDamage = 0; ContactDamageSum = 0; ContactSourceSerial = -1; ContactCount = 0;
            Kills = 0; XpCollected = 0; HealFraction = 0; EliteRewards.Clear(); Events.Clear();
        }
    }

    /// <summary>整局累计计数（日志/调试/测试用）。</summary>
    public sealed class CombatCounters
    {
        public int TotalKills;
        public int TotalXpCollected;
        public int TotalXpDropped;
        public int ShotsFired;
        public int Hits;
        public int EnemiesSpawned;
        public int EnemyPoolMiss;
        public int ProjectilePoolMiss;
        public int GemPoolMiss;
        public int Truncated;
        public int RingSkipped;
        public int Relocated;
        public int GemsExpired;
        public int GemsMerged;
        public int XpDirectCredited;
    }
}
