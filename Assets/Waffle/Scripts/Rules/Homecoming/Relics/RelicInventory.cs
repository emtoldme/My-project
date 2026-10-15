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

        public bool TryConsumeSuccessfulDodge(bool echoEnabled, bool playing, bool alive, float now, float cooldownSeconds, float damageFactor, float radius, out DodgeEchoPulse pulse)
        {
            pulse = default;
            if (!echoEnabled || !playing || !alive || now < readyAt) return false;
            float cooldown = cooldownSeconds > 0f ? cooldownSeconds : CooldownSeconds;
            readyAt = now + cooldown;
            pulse = new DodgeEchoPulse(radius, damageFactor, MaxTargets, readyAt);
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
        readonly RelicFxCue[] cues = new RelicFxCue[6];
        Guid runId;
        int capacity, cueCount, echoes, tugs, sips, surges, keyedDodges, verbUses;
        bool ready, mendUsed;
        float tugReadyAt, sipReadyAt, lanternHeat, lanternSurge, playTime, tinTime;

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
            tugReadyAt = sipReadyAt = lanternHeat = lanternSurge = playTime = tinTime = 0f;
            echoes = tugs = sips = surges = keyedDodges = verbUses = cueCount = 0;
        }

        public bool IsUnlocked(string itemId)
        {
            if (itemId == SewingTin || itemId == WorkGloves || itemId == OldGuitar) return true;
            if (itemId == LanternGlass) return echoes >= 1 || mendUsed || playTime >= 90f;
            if (itemId == ClockKey) return surges >= 1 || echoes >= 2;
            if (itemId == RainCup) return verbUses >= 3;
            return false;
        }

        public int RankOf(string itemId)
        {
            if (itemId == SewingTin) return tinTime >= 40f ? 3 : mendUsed ? 2 : 1;
            if (itemId == WorkGloves) return RankFrom(tugs, 3, 6);
            if (itemId == OldGuitar) return RankFrom(echoes, 2, 4);
            if (itemId == LanternGlass) return RankFrom(surges, 2, 4);
            if (itemId == ClockKey) return RankFrom(keyedDodges, 4, 8);
            if (itemId == RainCup) return RankFrom(sips, 2, 4);
            return 0;
        }

        public string UnlockHint(string itemId)
        {
            if (!RelicCatalog.TryGet(itemId, out _)) return "没有这件遗物";
            if (IsUnlocked(itemId)) return "可以领取";
            if (itemId == LanternGlass) return "先打出一次弦响，或用掉一次缝补，或带着遗物行动 90 秒";
            if (itemId == ClockKey) return "让灯芯旺过一次，或再打出两次弦响";
            if (itemId == RainCup) return "让任意遗物动作成功 3 次";
            return "还不能领取";
        }

        public bool TryDequeueFx(out RelicFxCue cue)
        {
            cue = default;
            if (cueCount <= 0) return false;
            cue = cues[0];
            for (int i = 1; i < cueCount; i++) cues[i - 1] = cues[i];
            cueCount--;
            return true;
        }

        public bool TryGrant(PoiReward reward)
        {
            if (!ready || reward.RunId != runId || !Id(reward.ClaimId) || !Id(reward.ItemId)) return false;
            string signature = reward.ItemId + "#" + reward.AdditionalSlots.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (claims.TryGetValue(reward.ClaimId, out string prior)) return prior == signature;
            if (!Legal(reward.ItemId, reward.AdditionalSlots)) return false;
            if (!IsUnlocked(reward.ItemId)) return false;
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
            if (lanternSurge > 0f && EquippedIndex(LanternGlass) >= 0)
                move += surges >= 2 ? 0.05f : LanternMoveBonus;
            return new HomecomingModifiers(health, move, pickup, dodge, guitar);
        }

        public void Advance(float dt, bool playing, bool alive, bool moving)
        {
            if (!ready || !playing || !alive || dt <= 0f) return;
            playTime += dt;
            if (EquippedIndex(SewingTin) >= 0) tinTime += dt;
            if (EquippedIndex(LanternGlass) < 0) { lanternHeat = 0f; lanternSurge = 0f; return; }
            if (lanternSurge > 0f) lanternSurge = Math.Max(0f, lanternSurge - dt);
            lanternHeat = moving ? lanternHeat + dt : Math.Max(0f, lanternHeat - dt * 0.5f);
            float need = surges >= 2 ? 3f : LanternHeatSeconds;
            if (lanternHeat >= need)
            {
                lanternHeat = 0f;
                lanternSurge = LanternSurgeSeconds;
                surges++;
                verbUses++;
                int rank = RankOf(LanternGlass);
                Push(Cue(LanternGlass, rank >= 3 ? "sfx.relic.lantern-glass.chroma" : "sfx.relic.lantern-glass",
                    RelicFxKind.WickFlare, rank, 0.30f, 0f, rank >= 3 ? 0.6f : 0f, 0f, 0.95f, 0.62f, 0.28f));
            }
        }

        public bool TryConsumeDodgeEcho(bool playing, bool alive, float now, out DodgeEchoPulse pulse)
        {
            pulse = default;
            int rank = RankOf(OldGuitar);
            bool key = EquippedIndex(ClockKey) >= 0;
            float cooldown = rank >= 3 ? (key ? 5.5f : 7f) : (key ? EchoCooldownWithKey : DodgeEchoTrigger.CooldownSeconds);
            float damage = rank >= 3 ? 0.65f : rank >= 2 ? 0.6f : 0.5f;
            float radius = rank >= 3 ? 3.4f : DodgeEchoTrigger.RadiusMeters;
            if (!echo.TryConsumeSuccessfulDodge(ReadModifiers().DodgeEcho, playing, alive, now, cooldown, damage, radius, out pulse))
                return false;
            echoes++;
            verbUses++;
            if (key) keyedDodges++;
            var kind = rank >= 3 ? RelicFxKind.ChromaticHit : rank >= 2 ? RelicFxKind.StringTear : RelicFxKind.StringRing;
            string sfx = rank >= 3 ? "sfx.relic.old-guitar.chroma" : rank >= 2 ? "sfx.relic.old-guitar.tear" : "sfx.relic.old-guitar";
            Push(Cue(OldGuitar, sfx, kind, rank, rank >= 3 ? 0.18f : 0.40f, radius, rank >= 3 ? 1.6f : 0f, rank >= 2 ? 1.2f : 0f, 0.72f, 0.42f, 0.24f));
            return true;
        }

        public bool TryNotifyDodge(bool playing, bool alive)
        {
            if (!ready || !playing || !alive || EquippedIndex(ClockKey) < 0) return false;
            keyedDodges++;
            verbUses++;
            int rank = RankOf(ClockKey);
            Push(Cue(ClockKey, "sfx.relic.clock-key", RelicFxKind.KeyTick, rank, 0.12f, 0f, 0f, 0f, 0.55f, 0.58f, 0.52f));
            return true;
        }

        public bool TryConsumeMend(bool playing, bool alive, bool wouldDie, out float healthFraction)
        {
            healthFraction = 0f;
            if (!ready || !playing || !alive || !wouldDie || mendUsed || EquippedIndex(SewingTin) < 0) return false;
            mendUsed = true;
            verbUses++;
            healthFraction = RankOf(SewingTin) >= 3 ? 0.30f : MendFraction;
            Push(Cue(SewingTin, "sfx.relic.sewing-tin", RelicFxKind.MendStitch, RankOf(SewingTin), 0.28f, 0.4f, 0f, 0f, 0.86f, 0.78f, 0.62f));
            return true;
        }

        public bool TryConsumeTug(bool playing, bool alive, float now, out float radius, out int maxGems)
        {
            radius = 0f;
            maxGems = 0;
            if (!ready || !playing || !alive || EquippedIndex(WorkGloves) < 0 || now < tugReadyAt) return false;
            int rank = RankOf(WorkGloves);
            tugReadyAt = now + (rank >= 3 ? 10f : rank >= 2 ? 11f : TugCooldown);
            radius = rank >= 3 ? 3.2f : rank >= 2 ? 2.9f : TugRadius;
            maxGems = rank >= 3 ? 8 : rank >= 2 ? 7 : TugMaxGems;
            tugs++;
            verbUses++;
            Push(Cue(WorkGloves, "sfx.relic.work-gloves", RelicFxKind.GloveTug, rank, 0.22f, radius, 0f, 0f, 0.45f, 0.28f, 0.16f));
            return true;
        }

        public bool TryConsumeSip(bool playing, bool alive, bool missingHealth, float now, out float healthFraction)
        {
            healthFraction = 0f;
            if (!ready || !playing || !alive || !missingHealth || EquippedIndex(RainCup) < 0 || now < sipReadyAt) return false;
            int rank = RankOf(RainCup);
            sipReadyAt = now + (rank >= 3 ? 12f : rank >= 2 ? 14f : SipCooldown);
            healthFraction = rank >= 3 ? 0.08f : rank >= 2 ? 0.07f : SipFraction;
            sips++;
            verbUses++;
            Push(Cue(RainCup, "sfx.relic.rain-cup", RelicFxKind.CupSip, rank, 0.35f, 0f, 0f, 0f, 0.82f, 0.78f, 0.68f));
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
            view = new RelicAbilityView(itemId, itemId, RankText(itemId), detail.Modifier, owned.Contains(itemId),
                equippedNow, echoRemaining, detail.ArtPath, detail.Name, detail.Material, verbReady, verbRemaining);
            return true;
        }

        string RankText(string itemId)
        {
            int rank = RankOf(itemId);
            if (!IsUnlocked(itemId)) return UnlockHint(itemId);
            if (itemId == SewingTin) return "针线盒 " + rank + "阶：最大生命 +8%，致命伤留下 " + (rank >= 3 ? "30%" : "25%") + " 生命。";
            if (itemId == WorkGloves)
            {
                float radius = rank >= 3 ? 3.2f : rank >= 2 ? 2.9f : 2.5f;
                int gems = rank >= 3 ? 8 : rank >= 2 ? 7 : 6;
                return "工作手套 " + rank + "阶：拾取 +15%，揽取 " + radius.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " 米内最多 " + gems + " 颗。";
            }
            if (itemId == OldGuitar)
            {
                float damage = rank >= 3 ? 0.65f : rank >= 2 ? 0.6f : 0.5f;
                return "旧吉他 " + rank + "阶：弦响伤害 " + damage.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " 倍，" + (rank >= 3 ? "带撕裂和色差。" : rank >= 2 ? "带一条撕裂。" : "只有弦响。");
            }
            if (itemId == LanternGlass) return "裂罩灯 " + rank + "阶：移速 +6%，走顺后再 +" + (rank >= 2 ? "5%" : "4%") + "。" + (rank >= 3 ? "旺起来时有轻微色差。" : "");
            if (itemId == ClockKey) return "钟钥匙 " + rank + "阶：闪避冷却缩短 8%。和吉他一起时弦响冷却更短。";
            if (itemId == RainCup) return "搪瓷杯 " + rank + "阶：没满血时回复 " + (rank >= 3 ? "8%" : rank >= 2 ? "7%" : "6%") + " 最大生命。";
            return "";
        }

        void Push(RelicFxCue cue)
        {
            if (cueCount >= cues.Length) return;
            cues[cueCount++] = cue;
        }

        static RelicFxCue Cue(string item, string sfx, RelicFxKind kind, int rank, float duration, float radius,
            float chromatic, float tear, float r, float g, float b)
        {
            return new RelicFxCue(item, sfx, kind, rank, duration, radius, chromatic, tear, r, g, b);
        }

        static int RankFrom(int uses, int second, int third)
        {
            return uses >= third ? 3 : uses >= second ? 2 : 1;
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
