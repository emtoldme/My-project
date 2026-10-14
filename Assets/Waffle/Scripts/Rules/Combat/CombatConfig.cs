using System;

namespace Waffle.Combat
{
    /// <summary>
    /// 战斗模块参数。全部是 v0.2.1 的设计初值（未经实测）；普通存活上限按 C-01A 任务卡取 30。
    /// [Serializable] + 公有字段，可以直接挂到 Inspector 上调。
    /// 玩家的伤害/射速/穿透/磁吸半径不在这里，由框架按升级计算后每 tick 通过 CombatInput 传入。
    /// </summary>
    [Serializable]
    public class CombatConfig
    {
        // ---- 场地 / 玩家（§1.1） ----
        /// <summary>场地半边长（m），60×60。</summary>
        public float arenaHalfSize = 30f;
        public bool unboundedWorld;
        /// <summary>玩家碰撞半径（m），接触判定用。</summary>
        public float playerRadius = 0.5f;
        /// <summary>一局时长（s）；now ≥ 该值后不再刷怪。</summary>
        public float runDuration = 180f;

        // ---- 武器（§1.2） ----
        public float targetRange = 8f;
        public float projectileSpeed = 15f;
        public float projectileLifetime = 0.8f;
        public float projectileRadius = 0.2f;
        public int projectilePoolSize = 16;

        // ---- 敌人（§1.3），四个阶段按 phaseStarts 划分，生成时锁定 ----
        public float[] phaseStarts = { 0f, 30f, 90f, 150f };
        public float[] phaseHp = { 15f, 25f, 40f, 55f };
        public float[] phaseSpeed = { 3.0f, 3.5f, 3.75f, 4.0f };
        public float enemyRadius = 0.5f;
        public int contactDamage = 10;
        public float separationRadius = 1.0f;
        public float separationMaxSpeed = 1.0f;
        /// <summary>远离挪位：距离 > viewRadius + relocateExtra 时挪到前方刷怪环。</summary>
        public float relocateExtra = 10f;

        // ---- 精英（§1.3），同一种敌人换参数 ----
        public float eliteHpMultiplier = 6f;
        public float eliteRadius = 0.8f;
        public float eliteSpeedMultiplier = 0.8f;
        public int eliteContactDamage = 20;
        public int eliteGemValue = 10;
        public float[] eliteTimes = { 120f, 150f, 170f };

        // ---- 刷怪（§1.4） ----
        /// <summary>是否启用按时间表刷怪（测试/调试可关掉，只用 DebugSpawnEnemy）。</summary>
        public bool spawningEnabled = true;
        /// <summary>刷怪环内外偏移：[R_view + ringInner, R_view + ringOuter]。</summary>
        public float spawnRingInner = 1f;
        public float spawnRingOuter = 5f;
        /// <summary>合法落点 |x|,|z| ≤ 该值（场地内缩 1 m）。</summary>
        public float spawnLegalHalf = 29f;
        public float[] waveIntervals = { 2.5f, 1.5f, 1.4f, 1.0f };
        public int[] waveCounts = { 2, 3, 4, 4 };
        /// <summary>前方扇区：从该时刻起启用。</summary>
        public float forwardSectorStart = 30f;
        public float forwardSectorChance = 0.5f;
        public float forwardSectorHalfAngleDeg = 45f;
        public float forwardSectorMinSpeed = 1f;
        /// <summary>计算玩家平均速度的时间窗（s）。</summary>
        public float playerVelocityWindow = 0.5f;
        /// <summary>方向推进事件（§1.4）：时间、数量；落在移动方向 ±20°，半径 R_view+3。</summary>
        public float[] pushTimes = { 60f, 160f };
        public int[] pushCounts = { 10, 12 };
        public float pushHalfAngleDeg = 20f;
        public float pushRadiusExtra = 3f;
        /// <summary>包围圈事件：时间、数量；半径 R_view+2，场地外的点跳过。</summary>
        public float[] ringTimes = { 90f, 120f, 165f };
        public int[] ringCounts = { 16, 16, 20 };
        public float ringRadiusExtra = 2f;
        /// <summary>普通存活上限（C-01A 任务卡：30；压测时由调试改到 100）。</summary>
        public int aliveCap = 30;
        /// <summary>事件可超出上限的数量。</summary>
        public int eventOverflow = 20;
        /// <summary>敌人池；必须 ≥ aliveCap + eventOverflow。</summary>
        public int enemyPoolSize = 120;

        // ---- 经验宝石（§1.5） ----
        public int gemValue = 1;
        /// <summary>普通宝石寿命（s）；进入磁吸后停表；大宝石永久。≤ 0 表示永不过期。</summary>
        public float gemLifetime = 20f;
        public float pickupRadius = 1.0f;
        public float magnetStartSpeed = 8f;
        public float magnetAcceleration = 20f;
        public float magnetMaxSpeed = 20f;
        /// <summary>占用上限（含大宝石），满了在创建时合并。</summary>
        public int maxGems = 150;
        public int gemPoolSize = 160;

        /// <summary>按 now 返回阶段序号（0..3）。</summary>
        public int PhaseAt(float now)
        {
            int p = 0;
            for (int i = 0; i < phaseStarts.Length; i++)
                if (now >= phaseStarts[i]) p = i;
            return p;
        }
    }
}
