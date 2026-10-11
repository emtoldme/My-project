using NUnit.Framework;

namespace Waffle.Tests
{
    /// <summary>F-02：终局后调试命令必须明确拒绝、不排队；重开（新建 RunSimulation）不继承面板与队列。</summary>
    public class TerminalCommandTests
    {
        private static SimDriver Defeated()
        {
            var d = new SimDriver();
            d.Sim.DebugHurt(100);
            d.Tick();
            Assert.AreEqual(RunPhase.Defeat, d.Sim.State.Phase);
            return d;
        }

        [TestCase("givexp 30")]
        [TestCase("hurt 10")]
        [TestCase("sethp 50")]
        [TestCase("settime 10")]
        public void Defeat_StateChangingCommands_ReturnRejection(string cmd)
        {
            var d = Defeated();
            string result = DebugCommands.Execute(d.Sim, cmd);
            StringAssert.StartsWith("已终局（Defeat），命令未执行", result);
            Assert.AreEqual(0, d.Sim.QueuedXp);
            Assert.AreEqual(0, d.Sim.QueuedDamage);
            Assert.AreEqual(0, d.Sim.State.Hp);
            Assert.AreEqual(1, d.Sim.State.Level);
            Assert.AreEqual(0f, d.Sim.State.Now);
        }

        [Test]
        public void Victory_GiveXp_ReturnsRejection_AndNothingQueued()
        {
            var d = new SimDriver();
            d.Sim.DebugSetTime(180f);
            d.Tick();
            Assert.AreEqual(RunPhase.Victory, d.Sim.State.Phase);
            StringAssert.StartsWith("已终局（Victory），命令未执行", DebugCommands.Execute(d.Sim, "givexp 30"));
            Assert.AreEqual(0, d.Sim.QueuedXp);
            d.Tick();
            Assert.AreEqual(0, d.Sim.State.Xp);
        }

        [Test]
        public void Playing_GiveXp_StillQueues()
        {
            var d = new SimDriver();
            StringAssert.StartsWith("排队 +30 XP", DebugCommands.Execute(d.Sim, "givexp 30"));
            Assert.AreEqual(30, d.Sim.QueuedXp);
        }

        [Test]
        public void QueuedXpAndDamage_ClearedWhenRunEnds()
        {
            var d = new SimDriver();
            d.Sim.FixedDt = 0.05f;
            d.Sim.DebugSetTime(179.99f);
            d.Sim.DebugGiveXp(30);
            d.Sim.DebugHurt(5);
            d.Tick(); // 同 tick：T8 受伤（不致死）、T10 结算 XP、T11 到时 → Victory
            Assert.AreEqual(RunPhase.Victory, d.Sim.State.Phase);
            Assert.AreEqual(0, d.Sim.QueuedXp);
            Assert.AreEqual(0, d.Sim.QueuedDamage);
            Assert.AreEqual(0, d.Sim.State.PendingLevelUps);

            // 已排队但还没结算的 XP 在终局时被清掉（同 tick T9 死亡，T10 不执行）
            var d2 = new SimDriver();
            d2.Sim.DebugGiveXp(30);
            d2.Sim.DebugSetHp(0);
            d2.Tick(); // T9 Defeat，T10 未执行
            Assert.AreEqual(RunPhase.Defeat, d2.Sim.State.Phase);
            Assert.AreEqual(0, d2.Sim.QueuedXp);
            Assert.AreEqual(1, d2.Sim.State.Level);
        }

        [Test]
        public void RestartFromLevelUp_NewRunHasNoPanelOrQueue()
        {
            var d = new SimDriver();
            d.Sim.DebugGiveXp(30);
            d.Tick();
            Assert.AreEqual(RunPhase.LevelUp, d.Sim.State.Phase);
            d.Sim.DebugGiveXp(7); // 面板打开时又排队了 XP

            // GameLoop.Restart = 重新加载场景 = 新建 RunSimulation
            var fresh = new SimDriver();
            Assert.AreEqual(RunPhase.Playing, fresh.Sim.State.Phase);
            Assert.AreEqual(0, fresh.Sim.State.PendingLevelUps);
            Assert.AreEqual(0, fresh.Sim.QueuedXp);
            Assert.AreEqual(0, fresh.Sim.PanelsShown);
            CollectionAssert.AreEqual(new[] { UpgradeKind.None, UpgradeKind.None, UpgradeKind.None }, fresh.Sim.CurrentOptions);
            for (int i = 0; i < 5; i++) fresh.Tick();
            Assert.AreEqual(RunPhase.Playing, fresh.Sim.State.Phase, "新局不会弹出旧局的升级面板");
        }
    }
}
