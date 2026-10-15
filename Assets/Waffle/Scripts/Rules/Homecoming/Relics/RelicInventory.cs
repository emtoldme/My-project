using System;
using System.Collections.Generic;
using Waffle.Homecoming;

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
        public readonly string ArtPath, Name, Material;
        public readonly bool VerbReady;
        public readonly float VerbRemaining;
        public RelicAbilityView(string itemId, string narrativeId, string text, HomecomingModifiers modifier,
            bool owned, bool equipped, float echoCooldownRemaining, string artPath, string name, string material,
            bool verbReady, float verbRemaining)
        {
            ItemId = itemId; NarrativeId = narrativeId; Text = text; Modifier = modifier;
            Owned = owned; Equipped = equipped; EchoCooldownRemaining = echoCooldownRemaining;
            ArtPath = artPath; Name = name; Material = material; VerbReady = verbReady; VerbRemaining = verbRemaining;
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

        public bool TryConsumeSuccessfulDodge(bool echoEnabled, bool playing, bool alive, float now, float cooldownSeconds, out DodgeEchoPulse pulse)
        {
            pulse = default;
            if (!echoEnabled || !playing || !alive || now < readyAt) return false;
            float cooldown = cooldownSeconds > 0f ? cooldownSeconds : CooldownSeconds;
            readyAt = now + cooldown;
            pulse = new DodgeEchoPulse(RadiusMeters, DamageFactor, MaxTargets, readyAt);
            return true;
        }
    }

    public sealed class RelicInventory : IRelicInventory
    {
        public const string SewingTin = "item.sewing-tin";
        public const string WorkGloves = "item.work-gloves";
        public const string OldGuitar = "item.old-guitar";
        public const string LanternGlass = "item.lantern-glass";
        public const string ClockKey = "item.clock-key";
        public const string RainCup = "item.rain-cup";
        public const float LanternMoveBonus = 0.04f;
        public const float LanternHeatSeconds = 4f;
        public const float LanternSurgeSeconds = 3f;
        public const float EchoCooldownWithKey = 6.5f;
        public const float MendFraction = 0.25f;
        public const float SipFraction = 0.06f;
        public const float SipCooldown = 16f;
        public const float TugCooldown = 12f;
        public const float TugRadius = 2.5f;
        public const int TugMaxGems = 6;

        readonly string[] equipped = new string[HomecomingContract.MaximumRelicSlots];
        readonly List<string> stored = new List<string>(4);
        readonly HashSet<string> owned = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> claims = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly DodgeEchoTrigger echo = new DodgeEchoTrigger();
        Guid runId;
        int capacity;
        bool ready, mendUsed;
        float tugReadyAt, sipReadyAt, lanternHeat, lanternSurge;

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
            mendUsed = false;
            tugReadyAt = sipReadyAt = lanternHeat = lanternSurge = 0f;
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
            if (EquippedIndex(LanternGlass) < 0) { lanternHeat = 0f; lanternSurge = 0f; }
            return true;
        }

        public bool TryUnequip(Guid runId, int slot)
        {
            if (!ready || runId != this.runId || slot < 0 || slot >= capacity || equipped[slot] == null) return false;
            stored.Add(equipped[slot]);
            equipped[slot] = null;
            if (EquippedIndex(LanternGlass) < 0) { lanternHeat = 0f; lanternSurge = 0f; }
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
            float health = 0f, move = 0f, pickup = 0f, dodge = 0f;
            bool guitar = false;
            for (int i = 0; i < capacity; i++)
            {
                if (!RelicCatalog.TryGet(equipped[i], out RelicDetail detail)) continue;
                health += detail.Modifier.MaxHealth;
                move += detail.Modifier.MoveSpeed;
                pickup += detail.Modifier.PickupRadius;
                dodge += detail.Modifier.DodgeCooldownReduction;
                guitar |= detail.Modifier.DodgeEcho;
            }
            if (lanternSurge > 0f && EquippedIndex(LanternGlass) >= 0) move += LanternMoveBonus;
            return new HomecomingModifiers(health, move, pickup, dodge, guitar);
        }

        public void Advance(float dt, bool playing, bool alive, bool moving)
        {
            if (!ready || !playing || !alive || dt <= 0f) return;
            if (EquippedIndex(LanternGlass) < 0) { lanternHeat = 0f; lanternSurge = 0f; return; }
            if (lanternSurge > 0f) lanternSurge = Math.Max(0f, lanternSurge - dt);
            lanternHeat = moving ? lanternHeat + dt : Math.Max(0f, lanternHeat - dt * 0.5f);
            if (lanternHeat >= LanternHeatSeconds)
            {
                lanternHeat = 0f;
                lanternSurge = LanternSurgeSeconds;
            }
        }

        public bool TryConsumeDodgeEcho(bool playing, bool alive, float now, out DodgeEchoPulse pulse)
        {
            float cooldown = EquippedIndex(ClockKey) >= 0 ? EchoCooldownWithKey : DodgeEchoTrigger.CooldownSeconds;
            return echo.TryConsumeSuccessfulDodge(ReadModifiers().DodgeEcho, playing, alive, now, cooldown, out pulse);
        }

        public bool TryConsumeMend(bool playing, bool alive, bool wouldDie, out float healthFraction)
        {
            healthFraction = 0f;
            if (!ready || !playing || !alive || !wouldDie || mendUsed || EquippedIndex(SewingTin) < 0) return false;
            mendUsed = true;
            healthFraction = MendFraction;
            return true;
        }

        public bool TryConsumeTug(bool playing, bool alive, float now, out float radius, out int maxGems)
        {
            radius = 0f;
            maxGems = 0;
            if (!ready || !playing || !alive || EquippedIndex(WorkGloves) < 0 || now < tugReadyAt) return false;
            tugReadyAt = now + TugCooldown;
            radius = TugRadius;
            maxGems = TugMaxGems;
            return true;
        }

        public bool TryConsumeSip(bool playing, bool alive, bool missingHealth, float now, out float healthFraction)
        {
            healthFraction = 0f;
            if (!ready || !playing || !alive || !missingHealth || EquippedIndex(RainCup) < 0 || now < sipReadyAt) return false;
            sipReadyAt = now + SipCooldown;
            healthFraction = SipFraction;
            return true;
        }

        public float DodgeEchoRemaining(float now) { return echo.Remaining(now); }

        public bool TryReadAbility(string itemId, float now, out RelicAbilityView view)
        {
            view = default;
            if (!RelicCatalog.TryGet(itemId, out RelicDetail detail)) return false;
            bool equippedNow = EquippedIndex(itemId) >= 0;
            float echoRemaining = itemId == OldGuitar && equippedNow ? echo.Remaining(now) : 0f;
            float verbRemaining = 0f;
            bool verbReady = false;
            if (equippedNow)
            {
                if (itemId == SewingTin) verbReady = !mendUsed;
                else if (itemId == WorkGloves) { verbRemaining = Math.Max(0f, tugReadyAt - now); verbReady = verbRemaining <= 0f; }
                else if (itemId == OldGuitar) { verbRemaining = echoRemaining; verbReady = echoRemaining <= 0f; }
                else if (itemId == LanternGlass) { verbRemaining = lanternSurge; verbReady = lanternSurge > 0f; }
                else if (itemId == ClockKey) verbReady = true;
                else if (itemId == RainCup) { verbRemaining = Math.Max(0f, sipReadyAt - now); verbReady = verbRemaining <= 0f; }
            }
            view = new RelicAbilityView(itemId, itemId, detail.Ability, detail.Modifier, owned.Contains(itemId),
                equippedNow, echoRemaining, detail.ArtPath, detail.Name, detail.Material, verbReady, verbRemaining);
            return true;
        }

        static bool Legal(string itemId, int additionalSlots)
        {
            return RelicCatalog.TryGet(itemId, out RelicDetail detail) && detail.AdditionalSlots == additionalSlots;
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
