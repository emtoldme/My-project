using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.Homecoming;
using Waffle.Homecoming.Places;
using Waffle.Homecoming.Profile;
using Waffle.Homecoming.Relics;

namespace Waffle.Tests
{
    public sealed class HomecomingIntegrationTests
    {
        static RunSimulation Create()
        {
            var sim = new RunSimulation(new RunConfig { useRunContent = true, unboundedWorld = true }, null, new CombatConfig { spawningEnabled = false });
            sim.InstallHomecoming(14104); sim.God = true; return sim;
        }
        static void Visit(RunSimulation sim, string template)
        {
            var site = HomecomingPlacesLayout.UniqueSites(14104).Single(s => s.TemplateId == template);
            sim.PlayerX = site.Position.X; sim.PlayerZ = site.Position.Y; sim.StepHomecoming();
        }
        [Test] public void RealPlaceIdsGrantSlotsOnceAndTextIsRequiredForGoals()
        {
            var sim = Create(); int hp = sim.State.Hp;
            Visit(sim, "poi.old-home");
            Assert.That(sim.Relics.Read().Capacity, Is.EqualTo(3));
            Assert.That(sim.State.Hp, Is.EqualTo(hp)); Assert.That(sim.State.MaxHp, Is.EqualTo(108));
            Assert.That(sim.HomecomingGoals.Read().Single(g => g.Id == "goal.homeward").Completed, Is.False);
            sim.ViewHomecomingNarrative("poi.old-home");
            Assert.That(sim.HomecomingGoals.Read().Single(g => g.Id == "goal.homeward").Completed, Is.True);
            Visit(sim, "poi.old-silo"); Visit(sim, "poi.old-home");
            Assert.That(sim.Relics.Read().Capacity, Is.EqualTo(4));
            Assert.That(sim.CurrentValue(UpgradeKind.PickupRange), Is.EqualTo(sim.Config.magnetRadius * 1.15f).Within(.001f));
            sim.UnequipHomecoming(sim.RunId, 0, false); sim.EquipHomecoming(sim.RunId, RelicInventory.SewingTin, 0, false);
            Assert.That(sim.State.Hp, Is.EqualTo(hp));
        }
        [Test] public void GuitarEchoUsesRealCombatDamageAndEightSecondCooldown()
        {
            var sim = Create(); Visit(sim, "poi.old-guitar");
            var enemy = sim.Combat.DebugSpawnEnemy(new Vec2(sim.PlayerX + 1, sim.PlayerZ), 100, 0);
            var input = InputSnapshot.None; input.DodgePressed = true;
            sim.Tick(.02f, input, 1, 1);
            Assert.That(sim.HomecomingEchoes, Is.EqualTo(1)); Assert.That(sim.Combat.HomecomingEchoHits, Is.GreaterThan(0));
            Assert.That(enemy.Hp, Is.LessThan(100));
            Assert.That(sim.HomecomingGoals.Read().Single(g => g.Id == "goal.unfinished-song").Completed, Is.True);
            sim.Tick(.02f, input, 2, 2); Assert.That(sim.HomecomingEchoes, Is.EqualTo(1));
        }
        [Test] public void PausedAndDeadCharactersCannotClaimAndOldRunsCannotEquip()
        {
            var sim = Create(); sim.RequestPause("test"); Visit(sim, "poi.old-home");
            Assert.That(sim.Relics.Read().Capacity, Is.EqualTo(2));
            sim.TogglePause("test"); sim.State.Hp = 0; sim.StepHomecoming();
            Assert.That(sim.Relics.Read().Capacity, Is.EqualTo(2));
            Assert.That(sim.EquipHomecoming(Guid.NewGuid(), RelicInventory.SewingTin, 0, true), Is.False);
        }
        [TestCase("{")][TestCase("{}")][TestCase("{\"schemaVersion\":1")]
        public void IncompleteSavesAreRejected(string json) => Assert.Throws<FormatException>(() => ProfileData.FromJson(json));
        [Test] public void SaveReplacementFailureRetainsOriginalBytes()
        {
            string directory = Path.Combine(Path.GetTempPath(), "HCIntegration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var storage = new ProfileFileStorage(directory);
                Assert.That(storage.SaveAtomic(new ProfileData().ToJson()).Success, Is.True);
                string path = Path.Combine(directory, "homecoming_profile.json"); string before = File.ReadAllText(path);
                using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    Assert.That(storage.SaveAtomic(new ProfileData { Revision = 2 }.ToJson()).Success, Is.False);
                Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            }
            finally { Directory.Delete(directory, true); }
        }
        [Test] public void BothDamagedFilesArePreservedWithoutOverwritingEachOther()
        {
            string directory = Path.Combine(Path.GetTempPath(), "HCDamaged-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(Path.Combine(directory, "homecoming_profile.json"), "{broken-main");
                File.WriteAllText(Path.Combine(directory, "homecoming_profile.json.bak"), "{broken-backup");
                var profile = new HomecomingProfile(new ProfileFileStorage(directory));
                Assert.That(profile.LoadStatus, Is.EqualTo(ProfileLoadStatus.CorruptedAllPreserved));
                var preserved = Directory.GetFiles(directory, "*.corrupted.*").Select(File.ReadAllText).ToArray();
                Assert.That(preserved, Is.EquivalentTo(new[] { "{broken-main", "{broken-backup" }));
            }
            finally { Directory.Delete(directory, true); }
        }
        [Test] public void WaitingWithoutExploringCannotFarmMetaExperience()
        {
            string directory = Path.Combine(Path.GetTempPath(), "HCEmpty-" + Guid.NewGuid().ToString("N"));
            try
            {
                var profile = new HomecomingProfile(new ProfileFileStorage(directory));
                Assert.That(profile.TrySettle(new ExplorationSettlement(Guid.NewGuid(), RunPhase.Defeat, 600, false,
                    new[] { "W01" }, null, null), out var receipt), Is.True);
                Assert.That(receipt.ExperienceGained, Is.EqualTo(0));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
