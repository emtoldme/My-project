using System;
using UnityEngine;
using Waffle;
using Waffle.Combat;
using Waffle.Finale;
using Waffle.GameplayNext;
using Waffle.Objectives;
using Waffle.Rules.Encounters;

namespace Reverb.Drafts.RC01.Presentation.GameplayNext
{
    public sealed class GameplayNextInstaller : MonoBehaviour, IGameplayModuleProvider
    {
        public GameLoop loop;
        public EncounterConfig encounters = new EncounterConfig();
        public FinaleConfig finale = new FinaleConfig();
        public Transform firstTuningAnchor, secondTuningAnchor;

        public GameplayModules CreateGameplayModules()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (loop == null || firstTuningAnchor == null || secondTuningAnchor == null)
                throw new InvalidOperationException("Gameplay modules require a host and two tuning anchors.");
            ValidateAnchor(firstTuningAnchor); ValidateAnchor(secondTuningAnchor);
            if (Vector3.Distance(firstTuningAnchor.position, secondTuningAnchor.position) < 4f)
                throw new InvalidOperationException("Tuning anchors must be at least four metres apart.");
            return new GameplayModules(new EncounterModule(encounters),
                new TuningObjectives(Point(firstTuningAnchor), Point(secondTuningAnchor)),
                new FinaleBossModule(loop.config.seed, finale));
        }

        static Vec2 Point(Transform anchor) => new Vec2(anchor.position.x, anchor.position.z);
        void ValidateAnchor(Transform anchor)
        {
            var p = Point(anchor);
            float limit = loop.combatConfig.arenaHalfSize - TuningObjectives.RadiusMeters;
            if (float.IsNaN(p.X) || float.IsNaN(p.Y) || float.IsInfinity(p.X) || float.IsInfinity(p.Y)
                || !loop.config.unboundedWorld && (Math.Abs(p.X) > limit || Math.Abs(p.Y) > limit))
                throw new InvalidOperationException("Tuning anchor is outside the playable arena.");
        }
    }
}
