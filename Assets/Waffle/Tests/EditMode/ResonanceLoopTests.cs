using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.Homecoming.Places;
using Waffle.Homecoming.Relics;
using Waffle.Homecoming.Profile;

namespace Waffle.Tests
{
    public sealed class ResonanceLoopTests
    {
        static RunSimulation Make(bool all = true)
        {
            var sim = new RunSimulation(new RunConfig { useRunContent = false, unboundedWorld = true, damage = 0, runDuration = 600 },
                null, new CombatConfig { spawningEnabled = false });
            sim.InstallHomecoming(14104); sim.State.Level = 50;
            if (all) foreach (var site in HomecomingPlacesLayout.UniqueSites(14104))
            { sim.PlayerX = site.Position.X; sim.PlayerZ = site.Position.Y; sim.StepHomecoming(); }
            return sim;
        }
        static void Tick(RunSimulation sim, bool dodge = false)
        { var input = InputSnapshot.None; input.DodgePressed = dodge; sim.Tick(.05f, input, sim.State.Frame + 1, sim.State.Frame + 1); sim.StepHomecoming(); }
        static void Fill(RunSimulation sim, int count = 3)
        { for (int i = 0; i < count; i++) sim.Combat.DebugSpawnGem(new Vec2(sim.PlayerX, sim.PlayerZ), 6); Tick(sim); }
        static void Ready(RunSimulation sim) { for (int i = 0; i < 10; i++) Tick(sim); sim.DebugSetTime(sim.State.Now + 8.1f); Tick(sim); }

        [Test] public void OnlyRealCollectedExperienceCreatesBoundedFragments()
        {
            var sim = Make(); sim.DebugGiveXp(100); Tick(sim);
            Assert.That(sim.FieldFragments, Is.Zero);
            Fill(sim, 4); Assert.That(sim.FieldFragments, Is.EqualTo(3)); Assert.That(sim.FragmentProgress, Is.Zero);
            Assert.That(sim.Combat.Counters.TotalXpCollected, Is.EqualTo(24));
            sim.DebugHurt(10); Tick(sim); Assert.That(sim.FieldFragments, Is.EqualTo(2));
        }
        [Test] public void RetuningRequiresAnEquippedPairAndAnActualKnownCamp()
        {
            var sim = Make(); Assert.That(sim.TryRetuneHomecoming(sim.RunId, ResonancePattern.ThreadLure, false), Is.True);
            sim.PlayerX += 10; Assert.That(sim.TryRetuneHomecoming(sim.RunId, ResonancePattern.Backstitch, false), Is.False);
            sim.PlayerX -= 10; Assert.That(sim.TryRetuneHomecoming(Guid.NewGuid(), ResonancePattern.Backstitch, false), Is.False);
            int guitarSlot = sim.Relics.Read().Slots.Single(s => s.ItemId == RelicInventory.OldGuitar).Slot;
            sim.UnequipHomecoming(sim.RunId, guitarSlot, false);
            Assert.That(sim.Resonance, Is.EqualTo(ResonancePattern.Unlinked));
            Assert.That(sim.TryRetuneHomecoming(sim.RunId, ResonancePattern.Backstitch, false), Is.False);
        }
        [Test] public void BackstitchExchangesPositionsAndSpendsOneFragmentWithoutChangingExperience()
        {
            var sim = Make(); Fill(sim); Assert.That(sim.TryRetuneHomecoming(sim.RunId, ResonancePattern.Backstitch, false), Is.True);
            var original = new Vec2(sim.PlayerX, sim.PlayerZ); int xp = sim.State.Xp;
            Tick(sim, true); Assert.That(sim.ReturnNeedleActive, Is.True); Assert.That(sim.HomecomingEchoes, Is.Zero);
            sim.PlayerX = original.X + 12; sim.PlayerZ = original.Y; Ready(sim); Tick(sim, true);
            Assert.That(Math.Abs(sim.PlayerX - original.X), Is.LessThan(1));
            Assert.That(sim.ReturnNeedle.X, Is.EqualTo(original.X + 12));
            Assert.That(sim.FieldFragments, Is.EqualTo(2)); Assert.That(sim.State.Xp, Is.EqualTo(xp));
            Assert.That(sim.HomecomingEchoes, Is.EqualTo(1));
        }
        [Test] public void NeedleBreaksOutsideItsTetherAndOnEnding()
        {
            var sim = Make(); sim.TryRetuneHomecoming(sim.RunId, ResonancePattern.Backstitch, false); Tick(sim, true);
            sim.PlayerX += 25; sim.StepHomecoming(); Assert.That(sim.ReturnNeedleActive, Is.False);
            sim.DebugHurt(10000); Tick(sim); Assert.That(sim.HearthGuard, Is.False); Assert.That(sim.FieldFragments, Is.Zero);
        }
        [Test] public void ThreadLureRedirectsRealChasersAndExpires()
        {
            var sim = Make(); Fill(sim); sim.TryRetuneHomecoming(sim.RunId, ResonancePattern.ThreadLure, false);
            float origin = sim.PlayerX; var enemy = sim.Combat.DebugSpawnEnemy(new Vec2(origin + 3, sim.PlayerZ), 1000, 1);
            Tick(sim, true); sim.PlayerX = origin + 10;
            float before = enemy.Position.X; Tick(sim);
            Assert.That(enemy.Position.X, Is.LessThan(before)); Assert.That(sim.FieldFragments, Is.EqualTo(2));
            sim.DebugSetTime(sim.State.Now + 3); before = enemy.Position.X; Tick(sim);
            Assert.That(enemy.Position.X, Is.GreaterThan(before)); Assert.That(sim.Combat.HomecomingLureActive, Is.False);
        }
        [Test] public void HearthGatherMovesExistingGemsWithoutMintingExperience()
        {
            var sim = Make(); Fill(sim, 1); sim.TryRetuneHomecoming(sim.RunId, ResonancePattern.HearthGather, false);
            for (int i = 0; i < 15; i++) sim.Combat.DebugSpawnGem(new Vec2(sim.PlayerX + 7, sim.PlayerZ + i * .001f), 1);
            Tick(sim, true); Assert.That(sim.FieldFragments, Is.Zero);
            Assert.That(sim.Combat.HomecomingMagnetizedGems, Is.EqualTo(12));
            Assert.That(sim.Combat.Counters.TotalXpDropped, Is.EqualTo(21));
            Assert.That(sim.Combat.Counters.TotalXpCollected, Is.EqualTo(6));
        }
        [Test] public void RekindlingTradesThreeFragmentsForOneGuardAndCannotStackIt()
        {
            var sim = Make(); Fill(sim); sim.DebugSetHp(60);
            Assert.That(sim.TryRekindleHomecoming(sim.RunId, false), Is.True);
            int healed = sim.State.Hp; Assert.That(healed, Is.GreaterThan(60)); Assert.That(sim.FieldFragments, Is.Zero);
            Assert.That(sim.HearthGuard, Is.True); Fill(sim);
            Assert.That(sim.TryRekindleHomecoming(sim.RunId, false), Is.False);
            sim.DebugHurt(10); Tick(sim); Assert.That(sim.State.Hp, Is.EqualTo(healed)); Assert.That(sim.HearthGuard, Is.False);
            Assert.That(sim.FieldFragments, Is.EqualTo(3));
            Assert.That(sim.HomecomingMemories.Any(id => id.EndsWith(".rekindled")), Is.True);
        }
        [Test] public void RetuningDoesNotResetTheSharedAbilityCooldownAndPauseFreezesNeedle()
        {
            var sim = Make(); Fill(sim); sim.TryRetuneHomecoming(sim.RunId, ResonancePattern.Backstitch, false); Tick(sim, true);
            float expiry = sim.ReturnNeedleUntil, now = sim.State.Now;
            sim.RequestPause("journal"); for (int i = 0; i < 40; i++) Tick(sim);
            Assert.That(sim.State.Now, Is.EqualTo(now)); Assert.That(sim.ReturnNeedleUntil, Is.EqualTo(expiry));
            sim.TogglePause("journal");
            Assert.That(sim.TryRetuneHomecoming(sim.RunId, ResonancePattern.ThreadLure, false), Is.True);
            for (int i = 0; i < 10; i++) Tick(sim);
            sim.DebugSetTime(now + 2); Tick(sim, true);
            Assert.That(sim.HomecomingEchoes, Is.Zero); Assert.That(sim.FieldFragments, Is.EqualTo(3));
        }
        [Test] public void CampCueIsDeferredUntilPlayingAndOnlyDispatchedOnce()
        {
            var sim = Make(); Fill(sim); sim.RequestPause("journal");
            Assert.That(sim.TryRekindleHomecoming(sim.RunId, true), Is.True);
            Assert.That(sim.Presentation.Events.Any(e => e.Type == PresentationEventType.ShieldReady), Is.False);
            Tick(sim); Assert.That(sim.Presentation.Events.Any(e => e.Type == PresentationEventType.ShieldReady), Is.False);
            sim.TogglePause("journal"); Tick(sim);
            Assert.That(sim.Presentation.Events.Count(e => e.Type == PresentationEventType.ShieldReady), Is.EqualTo(1));
            Tick(sim); Assert.That(sim.Presentation.Events.Any(e => e.Type == PresentationEventType.ShieldReady), Is.False);
        }
        [Test] public void VersionOneProfileKeepsProgressAndWritesNewMemoriesAsVersionTwo()
        {
            string directory = Path.Combine(Path.GetTempPath(), "HC02Migration-" + Guid.NewGuid().ToString("N"));
            try
            {
                var storage = new ProfileFileStorage(directory);
                var old = new ProfileData { SchemaVersion = 1, TotalExperience = 100, AvailablePoints = 1 };
                Assert.That(storage.SaveAtomic(old.ToJson()).Success, Is.True);
                var profile = new HomecomingProfile(storage);
                Assert.That(profile.TrySaveDiscoveredNarratives(new[] { "combo.backstitch" }), Is.True);
                var saved = ProfileData.FromJson(File.ReadAllText(Path.Combine(directory, "homecoming_profile.json")));
                Assert.That(saved.SchemaVersion, Is.EqualTo(2)); Assert.That(saved.TotalExperience, Is.EqualTo(100));
                Assert.That(saved.AvailablePoints, Is.EqualTo(1)); Assert.That(saved.DiscoveredNarratives, Does.Contain("combo.backstitch"));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
