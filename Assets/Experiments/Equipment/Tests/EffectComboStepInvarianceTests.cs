// TEST_ONLY：E-02 复核（review/e02-integration-followup）。粗步长与细步长的结算序列必须完全一致。
using System.Linq;
using NUnit.Framework;
using Waffle.Experiments.Equipment;

namespace Waffle.Experiments.Equipment.Tests
{
    public class EffectComboStepInvarianceTests
    {
        static ComboConfig Make(string name)
        {
            switch (name)
            {
                case "A": return ComboConfig.A();
                case "B": return ComboConfig.B();
                case "AB": return ComboConfig.AB();
                default: return ComboConfig.None();
            }
        }

        static string Exact(ComboRun r) => string.Join("\n", r.Log.Select(x =>
            x.Seq + "|" + x.ParentSeq + "|" + x.TriggerId + "|" + x.Time.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
            + "|" + x.Kind + "|" + x.Depth + "|" + x.Source + "|" + x.Target + "|" + x.Damage + "|" + x.HpAfter + "|" + x.Outcome));

        [TestCase("none")] [TestCase("A")] [TestCase("B")] [TestCase("AB")]
        public void CoarseSingleStep_EqualsFineSteps_ExactTimesAndDamage(string cfg)
        {
            var coarse = new ComboRun(Make(cfg), ComboLabScenario.DefaultTargets());
            coarse.DirectHit(coarse.Handle(1));
            coarse.Tick(2f);

            var fine = new ComboRun(Make(cfg), ComboLabScenario.DefaultTargets());
            fine.DirectHit(fine.Handle(1));
            for (int i = 0; i < 200; i++) fine.Tick(0.01f);

            var mid = new ComboRun(Make(cfg), ComboLabScenario.DefaultTargets());
            mid.DirectHit(mid.Handle(1));
            for (int i = 0; i < 7; i++) mid.Tick(0.3f); // 0.3 不整除 0.1/0.5，跨越边界

            Assert.AreEqual(Exact(fine), Exact(coarse));
            Assert.AreEqual(Exact(fine), Exact(mid));
            Assert.AreEqual(0, coarse.PendingCount);
            Assert.AreEqual(0, fine.PendingCount);
        }

        [Test]
        public void PauseInMiddle_DoesNotShiftScheduledTimes()
        {
            var a = new ComboRun(ComboConfig.AB(), ComboLabScenario.DefaultTargets());
            a.DirectHit(a.Handle(1));
            a.Tick(0.2f);
            a.Paused = true; a.Tick(5f); Assert.AreEqual(0, a.DirectHit(a.Handle(2)));
            a.Paused = false; a.Tick(1.8f);

            var b = new ComboRun(ComboConfig.AB(), ComboLabScenario.DefaultTargets());
            b.DirectHit(b.Handle(1));
            b.Tick(2f);
            Assert.AreEqual(Exact(b), Exact(a));
        }
    }
}
