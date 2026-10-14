using System;
using Waffle.Combat;
using Waffle.Homecoming.Places;
using Waffle.Homecoming.Relics;

namespace Waffle
{
    public partial class RunSimulation
    {
        public ResonancePattern Resonance { get; private set; }
        public int FieldFragments { get; private set; }
        public int FragmentProgress { get; private set; }
        public bool ReturnNeedleActive { get; private set; }
        public Vec2 ReturnNeedle { get; private set; }
        public float ReturnNeedleUntil { get; private set; }
        public bool HearthGuard { get; private set; }
        public float CampReadyAt { get; private set; }
        public int CampVisits { get; private set; }
        public Vec2 LastCampPosition { get; private set; }
        public float CampSignalUntil { get; private set; }
        public const int ExperiencePerFragment = 6;
        public const int MaximumFieldFragments = 3;
        bool campCuePending;

        void CollectFieldFragments(int xp)
        {
            if (Relics == null || xp <= 0 || FieldFragments >= MaximumFieldFragments) return;
            long progress = (long)FragmentProgress + xp;
            int gained = (int)Math.Min(MaximumFieldFragments - FieldFragments, progress / ExperiencePerFragment);
            FieldFragments += gained;
            FragmentProgress = FieldFragments >= MaximumFieldFragments ? 0 : (int)(progress % ExperiencePerFragment);
        }
        void LoseFieldFragment()
        { if (FieldFragments > 0) FieldFragments--; FragmentProgress = 0; }
        void RefreshResonance()
        {
            if (!ResonancePatterns.Available(Relics, Resonance)) { Resonance = ResonancePattern.Unlinked; ReturnNeedleActive = false; }
            if (ReturnNeedleActive && (State.Now >= ReturnNeedleUntil || Vec2.SqrDistance(ReturnNeedle, new Vec2(PlayerX, PlayerZ)) > 24 * 24)) ReturnNeedleActive = false;
        }
        public bool AtHomecomingCamp(out Waffle.Homecoming.PoiSite site)
        {
            site = default; if (Places == null) return false;
            foreach (var candidate in HomecomingPlacesLayout.SitesNear(HomecomingWorldSeed, new Vec2(PlayerX, PlayerZ), 6))
            {
                var status = Places.StatusOf(candidate.SiteId);
                if (status == Waffle.Homecoming.PoiStatus.Unseen) continue;
                // Story landmarks must be claimed; ordinary landmarks only need discovery.
                if (HomecomingPlaceCatalog.IsUnique(candidate.TemplateId) && status != Waffle.Homecoming.PoiStatus.Claimed) continue;
                site = candidate; return true;
            }
            return false;
        }
        public bool CanUseHomecomingCamp(Guid id, bool journalOwnsPause)
            => id == RunId && Relics != null && State.Hp > 0 && (State.Phase == RunPhase.Playing
                || (journalOwnsPause && State.Phase == RunPhase.Paused)) && AtHomecomingCamp(out _);
        public bool TryRetuneHomecoming(Guid id, ResonancePattern pattern, bool journalOwnsPause)
        {
            if (!CanUseHomecomingCamp(id, journalOwnsPause) || !ResonancePatterns.Available(Relics, pattern)) return false;
            Resonance = pattern; ReturnNeedleActive = false;
            RememberHomecoming(ResonancePatterns.Id(pattern));
            return true; // Retuning never resets the shared eight-second ability cooldown.
        }
        public bool TryRekindleHomecoming(Guid id, bool journalOwnsPause)
        {
            if (!CanUseHomecomingCamp(id, journalOwnsPause) || FieldFragments < 3 || State.Now < CampReadyAt
                || (HearthGuard && State.Hp >= State.MaxHp) || !AtHomecomingCamp(out var site)) return false;
            FieldFragments -= 3; FragmentProgress = 0; CampReadyAt = State.Now + 30;
            State.Hp = Math.Min(State.MaxHp, State.Hp + (int)Math.Ceiling(State.MaxHp * .15f));
            HearthGuard = true; CampVisits++; LastCampPosition = site.Position; CampSignalUntil = State.Now + 2;
            RememberHomecoming(site.TemplateId + ".rekindled");
            campCuePending = true;
            return true;
        }
        bool BlockWithHearth(float now, int amount)
        {
            if (!HearthGuard) return false;
            HearthGuard = false; InvulnerableUntil = Math.Max(InvulnerableUntil, now + .4f);
            Emit(PresentationEventType.ShieldBlock, PlayerX, PlayerZ, amount, State.Hp); return true;
        }
        void QueueResonanceDodge()
        {
            RefreshResonance();
            bool enabled = Resonance != ResonancePattern.Unlinked || Relics.ReadModifiers().DodgeEcho;
            if (Resonance == ResonancePattern.HearthGather && FieldFragments == 0) return;
            if (Resonance == ResonancePattern.Backstitch && ReturnNeedleActive && FieldFragments == 0) return;
            if (!Relics.TryConsumeResonanceDodge(enabled, State.Phase == RunPhase.Playing, State.Hp > 0, State.Now, out var pulse)) return;
            var origin = new Vec2(PlayerX, PlayerZ);
            if (Resonance == ResonancePattern.HearthGather)
            {
                FieldFragments--;
                Combat.MagnetizeHomecomingGems(origin, 8, 12);
                Emit(PresentationEventType.EchoHit, PlayerX, PlayerZ, 0, 0);
                return;
            }
            if (Resonance == ResonancePattern.Backstitch)
            {
                if (!ReturnNeedleActive)
                {
                    ReturnNeedleActive = true; ReturnNeedle = origin; ReturnNeedleUntil = State.Now + 24;
                    return;
                }
                FieldFragments--; PlayerX = ReturnNeedle.X; PlayerZ = ReturnNeedle.Y;
                ReturnNeedle = origin; ReturnNeedleUntil = State.Now + 24;
                Combat.RebaseHomecomingReturn(new Vec2(PlayerX, PlayerZ));
            }
            else if (Resonance == ResonancePattern.ThreadLure && FieldFragments > 0)
            {
                FieldFragments--; Combat.SetHomecomingLure(origin, State.Now, 2.5f, 6, 12);
            }
            Combat.QueueHomecomingEcho(new Vec2(PlayerX, PlayerZ), pulse.RadiusMeters, Config.damage * pulse.DamageFactor, pulse.MaxTargets);
            HomecomingEchoes++;
            HomecomingFact(Waffle.Homecoming.HomecomingEventKind.SpecialEffectTriggered, RelicInventory.OldGuitar);
        }
        void ClearResonanceForEnding()
        {
            ReturnNeedleActive = false; FieldFragments = FragmentProgress = 0; HearthGuard = false;
            campCuePending = false;
            Combat?.ClearHomecomingLure();
        }
        void DispatchCampCue()
        {
            if (!campCuePending) return;
            campCuePending = false;
            if (HearthGuard) Emit(PresentationEventType.ShieldReady, LastCampPosition.X, LastCampPosition.Y, 1, 0);
        }
    }
}
