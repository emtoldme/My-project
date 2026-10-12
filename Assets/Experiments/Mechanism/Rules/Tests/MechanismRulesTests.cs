// TEST_ONLY：M01 规则适配检查。dotnet 可跑（DotnetRunner~），Unity 下为 EditMode 测试（未在 Unity 验证）。
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reverb.Experiments.Mechanism;
using Reverb.Experiments.Mechanism.Rules;

namespace Reverb.Experiments.Mechanism.Rules.Tests
{
    /// <summary>测试宿主：模拟唯一 CombatModule 的 HP 与对象池代号。</summary>
    public sealed class FakeHost : IMechanismHost
    {
        public sealed class T { public int Serial, Gen, Hp; public float X, Z; public bool Alive => Hp > 0; }
        public readonly List<T> Targets = new List<T>();
        public readonly List<string> Log = new List<string>();
        public int DamageTotal;
        public T Add(int serial, float x, float z, int hp = 30) { var t = new T { Serial = serial, Gen = 1, Hp = hp, X = x, Z = z }; Targets.Add(t); return t; }
        public T Get(int s) => Targets.First(t => t.Serial == s);
        public MechanismTarget H(int s) { var t = Get(s); return new MechanismTarget(t.Serial, t.Gen); }
        public void Direct(MechanismRules r, double time, int s, int dmg)
        {
            var t = Get(s); t.Hp -= dmg; DamageTotal += dmg; Log.Add($"{time:0.###} Direct #{s} {dmg}");
            r.ReportDirectHit(new DirectHitRecord(time, new MechanismTarget(t.Serial, t.Gen), new MechanismVec2(t.X, t.Z), dmg));
        }
        public bool TryGetLiveTarget(MechanismTarget h, out MechanismVec2 p)
        {
            var t = Targets.FirstOrDefault(x => x.Serial == h.Serial && x.Gen == h.Generation && x.Alive);
            p = t == null ? default : new MechanismVec2(t.X, t.Z); return t != null;
        }
        public void CollectLiveTargets(List<MechanismLiveTarget> b)
        { foreach (var t in Targets) if (t.Alive) b.Add(new MechanismLiveTarget(new MechanismTarget(t.Serial, t.Gen), new MechanismVec2(t.X, t.Z))); }
        public DerivedDamageResult ApplyDerivedDamage(in DerivedDamageRequest r)
        {
            var h = r.Target;
            var t = Targets.FirstOrDefault(x => x.Serial == h.Serial && x.Gen == h.Generation && x.Alive);
            if (t == null) return DerivedDamageResult.Invalid;
            t.Hp -= r.Damage; DamageTotal += r.Damage;
            Log.Add($"{r.PlannedTime:0.###} {r.Kind} #{t.Serial} {r.Damage}");
            return t.Alive ? DerivedDamageResult.Applied : DerivedDamageResult.Killed;
        }
    }

    public class MechanismRulesTests
    {
        static FakeHost Scene()
        {
            var h = new FakeHost();
            h.Add(1, 0, 0); h.Add(2, 3, 0); h.Add(3, 0, 4); h.Add(4, 20, 0);
            return h;
        }

        static (FakeHost, MechanismRules) Run(MechanismMode m, double[] steps)
        {
            var h = Scene(); var r = new MechanismRules(m, new MechanismRulesConfig(), h);
            h.Direct(r, 0, 1, 10);
            double t = 0; foreach (var s in steps) { t += s; r.Tick(t); }
            return (h, r);
        }

        static double[] Uniform(double dt, int n) => Enumerable.Repeat(dt, n).ToArray();

        [TestCase(MechanismMode.None, 10, 0, 0)]
        [TestCase(MechanismMode.Echo, 15, 1, 0)]
        [TestCase(MechanismMode.Propagation, 14, 0, 1)]
        [TestCase(MechanismMode.Combined, 23, 1, 2)]
        public void FourModes_GiveReferenceTotals(MechanismMode m, int total, int echo, int prop)
        {
            var (h, r) = Run(m, Uniform(0.01, 200));
            Assert.AreEqual(total, h.DamageTotal);
            Assert.AreEqual(echo, r.EchoHits);
            Assert.AreEqual(prop, r.PropagationHits);
            Assert.AreEqual(0, r.PendingCount);
            Assert.AreEqual(0, r.ActiveChainCount);
            Assert.AreEqual(30, h.Get(4).Hp, "远处目标不应被击中");
        }

        [Test]
        public void Combined_OrderAndTargets()
        {
            var (h, _) = Run(MechanismMode.Combined, Uniform(0.01, 200));
            CollectionAssert.AreEqual(new[] { "0 Direct #1 10", "0.1 Propagation #2 4", "0.5 Echo #1 5", "0.6 EchoPropagation #3 4" }, h.Log);
        }

        [Test]
        public void None_ProducesNothing()
        {
            var h = Scene(); var r = new MechanismRules(MechanismMode.None, new MechanismRulesConfig(), h);
            h.Direct(r, 0, 1, 10); r.Tick(5);
            Assert.AreEqual(0, r.PendingCount); Assert.AreEqual(0, r.Events.Count); Assert.AreEqual(1, h.Log.Count);
        }

        [TestCase(MechanismMode.Echo)]
        [TestCase(MechanismMode.Propagation)]
        [TestCase(MechanismMode.Combined)]
        public void CoarseAndFineDt_Identical(MechanismMode m)
        {
            var fine = Run(m, Uniform(0.01, 200)).Item1.Log;
            Assert.AreEqual(fine, Run(m, new[] { 2.0 }).Item1.Log);
            Assert.AreEqual(fine, Run(m, Uniform(0.3, 7)).Item1.Log);
        }

        [Test]
        public void SingleCoarseTick_EventsBufferHoldsAllInOrderThenClears()
        {
            var (_, r) = Run(MechanismMode.Combined, new[] { 2.0 });
            CollectionAssert.AreEqual(new[] { DerivedKind.Propagation, DerivedKind.Echo, DerivedKind.EchoPropagation }, r.Events.Select(e => e.Request.Kind));
            r.Tick(3); Assert.AreEqual(0, r.Events.Count);
        }

        [Test]
        public void EqualDistance_TieBySerial_AndRadiusIsXZInclusive()
        {
            var h = new FakeHost();
            h.Add(1, 0, 0); h.Add(7, 0, 3); h.Add(5, 3, 0); h.Add(6, -3, 0); h.Add(9, 3, 4); // #9 距 5 米，在半径边界
            var r = new MechanismRules(MechanismMode.Propagation, new MechanismRulesConfig(), h);
            h.Direct(r, 0, 1, 10); r.Tick(1);
            Assert.AreEqual(26, h.Get(5).Hp);

            var h2 = new FakeHost(); h2.Add(1, 0, 0); h2.Add(2, 3.01f, 4); h2.Add(3, 0, 5);
            var r2 = new MechanismRules(MechanismMode.Propagation, new MechanismRulesConfig(), h2);
            h2.Direct(r2, 0, 1, 10); r2.Tick(1);
            Assert.AreEqual(26, h2.Get(3).Hp); Assert.AreEqual(30, h2.Get(2).Hp);

            var h3 = new FakeHost(); h3.Add(1, 0, 0); h3.Add(2, 4, 4);
            var r3 = new MechanismRules(MechanismMode.Propagation, new MechanismRulesConfig(), h3);
            h3.Direct(r3, 0, 1, 10); r3.Tick(1);
            Assert.AreEqual(0, r3.PropagationHits);
        }

        [Test]
        public void PoolGenerationChange_EchoSkippedAndNoChildren()
        {
            var h = Scene(); var r = new MechanismRules(MechanismMode.Combined, new MechanismRulesConfig(), h);
            h.Direct(r, 0, 1, 10);
            r.Tick(0.2);
            var t1 = h.Get(1); t1.Gen = 2; t1.Hp = 30; // 被回收后同槽位重用
            r.Tick(2);
            Assert.AreEqual(0, r.EchoHits);
            Assert.AreEqual(1, r.PropagationHits, "只有直接传播，回声及回声传播均不应发生");
            Assert.AreEqual(30, t1.Hp); Assert.AreEqual(30, h.Get(3).Hp);
            Assert.AreEqual(0, r.PendingCount); Assert.AreEqual(0, r.ActiveChainCount);
        }

        [Test]
        public void SameFrameRepeatedDeath_SecondPropagationPicksNextLiveTarget()
        {
            var h = new FakeHost();
            h.Add(1, 0, 0, 100); h.Add(2, 1, 0, 4); h.Add(3, 2, 0, 30);
            var r = new MechanismRules(MechanismMode.Propagation, new MechanismRulesConfig(), h);
            h.Direct(r, 0, 1, 10); h.Direct(r, 0, 1, 10);
            r.Tick(1);
            Assert.AreEqual(DerivedDamageResult.Killed, r.Events[0].Result);
            Assert.AreEqual(2, r.Events[0].Request.Target.Serial);
            Assert.AreEqual(3, r.Events[1].Request.Target.Serial);
            Assert.AreEqual(0, h.Get(2).Hp, "已死目标不能被再次扣血");
            Assert.AreEqual(2, r.Events.Count);
        }

        [Test]
        public void DeadEchoTarget_StopsEchoPropagation()
        {
            var h = Scene(); var r = new MechanismRules(MechanismMode.Combined, new MechanismRulesConfig(), h);
            h.Direct(r, 0, 1, 10); r.Tick(0.3); h.Get(1).Hp = 0; r.Tick(2);
            Assert.AreEqual(0, r.EchoHits); Assert.AreEqual(30, h.Get(3).Hp); Assert.AreEqual(0, r.PendingCount);
        }

        [Test]
        public void ConfigCopiedAtStart()
        {
            var cfg = new MechanismRulesConfig(); var h = Scene();
            var r = new MechanismRules(MechanismMode.Echo, cfg, h);
            cfg.EchoMultiplier = 3f; cfg.EchoDelay = 0.01;
            h.Direct(r, 0, 1, 10); r.Tick(0.4); Assert.AreEqual(0, r.EchoHits); r.Tick(0.5);
            Assert.AreEqual(15, h.DamageTotal);
        }

        [Test]
        public void BetweenRuns_NewInstanceAndClearAreClean_BudgetRecycled()
        {
            var h = Scene(); var r = new MechanismRules(MechanismMode.Combined, new MechanismRulesConfig(), h);
            for (int i = 0; i < 50; i++) { h.Get(1).Hp = 1000; h.Direct(r, i, 1, 10); r.Tick(i + 0.99); }
            Assert.AreEqual(0, r.PendingCount); Assert.AreEqual(0, r.ActiveChainCount);
            h.Direct(r, 100, 1, 10); r.Tick(100.2);
            Assert.Greater(r.PendingCount, 0);
            r.Clear();
            Assert.AreEqual(0, r.PendingCount); Assert.AreEqual(0, r.ActiveChainCount);
            Assert.AreEqual(0, r.EchoHits); Assert.AreEqual(0, r.PropagationHits); Assert.AreEqual(0, r.Events.Count);
            r.Tick(200); Assert.AreEqual(0, r.Events.Count);
            var fresh = new MechanismRules(MechanismMode.Combined, new MechanismRulesConfig(), Scene());
            Assert.AreEqual(0, fresh.PendingCount); Assert.AreEqual(0, fresh.EchoHits);
        }
    }
}
