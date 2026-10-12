using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Waffle.RunContent.Tests
{
    public class RunGrowthStateTests
    {
        private static RunGrowthState NewWithOffer(int seed = 7)
        {
            var s = new RunGrowthState(seed);
            Assert.IsTrue(s.EnqueueLevelUpReward());
            return s;
        }

        private static void ChooseFirst(RunGrowthState s, System.Func<string, bool> pred = null)
        {
            var v = s.ReadView();
            var id = v.Choices.Select(c => c.Id).FirstOrDefault(i => pred == null || pred(i)) ?? v.Choices[0].Id;
            Assert.IsTrue(s.TryChoose(v.OfferToken, id));
        }

        [Test]
        public void Catalog_CountsAndPlannedNeverDrawn()
        {
            var c = RunContentCatalog.Default;
            Assert.AreEqual(6, c.Count(ContentKind.Weapon)); Assert.AreEqual(18, c.Count(ContentKind.Passive));
            Assert.AreEqual(6, c.Count(ContentKind.Evolution)); Assert.AreEqual(6, c.Count(ContentKind.Pickup));
            foreach (var d in c.All)
            {
                Assert.AreEqual(d.Kind == ContentKind.Weapon ? 5 : d.Kind == ContentKind.Passive ? 3 : 1, d.MaxRank, d.Id);
                if (d.Status == ImplementationStatus.Planned) Assert.IsFalse(new RunGrowthState(1).IsDrawable(d.Id), d.Id);
            }
            var s = new RunGrowthState(3);
            Assert.AreEqual(0, s.RankOf("P12")); Assert.AreEqual(0, s.RankOf("P13")); // 回声/传播不开局赠送
            Assert.AreEqual(1, s.RankOf("W01"));
        }

        [Test]
        public void Draw_ThreeDistinctLegal_SeedReproducible_ViewDoesNotAdvanceRng()
        {
            var a = NewWithOffer(42); var b = NewWithOffer(42);
            a.ReadView(); a.ReadView();
            for (int i = 0; i < 25; i++)
            {
                var va = a.ReadView(); var vb = b.ReadView();
                CollectionAssert.AreEqual(va.Choices.Select(x => x.Id).ToArray(), vb.Choices.Select(x => x.Id).ToArray());
                Assert.AreEqual(va.Choices.Count, va.Choices.Select(x => x.Id).Distinct().Count());
                foreach (var ch in va.Choices) if (ch.Id != RunContentCatalog.HealFallbackId) Assert.IsTrue(a.IsDrawable(ch.Id));
                ChooseFirst(a); ChooseFirst(b);
                a.EnqueueLevelUpReward(); b.EnqueueLevelUpReward();
            }
        }

        [Test]
        public void SlotsAndTiersCapped_FallbackHealWhenExhausted()
        {
            var s = new RunGrowthState(5);
            for (int i = 0; i < 120; i++)
            {
                s.EnqueueLevelUpReward();
                var v = s.ReadView();
                Assert.LessOrEqual(v.Weapons.Count, 4); Assert.LessOrEqual(v.Passives.Count, 6);
                foreach (var o in v.Weapons.Concat(v.Passives)) Assert.LessOrEqual(o.Rank, o.MaxRank);
                foreach (var ch in v.Choices)
                {
                    if (ch.Id == RunContentCatalog.HealFallbackId) continue;
                    Assert.LessOrEqual(ch.NextRank, RunContentCatalog.Default.Get(ch.Id).MaxRank, ch.Id);
                }
                Assert.Greater(v.Choices.Count, 0);
                ChooseFirst(s);
            }
            var last = s.ReadView();
            Assert.AreEqual(0, last.Choices.Count); // 无待奖励
            s.EnqueueLevelUpReward();
            Assert.AreEqual(RunContentCatalog.HealFallbackId, s.ReadView().Choices.Single().Id);
        }

        [Test]
        public void EarlyOffers_ContainNewWeaponAndOwnedUpgrade()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var s = NewWithOffer(seed);
                var ids = s.ReadView().Choices.Select(c => c.Id).ToList();
                Assert.IsTrue(ids.Any(i => i != "W01" && i.StartsWith("W")), "seed " + seed);
                Assert.IsTrue(ids.Contains("W01"), "保底已有升级 seed " + seed);
            }
        }

        [Test]
        public void Token_RejectsStaleAndDuplicate_ResourcesSpentOnlyOnSuccess()
        {
            var s = NewWithOffer();
            long t0 = s.ReadView().OfferToken;
            Assert.IsTrue(s.TryReroll(t0));
            Assert.AreEqual(2, s.Rerolls);
            Assert.IsFalse(s.TryReroll(t0)); Assert.AreEqual(2, s.Rerolls);
            var v = s.ReadView(); string id = v.Choices[0].Id;
            Assert.IsFalse(s.TryBanish(v.OfferToken, "NOPE")); Assert.AreEqual(2, s.Banishes);
            Assert.IsTrue(s.TryBanish(v.OfferToken, id)); Assert.AreEqual(1, s.Banishes);
            Assert.IsTrue(s.IsBanished(id));
            v = s.ReadView();
            Assert.IsFalse(v.Choices.Any(c => c.Id == id)); Assert.AreEqual(3, v.Choices.Count);
            Assert.IsFalse(s.TryChoose(t0, v.Choices[0].Id));
            Assert.IsTrue(s.TryChoose(v.OfferToken, v.Choices[1].Id));
            Assert.IsFalse(s.TryChoose(v.OfferToken, v.Choices[1].Id)); // 重复点击
            Assert.IsFalse(s.TrySkip(v.OfferToken));
            Assert.AreEqual(2, s.Skips);
            var res = new List<GrowthResult>(); s.DrainResults(res);
            Assert.AreEqual(1, res.Count);
        }

        [Test]
        public void ResourceLimits_3Rerolls_2Banishes_2Skips()
        {
            var s = new RunGrowthState(9);
            for (int i = 0; i < 4; i++) s.EnqueueLevelUpReward();
            int ok = 0; for (int i = 0; i < 5; i++) if (s.TryReroll(s.OfferToken)) ok++;
            Assert.AreEqual(3, ok);
            ok = 0; for (int i = 0; i < 4; i++) if (s.TryBanish(s.OfferToken, s.CurrentChoiceIds[0])) ok++;
            Assert.AreEqual(2, ok);
            ok = 0; for (int i = 0; i < 4; i++) if (s.TrySkip(s.OfferToken)) ok++;
            Assert.AreEqual(2, ok);
            Assert.AreEqual(2, s.PendingRewards); // 跳过两次只消费两个奖励
            var res = new List<GrowthResult>(); s.DrainResults(res);
            Assert.IsTrue(res.All(r => r.Kind == GrowthResultKind.Skipped && r.HealFraction > 0));
        }

        [Test]
        public void PeriodicNewWeaponOpportunityRemainsAvailableAfterTheFirstOffer()
        {
            var s=new RunGrowthState(4,null,null,new[]{"W01","W02","W03","W04","P01","P02","P04"});
            for(int i=1;i<=9;i++)
            {
                s.EnqueueLevelUpReward();
                if(i%3==0) Assert.That(s.ReadView().Choices.Any(c=>c.Id.StartsWith("W") && c.CurrentRank==0),Is.True);
                var pick=s.ReadView().Choices.FirstOrDefault(c=>!c.Id.StartsWith("W") || c.CurrentRank>0);
                Assert.That(s.TryChoose(s.OfferToken,pick.Id),Is.True);
            }
        }

        private static RunGrowthState BuildToEvolution(int seed)
        {
            // 使用允许列表把池限制到 W01/P12/E01，以确定性堆满条件
            var s = new RunGrowthState(seed, null, null, new[] { "W01", "P12", "E01" });
            for (int i = 0; i < 6; i++) { s.EnqueueLevelUpReward(); ChooseFirst(s); }
            return s;
        }

        [Test]
        public void Evolution_OnlyViaElite_WhenEligible_ConsumedOnce_TokenOnce()
        {
            var s = BuildToEvolution(1);
            Assert.AreEqual(5, s.RankOf("W01")); Assert.GreaterOrEqual(s.RankOf("P12"), 2);
            Assert.IsFalse(s.ReadView().Evolutions.Single(e => e.Id == "E01").IsEligible, "UI must include the real time gate");
            s.SetViewRunSeconds(240f);
            Assert.IsTrue(s.ReadView().Evolutions.Single(e => e.Id == "E01").IsEligible);

            // 未满4分钟：精英奖励给普通成长，不吞箱
            Assert.IsTrue(s.TryEnqueueEliteReward(100, 200f));
            Assert.IsFalse(s.IsEvolutionOffer);
            Assert.IsTrue(s.TrySkip(s.OfferToken));

            Assert.IsFalse(s.TryEnqueueEliteReward(100, 300f)); // 同箱token拒重复
            Assert.IsTrue(s.TryEnqueueEliteReward(101, 300f));
            Assert.IsTrue(s.IsEvolutionOffer);
            Assert.IsFalse(s.TryReroll(s.OfferToken));
            Assert.IsTrue(s.TryChoose(s.OfferToken, "E01"));
            Assert.AreEqual(0, s.RankOf("W01")); Assert.AreEqual(1, s.RankOf("E01"));
            Assert.AreEqual("E01", s.ReadView().Weapons[0].Id); // 保留原槽位

            Assert.IsTrue(s.TryEnqueueEliteReward(102, 400f));
            Assert.IsFalse(s.IsEvolutionOffer); // 不再进化
            Assert.IsFalse(s.ReadView().Choices.Any(c => c.Id == "E01" || c.Id == "W01"));
        }

        [Test]
        public void RunEnd_StopsOffers_NewRunIsClean()
        {
            var s = BuildToEvolution(2);
            s.TryEnqueueEliteReward(1, 300f); s.TryChoose(s.OfferToken, "E01");
            s.EnqueueLevelUpReward(); s.EnqueueLevelUpReward();
            s.CloseForRunEnd();
            Assert.AreEqual(0, s.ReadView().Choices.Count); Assert.AreEqual(0, s.PendingRewards);
            Assert.IsFalse(s.EnqueueLevelUpReward());

            var fresh = new RunGrowthState(2);
            var v = fresh.ReadView();
            Assert.AreEqual(1, v.Weapons.Count); Assert.AreEqual("W01", v.Weapons[0].Id);
            Assert.AreEqual(0, v.Passives.Count); Assert.AreEqual(3, v.Rerolls); Assert.AreEqual(2, v.Banishes); Assert.AreEqual(2, v.Skips);
            Assert.IsTrue(fresh.TryEnqueueEliteReward(1, 300f)); // 新局token集合干净
        }

        [Test]
        public void Stats_StackAdditiveWithCaps()
        {
            var s = new RunGrowthState(4, null, null, new[] { "W01", "P01", "P02" });
            for (int i = 0; i < 6; i++) { s.EnqueueLevelUpReward(); ChooseFirst(s, id => id.StartsWith("P")); }
            Assert.AreEqual(3, s.RankOf("P01")); Assert.AreEqual(3, s.RankOf("P02"));
            var st = GrowthStats.From(s);
            Assert.AreEqual(1.36f, st.DamageMul, 1e-4); Assert.AreEqual(1.30f, st.RateMul, 1e-4);
            Assert.AreEqual(0.15f, st.FinalCooldown(0.1f), 1e-6);
            Assert.AreEqual(1f, st.FinalIncoming(0.5f), 1e-6);
            var w = WeaponRuntimeParams.From(s).Single();
            Assert.AreEqual(10f * 1.36f, w.Damage, 1e-4); Assert.AreEqual(0.60f / 1.3f, w.Cooldown, 1e-4);
        }
    }
}
