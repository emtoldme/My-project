using System;
using System.Collections.Generic;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.Homecoming;

namespace Waffle.Tests
{
    public sealed class HomecomingContractTests
    {
        [Test] public void SiteIdentitySurvivesNegativeCellsAndSeedChanges()
        {
            string id = HomecomingContract.SiteId(14104, -8, 12, "poi.old-silo");
            Assert.That(id, Is.EqualTo("hc1:14104:-8:12:poi.old-silo"));
            Assert.That(HomecomingContract.SiteId(14104, -8, 12, "poi.old-silo"), Is.EqualTo(id));
            Assert.That(HomecomingContract.SiteId(14105, -8, 12, "poi.old-silo"), Is.Not.EqualTo(id));
            Assert.That(HomecomingContract.SiteId(14104, 8, 12, "poi.old-silo"), Is.Not.EqualTo(id));
        }
        [TestCase("")][TestCase("poi:ambiguous")][TestCase("poi/relative")]
        public void ContentIdsCannotCollideWithTheSiteKeySeparator(string id)
            => Assert.Throws<ArgumentException>(() => HomecomingContract.SiteId(1, 0, 0, id));

        [Test] public void AllInspectableEntriesRequireNarrativeText()
        {
            Assert.Throws<ArgumentException>(() => new NarrativeEntry("item.guitar", "旧吉他", " ", NarrativeKind.Item));
            var entry = new NarrativeEntry("item.guitar", "旧吉他", "第三根弦还系着你打的结。", NarrativeKind.Item);
            Assert.That(entry.Body, Does.Contain("第三根弦"));
        }
        [Test] public void SettlementCopiesLiveLedgersBeforeTheyAreCleared()
        {
            var goals = new List<string> { "goal.homeward" };
            var settlement = new ExplorationSettlement(Guid.NewGuid(), RunPhase.Defeat, 123, false, null, null, goals);
            goals.Clear();
            Assert.That(settlement.CompletedGoalIds, Is.EquivalentTo(new[] { "goal.homeward" }));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)settlement.CompletedGoalIds).Add("injected"));
        }
        [Test] public void InventorySnapshotDoesNotChangeWhenAnEquipmentListChanges()
        {
            var slots = new List<RelicSlotView> { new RelicSlotView(0, "item.guitar", "item.guitar") };
            var view = new RelicInventoryView(2, slots, null);
            slots.Clear();
            Assert.That(view.Slots.Count, Is.EqualTo(1));
            Assert.That(view.StoredItems, Is.Empty);
        }
        [TestCase(RunPhase.Paused, true, false)]
        [TestCase(RunPhase.LevelUp, true, false)]
        [TestCase(RunPhase.Victory, true, false)]
        [TestCase(RunPhase.Playing, false, false)]
        [TestCase(RunPhase.Playing, true, true)]
        public void InteractionsRequireLivePlayingState(RunPhase phase, bool alive, bool allowed)
        {
            var context = new ExplorationContext(Guid.NewGuid(), 1, 0, .05f, new Vec2(0, 0), phase, alive);
            Assert.That(context.CanInteract, Is.EqualTo(allowed));
        }
    }
}
