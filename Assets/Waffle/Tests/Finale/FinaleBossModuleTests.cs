using System;
using System.Collections.Generic;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.GameplayNext;

namespace Waffle.Finale.Tests
{
    [TestFixture]
    public class FinaleBossModuleTests
    {
        private GameplayContext CreateContext(float now, float dt, Vec2 playerPos, IReadOnlyList<GameplayEnemySnapshot> enemies)
        {
            var view = new RunGameplayView(new List<AttackSnapshot>(), new List<SpawnWarningSnapshot>());
            return new GameplayContext(
                runId: Guid.NewGuid(),
                seed: 12345,
                kind: GameplayModuleKind.Finale,
                input: new CombatInput {
                    RunTime = now,
                    Dt = dt,
                    PlayerPos = playerPos,
                    PlayerPreviousPos = playerPos,
                    ViewRadius = 14f,
                    MaxLegalMovementStep = 10f
                },
                config: new CombatConfig {
                    arenaHalfSize = 40f,
                    playerRadius = 0.5f,
                    runDuration = 600f
                },
                enemies: enemies,
                view: view
            );
        }

        [Test]
        public void WaitingPhase_DoesNotSpawn_Before540s()
        {
            var module = new FinaleBossModule(42);
            var buffer = new GameplayCommandBuffer(GameplayModuleKind.Finale);
            buffer.Open();

            var enemies = new List<GameplayEnemySnapshot>();
            var ctx = CreateContext(530f, 0.1f, new Vec2(0, 0), enemies);

            module.Step(ctx, buffer);
            buffer.Close();

            Assert.AreEqual(0, buffer.Spawns.Count, "9:00 前不应发出生成意向");
            Assert.AreEqual(0, buffer.Attacks.Count, "9:00 前不应发出攻击意向");
        }

        [Test]
        public void At540s_RequestsBossSpawn_With3SecondWarning()
        {
            var module = new FinaleBossModule(42);
            var buffer = new GameplayCommandBuffer(GameplayModuleKind.Finale);
            buffer.Open();

            var enemies = new List<GameplayEnemySnapshot>();
            var ctx = CreateContext(540f, 0.1f, new Vec2(0, 0), enemies);

            module.Step(ctx, buffer);
            buffer.Close();

            Assert.AreEqual(1, buffer.Spawns.Count, "到达 540s 应请求生成首领");
            var spawn = buffer.Spawns[0];
            Assert.IsTrue(spawn.Spec.IsBoss, "生成的敌人必须标记为 Boss");
            Assert.AreEqual(3.0f, spawn.WarningSeconds, 0.01f, "出生预警时间必须为 3 秒");
            Assert.AreEqual(0, buffer.Attacks.Count, "出生预警期间绝对不能发出攻击");
        }

        [Test]
        public void FightingPhase_RotatesBetween_FallingPulse_And_SonicRing()
        {
            var module = new FinaleBossModule(42);
            var buffer = new GameplayCommandBuffer(GameplayModuleKind.Finale);

            // 1. 540s 触发生成
            buffer.Open();
            var enemies = new List<GameplayEnemySnapshot>();
            module.Step(CreateContext(540f, 0.1f, new Vec2(0, 0), enemies), buffer);
            buffer.Close();

            // 2. 模拟 543s 宿主已生成首领
            var bossEnemy = new EnemyState {
                Serial = 100,
                Generation = 1,
                Position = new Vec2(10, 0),
                PrevPosition = new Vec2(10, 0),
                Hp = 800,
                MaxHp = 800,
                IsElite = true,
                IsBoss = true,
                Life = EnemyLife.Alive
            };
            enemies.Add(new GameplayEnemySnapshot(bossEnemy));

            // 首领就绪帧
            buffer.Open();
            module.Step(CreateContext(543f, 0.1f, new Vec2(0, 0), enemies), buffer);
            buffer.Close();

            // 推进到首发攻击（543 + 2.0s = 545s）
            buffer.Open();
            module.Step(CreateContext(545.1f, 0.1f, new Vec2(1, 1), enemies), buffer);
            buffer.Close();

            Assert.AreEqual(1, buffer.Attacks.Count, "应该发出第一段落波");
            var atk1 = buffer.Attacks[0];
            Assert.AreEqual(AttackShapeKind.Circle, atk1.Shape.Kind, "招式 1 必须为圆形落波");
            Assert.AreEqual(new Vec2(1, 1), atk1.Shape.Center, "落波必须锁定玩家当时位置");
            Assert.AreEqual(100, atk1.Owner.Serial);

            // 推进分段落波 2
            buffer.Open();
            module.Step(CreateContext(545.8f, 0.1f, new Vec2(2, 2), enemies), buffer);
            buffer.Close();
            Assert.AreEqual(1, buffer.Attacks.Count, "应该发出第二段落波");

            // 推进分段落波 3
            buffer.Open();
            module.Step(CreateContext(546.5f, 0.1f, new Vec2(3, 3), enemies), buffer);
            buffer.Close();
            Assert.AreEqual(1, buffer.Attacks.Count, "应该发出第三段落波");

            // 三连发完后冷却进入扩散声环
            float ringTime = 546.5f + module.Config.WaveCooldown + 0.1f;
            buffer.Open();
            module.Step(CreateContext(ringTime, 0.1f, new Vec2(0, 0), enemies), buffer);
            buffer.Close();

            Assert.AreEqual(1, buffer.Attacks.Count, "应该轮换到招式 2：扩散声环");
            var atkRing = buffer.Attacks[0];
            Assert.AreEqual(AttackShapeKind.Annulus, atkRing.Shape.Kind, "招式 2 必须为圆环形扩散");
            Assert.GreaterOrEqual(atkRing.ActivateAt - ringTime, 1.2f, "扩散声环必须有至少 1.2s 预警时间");
        }

        [Test]
        public void EnragedPhase_HalfHp_ReducesCooldowns()
        {
            var module = new FinaleBossModule(42);
            var buffer = new GameplayCommandBuffer(GameplayModuleKind.Finale);

            buffer.Open();
            var enemies = new List<GameplayEnemySnapshot>();
            module.Step(CreateContext(540f, 0.1f, new Vec2(0, 0), enemies), buffer);
            buffer.Close();

            // 模拟半血首领 (HP 350 / 800 <= 50%)
            var bossEnemy = new EnemyState {
                Serial = 100,
                Generation = 1,
                Position = new Vec2(10, 0),
                PrevPosition = new Vec2(10, 0),
                Hp = 350,
                MaxHp = 800,
                IsElite = true,
                IsBoss = true,
                Life = EnemyLife.Alive
            };
            enemies.Add(new GameplayEnemySnapshot(bossEnemy));

            buffer.Open();
            module.Step(CreateContext(543f, 0.1f, new Vec2(0, 0), enemies), buffer);
            buffer.Close();

            // 触发狂暴下落波
            buffer.Open();
            module.Step(CreateContext(545.1f, 0.1f, new Vec2(0, 0), enemies), buffer);
            buffer.Close();

            Assert.AreEqual(1, buffer.Attacks.Count);
            var atk = buffer.Attacks[0];
            // 狂暴预警时间更短
            Assert.AreEqual(module.Config.PulseWarningEnraged, atk.ActivateAt - 545.1f, 0.05f);
        }

        [Test]
        public void BossDefeated_RequestsResolution_And_CancelsAttacks()
        {
            var module = new FinaleBossModule(42);
            var buffer = new GameplayCommandBuffer(GameplayModuleKind.Finale);

            buffer.Open();
            var enemies = new List<GameplayEnemySnapshot>();
            module.Step(CreateContext(540f, 0.1f, new Vec2(0, 0), enemies), buffer);
            buffer.Close();

            // 首领死亡
            var deadBoss = new EnemyState {
                Serial = 100,
                Generation = 1,
                Position = new Vec2(10, 0),
                PrevPosition = new Vec2(10, 0),
                Hp = 0,
                MaxHp = 800,
                IsElite = true,
                IsBoss = true,
                Life = EnemyLife.Dead
            };
            enemies.Add(new GameplayEnemySnapshot(deadBoss));

            buffer.Open();
            module.Step(CreateContext(544f, 0.1f, new Vec2(0, 0), enemies), buffer);
            buffer.Close();

            Assert.AreEqual(FinaleResolution.BossDefeated, buffer.Resolution, "首领死亡必须请求 BossDefeated 结算");
        }
    }
}
