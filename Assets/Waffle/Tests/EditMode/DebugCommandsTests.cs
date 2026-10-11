using NUnit.Framework;

namespace Waffle.Tests
{
    public class DebugCommandsTests
    {
        [Test]
        public void ParsesMinimalHookSet()
        {
            var d = new SimDriver();
            var sim = d.Sim;
            DebugCommands.Execute(sim, "settime 179.5");
            Assert.AreEqual(179.5f, sim.State.Now, 1e-4);
            DebugCommands.Execute(sim, "setdt 0.05");
            Assert.AreEqual(0.05f, sim.FixedDt, 1e-6);
            DebugCommands.Execute(sim, "setdt 0");
            Assert.AreEqual(0f, sim.FixedDt);
            DebugCommands.Execute(sim, "sethp 10");
            Assert.AreEqual(10, sim.State.Hp);
            DebugCommands.Execute(sim, "god on");
            Assert.IsTrue(sim.God);
            DebugCommands.Execute(sim, "god off");
            Assert.IsFalse(sim.God);
            DebugCommands.Execute(sim, "tracetick on");
            Assert.AreEqual(-1, sim.TraceTicks);
            DebugCommands.Execute(sim, "tracetick");
            Assert.AreEqual(1, sim.TraceTicks);
            DebugCommands.Execute(sim, "tracetick off");
            Assert.AreEqual(0, sim.TraceTicks);
            DebugCommands.Execute(sim, "givexp 5");
            d.Tick();
            Assert.AreEqual(2, sim.State.Level);
        }

        [Test]
        public void UnknownOrBadArgs_DoNotThrow()
        {
            var sim = new SimDriver().Sim;
            StringAssert.Contains("未知命令", DebugCommands.Execute(sim, "foo"));
            StringAssert.Contains("参数错误", DebugCommands.Execute(sim, "settime abc"));
            Assert.AreEqual("", DebugCommands.Execute(sim, "   "));
        }
    }
}
