using System;
using System.Collections.Generic;
using Waffle.Combat;
using Waffle.Homecoming;
using Waffle.Homecoming.Places;
using Waffle.Homecoming.Relics;

namespace Waffle
{
    public partial class RunSimulation
    {
        public PoiModule Places { get; private set; }
        public RelicInventory Relics { get; private set; }
        public HomecomingGoalsModule HomecomingGoals { get; private set; }
        public int HomecomingWorldSeed { get; private set; }
        public int HomecomingEchoes { get; private set; }
        readonly HashSet<string> homecomingMemories = new HashSet<string>(StringComparer.Ordinal);
        HomecomingModifiers homecomingStarting;
        int homecomingFactSequence;
        bool homecomingEnded;
        Vec2 lastHomecomingRegistration;

        public void InstallHomecoming(int worldSeed)
        {
            if (Places != null) return;
            HomecomingWorldSeed = worldSeed;
            Places = new PoiModule(); Places.Reset(RunId, worldSeed);
            Relics = new RelicInventory(); Relics.Reset(RunId, 2);
            HomecomingGoals = new HomecomingGoalsModule(); HomecomingGoals.Reset(RunId);
            RegisterHomecomingPlaces();
            RememberHomecoming("W01");
        }
        public void SetHomecomingStartingModifiers(HomecomingModifiers modifiers, bool fillBeforeDeparture = false)
        {
            if (State.Now > 0) return;
            homecomingStarting = modifiers;
            RefreshHomecomingHealth();
            if (fillBeforeDeparture) State.Hp = State.MaxHp;
        }
        public IReadOnlyList<string> HomecomingMemories => new List<string>(homecomingMemories).AsReadOnly();
        public bool RememberHomecoming(string id)
        {
            if (id == null || HomecomingNarrativeCatalog.Default.Resolve(id) == null) return false;
            return homecomingMemories.Add(id);
        }
        void HomecomingFact(HomecomingEventKind kind, string subject)
        {
            HomecomingGoals?.Observe(new HomecomingFact(RunId, (++homecomingFactSequence).ToString(), kind, subject));
            if (HomecomingGoals != null) foreach (var goal in HomecomingGoals.Read())
                if (goal.Completed) { RememberHomecoming(goal.Id); RememberHomecoming(goal.Id + ".complete"); }
        }
        public void ViewHomecomingNarrative(string id)
        {
            if (homecomingMemories.Contains(id)) HomecomingFact(HomecomingEventKind.NarrativeViewed, id);
        }
        void RegisterHomecomingPlaces()
        {
            var player = new Vec2(PlayerX, PlayerZ);
            foreach (var site in HomecomingPlacesLayout.SitesNear(HomecomingWorldSeed, player, 100)) Places.Register(site);
            foreach (var site in HomecomingPlacesLayout.UniqueSites(HomecomingWorldSeed)) Places.Register(site);
            lastHomecomingRegistration = player;
        }
        ExplorationContext HomecomingContext => new ExplorationContext(RunId, HomecomingWorldSeed, State.Now,
            LastDt, new Vec2(PlayerX, PlayerZ), State.Phase, State.Hp > 0);
        public void StepHomecoming()
        {
            if (Places == null) return;
            if (State.Phase.IsTerminal())
            {
                if (!homecomingEnded) { homecomingEnded = true; HomecomingFact(HomecomingEventKind.RunEnded, State.EndReason); }
                return;
            }
            if (!HomecomingContext.CanInteract) return;
            if (Vec2.SqrDistance(lastHomecomingRegistration, new Vec2(PlayerX, PlayerZ)) > 144) RegisterHomecomingPlaces();
            Places.Step(HomecomingContext);
            foreach (var id in Places.DiscoveredNarrativeIds())
                if (RememberHomecoming(id)) HomecomingFact(HomecomingEventKind.SiteDiscovered, id);
            if (Places.TrySelectAutoClaim(HomecomingContext, out string siteId)) TryClaimHomecoming(RunId, siteId);
            var content = ReadContentView();
            if (content != null)
            {
                foreach (var item in content.Weapons) RememberHomecoming(item.Id);
                foreach (var item in content.Passives) RememberHomecoming(item.Id);
            }
            foreach (var e in Presentation.Events) if (e.SourceId != null) RememberHomecoming(e.SourceId);
        }
        public bool TryClaimHomecoming(Guid runId, string siteId)
        {
            if (runId != RunId || Places == null || !Places.TryPrepareClaim(HomecomingContext, siteId, out var reward)) return false;
            bool accepted = Relics.TryGrant(reward); Places.ConfirmClaim(RunId, siteId, accepted);
            if (!accepted) return false;
            RememberHomecoming(reward.ItemId);
            HomecomingFact(HomecomingEventKind.SiteClaimed, reward.ItemId);
            foreach (var slot in Relics.Read().Slots) if (slot.ItemId == reward.ItemId) HomecomingFact(HomecomingEventKind.ItemEquipped, slot.ItemId);
            RefreshHomecomingHealth();
            Places.Step(HomecomingContext);
            return true;
        }
        public bool EquipHomecoming(Guid runId, string id, int slot, bool journalOwnsPause)
        {
            if (!CanChangeHomecomingEquipment(runId, journalOwnsPause) || !Relics.TryEquip(runId, id, slot)) return false;
            RefreshHomecomingHealth(); HomecomingFact(HomecomingEventKind.ItemEquipped, id); return true;
        }
        public bool UnequipHomecoming(Guid runId, int slot, bool journalOwnsPause)
        {
            if (!CanChangeHomecomingEquipment(runId, journalOwnsPause) || !Relics.TryUnequip(runId, slot)) return false;
            RefreshHomecomingHealth(); return true;
        }
        bool CanChangeHomecomingEquipment(Guid id, bool ownsPause) => Relics != null && id == RunId && State.Hp > 0
            && (State.Phase == RunPhase.Playing || (ownsPause && State.Phase == RunPhase.Paused));
        void RefreshHomecomingHealth()
        {
            State.MaxHp = Math.Max(1, (int)Math.Round(CurrentValue(UpgradeKind.MaxHp)));
            State.Hp = Math.Min(State.Hp, State.MaxHp);
        }
        float HomecomingFactor(UpgradeKind kind)
        {
            var r = Relics?.ReadModifiers() ?? default;
            if (kind == UpgradeKind.MaxHp) return 1 + ClampBonus(r.MaxHealth + homecomingStarting.MaxHealth, .25f);
            if (kind == UpgradeKind.MoveSpeed) return 1 + ClampBonus(r.MoveSpeed + homecomingStarting.MoveSpeed, .12f);
            if (kind == UpgradeKind.PickupRange) return 1 + ClampBonus(r.PickupRadius + homecomingStarting.PickupRadius, .4f);
            if (kind == UpgradeKind.DodgeCooldown) return 1 - ClampBonus(r.DodgeCooldownReduction + homecomingStarting.DodgeCooldownReduction, .15f);
            return 1;
        }
        static float ClampBonus(float value, float max) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Math.Max(0, Math.Min(max, value));
        void QueueHomecomingDodge()
        {
            if (Combat == null || Relics == null || !Relics.TryConsumeDodgeEcho(State.Phase == RunPhase.Playing, State.Hp > 0, State.Now, out var pulse)) return;
            Combat.QueueHomecomingEcho(new Vec2(PlayerX, PlayerZ), pulse.RadiusMeters, Config.damage * pulse.DamageFactor, pulse.MaxTargets);
            HomecomingEchoes++;
            HomecomingFact(HomecomingEventKind.SpecialEffectTriggered, RelicInventory.OldGuitar);
        }
        public ExplorationSettlement ReadHomecomingSettlement()
        {
            if (Outcome == null || Places == null) return null;
            var completed = new List<string>();
            foreach (var goal in HomecomingGoals.Read()) if (goal.Completed) completed.Add(goal.Id);
            return new ExplorationSettlement(RunId, Outcome.Phase, Outcome.Duration, Outcome.BossDefeated,
                HomecomingMemories, Places.ClaimedTemplateIds(), completed);
        }
    }
}
