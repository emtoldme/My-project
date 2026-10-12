// TEST_ONLY：E-02 组合实验检查。dotnet 可跑（DotnetRunner~/Tests），Unity 下作为 EditMode 测试（未在 Unity 验证）。
using System.Linq;
using NUnit.Framework;
using Waffle.Experiments.Equipment;

namespace Waffle.Experiments.Equipment.Tests
{
    public class EffectComboLabTests
    {
        static ComboEventKind[] Kinds(ComboRun r) => r.Log.Select(x => x.Kind).ToArray();
        static int Total(ComboRun r) => r.Log.Sum(x => x.Damage);

        [Test]
        public void NoneABAndAB_ProduceDistinctBoundedSequences()
        {
            var none = ComboLabScenario.RunOne(ComboConfig.None());
            var a = ComboLabScenario.RunOne(ComboConfig.A());
            var b = ComboLabScenario.RunOne(ComboConfig.B());
            var ab = ComboLabScenario.RunOne(ComboConfig.AB());
            Assert.AreEqual(new[] { ComboEventKind.Direct }, Kinds(none));
            Assert.AreEqual(new[] { ComboEventKind.Direct, ComboEventKind.Echo }, Kinds(a));
            Assert.AreEqual(new[] { ComboEventKind.Direct, ComboEventKind.Propagation }, Kinds(b));
            Assert.AreEqual(new[] { ComboEventKind.Direct, ComboEventKind.Propagation, ComboEventKind.Echo, ComboEventKind.Propagation }, Kinds(ab));
            Assert.AreEqual(new[] { 10, 15, 14, 23 }, new[] { Total(none), Total(a), Total(b), Total(ab) });
            // A+B：两次传播落在不同目标（T2 最近，T3 次之），远处 T4 不受影响
            var props = ab.Log.Where(x => x.Kind == ComboEventKind.Propagation).Select(x => x.Target.Id).ToArray();
            Assert.AreEqual(new[] { 2, 3 }, props);
            Assert.AreEqual(30, ab.Hp(4));
            Assert.That(new[] { none, a, b, ab }.All(r => r.PendingCount == 0));
        }

        [Test]
        public void SameInput_IsDeterministic_AndRunsAreIndependent()
        {
            var r1 = ComboLabScenario.RunOne(ComboConfig.AB());
            var r2 = ComboLabScenario.RunOne(ComboConfig.AB());
            Assert.AreEqual(r1.FormatLog(), r2.FormatLog());
            Assert.AreEqual(30, ComboLabScenario.RunOne(ComboConfig.None()).Hp(2), "另一局不受前一局影响");
        }

        [Test]
        public void ManyHits_StayWithinCaps_NoRunawayGrowth()
        {
            var cfg = ComboConfig.AB(); cfg.EchoDelay = 0f; cfg.PropagationDelay = 0f; cfg.MaxEventsPerTrigger = 3; cfg.MaxDepth = 2;
            var specs = Enumerable.Range(1, 10).Select(i => new ComboTargetSpec(i, i, 1000)).ToArray();
            var run = new ComboRun(cfg, specs);
            for (int i = 0; i < 50; i++) run.DirectHit(run.Handle(1 + i % 10));
            run.Tick(1f);
            foreach (var g in run.Log.GroupBy(x => x.TriggerId))
            {
                Assert.LessOrEqual(g.Count(), 1 + cfg.MaxEventsPerTrigger);
                Assert.LessOrEqual(g.Max(x => x.Depth), cfg.MaxDepth);
            }
            Assert.AreEqual(0, run.PendingCount);
            // 序号严格递增于每个时间点内（确定性队列：时间→序号）
            for (int i = 1; i < run.Log.Count; i++)
                Assert.That(run.Log[i].Time > run.Log[i - 1].Time || run.Log[i].Time == run.Log[i - 1].Time);
        }

        [Test]
        public void Paused_DoesNotAdvance_ThenResumes()
        {
            var run = new ComboRun(ComboConfig.A(), ComboLabScenario.DefaultTargets());
            run.DirectHit(run.Handle(1));
            run.Paused = true;
            for (int i = 0; i < 100; i++) run.Tick(0.1f);
            Assert.AreEqual(0f, run.Time);
            Assert.AreEqual(1, run.PendingCount);
            Assert.AreEqual(20, run.Hp(1));
            run.Paused = false;
            run.Tick(0.5f);
            Assert.AreEqual(0, run.PendingCount);
            Assert.AreEqual(15, run.Hp(1));
        }

        [Test]
        public void RecycledOrRespawnedTarget_IsNotHitByStaleEvent()
        {
            var run = new ComboRun(ComboConfig.AB(), ComboLabScenario.DefaultTargets());
            run.DirectHit(run.Handle(1));           // 已排队：传播->T2(0.1s)，回声->T1(0.5s)
            run.Recycle(2); run.Respawn(2, 30);     // T2 换代
            run.Recycle(1); run.Respawn(1, 30);     // T1 换代
            run.Tick(1f);
            Assert.AreEqual(30, run.Hp(1));
            Assert.AreEqual(30, run.Hp(2));
            Assert.AreEqual(2, run.Log.Count(x => x.Outcome == ComboOutcome.SkippedStale));
            Assert.AreEqual(0, run.PendingCount);
        }

        [Test]
        public void Restart_ClearsQueue_AndInvalidatesOldHandles()
        {
            var run = new ComboRun(ComboConfig.AB(), ComboLabScenario.DefaultTargets());
            var old = run.Handle(1);
            run.DirectHit(old);
            Assert.Greater(run.PendingCount, 0);
            run.Restart();
            Assert.AreEqual(0, run.PendingCount);
            Assert.AreEqual(0, run.Log.Count);
            Assert.AreEqual(0f, run.Time);
            run.DirectHit(old);                     // 旧局句柄
            run.Tick(2f);
            Assert.AreEqual(ComboOutcome.SkippedStale, run.Log[0].Outcome);
            Assert.AreEqual(30, run.Hp(1));
        }
    }
}
