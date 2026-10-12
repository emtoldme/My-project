using System;
using System.Collections.Generic;

namespace Waffle.RunContent
{
    public enum ContentKind { Weapon = 0, Passive = 1, Evolution = 2, Pickup = 3 }

    /// <summary>
    /// Planned：只有目录，不进正常抽卡池。
    /// RulesReady：成长/出牌/属性聚合在本规则核心可执行，战斗行为仍待 Codex 接线（06B 首批）。
    /// Wired：已由唯一 CombatModule 实际生效（由 Codex 接线后改，1号本轮不标）。
    /// </summary>
    public enum ImplementationStatus { Planned = 0, RulesReady = 1, Wired = 2 }

    /// <summary>某一阶的一个参数（键见 RC01/Growth/CATALOG.md 的参数字典）。</summary>
    public readonly struct ContentParam
    {
        public readonly string Key;
        public readonly float Value;
        public ContentParam(string key, float value) { Key = key; Value = value; }
    }

    /// <summary>一阶：参数 + 卡面文本（当前→下一阶的实际变化）。</summary>
    public sealed class ContentRank
    {
        public readonly int Rank;
        public readonly string EffectText;
        public readonly IReadOnlyList<ContentParam> Params;
        public ContentRank(int rank, string effectText, ContentParam[] ps)
        {
            Rank = rank; EffectText = effectText;
            Params = Array.AsReadOnly((ContentParam[])(ps ?? Array.Empty<ContentParam>()).Clone());
        }
        public float Get(string key, float fallback = 0f)
        {
            for (int i = 0; i < Params.Count; i++) if (Params[i].Key == key) return Params[i].Value;
            return fallback;
        }
    }

    /// <summary>进化前置：武器满阶 + 对应被动达到阶数 + 局内已过秒数，由精英奖励触发。</summary>
    public sealed class EvolutionRequirement
    {
        public readonly string WeaponId, PassiveId;
        public readonly int WeaponRank, PassiveRank;
        public readonly float MinRunSeconds;
        public EvolutionRequirement(string weaponId, int weaponRank, string passiveId, int passiveRank, float minRunSeconds)
        { WeaponId = weaponId; WeaponRank = weaponRank; PassiveId = passiveId; PassiveRank = passiveRank; MinRunSeconds = minRunSeconds; }
    }

    /// <summary>不可变内容定义。schemaVersion/id/kind/displayName/tags/maxRank/ranks/前置/权重/说明/状态/owner。</summary>
    public sealed class ContentDefinition
    {
        public const int CurrentSchemaVersion = 1;
        public readonly int SchemaVersion = CurrentSchemaVersion;
        public readonly string Id, DisplayName, Description, Owner;
        public readonly ContentKind Kind;
        public readonly IReadOnlyList<string> Tags;
        public readonly IReadOnlyList<ContentRank> Ranks;
        public readonly int Weight;
        public readonly ImplementationStatus Status;
        public readonly EvolutionRequirement Evolution; // 仅 Kind==Evolution
        public readonly string EvolvesFromWeapon;        // 仅 Kind==Evolution
        public int MaxRank => Ranks.Count;

        public ContentDefinition(string id, ContentKind kind, string displayName, string[] tags, ContentRank[] ranks,
            int weight, ImplementationStatus status, string description, EvolutionRequirement evolution = null, string owner = "1号")
        {
            Id = id; Kind = kind; DisplayName = displayName; Description = description; Owner = owner;
            Tags = Array.AsReadOnly((string[])(tags ?? Array.Empty<string>()).Clone());
            Ranks = Array.AsReadOnly((ContentRank[])(ranks ?? Array.Empty<ContentRank>()).Clone());
            Weight = weight; Status = status; Evolution = evolution;
            EvolvesFromWeapon = evolution?.WeaponId;
        }

        public ContentRank RankAt(int rank) => rank >= 1 && rank <= Ranks.Count ? Ranks[rank - 1] : null;
    }
}
