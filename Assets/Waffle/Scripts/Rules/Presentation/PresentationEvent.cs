using System.Collections.Generic;

namespace Waffle
{
    /// <summary>
    /// 表现事件种类（给音效/特效用，只读，不影响玩法）。
    /// 带“预留”的种类已定义，但本阶段框架里没有对应玩法步骤，由 C-00/C-01 战斗接入后在相应 tick 步骤发出。
    /// </summary>
    public enum PresentationEventType
    {
        /// <summary>T1：闪避发动（当前为占位闪避：按下即发出）。</summary>
        Dodge,
        /// <summary>T5：武器开火（预留）。</summary>
        Shot,
        /// <summary>T6：子弹命中敌人并扣血（预留）。</summary>
        EnemyHit,
        /// <summary>T7：敌人死亡结算（预留）。</summary>
        EnemyKilled,
        /// <summary>T8：玩家实际受到伤害（被无敌挡下时不发）。Value = 伤害，Data = 剩余 HP。</summary>
        PlayerHurt,
        /// <summary>T10：拾取宝石（预留）。Value = XP。</summary>
        GemPickup,
        /// <summary>升级面板显示（T12 进入 LevelUp，或连续升级时重抽下一张）。Data = 待升级数。</summary>
        LevelUpShown,
        /// <summary>升级选择被接受。Data = (int)UpgradeKind。</summary>
        LevelUpChosen,
        /// <summary>进入 Paused。</summary>
        Paused,
        /// <summary>从 Paused 回到暂停前的状态。Data = (int)恢复到的 RunPhase。</summary>
        Resumed,
        /// <summary>T11 进入 Victory（每局最多一次）。</summary>
        Victory,
        /// <summary>T9 进入 Defeat（每局最多一次）。</summary>
        Defeat,
        /// <summary>重开前发出（每个 RunSimulation 最多一次）。</summary>
        Restart,
        EchoHit,
        PropagationHit,
        EchoPropagationHit
    }

    /// <summary>一条表现事件。纯数据，值类型。位置在 XZ 平面。</summary>
    public struct PresentationEvent
    {
        public PresentationEventType Type;
        public long Frame;
        /// <summary>发出时的游戏时钟 now。</summary>
        public float Now;
        public float X;
        public float Z;
        public float Value;
        public int Data;
        // Derived hits carry source and target positions; existing events leave these at zero.
        public float FromX, FromZ;
        public int Generation;
        // Shot captures its actual firing direction before the target can die/recycle.
        public float DirectionX, DirectionZ;

        public override string ToString()
        {
            return Type + "@" + Frame;
        }
    }

    /// <summary>
    /// 本帧的表现事件列表。RunSimulation 写入，GameLoop 分发给视图后清空；Tick 的 T0 也会清空。
    /// 视图只能读取，不能写回玩法状态。
    /// </summary>
    public sealed class PresentationEventBuffer
    {
        private readonly List<PresentationEvent> events = new List<PresentationEvent>(32);

        public IReadOnlyList<PresentationEvent> Events { get { return events; } }
        public int Count { get { return events.Count; } }

        internal void Add(PresentationEvent e) { events.Add(e); }
        public void Clear() { events.Clear(); }
    }
}
