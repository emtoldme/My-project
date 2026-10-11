using System;

namespace Waffle
{
    /// <summary>
    /// 一局的可调参数（全部是 v0.2.1 的设计初值，未经实测）。
    /// 标了 [Serializable]，所以能直接在 GameLoop 的 Inspector 里改。
    /// </summary>
    [Serializable]
    public class RunConfig
    {
        /// <summary>一局时长（s），§1.7。</summary>
        public float runDuration = 180f;
        /// <summary>单帧逻辑 dt 上限（s），§0.3。</summary>
        public float maxDt = 0.05f;
        /// <summary>初始最大 HP，§1.1。</summary>
        public int maxHp = 100;
        /// <summary>受击无敌时长（s），§1.1 / §1.8 C-1。</summary>
        public float hitIFrame = 0.5f;
        /// <summary>玩家移动速度（m/s），§1.1。</summary>
        public float moveSpeed = 5f;
        /// <summary>玩家可活动范围半边长（m）：场地 60×60，减去半径 0.5。</summary>
        public float arenaHalfExtent = 29.5f;
        /// <summary>闪避输入缓冲（s），§1.1。</summary>
        public float dodgeBuffer = 0.1f;
        /// <summary>升级面板防误触（s，unscaled），§1.6。</summary>
        public float levelUpInputGuard = 0.3f;
        /// <summary>随机种子（升级抽卡）。</summary>
        public int seed = 12345;

        // ---- 待决策项（§5），先按推荐默认值，做成可调开关 ----
        /// <summary>§5-1：同一 tick 死亡并到 3:00 时判失败（true，推荐）还是判胜利（false）。</summary>
        public bool defeatWinsSameTick = true;
        /// <summary>§5-2：多个敌人同时接触是否叠加伤害（false = 不叠加，推荐）。本阶段还没有敌人，只保存设置。</summary>
        public bool stackContactDamage = false;
        /// <summary>§5-3：普通宝石寿命（s，推荐 20）。本阶段还没有宝石，只保存设置。</summary>
        public float gemLifetime = 20f;
    }
}
