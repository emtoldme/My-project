using System;

namespace Waffle.Homecoming.Profile
{
    /// <summary>
    /// Static rules, numeric bounds, costs and progression formulas for Homecoming profile.
    /// Pure C# rules, zero UnityEngine dependency.
    /// </summary>
    public static class ProfileConfig
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaxFileSizeBytes = 512 * 1024; // 512 KB sanity limit

        // Upgrade identifiers
        public const string UpgradeMendedCoat = "meta.mended-coat";
        public const string UpgradeTravelLight = "meta.travel-light";
        public const string UpgradeSortedSatchel = "meta.sorted-satchel";

        public const int MaxUpgradeRank = 3;

        // Experience rewards per unique trigger
        public const int XpFirstUniquePlace = 30;
        public const int XpFirstGoal = 40;
        public const int XpBossVictory = 30;
        public const int XpFailureGuarantee = 10;

        // Caps per run
        public const float MinimumDurationForFailureGuaranteeSeconds = 15f;
        public const int MaxXpPerSingleRun = 250;
        public const int MaxPointsPerSingleRun = 10;

        // Meta upgrade stat bonuses per rank
        public const float MendedCoatMaxHealthPerRank = 0.02f;   // +2% Max HP per rank (max +6%)
        public const float TravelLightMoveSpeedPerRank = 0.01f;   // +1% Move Speed per rank (max +3%)
        public const float SortedSatchelPickupRadiusPerRank = 0.03f; // +3% Pickup Radius per rank (max +9%)

        /// <summary>
        /// Cost in mending points for purchasing the next rank (Rank 1: 1, Rank 2: 2, Rank 3: 3).
        /// Returns -1 if already at max rank.
        /// </summary>
        public static int GetUpgradeCost(int currentRank)
        {
            if (currentRank < 0 || currentRank >= MaxUpgradeRank)
                return -1;
            return currentRank + 1; // 1, 2, 3
        }

        /// <summary>
        /// XP required to advance from (level) to (level + 1).
        /// Level 1 -> 2: 100 XP
        /// Level 2 -> 3: 125 XP
        /// Formula: 100 + (level - 1) * 25
        /// </summary>
        public static int GetExperienceForNextLevel(int currentLevel)
        {
            if (currentLevel < 1) currentLevel = 1;
            return 100 + (currentLevel - 1) * 25;
        }

        /// <summary>
        /// Total cumulative XP required to reach a given level from level 1.
        /// </summary>
        public static long GetTotalExperienceForLevel(int targetLevel)
        {
            if (targetLevel <= 1) return 0;
            long total = 0;
            for (int lvl = 1; lvl < targetLevel; lvl++)
            {
                total += GetExperienceForNextLevel(lvl);
            }
            return total;
        }

        /// <summary>
        /// Calculate current Level and points earned from total XP.
        /// Each level up grants +1 Available Mending Point.
        /// </summary>
        public static void CalculateLevelProgression(long totalXp, out int level, out int totalPointsEarned, out long xpInCurrentLevel, out int xpNeededForNext)
        {
            if (totalXp < 0) totalXp = 0;
            level = 1;
            long xpRemaining = totalXp;
            while (true)
            {
                int req = GetExperienceForNextLevel(level);
                if (xpRemaining >= req)
                {
                    xpRemaining -= req;
                    level++;
                }
                else
                {
                    xpInCurrentLevel = xpRemaining;
                    xpNeededForNext = req;
                    break;
                }
            }
            totalPointsEarned = level - 1;
        }

        public static bool IsKnownUpgradeId(string id)
        {
            return id == UpgradeMendedCoat || id == UpgradeTravelLight || id == UpgradeSortedSatchel;
        }

        public static string GetUpgradeNarrativeId(string upgradeId)
        {
            return upgradeId;
        }
    }
}
