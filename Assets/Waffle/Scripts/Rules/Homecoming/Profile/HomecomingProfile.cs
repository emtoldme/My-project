using System;
using System.Collections.Generic;
using Waffle.Combat;

namespace Waffle.Homecoming.Profile
{
    /// <summary>
    /// Pure C# domain implementation of IHomecomingProfile.
    /// Manages lifetime experience, level advancement, mending points, meta upgrade purchases,
    /// deduplicated terminal run settlements, and transactional write-before-publish persistence.
    /// </summary>
    public sealed class HomecomingProfile : IHomecomingProfile
    {
        private readonly IProfileStoragePort storagePort;
        private ProfileData currentData;
        private bool isReadOnly = false;
        private string lastStatusMessage = "";
        private ProfileLoadStatus loadStatus = ProfileLoadStatus.Success;

        public HomecomingProfile(IProfileStoragePort storagePort)
        {
            this.storagePort = storagePort ?? throw new ArgumentNullException(nameof(storagePort));
            LoadProfile();
        }

        public ProfileLoadStatus LoadStatus => loadStatus;
        public string StatusMessage => lastStatusMessage;
        public bool IsReadOnly => isReadOnly;

        private void LoadProfile()
        {
            var loadRes = storagePort.Load();
            loadStatus = loadRes.Status;
            lastStatusMessage = loadRes.StatusMessage;

            if (loadStatus == ProfileLoadStatus.VersionTooNewReadOnly)
            {
                isReadOnly = true;
                currentData = ProfileData.FromJson(loadRes.JsonPayload);
                return;
            }

            if (loadRes.JsonPayload != null)
            {
                currentData = ProfileData.FromJson(loadRes.JsonPayload);
            }
            else
            {
                currentData = new ProfileData();
                // Save initial new profile
                PersistCurrentData(isNewCreation: true);
            }
        }

        /// <summary>
        /// Reads an immutable view of the profile.
        /// </summary>
        public HomecomingProfileView Read()
        {
            ProfileConfig.CalculateLevelProgression(currentData.TotalExperience, out int level, out _, out _, out _);

            var upgradesList = new List<MetaUpgradeView>(3);
            AddUpgradeView(upgradesList, ProfileConfig.UpgradeMendedCoat);
            AddUpgradeView(upgradesList, ProfileConfig.UpgradeTravelLight);
            AddUpgradeView(upgradesList, ProfileConfig.UpgradeSortedSatchel);

            return new HomecomingProfileView(
                currentData.TotalExperience,
                level,
                currentData.AvailablePoints,
                upgradesList,
                currentData.DiscoveredNarratives
            );
        }

        private void AddUpgradeView(List<MetaUpgradeView> list, string upgradeId)
        {
            currentData.Upgrades.TryGetValue(upgradeId, out int rank);
            int cost = ProfileConfig.GetUpgradeCost(rank);
            list.Add(new MetaUpgradeView(
                upgradeId,
                ProfileConfig.GetUpgradeNarrativeId(upgradeId),
                rank,
                ProfileConfig.MaxUpgradeRank,
                cost
            ));
        }

        /// <summary>
        /// Reads starting modifiers granted by purchased meta-upgrades.
        /// Fixed at the start of a run; host does not hot-reload during run.
        /// </summary>
        public HomecomingModifiers ReadStartingModifiers()
        {
            currentData.Upgrades.TryGetValue(ProfileConfig.UpgradeMendedCoat, out int coatRank);
            currentData.Upgrades.TryGetValue(ProfileConfig.UpgradeTravelLight, out int lightRank);
            currentData.Upgrades.TryGetValue(ProfileConfig.UpgradeSortedSatchel, out int satchelRank);

            float hpBonus = coatRank * ProfileConfig.MendedCoatMaxHealthPerRank;
            float speedBonus = lightRank * ProfileConfig.TravelLightMoveSpeedPerRank;
            float pickupBonus = satchelRank * ProfileConfig.SortedSatchelPickupRadiusPerRank;

            return new HomecomingModifiers(
                maxHealth: hpBonus,
                moveSpeed: speedBonus,
                pickupRadius: pickupBonus,
                dodgeCooldownReduction: 0f,
                dodgeEcho: false
            );
        }

        /// <summary>
        /// Transactional settlement of a completed run.
        /// Deduplicates run by RunId, awards XP for first-time discoveries and terminal outcomes.
        /// Crucially: writes to disk BEFORE publishing memory changes!
        /// </summary>
        public bool TrySettle(ExplorationSettlement settlement, out SettlementReceipt receipt)
        {
            if (settlement == null)
                throw new ArgumentNullException(nameof(settlement));

            if (isReadOnly)
            {
                receipt = new SettlementReceipt(settlement.RunId, 0, 0, false);
                return false;
            }

            // Reject empty RunId
            if (settlement.RunId == Guid.Empty)
            {
                receipt = new SettlementReceipt(Guid.Empty, 0, 0, false);
                return false;
            }

            // Reject negative duration or NaN
            if (settlement.Duration < 0 || float.IsNaN(settlement.Duration) || float.IsInfinity(settlement.Duration))
            {
                receipt = new SettlementReceipt(settlement.RunId, 0, 0, false);
                return false;
            }

            // Deduplication: Already settled runs return the exact same receipt with AlreadySettled = true
            string runKey = settlement.RunId.ToString();
            if (currentData.SettledRuns.TryGetValue(runKey, out var existingRecord))
            {
                receipt = new SettlementReceipt(settlement.RunId, existingRecord.ExperienceGained, existingRecord.PointsGained, true);
                return true;
            }

            // Only terminal runs can be settled (Victory or Defeat)
            if (settlement.Phase != RunPhase.Victory && settlement.Phase != RunPhase.Defeat)
            {
                receipt = new SettlementReceipt(settlement.RunId, 0, 0, false);
                return false;
            }

            // Work on a tentative candidate copy
            var candidate = currentData.Clone();
            int xpEarned = 0;

            // 1. First-time unique place claims
            if (settlement.ClaimedTemplateIds != null)
            {
                foreach (var templateId in settlement.ClaimedTemplateIds)
                {
                    if (string.IsNullOrWhiteSpace(templateId)) continue;
                    if (candidate.ClaimedTemplates.Add(templateId))
                    {
                        xpEarned += ProfileConfig.XpFirstUniquePlace;
                    }
                }
            }

            // 2. First-time goal completions
            if (settlement.CompletedGoalIds != null)
            {
                foreach (var goalId in settlement.CompletedGoalIds)
                {
                    if (string.IsNullOrWhiteSpace(goalId)) continue;
                    if (candidate.CompletedGoals.Add(goalId))
                    {
                        xpEarned += ProfileConfig.XpFirstGoal;
                    }
                }
            }

            // 3. Boss Victory outcome
            if (settlement.BossDefeated && settlement.Phase == RunPhase.Victory)
            {
                xpEarned += ProfileConfig.XpBossVictory;
            }
            else if (settlement.Phase == RunPhase.Defeat)
            {
                // Effective failure guarantee: only if duration >= threshold (not immediate instant retry)
                if (settlement.Duration >= ProfileConfig.MinimumDurationForFailureGuaranteeSeconds)
                {
                    xpEarned += ProfileConfig.XpFailureGuarantee;
                }
            }

            // 4. Update discovered narratives without granting extra arbitrary XP (narratives recorded permanently)
            if (settlement.DiscoveredNarrativeIds != null)
            {
                foreach (var narrId in settlement.DiscoveredNarrativeIds)
                {
                    if (string.IsNullOrWhiteSpace(narrId)) continue;
                    candidate.DiscoveredNarratives.Add(narrId);
                }
            }

            // Cap single run XP
            if (xpEarned > ProfileConfig.MaxXpPerSingleRun)
            {
                xpEarned = ProfileConfig.MaxXpPerSingleRun;
            }

            // Calculate level up and mending points gained
            long previousXp = candidate.TotalExperience;
            long newXp = previousXp + xpEarned;

            ProfileConfig.CalculateLevelProgression(previousXp, out _, out int prevTotalEarned, out _, out _);
            ProfileConfig.CalculateLevelProgression(newXp, out _, out int newTotalEarned, out _, out _);

            int pointsGained = newTotalEarned - prevTotalEarned;
            if (pointsGained > ProfileConfig.MaxPointsPerSingleRun)
            {
                pointsGained = ProfileConfig.MaxPointsPerSingleRun;
            }

            candidate.TotalExperience = newXp;
            candidate.AvailablePoints += pointsGained;
            candidate.Revision++;

            // Record settlement receipt
            candidate.SettledRuns[runKey] = new SettlementReceiptRecord(xpEarned, pointsGained);

            // TRANSACTION BOUNDARY: Write to storage FIRST!
            var saveResult = storagePort.SaveAtomic(candidate.ToJson());
            if (!saveResult.Success)
            {
                lastStatusMessage = "Settlement save failed: " + saveResult.ErrorMessage;
                receipt = new SettlementReceipt(settlement.RunId, 0, 0, false);
                return false;
            }

            // Success: Now publish to in-memory state!
            currentData = candidate;
            lastStatusMessage = "Settlement saved successfully.";
            receipt = new SettlementReceipt(settlement.RunId, xpEarned, pointsGained, false);
            return true;
        }

        /// <summary>
        /// Purchases a meta upgrade rank using available mending points.
        /// Transactional: writes to disk BEFORE publishing purchase.
        /// </summary>
        public bool TryPurchase(string upgradeId)
        {
            if (isReadOnly) return false;
            if (string.IsNullOrWhiteSpace(upgradeId) || !ProfileConfig.IsKnownUpgradeId(upgradeId))
                return false;

            currentData.Upgrades.TryGetValue(upgradeId, out int currentRank);
            int cost = ProfileConfig.GetUpgradeCost(currentRank);
            if (cost < 0) return false; // Already max rank
            if (currentData.AvailablePoints < cost) return false; // Not enough points

            var candidate = currentData.Clone();
            candidate.AvailablePoints -= cost;
            candidate.TotalPointsSpent += cost;
            candidate.Upgrades[upgradeId] = currentRank + 1;
            candidate.Revision++;

            // Write to storage FIRST!
            var saveResult = storagePort.SaveAtomic(candidate.ToJson());
            if (!saveResult.Success)
            {
                lastStatusMessage = "Purchase save failed: " + saveResult.ErrorMessage;
                return false;
            }

            // Publish on disk success
            currentData = candidate;
            lastStatusMessage = "Upgrade " + upgradeId + " purchased to rank " + (currentRank + 1);
            return true;
        }

        /// <summary>
        /// Dedicated transaction for saving newly discovered narratives upon viewing or closing,
        /// ensuring viewed texts persist even on ordinary game quit without spoofing end-game XP.
        /// </summary>
        public bool TrySaveDiscoveredNarratives(IEnumerable<string> narrativeIds)
        {
            if (isReadOnly || narrativeIds == null) return false;

            var candidate = currentData.Clone();
            bool anyNew = false;
            foreach (var nid in narrativeIds)
            {
                if (string.IsNullOrWhiteSpace(nid)) continue;
                if (candidate.DiscoveredNarratives.Add(nid))
                {
                    anyNew = true;
                }
            }

            if (!anyNew) return true; // Already saved, no write needed

            candidate.Revision++;
            var saveRes = storagePort.SaveAtomic(candidate.ToJson());
            if (!saveRes.Success)
            {
                lastStatusMessage = "Narrative save failed: " + saveRes.ErrorMessage;
                return false;
            }

            currentData = candidate;
            return true;
        }

        private void PersistCurrentData(bool isNewCreation = false)
        {
            if (isReadOnly) return;
            var saveRes = storagePort.SaveAtomic(currentData.ToJson());
            if (!saveRes.Success)
            {
                lastStatusMessage = (isNewCreation ? "Initial profile creation" : "Profile save") + " failed: " + saveRes.ErrorMessage;
            }
        }
    }
}
