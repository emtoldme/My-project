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
        // ---- 升级相关的基础值（Lv0）。升级数值 = 基础值 + 每级增量之和（见 upgrades） ----
        /// <summary>武器基础伤害，§1.2。</summary>
        public float damage = 10f;
        /// <summary>武器基础开火间隔（s），§1.2。</summary>
        public float fireInterval = 0.60f;
        /// <summary>基础穿透数，§1.2。</summary>
        public float pierce = 1f;
        /// <summary>基础磁吸半径（m），§1.5。</summary>
        public float magnetRadius = 3.0f;
        /// <summary>基础闪避冷却（s），§1.1。</summary>
        public float dodgeCooldown = 1.5f;
        /// <summary>闪避设计初值：0.2秒内移动3米，并在此期间无敌。</summary>
        public float dodgeDuration = 0.2f;
        public float dodgeSpeed = 15f;

        /// <summary>升级表：每级增量、升级回血等（§1.6 表 U-1，默认值换算成增量）。</summary>
        public UpgradeConfig upgrades = new UpgradeConfig();

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
        /// <summary>多个敌人同时接触是否叠加伤害；默认只取最大一次。</summary>
        public bool stackContactDamage = false;
        /// <summary>普通宝石寿命，作为游戏配置的唯一入口。</summary>
        public float gemLifetime = 20f;
    }

    /// <summary>
    /// 升级表（§1.6）。每个数组的第 k 个元素 = 从 Lv k 升到 Lv k+1 时增加的量；
    /// 数组长度 = 该选项的最高等级。默认值在默认基础值下正好得到 v0.2.1 表 U-1 的数字。
    /// </summary>
    [Serializable]
    public class UpgradeConfig
    {
        public float[] damageSteps = { 5f, 5f, 5f, 5f };                    // 10/15/20/25/30
        public float[] fireIntervalSteps = { -0.12f, -0.08f, -0.06f, -0.04f }; // 0.60/0.48/0.40/0.34/0.30
        public float[] pierceSteps = { 1f, 1f };                            // 1/2/3
        public float[] moveSpeedSteps = { 0.4f, 0.4f, 0.4f };               // 5.0/5.4/5.8/6.2
        public float[] maxHpSteps = { 25f, 25f, 25f, 25f };                 // 100/125/150/175/200
        public float[] magnetRadiusSteps = { 1.5f, 1.5f, 2.0f };            // 3.0/4.5/6.0/8.0
        public float[] dodgeCooldownSteps = { -0.25f, -0.25f, -0.2f };      // 1.5/1.25/1.0/0.8
        /// <summary>选“生命强化”时额外回复的 HP。</summary>
        public int maxHpUpgradeHeal = 25;
        /// <summary>兜底选项“回复”：回复最大 HP 的比例。</summary>
        public float healFraction = 0.30f;
    }
}
