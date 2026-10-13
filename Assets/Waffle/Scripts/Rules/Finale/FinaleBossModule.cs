using System;
using Waffle.Combat;
using Waffle.GameplayNext;

namespace Waffle.Finale
{
    /// <summary>
    /// AI-C：终局首领纯规则状态机（GP-C）。
    /// - 9:00 (540s) 发出出生预警，约 3 秒后 (543s) 在玩家视野外围合法区域生成 1 只首领。
    /// - 招式 1：三连落波（Triple Falling Pulse）。逐次锁定玩家当时位置，给出圆形危险预警后依次激活（最多同时锁定 3 个圆）。
    /// - 招式 2：扩散声环（Expanding Sonic Ring）。从首领处扩散，提前给出至少 1.2 秒预警，真实内外半径与扩张速度。
    /// - HP 小于等于 50% 时进入半血激怒阶段，缩短招式间隔，不新增第三招，不同 tick 堆叠两种无预警攻击。
    /// - 输出 FinaleSnapshot，不自持第二份可扣血 HP，首领死亡由宿主 T7 确认。
    /// </summary>
    public sealed class FinaleBossModule : IFinaleModule
    {
        public readonly FinaleConfig Config;
        private readonly CombatRng rng;

        private enum BossPhase
        {
            Waiting,        // 尚未到 9:00
            WarningSpawn,   // 出生预警中 (540s ~ 543s)
            Fighting,       // 战斗中 (>50% HP)
            Enraged,        // 狂暴阶段 (<=50% HP)
            Defeated        // 已被宿主击杀
        }

        private BossPhase phase = BossPhase.Waiting;
        private bool spawnRequested;
        private bool bossHasExisted;
        private float nextSpawnRetry;
        private EnemyKey bossKey;
        private bool bossAlive;
        private float bossCurrentHp;
        private float bossMaxHp;

        private long spawnSequence = 0;
        private long attackSequence = 0;

        private float nextAttackTime = 0f;
        private int attackCycle = 0; // 0 = 三连落波, 1 = 扩散声环
        private string currentAttackLabel = "";

        private int waveSubIndex = 0;
        private float nextSubWaveTime = 0f;

        public FinaleBossModule(int seed, FinaleConfig config = null)
        {
            Config = config ?? new FinaleConfig();
            rng = new CombatRng(seed ^ 0x5a1f10c3);
            bossMaxHp = Config.BossMaxHp;
            bossCurrentHp = Config.BossMaxHp;
        }

        public void Step(GameplayContext context, GameplayCommandBuffer commands)
        {
            float now = context.Now;

            UpdateBossStatus(context);

            switch (phase)
            {
                case BossPhase.Waiting:
                    if (now >= Config.SpawnWarningStartTime)
                    {
                        phase = BossPhase.WarningSpawn;
                        currentAttackLabel = "首领逼近中...";
                        RequestBossSpawn(context, commands);
                    }
                    break;

                case BossPhase.WarningSpawn:
                    if (bossAlive)
                    {
                        phase = BossPhase.Fighting;
                        nextAttackTime = now + Config.FirstAttackDelay;
                        currentAttackLabel = "";
                    }
                    else if (spawnRequested && bossHasExisted && !bossAlive)
                    {
                        phase = BossPhase.Defeated;
                        currentAttackLabel = "首领已消灭";
                        commands.PublishFinaleAttack(currentAttackLabel);
                        commands.RequestFinaleResolution(FinaleResolution.BossDefeated);
                        return;
                    }
                    else if (!bossHasExisted && now >= nextSpawnRetry && !HasPendingSpawn(context))
                        RequestBossSpawn(context, commands);
                    break;

                case BossPhase.Fighting:
                case BossPhase.Enraged:
                    if (!bossAlive && spawnRequested)
                    {
                        phase = BossPhase.Defeated;
                        currentAttackLabel = "首领已消灭";
                        commands.PublishFinaleAttack(currentAttackLabel);
                        commands.RequestFinaleResolution(FinaleResolution.BossDefeated);
                        return;
                    }

                    if (phase == BossPhase.Fighting && bossCurrentHp <= bossMaxHp * 0.5f)
                    {
                        phase = BossPhase.Enraged;
                    }

                    if (bossAlive)
                    {
                        Vec2 toPlayer = context.PlayerPosition - GetBossPosition(context);
                        float dist = toPlayer.Length;
                        Vec2 vel = dist > 1e-4f ? toPlayer * (Config.BossSpeed / dist) : Vec2.Zero;
                        commands.TryMove(new MovementIntent(bossKey, vel, useSeparation: true, allowRelocation: false));
                    }

                    UpdateAttacks(context, commands);
                    break;

                case BossPhase.Defeated:
                    commands.RequestFinaleResolution(FinaleResolution.BossDefeated);
                    currentAttackLabel = "首领已消灭";
                    break;
            }

            commands.PublishFinaleAttack(currentAttackLabel);
        }

        private void UpdateBossStatus(GameplayContext context)
        {
            bossAlive = false;
            for (int i = 0; i < context.Enemies.Count; i++)
            {
                var enemy = context.Enemies[i];
                if (enemy.IsBoss)
                {
                    bossHasExisted = true;
                    bossKey = enemy.Key;
                    bossAlive = enemy.IsAlive;
                    bossCurrentHp = enemy.Hp;
                    bossMaxHp = enemy.MaxHp;
                    break;
                }
            }
        }

        private Vec2 GetBossPosition(GameplayContext context)
        {
            for (int i = 0; i < context.Enemies.Count; i++)
            {
                if (context.Enemies[i].Key.Equals(bossKey))
                    return context.Enemies[i].Position;
            }
            return context.PlayerPosition + new Vec2(0, 10f);
        }

        private Vec2 CalculateBossSpawnPosition(GameplayContext context)
        {
            float maxCoord = context.ArenaHalfSize - Config.BossRadius - 1f;
            Vec2 candidate = context.PlayerPosition;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float angle = rng.Range(0f, (float)(Math.PI * 2));
                candidate = context.PlayerPosition + Vec2.FromAngle(angle) * rng.Range(10f, 12f);
                if (Math.Abs(candidate.X) <= maxCoord && Math.Abs(candidate.Y) <= maxCoord) return candidate;
            }
            Vec2 inward = (Vec2.Zero - context.PlayerPosition).Normalized;
            if (inward.SqrLength < .5f) inward = new Vec2(1f, 0f);
            candidate = context.PlayerPosition + inward * 11f;
            float clampedX = Math.Max(-maxCoord, Math.Min(maxCoord, candidate.X));
            float clampedY = Math.Max(-maxCoord, Math.Min(maxCoord, candidate.Y));
            return new Vec2(clampedX, clampedY);
        }

        bool HasPendingSpawn(GameplayContext context)
        {
            foreach (var warning in context.SpawnWarnings) if (warning.Spawn.Spec.IsBoss) return true;
            return false;
        }

        void RequestBossSpawn(GameplayContext context, GameplayCommandBuffer commands)
        {
            var spec = new EnemySpawnSpec(Config.BossArchetypeId, Config.BossMaxHp, Config.BossSpeed,
                Config.BossRadius, Config.BossContactDamage, Config.BossGemValue, true, true);
            commands.TrySpawn(new SpawnIntent(++spawnSequence, CalculateBossSpawnPosition(context), spec, Config.SpawnWarningDuration));
            spawnRequested = true;
            nextSpawnRetry = context.Now + Config.SpawnWarningDuration + 2f;
        }

        private void UpdateAttacks(GameplayContext context, GameplayCommandBuffer commands)
        {
            float now = context.Now;
            bool isEnraged = (phase == BossPhase.Enraged);

            if (waveSubIndex > 0)
            {
                if (now >= nextSubWaveTime)
                {
                    ExecuteFallingPulse(context, commands, isEnraged);
                }
                return;
            }

            if (now < nextAttackTime) return;

            if (attackCycle == 0)
            {
                waveSubIndex = 1;
                currentAttackLabel = isEnraged ? "三连落波 [狂暴]" : "三连落波";
                ExecuteFallingPulse(context, commands, isEnraged);
            }
            else
            {
                currentAttackLabel = isEnraged ? "扩散声环 [狂暴]" : "扩散声环";
                ExecuteSonicRing(context, commands, isEnraged);
                attackCycle = 0;
                float cooldown = isEnraged ? Config.RingCooldownEnraged : Config.RingCooldown;
                nextAttackTime = now + (isEnraged ? Config.RingWarningEnraged : Config.RingWarning)
                    + Config.RingActiveDuration + cooldown;
            }
        }

        private void ExecuteFallingPulse(GameplayContext context, GameplayCommandBuffer commands, bool isEnraged)
        {
            float now = context.Now;
            Vec2 targetPos = context.PlayerPosition;
            float warningTime = isEnraged ? Config.PulseWarningEnraged : Config.PulseWarning;
            float activeDuration = Config.PulseActiveDuration;

            var shape = new AttackShape(
                kind: AttackShapeKind.Circle,
                center: targetPos,
                radius: Config.PulseRadius
            );

            attackSequence++;
            commands.TryAttack(new HostileAttackIntent(
                sequence: attackSequence,
                owner: bossKey,
                shape: shape,
                activateAt: now + warningTime,
                endAt: now + warningTime + activeDuration,
                damage: Config.PulseDamage
            ));

            waveSubIndex++;
            if (waveSubIndex > 3)
            {
                waveSubIndex = 0;
                attackCycle = 1;
                float cooldown = isEnraged ? Config.WaveCooldownEnraged : Config.WaveCooldown;
                nextAttackTime = now + cooldown;
            }
            else
            {
                float subInterval = isEnraged ? Config.PulseSubIntervalEnraged : Config.PulseSubInterval;
                nextSubWaveTime = now + subInterval;
            }
        }

        private void ExecuteSonicRing(GameplayContext context, GameplayCommandBuffer commands, bool isEnraged)
        {
            float now = context.Now;
            Vec2 bossPos = GetBossPosition(context);
            float warningTime = isEnraged ? Config.RingWarningEnraged : Config.RingWarning;
            float activeDuration = Config.RingActiveDuration;

            var shape = new AttackShape(
                kind: AttackShapeKind.Annulus,
                center: bossPos,
                radius: Config.RingRadius,
                innerRadius: Config.RingInnerRadius,
                expansionSpeed: Config.RingExpansionSpeed
            );

            attackSequence++;
            commands.TryAttack(new HostileAttackIntent(
                sequence: attackSequence,
                owner: bossKey,
                shape: shape,
                activateAt: now + warningTime,
                endAt: now + warningTime + activeDuration,
                damage: Config.RingDamage
            ));
        }

        public void Clear()
        {
            phase = BossPhase.Waiting;
            spawnRequested = false;
            bossHasExisted = false;
            nextSpawnRetry = 0f;
            bossKey = default;
            bossAlive = false;
            bossCurrentHp = bossMaxHp;
            spawnSequence = 0;
            attackSequence = 0;
            nextAttackTime = 0f;
            attackCycle = 0;
            waveSubIndex = 0;
            nextSubWaveTime = 0f;
            currentAttackLabel = "";
        }
    }
}
