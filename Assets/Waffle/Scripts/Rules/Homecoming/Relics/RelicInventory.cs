using System;
using System.Collections.Generic;

namespace Waffle.Homecoming.Relics
{
    public readonly struct DodgeEchoPulse
    {
        public readonly float RadiusMeters;
        public readonly float DamageFactor;
        public readonly int MaxTargets;
        public readonly float ReadyAgainAt;
        public DodgeEchoPulse(float radiusMeters, float damageFactor, int maxTargets, float readyAgainAt)
        {
            RadiusMeters = radiusMeters; DamageFactor = damageFactor; MaxTargets = maxTargets; ReadyAgainAt = readyAgainAt;
        }
    }

    public readonly struct RelicAbilityView
    {
        public readonly string ItemId;
        public readonly string NarrativeId;
        public readonly string Text;
        public readonly HomecomingModifiers Modifier;
        public readonly bool Owned;
        public readonly bool Equipped;
        public readonly float EchoCooldownRemaining;
        public RelicAbilityView(string itemId, string narrativeId, string text, HomecomingModifiers modifier,
            bool owned, bool equipped, float echoCooldownRemaining)
        {
            ItemId = itemId; NarrativeId = narrativeId; Text = text; Modifier = modifier;
            Owned = owned; Equipped = equipped; EchoCooldownRemaining = echoCooldownRemaining;
        }
    }

    /// <summary>
    /// 成功闪避后的一次残响。只发规格，不搜敌人，也不扣血。
    /// </summary>
    public sealed class DodgeEchoTrigger
    {
        public const float CooldownSeconds = 8f;
        public const float RadiusMeters = 3f;
        public const float DamageFactor = 0.5f;
        public const int MaxTargets = 12;

        float readyAt;

        public void Clear() { readyAt = 0f; }

        public float Remaining(float now) { return Math.Max(0f, readyAt - now); }

        public bool TryConsumeSuccessfulDodge(bool echoEnabled, bool playing, bool alive, float now, out DodgeEchoPulse pulse)
        {
            pulse = default;
            if (!echoEnabled || !playing || !alive || now < readyAt) return false;
            readyAt = now + CooldownSeconds;
            pulse = new DodgeEchoPulse(RadiusMeters, DamageFactor, MaxTargets, readyAt);
            return true;
        }
    }

    public sealed class RelicInventory : IRelicInventory
    {
        public const string SewingTin = "item.sewing-tin";
        public const string WorkGloves = "item.work-gloves";
        public const string OldGuitar = "item.old-guitar";

        readonly string[] equipped = new string[HomecomingContract.MaximumRelicSlots];
        readonly List<string> stored = new List<string>(4);
        readonly HashSet<string> owned = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> claims = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly DodgeEchoTrigger echo = new DodgeEchoTrigger();
        Guid runId;
        int capacity;
        bool ready;

        public void Reset(Guid runId, int startingSlots)
        {
            this.runId = runId;
            capacity = HomecomingContract.StartingRelicSlots;
            ready = runId != Guid.Empty && startingSlots == HomecomingContract.StartingRelicSlots;
            for (int i = 0; i < equipped.Length; i++) equipped[i] = null;
            stored.Clear();
            owned.Clear();
            claims.Clear();
            echo.Clear();
        }

        public bool TryGrant(PoiReward reward)
        {
            if (!ready || reward.RunId != runId || !Id(reward.ClaimId) || !Id(reward.ItemId)) return false;
            string signature = reward.ItemId + "#" + reward.AdditionalSlots.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (claims.TryGetValue(reward.ClaimId, out string prior)) return prior == signature;
            if (!Legal(reward.ItemId, reward.AdditionalSlots)) return false;
            if (owned.Contains(reward.ItemId)) return false;
            if (capacity + reward.AdditionalSlots > HomecomingContract.MaximumRelicSlots) return false;
            capacity += reward.AdditionalSlots;
            owned.Add(reward.ItemId);
            int empty = -1;
            for (int i = 0; i < capacity; i++) if (equipped[i] == null) { empty = i; break; }
            if (empty >= 0) equipped[empty] = reward.ItemId;
            else stored.Add(reward.ItemId);
            claims.Add(reward.ClaimId, signature);
            return true;
        }

        public bool TryEquip(Guid runId, string itemId, int slot)
        {
            if (!ready || runId != this.runId || !owned.Contains(itemId) || slot < 0 || slot >= capacity) return false;
            int from = EquippedIndex(itemId);
            if (from == slot) return true;
            string displaced = equipped[slot];
            if (from >= 0) equipped[from] = null;
            else stored.Remove(itemId);
            equipped[slot] = itemId;
            if (displaced != null) stored.Add(displaced);
            return true;
        }

        public bool TryUnequip(Guid runId, int slot)
        {
            if (!ready || runId != this.runId || slot < 0 || slot >= capacity || equipped[slot] == null) return false;
            stored.Add(equipped[slot]);
            equipped[slot] = null;
            return true;
        }

        public RelicInventoryView Read()
        {
            var slots = new List<RelicSlotView>(capacity);
            for (int i = 0; i < capacity; i++)
                slots.Add(new RelicSlotView(i, equipped[i], equipped[i]));
            return new RelicInventoryView(capacity, slots, stored);
        }

        public HomecomingModifiers ReadModifiers()
        {
            float health = 0f, pickup = 0f;
            bool guitar = false;
            for (int i = 0; i < capacity; i++)
            {
                if (equipped[i] == SewingTin) health += 0.08f;
                else if (equipped[i] == WorkGloves) pickup += 0.15f;
                else if (equipped[i] == OldGuitar) guitar = true;
            }
            return new HomecomingModifiers(health, 0f, pickup, 0f, guitar);
        }

        public bool TryConsumeDodgeEcho(bool playing, bool alive, float now, out DodgeEchoPulse pulse)
        {
            return echo.TryConsumeSuccessfulDodge(ReadModifiers().DodgeEcho, playing, alive, now, out pulse);
        }

        public float DodgeEchoRemaining(float now) { return echo.Remaining(now); }

        public bool TryReadAbility(string itemId, float now, out RelicAbilityView view)
        {
            view = default;
            HomecomingModifiers modifier;
            string text;
            if (itemId == SewingTin) { modifier = new HomecomingModifiers(maxHealth: 0.08f); text = "最大生命 +8%。装备时才生效，拿到时不补满当前生命。"; }
            else if (itemId == WorkGloves) { modifier = new HomecomingModifiers(pickupRadius: 0.15f); text = "拾取范围 +15%。只有装备着才生效。"; }
            else if (itemId == OldGuitar)
            {
                modifier = new HomecomingModifiers(dodgeEcho: true);
                text = "成功闪避后留下一次残响：8秒冷却，半径3米，基础伤害的一半，最多12个目标。";
            }
            else return false;
            bool equippedNow = EquippedIndex(itemId) >= 0;
            float remaining = itemId == OldGuitar && equippedNow ? echo.Remaining(now) : 0f;
            view = new RelicAbilityView(itemId, itemId, text, modifier, owned.Contains(itemId), equippedNow, remaining);
            return true;
        }

        static bool Legal(string itemId, int additionalSlots)
        {
            if (itemId == SewingTin || itemId == WorkGloves) return additionalSlots == 1;
            if (itemId == OldGuitar) return additionalSlots == 0;
            return false;
        }

        int EquippedIndex(string itemId)
        {
            for (int i = 0; i < capacity; i++) if (equipped[i] == itemId) return i;
            return -1;
        }

        static bool Id(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 64) return false;
            foreach (char c in id)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '.' && c != '_' && c != '-')
                    return false;
            return true;
        }
    }
}
