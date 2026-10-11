using NUnit.Framework;

namespace Waffle.Tests
{
    /// <summary>
    /// 回归测试：RunConfig 是升级数值的唯一来源。
    /// 用户在 Unity 中发现的两个 bug：(a) 改基础移速无效；(b) 基础 HP 200 时第一次生命强化降到 125。
    /// </summary>
    public class UpgradeConfigTests
    {
        private const float Eps = 1e-4f;

        private static SimDriver PanelWith(RunConfig config, UpgradeKind kind)
        {
            var d = new SimDriver(config);
            d.Sim.DebugGiveXp(5);
            d.Tick();
            Assert.AreEqual(RunPhase.LevelUp, d.Sim.State.Phase);
            d.Sim.CurrentOptions[0] = kind; // 不依赖随机抽卡
            return d;
        }

        private static float MeasureSpeed(SimDriver d)
        {
            d.Sim.FixedDt = 0.05f;
            float x0 = d.Sim.PlayerX;
            var s = InputSnapshot.None; s.MoveX = 1f;
            d.Tick(0.05f, s);
            return (d.Sim.PlayerX - x0) / 0.05f;
        }

        [Test]
        public void BugA_BaseMoveSpeed7_Is7AtLv0_And7Point4AfterOneUpgrade()
        {
            var config = new RunConfig { moveSpeed = 7f };
            var d = new SimDriver(config);
            Assert.AreEqual(7f, MeasureSpeed(d), Eps);

            d = PanelWith(config, UpgradeKind.MoveSpeed);
            Assert.IsTrue(d.ChooseAfterGuard(0));
            Assert.AreEqual(7.4f, d.Sim.CurrentValue(UpgradeKind.MoveSpeed), Eps);
            Assert.AreEqual(7.4f, MeasureSpeed(d), Eps);
        }

        [Test]
        public void BugB_BaseHp200_FirstUpgradeGives225_AndHeals25()
        {
            var config = new RunConfig { maxHp = 200 };
            var d = new SimDriver(config);
            Assert.AreEqual(200, d.Sim.State.MaxHp);
            Assert.AreEqual(200, d.Sim.State.Hp);

            d = PanelWith(config, UpgradeKind.MaxHp);
            Assert.IsTrue(d.ChooseAfterGuard(0));
            Assert.AreEqual(225, d.Sim.State.MaxHp);
            Assert.AreEqual(225, d.Sim.State.Hp, "满血 200 + 25 回复 = 225");

            // 受伤后再升级：回复 25，且不超过新的最大值
            var d2 = new SimDriver(config);
            d2.Sim.DebugSetHp(100);
            d2.Sim.DebugGiveXp(5);
            d2.Tick();
            d2.Sim.CurrentOptions[0] = UpgradeKind.MaxHp;
            Assert.IsTrue(d2.ChooseAfterGuard(0));
            Assert.AreEqual(225, d2.Sim.State.MaxHp);
            Assert.AreEqual(125, d2.Sim.State.Hp);
        }

        [Test]
        public void Heal_UsesConfigMaxHpAndFraction()
        {
            var config = new RunConfig { maxHp = 200 };
            var d = new SimDriver(config);
            d.Sim.DebugSetHp(50);
            d.Sim.DebugGiveXp(5);
            d.Tick();
            d.Sim.CurrentOptions[0] = UpgradeKind.Heal;
            Assert.IsTrue(d.ChooseAfterGuard(0));
            Assert.AreEqual(110, d.Sim.State.Hp, "50 + 30% × 200");
        }

        [Test]
        public void DefaultConfig_MatchesV021TableU1()
        {
            var c = new RunConfig();
            AssertTable(c, UpgradeKind.Damage, 10, 15, 20, 25, 30);
            AssertTable(c, UpgradeKind.FireRate, 0.60f, 0.48f, 0.40f, 0.34f, 0.30f);
            AssertTable(c, UpgradeKind.Pierce, 1, 2, 3);
            AssertTable(c, UpgradeKind.MoveSpeed, 5.0f, 5.4f, 5.8f, 6.2f);
            AssertTable(c, UpgradeKind.MaxHp, 100, 125, 150, 175, 200);
            AssertTable(c, UpgradeKind.PickupRange, 3.0f, 4.5f, 6.0f, 8.0f);
            AssertTable(c, UpgradeKind.DodgeCooldown, 1.5f, 1.25f, 1.0f, 0.8f);
            Assert.AreEqual(25, c.upgrades.maxHpUpgradeHeal);
            Assert.AreEqual(0.30f, c.upgrades.healFraction, Eps);
            // 总等级 4+4+2+3+4+3+3 = 23（§1.6 兜底可达性）
            int total = 0;
            for (int i = 0; i < UpgradeRules.RealKindCount; i++) total += UpgradeRules.MaxLevel((UpgradeKind)i, c);
            Assert.AreEqual(23, total);
        }

        private static void AssertTable(RunConfig c, UpgradeKind kind, params float[] expected)
        {
            Assert.AreEqual(expected.Length - 1, UpgradeRules.MaxLevel(kind, c), kind + " 最高等级");
            for (int lv = 0; lv < expected.Length; lv++)
                Assert.AreEqual(expected[lv], UpgradeRules.Value(kind, lv, c), Eps, kind + " Lv" + lv);
            // 超过最高等级时保持在满级数值
            Assert.AreEqual(expected[expected.Length - 1], UpgradeRules.Value(kind, expected.Length + 3, c), Eps);
        }

        [TestCase(UpgradeKind.Damage, 12f, 17f)]
        [TestCase(UpgradeKind.FireRate, 0.8f, 0.68f)]
        [TestCase(UpgradeKind.Pierce, 2f, 3f)]
        [TestCase(UpgradeKind.PickupRange, 4f, 5.5f)]
        [TestCase(UpgradeKind.DodgeCooldown, 2f, 1.75f)]
        public void OtherKinds_UseConfigBase(UpgradeKind kind, float baseValue, float afterOne)
        {
            var config = new RunConfig();
            switch (kind)
            {
                case UpgradeKind.Damage: config.damage = baseValue; break;
                case UpgradeKind.FireRate: config.fireInterval = baseValue; break;
                case UpgradeKind.Pierce: config.pierce = baseValue; break;
                case UpgradeKind.PickupRange: config.magnetRadius = baseValue; break;
                case UpgradeKind.DodgeCooldown: config.dodgeCooldown = baseValue; break;
            }
            var d = PanelWith(config, kind);
            Assert.AreEqual(baseValue, d.Sim.CurrentValue(kind), Eps);
            Assert.IsTrue(d.ChooseAfterGuard(0));
            Assert.AreEqual(afterOne, d.Sim.CurrentValue(kind), Eps);
        }

        [Test]
        public void ConfigSteps_EditableAndMaxLevelFollowsArrayLength()
        {
            var config = new RunConfig();
            config.upgrades.moveSpeedSteps = new[] { 1f };
            var d = PanelWith(config, UpgradeKind.MoveSpeed);
            Assert.IsTrue(d.ChooseAfterGuard(0));
            Assert.AreEqual(6f, d.Sim.CurrentValue(UpgradeKind.MoveSpeed), Eps);
            Assert.AreEqual(1, UpgradeRules.MaxLevel(UpgradeKind.MoveSpeed, config));
        }

        [Test]
        public void Simulation_UsesTheSameConfigInstance()
        {
            var config = new RunConfig();
            var sim = new RunSimulation(config, new ListLog());
            Assert.AreSame(config, sim.Config);
            config.moveSpeed = 9f; // Inspector 运行中修改同一对象也会生效
            Assert.AreEqual(9f, sim.CurrentValue(UpgradeKind.MoveSpeed), Eps);
        }
    }
}
