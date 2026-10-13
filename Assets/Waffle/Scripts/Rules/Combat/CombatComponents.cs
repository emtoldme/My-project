using System.Collections.Generic;
using Waffle.RunContent.Components;

namespace Waffle.Combat
{
    public sealed partial class CombatModule
    {
        RunComponentRuntime components;
        readonly List<EnemySnapshot> componentEnemies = new List<EnemySnapshot>(64);
        readonly List<RuleCommand> componentCommands = new List<RuleCommand>(64);
        public IReadOnlyList<ResidualZoneField.Point> RunTrails => components?.TrailPoints;
        public bool RunTrailLinks => runGrowth != null && runGrowth.RankOf("W06") >= 3;
        public float RunTrailRadius => runGrowth == null ? 0 : runStats.FinalRadius(runGrowth.Catalog.Get("W06").RankAt(runGrowth.RankOf("W06"))?.Get("radius") ?? 0);
        public void NotifyRunDodge(Vec2 at) { components?.NotifyDodge(at); }
        public void RebaseRunMovement(Vec2 at) { components?.RebaseMovement(at); }

        void StepRunComponents(CombatInput input)
        {
            if (components == null) return;
            componentEnemies.Clear(); componentCommands.Clear();
            foreach (var e in enemies) if (e.IsAlive)
                componentEnemies.Add(new EnemySnapshot(e.Serial, e.Generation, e.Position, e.Radius, e.IsElite));
            components.Advance(new ComponentAdvanceInput { Now = now, Dt = input.Dt, PlayerPos = player,
                MaxLegalStep = input.MaxLegalMovementStep }, componentEnemies, componentCommands);
            foreach (var c in componentCommands)
            {
                if (c.Kind == RuleCommandKind.TargetDamage)
                {
                    if (!TryGetEnemy(c.Serial, c.Generation, out var target)) continue;
                    Counters.Hits++;
                    ApplyEnemyDamage(target, c.Damage, PresentationEventType.EnemyHit, c.From, c.ContentId, c.Slot,
                        c.RootId, c.AllowsMechanism, c.ChainId, c.Depth, c.BudgetLeft);
                }
                else if (c.Kind == RuleCommandKind.AreaDamage)
                {
                    DamageArea(c.Position, c.Radius, c.Damage, c.ContentId, c.Slot, c.BudgetLeft, allowMechanisms: false);
                    EmitRun(PresentationEventType.WeaponZone, c.Position, c.ContentId, c.Slot, c.Damage, c.Radius, .3f);
                }
                else if (c.Kind == RuleCommandKind.ZoneSpawned)
                    EmitRun(PresentationEventType.WeaponZone, c.Position, c.ContentId, c.Slot, 0, c.Radius, c.Duration);
                // W01's migration sample remains opt-in; its existing projectile path stays the only live owner.
            }
        }
    }
}
