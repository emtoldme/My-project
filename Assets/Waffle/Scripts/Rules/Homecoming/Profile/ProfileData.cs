using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Waffle.Homecoming.Profile
{
    /// <summary>
    /// In-memory and serializable DTO for player profile state.
    /// Hand-crafted robust serialization with zero external dependencies,
    /// ensuring standard net8.0 and Unity .NET compatibility.
    /// </summary>
    public sealed class ProfileData
    {
        public int SchemaVersion { get; set; } = ProfileConfig.CurrentSchemaVersion;
        public long Revision { get; set; } = 1;
        public long TotalExperience { get; set; } = 0;
        public int AvailablePoints { get; set; } = 0;
        public int TotalPointsSpent { get; set; } = 0;

        // Upgrade ranks
        public Dictionary<string, int> Upgrades { get; set; } = new Dictionary<string, int>();

        // Discovery ledgers (permanent deduplication)
        public HashSet<string> DiscoveredNarratives { get; set; } = new HashSet<string>();
        public HashSet<string> ClaimedTemplates { get; set; } = new HashSet<string>();
        public HashSet<string> CompletedGoals { get; set; } = new HashSet<string>();

        // Run settlements history for deduplication
        public Dictionary<string, SettlementReceiptRecord> SettledRuns { get; set; } = new Dictionary<string, SettlementReceiptRecord>();

        public ProfileData()
        {
            InitializeDefaults();
        }

        public void InitializeDefaults()
        {
            if (!Upgrades.ContainsKey(ProfileConfig.UpgradeMendedCoat))
                Upgrades[ProfileConfig.UpgradeMendedCoat] = 0;
            if (!Upgrades.ContainsKey(ProfileConfig.UpgradeTravelLight))
                Upgrades[ProfileConfig.UpgradeTravelLight] = 0;
            if (!Upgrades.ContainsKey(ProfileConfig.UpgradeSortedSatchel))
                Upgrades[ProfileConfig.UpgradeSortedSatchel] = 0;
        }

        public ProfileData Clone()
        {
            var copy = new ProfileData
            {
                SchemaVersion = this.SchemaVersion,
                Revision = this.Revision,
                TotalExperience = this.TotalExperience,
                AvailablePoints = this.AvailablePoints,
                TotalPointsSpent = this.TotalPointsSpent,
                Upgrades = new Dictionary<string, int>(this.Upgrades),
                DiscoveredNarratives = new HashSet<string>(this.DiscoveredNarratives),
                ClaimedTemplates = new HashSet<string>(this.ClaimedTemplates),
                CompletedGoals = new HashSet<string>(this.CompletedGoals),
                SettledRuns = new Dictionary<string, SettlementReceiptRecord>()
            };
            foreach (var kvp in this.SettledRuns)
            {
                copy.SettledRuns[kvp.Key] = kvp.Value.Clone();
            }
            return copy;
        }

        public string ToJson()
        {
            var sb = new StringBuilder(1024);
            sb.Append("{\n");
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"schemaVersion\": {0},\n", SchemaVersion);
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"revision\": {0},\n", Revision);
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"totalExperience\": {0},\n", TotalExperience);
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"availablePoints\": {0},\n", AvailablePoints);
            sb.AppendFormat(CultureInfo.InvariantCulture, "  \"totalPointsSpent\": {0},\n", TotalPointsSpent);

            // Upgrades
            sb.Append("  \"upgrades\": {\n");
            int uCount = 0;
            foreach (var kvp in Upgrades)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "    \"{0}\": {1}{2}\n",
                    Escape(kvp.Key), kvp.Value, (++uCount < Upgrades.Count ? "," : ""));
            }
            sb.Append("  },\n");

            // DiscoveredNarratives
            sb.Append("  \"discoveredNarratives\": [");
            WriteList(sb, DiscoveredNarratives);
            sb.Append("],\n");

            // ClaimedTemplates
            sb.Append("  \"claimedTemplates\": [");
            WriteList(sb, ClaimedTemplates);
            sb.Append("],\n");

            // CompletedGoals
            sb.Append("  \"completedGoals\": [");
            WriteList(sb, CompletedGoals);
            sb.Append("],\n");

            // SettledRuns
            sb.Append("  \"settledRuns\": {\n");
            int rCount = 0;
            foreach (var kvp in SettledRuns)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "    \"{0}\": {{\"xp\":{1},\"points\":{2}}}{3}\n",
                    Escape(kvp.Key), kvp.Value.ExperienceGained, kvp.Value.PointsGained, (++rCount < SettledRuns.Count ? "," : ""));
            }
            sb.Append("  }\n");

            sb.Append("}");
            return sb.ToString();
        }

        private static void WriteList(StringBuilder sb, IEnumerable<string> items)
        {
            bool first = true;
            foreach (var it in items)
            {
                if (!first) sb.Append(", ");
                sb.Append("\"").Append(Escape(it)).Append("\"");
                first = false;
            }
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public static ProfileData FromJson(string json)
        {
            var root = ProfileJson.Read(json);
            long Number(string key) => root.TryGetValue(key, out var value) && value is long n ? n : throw new FormatException("Missing integer: " + key);
            var data = new ProfileData {
                SchemaVersion = checked((int)Number("schemaVersion")), Revision = Number("revision"), TotalExperience = Number("totalExperience"),
                AvailablePoints = checked((int)Number("availablePoints")), TotalPointsSpent = checked((int)Number("totalPointsSpent")) };
            Dictionary<string, object> Map(string key) => root.TryGetValue(key, out var value) && value is Dictionary<string, object> map ? map : throw new FormatException("Missing map: " + key);
            data.Upgrades.Clear();
            foreach (var kv in Map("upgrades")) data.Upgrades.Add(kv.Key, kv.Value is long n ? checked((int)n) : throw new FormatException("Invalid rank"));
            void ReadSet(string key, HashSet<string> set) {
                if (!root.TryGetValue(key, out var value) || !(value is List<object> list)) throw new FormatException("Missing list: " + key);
                foreach (var entry in list) if (!(entry is string id) || !set.Add(id)) throw new FormatException("Invalid or duplicate ID");
            }
            ReadSet("discoveredNarratives", data.DiscoveredNarratives); ReadSet("claimedTemplates", data.ClaimedTemplates); ReadSet("completedGoals", data.CompletedGoals);
            foreach (var kv in Map("settledRuns")) {
                if (!(kv.Value is Dictionary<string, object> receipt) || !receipt.TryGetValue("xp", out var x) || !(x is long xp)
                    || !receipt.TryGetValue("points", out var pt) || !(pt is long points)) throw new FormatException("Incomplete receipt");
                data.SettledRuns.Add(kv.Key, new SettlementReceiptRecord(checked((int)xp), checked((int)points)));
            }
            data.ValidateOrThrow(); return data;
        }

        public void ValidateOrThrow()
        {
            if (SchemaVersion <= 0) throw new InvalidOperationException("Invalid schema version: " + SchemaVersion);
            if (Revision < 0) throw new InvalidOperationException("Negative revision: " + Revision);
            if (TotalExperience < 0 || TotalExperience > 1000000000) throw new InvalidOperationException("Negative total experience");
            if (AvailablePoints < 0) throw new InvalidOperationException("Negative available points");
            if (TotalPointsSpent < 0) throw new InvalidOperationException("Negative points spent");

            ProfileConfig.CalculateLevelProgression(TotalExperience, out _, out int totalEarned, out _, out _);
            if ((long)AvailablePoints + TotalPointsSpent > totalEarned)
            {
                // Inconsistency check: spent + available cannot exceed total earned from levels
                throw new InvalidOperationException("Total points exceed earned points");
            }

            foreach (var kvp in Upgrades)
            {
                if (!ProfileConfig.IsKnownUpgradeId(kvp.Key)) throw new FormatException("Unknown upgrade");
                if (kvp.Value < 0 || kvp.Value > ProfileConfig.MaxUpgradeRank)
                    throw new InvalidOperationException("Upgrade rank out of range for " + kvp.Key);
            }
            if (Upgrades.Count != 3) throw new FormatException("Missing upgrade state");
            foreach (var id in DiscoveredNarratives) if (Places.HomecomingNarrativeCatalog.Default.Resolve(id) == null) throw new FormatException("Unknown memory");
            foreach (var id in ClaimedTemplates) if (!Places.HomecomingPlaceCatalog.IsUnique(id)) throw new FormatException("Unknown story place");
            foreach (var id in CompletedGoals) if (id != "goal.homeward" && id != "goal.harvest-memory" && id != "goal.unfinished-song") throw new FormatException("Unknown goal");
            foreach (var kv in SettledRuns) if (!Guid.TryParse(kv.Key, out var run) || run == Guid.Empty || kv.Value.ExperienceGained < 0
                || kv.Value.ExperienceGained > 250 || kv.Value.PointsGained < 0 || kv.Value.PointsGained > 10) throw new FormatException("Invalid receipt");

        }

    }

    public sealed class SettlementReceiptRecord
    {
        public int ExperienceGained { get; }
        public int PointsGained { get; }

        public SettlementReceiptRecord(int xp, int pts)
        {
            ExperienceGained = xp;
            PointsGained = pts;
        }

        public SettlementReceiptRecord Clone() => new SettlementReceiptRecord(ExperienceGained, PointsGained);
    }
}
