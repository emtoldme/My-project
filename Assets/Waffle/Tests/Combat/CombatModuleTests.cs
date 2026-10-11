using NUnit.Framework;
using Waffle.Combat;

namespace Waffle.Tests.Combat
{
    /// <summary>C-01A 关键用例（纯规则，dotnet 与 Unity EditMode 都能跑）。</summary>
    public class CombatModuleTests
    {
        private const float Dt = 1f / 60f;

        private static CombatConfig NoSpawnConfig()
        {
            return new CombatConfig { spawningEnabled = false };
        }

        private static CombatInput Input(float now, Vec2 player, float damage = 10f, float interval = 0.6f, int pierce = 1, float magnet = 3f, float dt = Dt)
        {
            return new CombatInput
            {
                Dt = dt,
                RunTime = now,
                PlayerPos = player,
                ViewRadius = 14f,
                Stats = new CombatPlayerStats { Damage = damage, FireInterval = interval, Pierce = pierce, MagnetRadius = magnet }
            };
        }

        /// <summary>模拟框架一个 tick：T3–T8、T10、T13。返回 (kills, xp)。</summary>
        private static void FullTick(CombatModule m, CombatInput input, out int kills, out int xp, out int contact)
        {
            CombatStepResult r = m.StepCombat(input);
            kills = r.Kills;
            contact = r.ContactDamage;
            xp = m.StepGems(input).XpCollected;
            m.Release();
        }

        [Test]
        public void NoTarget_NoError_NoShot_WeaponStaysReady()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            float now = 0f;
            for (int i = 0; i < 300; i++)
            {
                int k, x, c;
                Assert.DoesNotThrow(() => FullTick(m, Input(now, Vec2.Zero), out k, out x, out c));
                now += Dt;
            }
            Assert.AreEqual(0, m.Counters.ShotsFired);
            Assert.AreEqual(0, m.Projectiles.Count);
            Assert.AreEqual(0f, m.WeaponTimer);
            Assert.IsNull(m.FindNearestTarget());

            // 目标进入范围后同一 tick 立刻开火
            m.DebugSpawnEnemy(new Vec2(5f, 0f), 100f, 0f);
            m.StepCombat(Input(now, Vec2.Zero));
            Assert.AreEqual(1, m.Counters.ShotsFired);
        }

        [Test]
        public void TargetOutOfRange_NotSelected()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            m.DebugSpawnEnemy(new Vec2(8.5f, 0f), 15f, 0f);
            m.StepCombat(Input(0f, Vec2.Zero));
            Assert.AreEqual(0, m.Counters.ShotsFired);
        }

        [Test]
        public void NearestAliveTargetChosen_TieBreaksBySerial()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            var far = m.DebugSpawnEnemy(new Vec2(6f, 0f), 100f, 0f);
            var a = m.DebugSpawnEnemy(new Vec2(0f, 4f), 100f, 0f);
            var b = m.DebugSpawnEnemy(new Vec2(0f, -4f), 100f, 0f);
            m.StepCombat(Input(0f, Vec2.Zero));
            Assert.AreSame(a, m.FindNearestTarget());
            Assert.Less(a.Serial, b.Serial);
            Assert.AreEqual(0f, m.Projectiles[0].Direction.X, 1e-5f);
            Assert.AreEqual(1f, m.Projectiles[0].Direction.Y, 1e-5f);
            Assert.IsTrue(far.IsAlive);
        }

        [Test]
        public void OneShotKill_TwoProjectilesSameTick_RewardsExactlyOnce()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            // 两颗子弹都已在飞，并且本 tick 都会扫到同一个敌人
            var target = m.DebugSpawnEnemy(new Vec2(3f, 0f), 15f, 0f);
            // 第一颗：t=0 开火
            m.StepCombat(Input(0f, Vec2.Zero, damage: 20f, interval: 0.01f, dt: 0.01f));
            m.Release();
            // 第二颗：下一 tick 开火（间隔 0.01）
            m.StepCombat(Input(0.01f, Vec2.Zero, damage: 20f, interval: 0.01f, dt: 0.01f));
            m.Release();
            Assert.AreEqual(2, m.Projectiles.Count, "两颗子弹在飞");
            Assert.IsTrue(target.IsAlive);

            // 大 dt 让两颗子弹在同一 tick 都穿过敌人
            int totalKills = 0, totalXp = 0;
            var input = Input(0.02f, Vec2.Zero, damage: 20f, interval: 10f, magnet: 0.1f, dt: 0.3f);
            CombatStepResult r = m.StepCombat(input);
            totalKills += r.Kills;
            int killedEvents = 0, hitEvents = 0;
            foreach (var e in r.Events)
            {
                if (e.Type == PresentationEventType.EnemyKilled) killedEvents++;
                if (e.Type == PresentationEventType.EnemyHit) hitEvents++;
            }
            m.StepGems(input);
            m.Release();
            Assert.AreEqual(1, totalKills);
            Assert.AreEqual(1, killedEvents);
            Assert.AreEqual(1, hitEvents, "第二颗子弹穿过已死亡的敌人，不算命中");
            Assert.AreEqual(1, m.Counters.TotalKills);
            Assert.AreEqual(1, m.Counters.TotalXpDropped);
            Assert.AreEqual(1, m.Gems.Count, "只掉 1 颗宝石");
            foreach (var p in m.Projectiles) Assert.AreEqual(1, p.PierceLeft, "后面的子弹没消耗穿透，继续飞");

            // 后续 tick 不会重复结算
            for (int i = 0; i < 10; i++)
            {
                int k, x, c;
                FullTick(m, Input(0.4f + i * Dt, new Vec2(0f, 20f), interval: 10f), out k, out x, out c);
                totalKills += k;
            }
            Assert.AreEqual(1, totalKills);
            Assert.AreEqual(1, m.Counters.TotalKills);
            Assert.AreEqual(0, m.Counters.TotalXpCollected + totalXp, "玩家远离，没捡");
        }

        [Test]
        public void SameTarget_HitAgainByLaterShot_ButNotTwiceBySameProjectile()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            var e = m.DebugSpawnEnemy(new Vec2(4f, 0f), 35f, 0f);
            float now = 0f;
            int hits = 0;
            for (int i = 0; i < 200 && e.IsAlive; i++)
            {
                var r = m.StepCombat(Input(now, Vec2.Zero, damage: 10f, interval: 0.6f));
                foreach (var ev in r.Events) if (ev.Type == PresentationEventType.EnemyHit) hits++;
                m.StepGems(Input(now, Vec2.Zero));
                m.Release();
                now += Dt;
            }
            Assert.IsFalse(e.IsAlive);
            Assert.AreEqual(4, hits, "35 HP / 10 伤害 = 4 发，每发各命中一次");
            Assert.AreEqual(4, m.Counters.ShotsFired);
            Assert.AreEqual(1, m.Counters.TotalKills);
        }

        [Test]
        public void SameProjectile_OverlappingEnemyForManyTicks_HitsOnlyOnce()
        {
            var cfg = NoSpawnConfig();
            cfg.projectileSpeed = 0.01f; // 子弹几乎不动，持续与敌人重叠
            var m = new CombatModule(cfg, 1);
            var e = m.DebugSpawnEnemy(new Vec2(0.3f, 0f), 1000f, 0f);
            for (int i = 0; i < 20; i++)
            {
                m.StepCombat(Input(i * Dt, Vec2.Zero, pierce: 3, interval: 100f));
                m.Release();
            }
            Assert.AreEqual(1, m.Counters.ShotsFired);
            Assert.AreEqual(1, m.Counters.Hits);
            Assert.AreEqual(990f, e.Hp, 1e-4f);
        }

        [Test]
        public void Pierce_HitsUpToPierceEnemies_InEntryOrder()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            var e1 = m.DebugSpawnEnemy(new Vec2(2f, 0f), 100f, 0f);
            var e2 = m.DebugSpawnEnemy(new Vec2(4f, 0f), 100f, 0f);
            var e3 = m.DebugSpawnEnemy(new Vec2(6f, 0f), 100f, 0f);
            m.StepCombat(Input(0f, Vec2.Zero, pierce: 2, interval: 100f, dt: 0.5f)); // 0.5 s 飞 7.5 m
            Assert.AreEqual(90f, e1.Hp);
            Assert.AreEqual(90f, e2.Hp);
            Assert.AreEqual(100f, e3.Hp);
            m.Release();
            Assert.AreEqual(0, m.Projectiles.Count, "穿透用完回池");
        }

        [Test]
        public void SweptHit_CatchesCrossingMovingEnemy()
        {
            // 子弹沿 +x，敌人沿 -z 高速横穿；只查移动后位置会漏判，相对扫掠能命中
            Vec2 b0 = new Vec2(0f, 0f), b1 = new Vec2(1f, 0f);
            Vec2 e0 = new Vec2(0.5f, 1.0f), e1 = new Vec2(0.5f, -1.0f);
            float s;
            Assert.IsTrue(CombatModule.SweptEntry(b0 - e0, b1 - e1, 0.7f, out s));
            Assert.That(s, Is.InRange(0f, 1f));
            // 对照：只用移动后的敌人位置做线段判定 → 漏判
            Assert.IsFalse(CombatModule.SweptEntry(b0 - e1, b1 - e1, 0.7f, out s));
        }

        [Test]
        public void Contact_MultipleEnemies_ReportsSingleHighestDamage()
        {
            var cfg = NoSpawnConfig();
            var m = new CombatModule(cfg, 1);
            var n1 = m.DebugSpawnEnemy(new Vec2(0.8f, 0f), 1000f, 0f);
            var n2 = m.DebugSpawnEnemy(new Vec2(-0.8f, 0f), 1000f, 0f);
            m.DebugSpawnEnemy(new Vec2(5f, 5f), 1000f, 0f); // 不接触
            var r = m.StepCombat(Input(0f, Vec2.Zero, interval: 100f, dt: 0f));
            Assert.AreEqual(10, r.ContactDamage);
            Assert.AreEqual(2, r.ContactCount);
            Assert.AreEqual(20, r.ContactDamageSum);
            Assert.AreEqual(n1.Serial, r.ContactSourceSerial, "同伤害取序号小的");
            Assert.IsTrue(n2.IsAlive);
        }

        [Test]
        public void Contact_EliteDominates_AndDeadThisTickDealsNone()
        {
            var cfg = NoSpawnConfig();
            cfg.eliteTimes = new float[0];
            var m = new CombatModule(cfg, 1);
            // 被本 tick 打死的敌人不造成接触伤害（T7 在 T8 之前）
            var weak = m.DebugSpawnEnemy(new Vec2(0.6f, 0f), 5f, 0f);
            var r = m.StepCombat(Input(0f, Vec2.Zero, damage: 10f, interval: 100f));
            Assert.IsFalse(weak.IsAlive);
            Assert.AreEqual(1, r.Kills);
            Assert.AreEqual(0, r.ContactDamage);
            Assert.AreEqual(-1, r.ContactSourceSerial);
        }

        [Test]
        public void Contact_OnlyRaw_ModuleDoesNotApplyIFrames()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            m.DebugSpawnEnemy(new Vec2(0.5f, 0f), 1000f, 0f);
            for (int i = 0; i < 5; i++)
            {
                var r = m.StepCombat(Input(i * Dt, Vec2.Zero, damage: 0f, interval: 100f));
                Assert.AreEqual(10, r.ContactDamage, "每 tick 都如实报告，无敌由框架判定");
                m.Release();
            }
        }

        [Test]
        public void XpPickup_KillThenWalkToGem_CollectsExactlyOnce()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            m.DebugSpawnEnemy(new Vec2(5f, 0f), 10f, 0f);
            int kills, xp, c, totalXp = 0, totalKills = 0;
            FullTick(m, Input(0f, Vec2.Zero, damage: 10f, interval: 100f, dt: 0.4f), out kills, out xp, out c);
            totalKills += kills; totalXp += xp;
            Assert.AreEqual(1, totalKills);
            Assert.AreEqual(1, m.Gems.Count);
            Assert.AreEqual(0, totalXp, "宝石在 5 m 外，磁吸 3 m 外");

            // 走到 2.5 m 处：进入磁吸，被吸过来并拾取
            float now = 0.4f;
            for (int i = 0; i < 60; i++)
            {
                FullTick(m, Input(now, new Vec2(2.5f, 0f), interval: 100f), out kills, out xp, out c);
                totalXp += xp;
                now += Dt;
            }
            Assert.AreEqual(1, totalXp);
            Assert.AreEqual(0, m.Gems.Count);
            Assert.AreEqual(1, m.Counters.TotalXpCollected);
        }

        [Test]
        public void XpPickup_EliteValue_AndGemPickupEvent()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            m.DebugSpawnGem(new Vec2(0.5f, 0f), 10);
            var r = m.StepGems(Input(0f, Vec2.Zero));
            Assert.AreEqual(10, r.XpCollected);
            Assert.AreEqual(1, r.Events.Count);
            Assert.AreEqual(PresentationEventType.GemPickup, r.Events[0].Type);
            Assert.AreEqual(10f, r.Events[0].Value);
            m.Release();
            Assert.AreEqual(0, m.StepGems(Input(Dt, Vec2.Zero)).XpCollected, "不会重复拾取");
        }

        [Test]
        public void Gem_ExpiresAfterLifetime_UnlessMagnetized()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            m.DebugSpawnGem(new Vec2(20f, 0f), 1);
            float now = 0f;
            int xp = 0;
            for (int i = 0; i < 21 * 20; i++) { xp += m.StepGems(Input(now, Vec2.Zero, dt: 0.05f)).XpCollected; m.Release(); now += 0.05f; }
            Assert.AreEqual(0, m.Gems.Count);
            Assert.AreEqual(1, m.Counters.GemsExpired);
            Assert.AreEqual(0, xp);
        }

        [Test]
        public void GemCap_MergeOnCreate_ConservesXp_AndNeverExceedsCap()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            for (int i = 0; i < 200; i++) m.DebugSpawnGem(new Vec2(10f + (i % 10), 10f), 1);
            Assert.AreEqual(150, m.Gems.Count);
            int big = 0, sum = 0;
            foreach (var g in m.Gems) { sum += g.Value; if (g.IsBig) { big++; Assert.AreEqual(51, g.Value); } }
            Assert.AreEqual(1, big);
            Assert.AreEqual(200, sum);
            Assert.AreEqual(0, m.Counters.GemPoolMiss);
        }

        [Test]
        public void Restart_NewModuleIsClean_OldModuleUnaffected()
        {
            var cfg = new CombatConfig();
            var a = new CombatModule(cfg, 7);
            float now = 0f;
            for (int i = 0; i < 60 * 40; i++)
            {
                int k, x, c;
                FullTick(a, Input(now, Vec2.Zero), out k, out x, out c);
                now += Dt;
            }
            Assert.Greater(a.Counters.EnemiesSpawned, 0);

            var b = new CombatModule(cfg, 7);
            Assert.AreEqual(0, b.Enemies.Count);
            Assert.AreEqual(0, b.Projectiles.Count);
            Assert.AreEqual(0, b.Gems.Count);
            Assert.AreEqual(0, b.Counters.TotalKills);
            Assert.AreEqual(0f, b.WeaponTimer);
            var r = b.StepCombat(Input(0f, Vec2.Zero));
            Assert.AreEqual(2, b.Counters.EnemiesSpawned, "新一局 t=0 刷第一波 2 只");
            Assert.AreEqual(0, r.Kills);
        }

        [Test]
        public void SameSeed_SameResult_Deterministic()
        {
            var cfg = new CombatConfig();
            var a = new CombatModule(cfg, 42);
            var b = new CombatModule(cfg, 42);
            float now = 0f;
            for (int i = 0; i < 60 * 45; i++)
            {
                int k, x, c;
                Vec2 p = new Vec2((float)System.Math.Sin(now * 0.3f) * 10f, 0f);
                FullTick(a, Input(now, p), out k, out x, out c);
                FullTick(b, Input(now, p), out k, out x, out c);
                now += Dt;
            }
            Assert.AreEqual(a.Counters.TotalKills, b.Counters.TotalKills);
            Assert.AreEqual(a.Enemies.Count, b.Enemies.Count);
            for (int i = 0; i < a.Enemies.Count; i++)
                Assert.AreEqual(a.Enemies[i].Position.X, b.Enemies[i].Position.X);
        }

        [Test]
        public void AliveCap30_NormalWavesTruncated_PoolNeverMisses()
        {
            var cfg = new CombatConfig();
            var m = new CombatModule(cfg, 3);
            float now = 0f;
            int maxAlive = 0;
            for (int i = 0; i < 60 * 100; i++)
            {
                // 无伤害：敌人只增不减
                var input = Input(now, Vec2.Zero, damage: 0f);
                m.StepCombat(input);
                m.StepGems(input);
                m.Release();
                if (m.AliveCount > maxAlive) maxAlive = m.AliveCount;
                now += Dt;
            }
            Assert.LessOrEqual(maxAlive, cfg.aliveCap + cfg.eventOverflow);
            Assert.Greater(m.Counters.Truncated, 0);
            Assert.AreEqual(0, m.Counters.EnemyPoolMiss);
        }

        [Test]
        public void StaleReference_RejectedAfterRelease()
        {
            var m = new CombatModule(NoSpawnConfig(), 1);
            var e = m.DebugSpawnEnemy(new Vec2(3f, 0f), 5f, 0f);
            int serial = e.Serial, gen = e.Generation;
            EnemyState found;
            Assert.IsTrue(m.TryGetEnemy(serial, gen, out found));
            m.StepCombat(Input(0f, Vec2.Zero, dt: 0.3f));
            m.Release();
            Assert.IsFalse(m.TryGetEnemy(serial, gen, out found));
            Assert.AreEqual(gen + 1, e.Generation);
        }
    }
}
