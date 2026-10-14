using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.Homecoming;
using Waffle.Homecoming.Profile;

namespace Waffle.Tests
{
    public sealed class HomecomingProfileTests
    {
        private sealed class MemoryStoragePort : IProfileStoragePort
        {
            public string StoredJson;
            public ProfileLoadStatus ForceLoadStatus = ProfileLoadStatus.Success;
            public bool FailNextSave = false;
            public int SaveCallCount = 0;

            public MemoryStoragePort(string initialJson = null)
            {
                StoredJson = initialJson;
            }

            public ProfileLoadResult Load()
            {
                if (ForceLoadStatus == ProfileLoadStatus.CreatedNew || StoredJson == null)
                    return ProfileLoadResult.NewProfile();

                if (ForceLoadStatus == ProfileLoadStatus.VersionTooNewReadOnly)
                    return new ProfileLoadResult(ProfileLoadStatus.VersionTooNewReadOnly, StoredJson, "Version too new");

                if (ForceLoadStatus == ProfileLoadStatus.CorruptedAllPreserved)
                    return new ProfileLoadResult(ProfileLoadStatus.CorruptedAllPreserved, null, "All corrupted");

                return new ProfileLoadResult(ProfileLoadStatus.Success, StoredJson, "Loaded");
            }

            public ProfileSaveResult SaveAtomic(string jsonPayload)
            {
                SaveCallCount++;
                if (FailNextSave)
                {
                    FailNextSave = false; // consume
                    return ProfileSaveResult.Fail("Simulated disk write failure");
                }
                StoredJson = jsonPayload;
                return ProfileSaveResult.Ok();
            }
        }

        [Test]
        public void InitialProfileCreation_StartsAtLevel1WithZeroPoints()
        {
            var mem = new MemoryStoragePort();
            var profile = new HomecomingProfile(mem);

            var view = profile.Read();
            Assert.That(view.Level, Is.EqualTo(1));
            Assert.That(view.TotalExperience, Is.EqualTo(0));
            Assert.That(view.AvailablePoints, Is.EqualTo(0));
            Assert.That(view.Upgrades.Count, Is.EqualTo(3));
            Assert.That(mem.SaveCallCount, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void ProgressionCalculation_MatchesSeedThresholdsAndPoints()
        {
            // Level 1 -> 2 requires 100 XP
            ProfileConfig.CalculateLevelProgression(99, out int lvl, out int pts, out _, out _);
            Assert.That(lvl, Is.EqualTo(1));
            Assert.That(pts, Is.EqualTo(0));

            ProfileConfig.CalculateLevelProgression(100, out lvl, out pts, out _, out _);
            Assert.That(lvl, Is.EqualTo(2));
            Assert.That(pts, Is.EqualTo(1));

            // Level 2 -> 3 requires 125 XP (Total 225)
            ProfileConfig.CalculateLevelProgression(224, out lvl, out pts, out _, out _);
            Assert.That(lvl, Is.EqualTo(2));
            Assert.That(pts, Is.EqualTo(1));

            ProfileConfig.CalculateLevelProgression(225, out lvl, out pts, out _, out _);
            Assert.That(lvl, Is.EqualTo(3));
            Assert.That(pts, Is.EqualTo(2));
        }

        [Test]
        public void Settlement_AwardsFirstTimeUniquePlacesAndGoals_WithPermanentDeduplication()
        {
            var mem = new MemoryStoragePort();
            var profile = new HomecomingProfile(mem);

            var run1 = Guid.NewGuid();
            var settlement1 = new ExplorationSettlement(
                run1,
                RunPhase.Victory,
                duration: 60f,
                bossDefeated: true, // +30 XP
                narratives: new[] { "poi.old-home", "item.sewing-tin" },
                claimedTemplates: new[] { "poi.old-home", "poi.old-silo" }, // 2 * 30 = 60 XP
                goals: new[] { "goal.homeward" } // +40 XP
            ); // Total = 30 + 60 + 40 = 130 XP -> Level 2 (+1 point), 30 XP in level 2

            bool ok1 = profile.TrySettle(settlement1, out var receipt1);
            Assert.That(ok1, Is.True);
            Assert.That(receipt1.ExperienceGained, Is.EqualTo(130));
            Assert.That(receipt1.PointsGained, Is.EqualTo(1));
            Assert.That(receipt1.AlreadySettled, Is.False);

            var view1 = profile.Read();
            Assert.That(view1.TotalExperience, Is.EqualTo(130));
            Assert.That(view1.Level, Is.EqualTo(2));
            Assert.That(view1.AvailablePoints, Is.EqualTo(1));
            Assert.That(view1.DiscoveredNarrativeIds, Does.Contain("poi.old-home"));

            // Run 2: Same unique places and goals should NOT award XP again!
            var run2 = Guid.NewGuid();
            var settlement2 = new ExplorationSettlement(
                run2,
                RunPhase.Defeat,
                duration: 20f, // failure guarantee = +10 XP
                bossDefeated: false,
                narratives: new[] { "poi.old-home" },
                claimedTemplates: new[] { "poi.old-home" }, // already claimed, 0 XP
                goals: new[] { "goal.homeward" } // already completed, 0 XP
            );

            bool ok2 = profile.TrySettle(settlement2, out var receipt2);
            Assert.That(ok2, Is.True);
            Assert.That(receipt2.ExperienceGained, Is.EqualTo(10));
            Assert.That(receipt2.PointsGained, Is.EqualTo(0));
            Assert.That(profile.Read().TotalExperience, Is.EqualTo(140));
        }

        [Test]
        public void Settlement_DuplicateRunId_ReturnsOriginalReceiptWithoutDuplicateAward()
        {
            var mem = new MemoryStoragePort();
            var profile = new HomecomingProfile(mem);

            var runId = Guid.NewGuid();
            var settlement = new ExplorationSettlement(
                runId,
                RunPhase.Victory,
                duration: 100f,
                bossDefeated: true,
                narratives: null,
                claimedTemplates: new[] { "poi.old-home" },
                goals: null
            );

            Assert.That(profile.TrySettle(settlement, out var receipt1), Is.True);
            Assert.That(receipt1.AlreadySettled, Is.False);
            long xpAfterRun1 = profile.Read().TotalExperience;

            // Re-attempt settling the same runId
            Assert.That(profile.TrySettle(settlement, out var receipt2), Is.True);
            Assert.That(receipt2.AlreadySettled, Is.True);
            Assert.That(receipt2.ExperienceGained, Is.EqualTo(receipt1.ExperienceGained));
            Assert.That(receipt2.PointsGained, Is.EqualTo(receipt1.PointsGained));
            Assert.That(profile.Read().TotalExperience, Is.EqualTo(xpAfterRun1), "XP must not increase on replay!");
        }

        [Test]
        public void Settlement_FailureGuarantee_RequiresMinimumDuration()
        {
            var mem = new MemoryStoragePort();
            var profile = new HomecomingProfile(mem);

            // Instant abort (5 seconds) -> No failure guarantee
            var shortRun = new ExplorationSettlement(Guid.NewGuid(), RunPhase.Defeat, 5f, false, null, null, null);
            profile.TrySettle(shortRun, out var shortReceipt);
            Assert.That(shortReceipt.ExperienceGained, Is.EqualTo(0));

            // Valid exploration failure (20 seconds) -> +10 XP
            var longRun = new ExplorationSettlement(Guid.NewGuid(), RunPhase.Defeat, 20f, false, new[] { "poi.old-home" }, null, null);
            profile.TrySettle(longRun, out var longReceipt);
            Assert.That(longReceipt.ExperienceGained, Is.EqualTo(10));
        }

        [Test]
        public void Settlement_RejectsInvalidInputs_AndNonTerminalPhases()
        {
            var mem = new MemoryStoragePort();
            var profile = new HomecomingProfile(mem);

            // Empty RunId
            var emptyRun = new ExplorationSettlement(Guid.Empty, RunPhase.Victory, 10f, true, null, null, null);
            Assert.That(profile.TrySettle(emptyRun, out _), Is.False);

            // Negative duration
            var negDur = new ExplorationSettlement(Guid.NewGuid(), RunPhase.Victory, -1f, true, null, null, null);
            Assert.That(profile.TrySettle(negDur, out _), Is.False);

            // Non-terminal phase (Playing or Paused)
            var playingRun = new ExplorationSettlement(Guid.NewGuid(), RunPhase.Playing, 50f, false, null, null, null);
            Assert.That(profile.TrySettle(playingRun, out _), Is.False);
        }

        [Test]
        public void TransactionalSave_FailedSaveDoesNotCommitMemoryState_AndAllowsRetry()
        {
            var mem = new MemoryStoragePort();
            var profile = new HomecomingProfile(mem);

            // Prepare settlement
            var runId = Guid.NewGuid();
            var settlement = new ExplorationSettlement(
                runId, RunPhase.Victory, 50f, true, null, new[] { "poi.old-home" }, null);

            // Simulate disk failure
            mem.FailNextSave = true;
            bool ok = profile.TrySettle(settlement, out var receipt);
            Assert.That(ok, Is.False, "Settlement must report failure when storage fails!");
            Assert.That(profile.Read().TotalExperience, Is.EqualTo(0), "XP must not be published if save failed!");

            // Retry save with disk OK
            bool retryOk = profile.TrySettle(settlement, out var retryReceipt);
            Assert.That(retryOk, Is.True, "Retry should succeed");
            Assert.That(retryReceipt.ExperienceGained, Is.GreaterThan(0));
            Assert.That(profile.Read().TotalExperience, Is.EqualTo(retryReceipt.ExperienceGained));
        }

        [Test]
        public void Upgrades_PurchasingDeductsPoints_IncreasesRank_AndUpdatesModifiers()
        {
            var mem = new MemoryStoragePort();
            var profile = new HomecomingProfile(mem);

            // Grant 300 XP (enough for Level 3 -> 2 points)
            var run = new ExplorationSettlement(
                Guid.NewGuid(), RunPhase.Victory, 100f, true, null,
                new[] { "poi.old-home", "poi.old-silo", "poi.old-guitar" }, // 90 XP
                new[] { "goal.homeward", "goal.harvest-memory", "goal.unfinished-song" } // 120 XP + 30 boss = 240 XP -> Lvl 3 (225 XP) = 2 points
            );
            profile.TrySettle(run, out _);

            Assert.That(profile.Read().AvailablePoints, Is.EqualTo(2));

            // Rank 1 cost = 1 point
            Assert.That(profile.TryPurchase(ProfileConfig.UpgradeMendedCoat), Is.True);
            Assert.That(profile.Read().AvailablePoints, Is.EqualTo(1));

            var mods = profile.ReadStartingModifiers();
            Assert.That(mods.MaxHealth, Is.EqualTo(ProfileConfig.MendedCoatMaxHealthPerRank).Within(1e-5));

            // Rank 2 cost = 2 points. We only have 1 point left -> must fail!
            Assert.That(profile.TryPurchase(ProfileConfig.UpgradeMendedCoat), Is.False);
            Assert.That(profile.Read().AvailablePoints, Is.EqualTo(1)); // Points preserved
        }

        [Test]
        public void FileStorage_RoundTrip_WithBackupRecovery_AndAtomicReplace()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "HomecomingTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var storage = new ProfileFileStorage(tempDir, "profile.json");
                var profile = new HomecomingProfile(storage);

                var runId = Guid.NewGuid();
                var settlement = new ExplorationSettlement(runId, RunPhase.Victory, 50f, true,
                    new[] { "item.sewing-tin" }, new[] { "poi.old-home" }, null);
                Assert.That(profile.TrySettle(settlement, out _), Is.True);

                string mainFile = Path.Combine(tempDir, "profile.json");
                string backupFile = Path.Combine(tempDir, "profile.json.bak");
                Assert.That(File.Exists(mainFile), Is.True);

                // Second settlement creates a valid backup
                var runId2 = Guid.NewGuid();
                profile.TrySettle(new ExplorationSettlement(runId2, RunPhase.Victory, 50f, true, null, new[] { "poi.old-silo" }, null), out _);
                Assert.That(File.Exists(backupFile), Is.True);

                // Corrupt main file
                File.WriteAllText(mainFile, "{ corrupted invalid json content !!!");

                // New instance loads and should automatically recover from backup!
                var storage2 = new ProfileFileStorage(tempDir, "profile.json");
                var profile2 = new HomecomingProfile(storage2);

                Assert.That(profile2.LoadStatus, Is.EqualTo(ProfileLoadStatus.RecoveredFromBackup));
                Assert.That(profile2.Read().TotalExperience, Is.GreaterThan(0));
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Test]
        public void FutureVersion_TreatedAsReadOnly_PreventsOverwriting()
        {
            var mem = new MemoryStoragePort("{\n  \"schemaVersion\": 999,\n  \"revision\": 1,\n  \"totalExperience\": 500,\n  \"availablePoints\": 2,\n  \"totalPointsSpent\": 0,\n  \"upgrades\": {},\n  \"discoveredNarratives\": [],\n  \"claimedTemplates\": [],\n  \"completedGoals\": [],\n  \"settledRuns\": {}\n}");
            mem.ForceLoadStatus = ProfileLoadStatus.VersionTooNewReadOnly;

            var profile = new HomecomingProfile(mem);
            Assert.That(profile.IsReadOnly, Is.True);

            // Purchase and Settle must be rejected in read-only mode
            Assert.That(profile.TryPurchase(ProfileConfig.UpgradeMendedCoat), Is.False);
            Assert.That(profile.TrySettle(new ExplorationSettlement(Guid.NewGuid(), RunPhase.Victory, 50f, true, null, null, null), out _), Is.False);
        }
    }
}
