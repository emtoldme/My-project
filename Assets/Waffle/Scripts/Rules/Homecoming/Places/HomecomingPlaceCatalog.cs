using System;
using System.Collections.Generic;

namespace Waffle.Homecoming.Places
{
    /// <summary>HC-A 地点模板与奖励表（CONTENT_SEED 首版）。</summary>
    public static class HomecomingPlaceCatalog
    {
        public const string OldHome = "poi.old-home", OldSilo = "poi.old-silo", OldGuitar = "poi.old-guitar";
        public const string FieldSwing = "poi.field-swing", FieldTractor = "poi.field-tractor", FieldRadio = "poi.field-radio";
        public const string SewingTin = "item.sewing-tin", WorkGloves = "item.work-gloves", OldGuitarItem = "item.old-guitar";

        public static readonly IReadOnlyList<string> UniqueTemplates = Array.AsReadOnly(new[] { OldHome, OldSilo, OldGuitar });
        /// <summary>远处通用模板：只给叙事、不给奖励，复用已有秋千/农机/广播景物。</summary>
        public static readonly IReadOnlyList<string> GenericTemplates = Array.AsReadOnly(new[] { FieldSwing, FieldTractor, FieldRadio });

        public readonly struct Grant
        {
            public readonly string ItemId; public readonly int AdditionalSlots; public readonly string Description;
            public Grant(string item, int slots, string desc) { ItemId = item; AdditionalSlots = slots; Description = desc; }
        }

        static readonly Dictionary<string, Grant> rewards = new Dictionary<string, Grant>(StringComparer.Ordinal)
        {
            { OldHome, new Grant(SewingTin, 1, "针线盒，并多出一个遗物栏") },
            { OldSilo, new Grant(WorkGloves, 1, "工作手套，并多出一个遗物栏") },
            { OldGuitar, new Grant(OldGuitarItem, 0, "旧吉他") },
        };

        public static bool IsKnownTemplate(string id) => id != null && (IsUnique(id) || Contains(GenericTemplates, id));
        public static bool IsUnique(string id) => id != null && Contains(UniqueTemplates, id);
        public static bool HasReward(string id) => id != null && rewards.ContainsKey(id);
        public static Grant RewardFor(string id) => rewards.TryGetValue(id ?? "", out var g) ? g : default;
        public static string RewardDescription(string id) => rewards.TryGetValue(id ?? "", out var g) ? g.Description : "只有一段旧事";
        /// <summary>地点叙事ID沿用模板ID（CONTRACTS：地点沿用其内容ID）。</summary>
        public static string NarrativeIdFor(string templateId) => IsKnownTemplate(templateId) ? templateId : null;

        static bool Contains(IReadOnlyList<string> l, string v) { foreach (var x in l) if (x == v) return true; return false; }
    }
}
