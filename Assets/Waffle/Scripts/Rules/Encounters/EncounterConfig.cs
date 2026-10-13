using System;

namespace Waffle.Rules.Encounters
{
    public static class EncounterArchetypes
    {
        public const string Chaser = "chaser";   // 无移动意图 → 宿主既有追击
        public const string Charger = "charger";
        public const string Dropper = "dropper";
        public const string Elite = "elite";     // 追击型精英
    }

    /// <summary>所有可调初值。逻辑里不写死数字。</summary>
    [Serializable]
    public class ChargerConfig
    {
        public float approachSpeed = 3.0f;
        public float triggerRange = 5.0f;      // 进入此距离后停步预警
        public float telegraph = 0.8f;
        public float dashDuration = 0.35f;
        public float dashSpeed = 16f;           // 冲锋长度 = 5.6m
        public float recover = 1.2f;
        public float hitHalfWidth = 0.5f;       // 胶囊半宽（=冲锋者半径）
        public int damage = 10;                 // 与现有普通接触伤害 10 同口径
        public float budgetRetryDelay = 0.25f;  // 攻击预算满时延后再试，不排队
    }

    [Serializable]
    public class DropperConfig
    {
        public float moveSpeed = 2.6f;
        public float keepMin = 6f, keepMax = 9f;
        public float interval = 4f;
        public float firstShotMin = 1.5f, firstShotMax = 4f; // 出生后首发随机错开
        public float telegraph = 1.0f;
        public float active = 0.2f;
        public float radius = 1.1f;
        public int damage = 10;
        public float budgetRetryDelay = 0.25f;
    }

    /// <summary>生成规格（映射到 GP00 EnemySpawnSpec）。</summary>
    [Serializable]
    public class EncounterArchetypeSpec
    {
        public string id;
        public float maxHp, speed, radius;
        public int contactDamage, gemValue;
        public bool elite;
        public EncounterArchetypeSpec() { }
        public EncounterArchetypeSpec(string id, float hp, float speed, float radius, int contact, int gem, bool elite = false)
        { this.id = id; maxHp = hp; this.speed = speed; this.radius = radius; contactDamage = contact; gemValue = gem; this.elite = elite; }
    }

    [Serializable]
    public class EncounterStage
    {
        public float startTime;
        public float spawnInterval;
        public int normalCap;          // 该段普通敌人目标上限（再与全局上限取小）
        public float wChaser, wCharger, wDropper;
        public EncounterStage() { }
        public EncounterStage(float t, float iv, int cap, float a, float b, float c)
        { startTime = t; spawnInterval = iv; normalCap = cap; wChaser = a; wCharger = b; wDropper = c; }
    }

    [Serializable]
    public class EncounterConfig
    {
        public ChargerConfig charger = new ChargerConfig();
        public DropperConfig dropper = new DropperConfig();

        // 模块自限（宿主 GP00 另有同口径硬上限；这里只为不发注定被拒的请求）
        public int normalEnemyCap = 48;
        public int attackCap = 12;
        public int normalAttackCap = 8;
        public int pendingSpawnCap = 8;

        public float spawnRingMin = 14f, spawnRingMax = 18f;
        public float spawnWarning = 0.6f;           // 出生预警（D 显示），≤ GP00 maxWarningTime 5s
        public int maxSpawnsPerTick = 1;            // 大 dt 不补发
        public float reactionAllowance = 0.25f;     // 侧躲可行性校验用的玩家反应时间

        // 安装 A 后旧刷怪关闭，A 自己覆盖 4:00/8:00 精英
        public float[] eliteTimes = { 240f, 480f };
        public float eliteRetryDelay = 2f;          // 被拒/被占位时等待后用新 Sequence 重试
        public float eliteRetryWindow = 30f;        // 超过窗口放弃，不补发

        public EncounterArchetypeSpec chaserSpec = new EncounterArchetypeSpec(EncounterArchetypes.Chaser, 25f, 3.5f, 0.5f, 10, 1);
        public EncounterArchetypeSpec chargerSpec = new EncounterArchetypeSpec(EncounterArchetypes.Charger, 30f, 3.0f, 0.5f, 10, 2);
        public EncounterArchetypeSpec dropperSpec = new EncounterArchetypeSpec(EncounterArchetypes.Dropper, 20f, 2.6f, 0.5f, 10, 2);
        public EncounterArchetypeSpec eliteSpec = new EncounterArchetypeSpec(EncounterArchetypes.Elite, 240f, 2.8f, 0.8f, 20, 10, true);

        public EncounterStage[] stages =
        {
            new EncounterStage(0f,   1.20f, 24, 1.00f, 0.00f, 0.00f),
            new EncounterStage(60f,  1.20f, 24, 0.92f, 0.08f, 0.00f),
            new EncounterStage(120f, 1.00f, 32, 0.72f, 0.18f, 0.10f),
            new EncounterStage(240f, 0.85f, 38, 0.60f, 0.22f, 0.18f),
            new EncounterStage(360f, 0.72f, 44, 0.52f, 0.24f, 0.24f),
            new EncounterStage(480f, 0.62f, 48, 0.46f, 0.26f, 0.28f),
        };

        /// <summary>冲锋侧躲可行性：玩家在 (预警-反应) 内横移距离 ≥ 半宽+玩家半径。</summary>
        public bool SidestepFeasible(float playerSpeed, float playerRadius, out float need, out float have)
        {
            need = charger.hitHalfWidth + playerRadius;
            have = playerSpeed * Math.Max(0f, charger.telegraph - reactionAllowance);
            return have >= need;
        }

        public int StageAt(float t)
        {
            int idx = 0;
            for (int i = 0; i < stages.Length; i++) if (t >= stages[i].startTime) idx = i;
            return idx;
        }
    }
}
