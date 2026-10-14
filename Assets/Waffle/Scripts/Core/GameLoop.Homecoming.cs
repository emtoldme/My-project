using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Waffle.Homecoming;
using Waffle.Homecoming.Places;
using Waffle.Homecoming.Profile;
using Waffle.Homecoming.Relics;

namespace Waffle
{
    public partial class GameLoop : IHomecomingViewSource, IHomecomingCommands
    {
        public bool homecomingEnabled;
        public HomecomingProfile HomecomingProfile { get; private set; }
        public bool BeforeDeparture { get; private set; }
        public bool HomecomingJournalOpen { get; private set; }
        public bool CanPurchaseHomecoming => BeforeDeparture || (Sim != null && Sim.State.Phase.IsTerminal() && settlementSaved);
        public bool HomecomingSaveFailed { get; private set; }
        public string HomecomingSaveMessage { get; private set; } = "";
        public SettlementReceipt? HomecomingReceipt { get; private set; }
        public string HomecomingSaveDirectory { get; private set; }
        bool journalResume, settlementAttempted, settlementSaved;
        int attemptedMemoryCount = -1;
        string pendingPurchase;
        readonly HashSet<string> viewedMemories = new HashSet<string>(StringComparer.Ordinal);

        void PrepareHomecomingProfile()
        {
            if (!homecomingEnabled) return;
            HomecomingSaveDirectory = Environment.GetEnvironmentVariable("REVERB_HC_REVIEW_SAVE");
            if (string.IsNullOrWhiteSpace(HomecomingSaveDirectory)) HomecomingSaveDirectory = Path.Combine(Application.persistentDataPath, "Homecoming");
            HomecomingProfile = new HomecomingProfile(new ProfileFileStorage(HomecomingSaveDirectory));
            HomecomingSaveMessage = HomecomingProfile.LoadStatus == ProfileLoadStatus.RecoveredFromBackup ? "已从备份恢复故乡档案"
                : HomecomingProfile.IsReadOnly ? "档案来自较新版本，本次只读，不会覆盖"
                : HomecomingProfile.LoadStatus == ProfileLoadStatus.CorruptedAllPreserved ? "档案损坏副本已保留，已建立新档案" : "故乡档案已就绪";
        }
        void PrepareHomecomingRun()
        {
            if (!homecomingEnabled) return;
            BeforeDeparture = true; HomecomingJournalOpen = true;
            Sim.SetHomecomingStartingModifiers(HomecomingProfile.ReadStartingModifiers(), true);
            Sim.RequestPause("出发前整理行囊");
        }
        public void InstallHomecomingWorld(int seed)
        {
            if (homecomingEnabled) Sim.InstallHomecoming(seed);
        }
        public bool StartHomecomingJourney()
        {
            if (!BeforeDeparture || Sim?.Places == null || HomecomingSaveFailed) return false;
            Sim.SetHomecomingStartingModifiers(HomecomingProfile.ReadStartingModifiers(), true);
            BeforeDeparture = false; HomecomingJournalOpen = false; journalResume = false;
            Sim.TogglePause("沿田间路出发"); SyncAfterStateChange(); return true;
        }
        public bool TryOpenJournal()
        {
            if (!homecomingEnabled || Sim == null || Sim.State.Phase == RunPhase.LevelUp) return false;
            if (HomecomingJournalOpen) return true;
            journalResume = Sim.State.Phase == RunPhase.Playing;
            HomecomingJournalOpen = true;
            if (journalResume) { Sim.RequestPause("翻看故乡日志"); SyncAfterStateChange(); }
            return true;
        }
        public void CloseJournal()
        {
            if (BeforeDeparture) return;
            HomecomingJournalOpen = false;
            if (journalResume && Sim.State.Phase == RunPhase.Paused) { Sim.TogglePause("合上故乡日志"); SyncAfterStateChange(); }
            journalResume = false;
        }
        public IReadOnlyList<PoiView> ReadHomecomingPlaceVisuals()
        {
            if (Sim?.Places == null) return Array.Empty<PoiView>();
            var views = new List<PoiView>();
            foreach (var site in HomecomingPlacesLayout.SitesNear(Sim.HomecomingWorldSeed, new Combat.Vec2(Sim.PlayerX, Sim.PlayerZ), 65, 8))
                views.Add(new PoiView(site, Sim.Places.StatusOf(site.SiteId), HomecomingPlaceCatalog.RewardDescription(site.TemplateId)));
            return views.AsReadOnly();
        }
        public IReadOnlyList<PoiView> ReadNearby() => Sim?.Places?.ReadNearby() ?? Array.Empty<PoiView>();
        public RelicInventoryView ReadRelics() => Sim?.Relics?.Read() ?? new RelicInventoryView(2, null, null);
        public IReadOnlyList<GoalView> ReadGoals() => Sim?.HomecomingGoals?.Read() ?? Array.Empty<GoalView>();
        public HomecomingProfileView ReadProfile()
        {
            var view = HomecomingProfile?.Read(); if (view == null) return null;
            var memories = new HashSet<string>(view.DiscoveredNarrativeIds, StringComparer.Ordinal);
            if (Sim != null) foreach (var id in Sim.HomecomingMemories) memories.Add(id);
            foreach (var goal in ReadGoals()) memories.Add(goal.NarrativeId);
            foreach (var up in view.Upgrades) memories.Add(up.NarrativeId);
            var sorted = new List<string>(memories); sorted.Sort(StringComparer.Ordinal);
            return new HomecomingProfileView(view.TotalExperience, view.Level, view.AvailablePoints, view.Upgrades, sorted);
        }
        public NarrativeEntry ReadNarrative(string id) => HomecomingNarrativeCatalog.Default.Resolve(id);
        public bool TryViewHomecomingNarrative(string id)
        {
            if (!HomecomingJournalOpen || ReadNarrative(id) == null) return false;
            bool known = false; foreach (var entry in ReadProfile().DiscoveredNarrativeIds) if (entry == id) { known = true; break; }
            if (!known) return false;
            Sim.RememberHomecoming(id);
            if (viewedMemories.Add(id)) Sim.ViewHomecomingNarrative(id);
            return true;
        }
        public bool TryInteract(Guid runId, string siteId) => !BeforeDeparture && Sim != null && Sim.TryClaimHomecoming(runId, siteId);
        public bool TryEquip(Guid runId, string itemId, int slot) => !BeforeDeparture && Sim != null && Sim.EquipHomecoming(runId, itemId, slot, journalResume);
        public bool TryUnequip(Guid runId, int slot) => !BeforeDeparture && Sim != null && Sim.UnequipHomecoming(runId, slot, journalResume);
        public bool CanRetuneHomecoming => !BeforeDeparture && Sim != null && Sim.CanUseHomecomingCamp(Sim.RunId, journalResume);
        public bool TryRetuneHomecoming(ResonancePattern pattern) => CanRetuneHomecoming && Sim.TryRetuneHomecoming(Sim.RunId, pattern, journalResume);
        public bool TryRekindleHomecoming() => CanRetuneHomecoming && Sim.TryRekindleHomecoming(Sim.RunId, journalResume);
        public bool TryPurchaseUpgrade(string upgradeId)
        {
            if (!CanPurchaseHomecoming || HomecomingProfile.IsReadOnly) return false;
            bool ok = HomecomingProfile.TryPurchase(upgradeId);
            bool diskFailed = !ok && HomecomingProfile.StatusMessage.StartsWith("Purchase save failed", StringComparison.Ordinal);
            if (diskFailed) pendingPurchase = upgradeId;
            if (ok) { pendingPurchase = null; HomecomingSaveFailed = false; HomecomingSaveMessage = "修补已保存，下次出发生效"; }
            else if (diskFailed) { HomecomingSaveFailed = true; HomecomingSaveMessage = "修补保存失败，点数未扣；请重试"; }
            return ok;
        }
        void UpdateHomecomingPersistence()
        {
            if (!homecomingEnabled || Sim.Places == null) return;
            if (Sim.Outcome != null && !settlementAttempted)
            {
                settlementAttempted = true; HomecomingJournalOpen = true; journalResume = false;
                SaveHomecomingSettlement();
            }
            else if (Sim.Outcome == null && Sim.HomecomingMemories.Count != attemptedMemoryCount)
                SaveHomecomingMemories();
        }
        bool SaveHomecomingMemories()
        {
            attemptedMemoryCount = Sim.HomecomingMemories.Count;
            if (HomecomingProfile.IsReadOnly) return true;
            bool ok = HomecomingProfile.TrySaveDiscoveredNarratives(Sim.HomecomingMemories);
            HomecomingSaveFailed = !ok;
            HomecomingSaveMessage = ok ? "记忆已保存" : "记忆保存失败，请重试；本次发现仍保留在行囊中";
            return ok;
        }
        bool SaveHomecomingSettlement()
        {
            var settlement = Sim.ReadHomecomingSettlement(); if (settlement == null) return true;
            if (HomecomingProfile.IsReadOnly) { settlementSaved = true; return true; }
            bool ok = HomecomingProfile.TrySettle(settlement, out var receipt);
            HomecomingSaveFailed = !ok;
            if (ok) { settlementSaved = true; HomecomingReceipt = receipt; HomecomingSaveMessage = "此行所得已保存"; }
            else HomecomingSaveMessage = "结算保存失败，所得尚未入档；请重试后再出发";
            return ok;
        }
        public bool FlushHomecoming()
        {
            if (!homecomingEnabled || Sim?.Places == null) return true;
            if (pendingPurchase != null && !TryPurchaseUpgrade(pendingPurchase)) return false;
            return Sim.Outcome != null ? SaveHomecomingSettlement() : SaveHomecomingMemories();
        }
        void OnApplicationQuit() { if (homecomingEnabled) FlushHomecoming(); }
    }
}
