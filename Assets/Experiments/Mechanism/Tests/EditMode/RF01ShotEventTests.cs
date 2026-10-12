using System.Linq;
using NUnit.Framework;
using Waffle;
using Waffle.Combat;

namespace Reverb.Experiments.Mechanism.Tests
{
    public class RF01ShotEventTests
    {
        [Test]
        public void ShotKeepsOriginalDirection_WhenItsTargetDiesAndIsReleasedInSameTick()
        {
            var combat = new CombatModule(new CombatConfig { spawningEnabled = false, separationRadius = 0f }, 12345);
            var original = combat.DebugSpawnEnemy(new Vec2(.5f, 0f), 1, 0);
            combat.DebugSpawnEnemy(new Vec2(0f, 3f), 100, 0);
            int serial = original.Serial;
            var result = combat.StepCombat(new CombatInput { Dt = .1f, RunTime = .1f, PlayerPos = Vec2.Zero,
                Stats = new CombatPlayerStats { Damage = 10, FireInterval = 100, Pierce = 1, MagnetRadius = 0 } });
            var shot = result.Events.Single(e => e.Type == PresentationEventType.Shot);
            Assert.That(result.Events.Count(e => e.Type == PresentationEventType.EnemyKilled), Is.EqualTo(1));
            combat.Release();
            Assert.That(combat.FindNearestTarget().Serial, Is.Not.EqualTo(serial));
            Assert.That(shot.Data, Is.EqualTo(serial));
            Assert.That(shot.DirectionX, Is.EqualTo(1f));
            Assert.That(shot.DirectionZ, Is.Zero);
        }
    }
}
