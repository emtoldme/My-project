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
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Empty json string");

            var data = new ProfileData();
            data.Upgrades.Clear();

            // Simple token-based parser for our deterministic JSON schema
            int p = 0;
            SkipWhitespace(json, ref p);
            if (p >= json.Length || json[p] != '{') throw new FormatException("Invalid JSON root");
            p++;

            while (p < json.Length)
            {
                SkipWhitespace(json, ref p);
                if (p >= json.Length || json[p] == '}') break;

                string key = ReadString(json, ref p);
                SkipWhitespace(json, ref p);
                if (p >= json.Length || json[p] != ':') throw new FormatException("Expected ':' after key " + key);
                p++; // skip ':'
                SkipWhitespace(json, ref p);

                switch (key)
                {
                    case "schemaVersion":
                        data.SchemaVersion = (int)ReadLong(json, ref p);
                        break;
                    case "revision":
                        data.Revision = ReadLong(json, ref p);
                        break;
                    case "totalExperience":
                        data.TotalExperience = ReadLong(json, ref p);
                        break;
                    case "availablePoints":
                        data.AvailablePoints = (int)ReadLong(json, ref p);
                        break;
                    case "totalPointsSpent":
                        data.TotalPointsSpent = (int)ReadLong(json, ref p);
                        break;
                    case "upgrades":
                        ReadUpgradesMap(json, ref p, data.Upgrades);
                        break;
                    case "discoveredNarratives":
                        ReadStringSet(json, ref p, data.DiscoveredNarratives);
                        break;
                    case "claimedTemplates":
                        ReadStringSet(json, ref p, data.ClaimedTemplates);
                        break;
                    case "completedGoals":
                        ReadStringSet(json, ref p, data.CompletedGoals);
                        break;
                    case "settledRuns":
                        ReadSettledRunsMap(json, ref p, data.SettledRuns);
                        break;
                    default:
                        SkipValue(json, ref p);
                        break;
                }

                SkipWhitespace(json, ref p);
                if (p < json.Length && json[p] == ',') p++;
            }

            data.InitializeDefaults();
            data.ValidateOrThrow();
            return data;
        }

        public void ValidateOrThrow()
        {
            if (SchemaVersion <= 0) throw new InvalidOperationException("Invalid schema version: " + SchemaVersion);
            if (Revision < 0) throw new InvalidOperationException("Negative revision: " + Revision);
            if (TotalExperience < 0) throw new InvalidOperationException("Negative total experience");
            if (AvailablePoints < 0) throw new InvalidOperationException("Negative available points");
            if (TotalPointsSpent < 0) throw new InvalidOperationException("Negative points spent");

            ProfileConfig.CalculateLevelProgression(TotalExperience, out _, out int totalEarned, out _, out _);
            if (AvailablePoints + TotalPointsSpent > totalEarned)
            {
                // Inconsistency check: spent + available cannot exceed total earned from levels
                throw new InvalidOperationException("Total points exceed earned points");
            }

            foreach (var kvp in Upgrades)
            {
                if (kvp.Value < 0 || kvp.Value > ProfileConfig.MaxUpgradeRank)
                    throw new InvalidOperationException("Upgrade rank out of range for " + kvp.Key);
            }
        }

        private static void SkipWhitespace(string s, ref int p)
        {
            while (p < s.Length && (char.IsWhiteSpace(s[p]) || s[p] == '\r' || s[p] == '\n')) p++;
        }

        private static string ReadString(string s, ref int p)
        {
            if (p >= s.Length || s[p] != '"') throw new FormatException("Expected '\"'");
            p++;
            var sb = new StringBuilder();
            while (p < s.Length)
            {
                char c = s[p++];
                if (c == '\\')
                {
                    if (p >= s.Length) throw new FormatException("Unexpected EOF after '\\'");
                    char esc = s[p++];
                    if (esc == '"') sb.Append('"');
                    else if (esc == '\\') sb.Append('\\');
                    else sb.Append(esc);
                }
                else if (c == '"')
                {
                    return sb.ToString();
                }
                else
                {
                    sb.Append(c);
                }
            }
            throw new FormatException("Unterminated string");
        }

        private static long ReadLong(string s, ref int p)
        {
            int start = p;
            if (p < s.Length && (s[p] == '-' || s[p] == '+')) p++;
            while (p < s.Length && char.IsDigit(s[p])) p++;
            string num = s.Substring(start, p - start);
            return long.Parse(num, CultureInfo.InvariantCulture);
        }

        private static void ReadStringSet(string s, ref int p, HashSet<string> set)
        {
            set.Clear();
            if (p >= s.Length || s[p] != '[') throw new FormatException("Expected '['");
            p++;
            while (p < s.Length)
            {
                SkipWhitespace(s, ref p);
                if (p >= s.Length || s[p] == ']') { p++; break; }
                string item = ReadString(s, ref p);
                set.Add(item);
                SkipWhitespace(s, ref p);
                if (p < s.Length && s[p] == ',') p++;
            }
        }

        private static void ReadUpgradesMap(string s, ref int p, Dictionary<string, int> dict)
        {
            dict.Clear();
            if (p >= s.Length || s[p] != '{') throw new FormatException("Expected '{'");
            p++;
            while (p < s.Length)
            {
                SkipWhitespace(s, ref p);
                if (p >= s.Length || s[p] == '}') { p++; break; }
                string key = ReadString(s, ref p);
                SkipWhitespace(s, ref p);
                if (p >= s.Length || s[p] != ':') throw new FormatException("Expected ':'");
                p++;
                SkipWhitespace(s, ref p);
                int val = (int)ReadLong(s, ref p);
                dict[key] = val;
                SkipWhitespace(s, ref p);
                if (p < s.Length && s[p] == ',') p++;
            }
        }

        private static void ReadSettledRunsMap(string s, ref int p, Dictionary<string, SettlementReceiptRecord> map)
        {
            map.Clear();
            if (p >= s.Length || s[p] != '{') throw new FormatException("Expected '{'");
            p++;
            while (p < s.Length)
            {
                SkipWhitespace(s, ref p);
                if (p >= s.Length || s[p] == '}') { p++; break; }
                string runId = ReadString(s, ref p);
                SkipWhitespace(s, ref p);
                if (p >= s.Length || s[p] != ':') throw new FormatException("Expected ':'");
                p++;
                SkipWhitespace(s, ref p);
                if (p >= s.Length || s[p] != '{') throw new FormatException("Expected '{' for record");
                p++;
                int xp = 0, pts = 0;
                while (p < s.Length)
                {
                    SkipWhitespace(s, ref p);
                    if (p >= s.Length || s[p] == '}') { p++; break; }
                    string prop = ReadString(s, ref p);
                    SkipWhitespace(s, ref p);
                    if (p >= s.Length || s[p] != ':') throw new FormatException("Expected ':'");
                    p++;
                    SkipWhitespace(s, ref p);
                    if (prop == "xp") xp = (int)ReadLong(s, ref p);
                    else if (prop == "points") pts = (int)ReadLong(s, ref p);
                    else SkipValue(s, ref p);
                    SkipWhitespace(s, ref p);
                    if (p < s.Length && s[p] == ',') p++;
                }
                map[runId] = new SettlementReceiptRecord(xp, pts);
                SkipWhitespace(s, ref p);
                if (p < s.Length && s[p] == ',') p++;
            }
        }

        private static void SkipValue(string s, ref int p)
        {
            SkipWhitespace(s, ref p);
            if (p >= s.Length) return;
            if (s[p] == '"') { ReadString(s, ref p); return; }
            if (s[p] == '{')
            {
                p++; int depth = 1;
                while (p < s.Length && depth > 0)
                {
                    if (s[p] == '{') depth++;
                    else if (s[p] == '}') depth--;
                    p++;
                }
                return;
            }
            if (s[p] == '[')
            {
                p++; int depth = 1;
                while (p < s.Length && depth > 0)
                {
                    if (s[p] == '[') depth++;
                    else if (s[p] == ']') depth--;
                    p++;
                }
                return;
            }
            while (p < s.Length && s[p] != ',' && s[p] != '}' && s[p] != ']') p++;
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
