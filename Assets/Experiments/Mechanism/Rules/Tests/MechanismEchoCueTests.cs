// TEST_ONLY：M01 只读回声预备查询检查。dotnet 可跑，Unity 下为 EditMode 测试（未在 Unity 验证）。
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reverb.Experiments.Mechanism;
using Reverb.Experiments.Mechanism.Rules;

namespace Reverb.Experiments.Mechanism.Rules.Tests
{
    public class MechanismEchoCueTests
    {
        static FakeHost Scene()
        {
            var h = new FakeHost();
            h.Add(1, 0, 0); h.Add(2, 3, 0); h.Add(3, 0, 4); h.Add(4, 20, 0);
            return h;
        }

        [TestCase(MechanismMode.None)]
        [TestCase(MechanismMode.Propagation)]
        public void NoneAndPropagationOnly_Empty(MechanismMode m)
        {
            var h = Scene(); var r = new MechanismRules(m, new MechanismRulesConfig(), h);
            var list = new List<PendingEchoInfo> { new PendingEchoInfo(new MechanismTarget(9, 9), 1, 2) };
            h.Direct(r, 0, 1, 10);
            r.CopyPendingEchoes(list);
            Assert.AreEqual(0, list.Count, "传播不算回声预备；旧内容须清空");
        }

        [Test]
        public void PendingEcho_HasTargetAndTimes_MultiplePerTargetInOrder()
        {
            var h = Scene(); var r = new MechanismRules(MechanismMode.Combined, new MechanismRulesConfig(), h);
            var list = new List<PendingEchoInfo>();
            h.Direct(r, 1.0, 1, 10); h.Direct(r, 1.2, 1, 10);
            r.Tick(1.25); // 两条传播已结算，回声仍排队
            r.CopyPendingEchoes(list);
            Assert.AreEqual(2, list.Count);
            Assert.IsTrue(list[0].Target.Same(h.H(1)));
            Assert.AreEqual(1.0, list[0].ScheduledAt); Assert.AreEqual(1.5, list[0].DueAt, 1e-12);
            Assert.AreEqual(1.2, list[1].ScheduledAt); Assert.AreEqual(1.7, list[1].DueAt, 1e-12);
            r.Tick(1.5); r.CopyPendingEchoes(list);
            Assert.AreEqual(1, list.Count); Assert.AreEqual(1.2, list[0].ScheduledAt);
            r.Tick(3); r.CopyPendingEchoes(list);
            Assert.AreEqual(0, list.Count);
            Assert.Throws<ArgumentNullException>(() => r.CopyPendingEchoes(null));
        }

        static List<string> Sequence(bool query)
        {
            var h = Scene(); var r = new MechanismRules(MechanismMode.Combined, new MechanismRulesConfig(), h);
            var list = new List<PendingEchoInfo>(); var trace = new List<string>();
            h.Direct(r, 0, 1, 10); h.Direct(r, 0.05, 2, 10);
            for (int i = 1; i <= 200; i++)
            {
                if (query) { r.CopyPendingEchoes(list); r.CopyPendingEchoes(list); }
                r.Tick(i * 0.01);
                if (query) r.CopyPendingEchoes(list);
                trace.Add($"{r.PendingCount}/{r.ActiveChainCount}/{r.EchoHits}/{r.PropagationHits}/{h.DamageTotal}|" + string.Join(";", r.Events));
            }
            trace.AddRange(h.Log); trace.AddRange(h.Targets.Select(t => $"#{t.Serial}={t.Hp}"));
            return trace;
        }

        [Test]
        public void Query_DoesNotChangeDamageQueueOrCounters()
        {
            var a = Sequence(false); var b = Sequence(true);
            CollectionAssert.AreEqual(a, b);
            var (h, r) = (Scene(), (MechanismRules)null);
            r = new MechanismRules(MechanismMode.Combined, new MechanismRulesConfig(), h);
            h.Direct(r, 0, 1, 10); r.Tick(2);
            Assert.AreEqual(23, h.DamageTotal, "10/15/14/23 对照保持");
        }

        [Test]
        public void InvalidGenerationOrDeadTarget_Excluded()
        {
            var h = Scene(); var r = new MechanismRules(MechanismMode.Echo, new MechanismRulesConfig(), h);
            var list = new List<PendingEchoInfo>();
            h.Direct(r, 0, 1, 10); h.Direct(r, 0, 2, 10);
            r.CopyPendingEchoes(list); Assert.AreEqual(2, list.Count);
            h.Get(1).Gen = 2;        // 回池后重用
            h.Get(2).Hp = 0;         // 已死亡
            r.CopyPendingEchoes(list); Assert.AreEqual(0, list.Count);
            Assert.AreEqual(2, r.PendingCount, "查询不移除队列");
        }

        [Test]
        public void ClearAndNewRun_NoStaleCues()
        {
            var h = Scene(); var r = new MechanismRules(MechanismMode.Combined, new MechanismRulesConfig(), h);
            var list = new List<PendingEchoInfo>();
            h.Direct(r, 0, 1, 10); r.CopyPendingEchoes(list); Assert.AreEqual(1, list.Count);
            r.Clear(); r.CopyPendingEchoes(list); Assert.AreEqual(0, list.Count);
            var fresh = new MechanismRules(MechanismMode.Combined, new MechanismRulesConfig(), Scene());
            fresh.CopyPendingEchoes(list); Assert.AreEqual(0, list.Count);
        }
    }
}
