using System;
using System.Collections.Generic;
using Reverb.Experiments.Mechanism.Rules;
using Waffle;
using Waffle.Combat;

namespace Reverb.Experiments.Mechanism.Integration
{
    // Rule host has no HP copy and no independent clock. Only CombatModule mutates HP/death.
    public sealed class M01CombatBridge : ICombatRuleExtension, IMechanismHost
    {
        readonly CombatModule combat;
        public readonly MechanismRules Rules;
        public bool Enabled = true;
        public M01CombatBridge(CombatModule combat, MechanismMode mode)
        {
            this.combat = combat;
            Rules = new MechanismRules(mode, new MechanismRulesConfig(), this);
            combat.InstallRuleExtension(this);
        }
        public void OnDirectHit(float time, int serial, int generation, Vec2 position, float damage)
        {
            if (!Enabled) return;
            Rules.ReportDirectHit(new DirectHitRecord(time, new MechanismTarget(serial, generation),
                new MechanismVec2(position.X, position.Y), (int)Math.Round(damage, MidpointRounding.AwayFromZero)));
        }
        public void AfterProjectiles(float time) { if (Enabled) Rules.Tick(time); }
        public bool TryGetLiveTarget(MechanismTarget target, out MechanismVec2 position)
        {
            bool valid = combat.TryGetEnemy(target.Serial, target.Generation, out var enemy);
            position = valid ? new MechanismVec2(enemy.Position.X, enemy.Position.Y) : default;
            return valid;
        }
        public void CollectLiveTargets(List<MechanismLiveTarget> buffer)
        {
            foreach (var enemy in combat.Enemies)
                if (enemy.IsAlive) buffer.Add(new MechanismLiveTarget(new MechanismTarget(enemy.Serial, enemy.Generation),
                    new MechanismVec2(enemy.Position.X, enemy.Position.Y)));
        }
        public DerivedDamageResult ApplyDerivedDamage(in DerivedDamageRequest request)
        {
            var type = request.Kind == DerivedKind.Echo ? PresentationEventType.EchoHit
                : request.Kind == DerivedKind.Propagation ? PresentationEventType.PropagationHit : PresentationEventType.EchoPropagationHit;
            var result = combat.ApplyRuleDamage(request.Target.Serial, request.Target.Generation, request.Damage,
                type, new Vec2(request.From.X, request.From.Z));
            return result == CombatRuleDamageResult.Killed ? DerivedDamageResult.Killed
                : result == CombatRuleDamageResult.Applied ? DerivedDamageResult.Applied : DerivedDamageResult.Invalid;
        }
    }
}
