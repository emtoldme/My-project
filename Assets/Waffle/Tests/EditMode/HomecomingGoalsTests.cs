using System;
using System.Collections.Generic;
using NUnit.Framework;
using Waffle.Homecoming;

namespace Waffle.Tests
{
    [TestFixture]
    public sealed class HomecomingGoalsTests
    {
        private HomecomingGoalsModule _module;
        private Guid _runId;

        [SetUp]
        public void SetUp()
        {
            _module = new HomecomingGoalsModule();
            _runId = Guid.NewGuid();
            _module.Reset(_runId);
        }

        [Test]
        public void InitialState_HasThreeUncompletedGoals()
        {
            var goals = _module.Read();
            Assert.That(goals.Count, Is.EqualTo(3));

            Assert.That(goals[0].Id, Is.EqualTo(HomecomingGoalsModule.GoalHomeward));
            Assert.That(goals[0].Completed, Is.False);

            Assert.That(goals[1].Id, Is.EqualTo(HomecomingGoalsModule.GoalHarvestMemory));
            Assert.That(goals[1].Completed, Is.False);

            Assert.That(goals[2].Id, Is.EqualTo(HomecomingGoalsModule.GoalUnfinishedSong));
            Assert.That(goals[2].Completed, Is.False);
        }

        [Test]
        public void RejectsStaleOrMismatchedRunId()
        {
            var oldRunId = Guid.NewGuid();
            var fact = new HomecomingFact(oldRunId, "evt-1", HomecomingEventKind.SiteClaimed, "item.sewing-tin");
            _module.Observe(fact);

            var goals = _module.Read();
            Assert.That(goals[0].Completed, Is.False);
            Assert.That(goals[0].ProgressText, Does.Contain("尚未发现老屋"));
        }

        [Test]
        public void DeduplicatesByEventId()
        {
            var fact1 = new HomecomingFact(_runId, "evt-same", HomecomingEventKind.SiteDiscovered, "poi.old-home");
            _module.Observe(fact1);

            // Same eventId with different content should be discarded
            var fact2 = new HomecomingFact(_runId, "evt-same", HomecomingEventKind.SiteClaimed, "item.sewing-tin");
            _module.Observe(fact2);

            var goals = _module.Read();
            Assert.That(goals[0].Completed, Is.False);
            Assert.That(goals[0].ProgressText, Does.Contain("已发现老房子"));
        }

        [Test]
        public void Goal1_Homeward_CompletesWhenDiscoveredAndClaimed()
        {
            // Step 1: Discover home
            _module.Observe(new HomecomingFact(_runId, "evt-10", HomecomingEventKind.SiteDiscovered, "poi.old-home"));
            var goals = _module.Read();
            Assert.That(goals[0].Completed, Is.False);
            Assert.That(goals[0].ProgressText, Does.Contain("寻觅窗台针线盒"));

            // Step 2: Claim sewing tin
            _module.Observe(new HomecomingFact(_runId, "evt-11", HomecomingEventKind.SiteClaimed, "item.sewing-tin"));
            goals = _module.Read();
            Assert.That(goals[0].Completed, Is.True);
            Assert.That(goals[0].ProgressText, Does.Contain("已达成"));
        }

        [Test]
        public void Goal2_HarvestMemory_CompletesWhenSiloViewed_GlovesClaimed_AndEquipped()
        {
            // 1. View silo
            _module.Observe(new HomecomingFact(_runId, "evt-20", HomecomingEventKind.SiteDiscovered, "poi.old-silo"));
            Assert.That(_module.Read()[1].Completed, Is.False);

            // 2. Claim gloves
            _module.Observe(new HomecomingFact(_runId, "evt-21", HomecomingEventKind.SiteClaimed, "item.work-gloves"));
            Assert.That(_module.Read()[1].Completed, Is.False);
            Assert.That(_module.Read()[1].ProgressText, Does.Contain("待装备入槽"));

            // 3. Equip gloves
            _module.Observe(new HomecomingFact(_runId, "evt-22", HomecomingEventKind.ItemEquipped, "item.work-gloves"));
            var goals = _module.Read();
            Assert.That(goals[1].Completed, Is.True);
            Assert.That(goals[1].ProgressText, Does.Contain("已达成"));
        }

        [Test]
        public void Goal3_UnfinishedSong_CompletesWhenClaimed_Equipped_AndEchoTriggered()
        {
            // 1. Claim guitar
            _module.Observe(new HomecomingFact(_runId, "evt-30", HomecomingEventKind.SiteClaimed, "item.old-guitar"));
            Assert.That(_module.Read()[2].Completed, Is.False);

            // 2. Equip guitar
            _module.Observe(new HomecomingFact(_runId, "evt-31", HomecomingEventKind.ItemEquipped, "item.old-guitar"));
            Assert.That(_module.Read()[2].Completed, Is.False);
            Assert.That(_module.Read()[2].ProgressText, Does.Contain("闪避时触发一次残响"));

            // 3. Trigger echo
            _module.Observe(new HomecomingFact(_runId, "evt-32", HomecomingEventKind.SpecialEffectTriggered, "item.old-guitar.echo"));
            var goals = _module.Read();
            Assert.That(goals[2].Completed, Is.True);
            Assert.That(goals[2].ProgressText, Does.Contain("已达成"));
        }

        [Test]
        public void Reset_ClearsAllProgressAndGeneratesFreshGoalViews()
        {
            // Complete goal 1
            _module.Observe(new HomecomingFact(_runId, "e1", HomecomingEventKind.SiteClaimed, "poi.old-home"));
            Assert.That(_module.Read()[0].Completed, Is.True);

            // Reset with new run
            var nextRun = Guid.NewGuid();
            _module.Reset(nextRun);

            var goals = _module.Read();
            Assert.That(goals[0].Completed, Is.False);
            Assert.That(goals[1].Completed, Is.False);
            Assert.That(goals[2].Completed, Is.False);
        }
    }
}
