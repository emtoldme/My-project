using System.Linq;
using NUnit.Framework;
using Waffle;
using Waffle.Combat;
using Waffle.GameplayNext;

namespace Waffle.Objectives.Tests
{
    public class TuningObjectivesTests
    {
        [Test]
        public void FirstTuneHealsOnceAndIgnoresAForeignRun()
        {
            var module = new TuningObjectives(Vec2.Zero, Vec2.Zero);
            var sim = Make(module);
            module.ConfirmReward(System.Guid.NewGuid(), TuningObjectives.FirstId, true);
            sim.DebugSetTime(150f);
            Tick(sim);
            Tick(sim);
            Tick(sim);
            Tick(sim);
            Assert.That(sim.State.Hp, Is.EqualTo(70));
            Assert.That(sim.State.MaxHp, Is.EqualTo(100));
            Assert.That(Count(sim, "D02"), Is.EqualTo(1));
            int hp = sim.State.Hp;
            Tick(sim);
            Assert.That(sim.State.Hp, Is.EqualTo(hp));
            Assert.That(Count(sim, "D02"), Is.EqualTo(0));
        }

        [Test]
        public void LeavingPausesAndTheEdgeCounts()
        {
            var module = new TuningObjectives(Vec2.Zero, Vec2.Zero);
            var sim = Make(module);
            sim.DebugSetTime(150f);
            Tick(sim);
            Assert.That(sim.ReadGameplayView().Objective.Progress, Is.EqualTo(0.25f).Within(0.0001f));
            sim.PlayerX = 6f;
            Tick(sim);
            Assert.That(sim.ReadGameplayView().Objective.Progress, Is.EqualTo(0.25f).Within(0.0001f));
            sim.PlayerX = 2f;
            Tick(sim);
            Tick(sim);
            Tick(sim);
            Assert.That(sim.State.Hp, Is.EqualTo(70));
        }

        [Test]
        public void StandingJustOutsideDoesNotChannel()
        {
            var module = new TuningObjectives(Vec2.Zero, Vec2.Zero);
            var sim = Make(module);
            sim.PlayerX = 2.01f;
            sim.DebugSetTime(150f);
            Tick(sim);
            Tick(sim);
            Tick(sim);
            Tick(sim);
            Assert.That(sim.ReadGameplayView().Objective.Status, Is.EqualTo(ObjectiveStatus.Channeling).Or.EqualTo(ObjectiveStatus.Available));
            Assert.That(sim.ReadGameplayView().Objective.Progress, Is.EqualTo(0f));
            Assert.That(sim.State.Hp, Is.EqualTo(50));
        }

        [Test]
        public void MissingTheFirstDoesNotBlockTheSecond()
        {
            var module = new TuningObjectives(Vec2.Zero, new Vec2(0f, 4f));
            var sim = Make(module);
            sim.PlayerX = 8f;
            sim.DebugSetTime(150f);
            sim.FixedDt = 60f;
            Tick(sim);
            Assert.That(sim.ReadGameplayView().Objective.Status, Is.EqualTo(ObjectiveStatus.Expired));
            sim.FixedDt = 1f;
            sim.PlayerX = 0f;
            sim.PlayerZ = 4f;
            sim.DebugSetTime(390f);
            Tick(sim);
            Tick(sim);
            Tick(sim);
            Tick(sim);
            Assert.That(Count(sim, "D03"), Is.EqualTo(1));
            Assert.That(sim.State.Xp, Is.EqualTo(0));
            Assert.That(sim.State.Hp, Is.EqualTo(50));
        }

        [Test]
        public void DeathOnTheCompletionTickPaysNothing()
        {
            var module = new TuningObjectives(Vec2.Zero, Vec2.Zero);
            var sim = Make(module);
            sim.DebugSetTime(150f);
            Tick(sim);
            Tick(sim);
            Tick(sim);
            sim.DebugHurt(1000);
            Tick(sim);
            Assert.That(sim.State.Phase, Is.EqualTo(RunPhase.Defeat));
            Assert.That(sim.State.Hp, Is.EqualTo(0));
            Assert.That(Count(sim, "D02"), Is.EqualTo(0));
        }

        static RunSimulation Make(TuningObjectives module)
        {
            var config = new RunConfig
            {
                runDuration = 600f, damage = 0, hitIFrame = 0.01f, maxHp = 100,
                gameplayNext = new GameplayNextSettings { enabled = true }
            };
            var combat = new CombatConfig
            {
                spawningEnabled = false, contactDamage = 0, projectileSpeed = 1000f,
                phaseSpeed = new[] { 0f, 0f, 0f, 0f }
            };
            var sim = new RunSimulation(config, null, combat);
            sim.InstallGameplayModules(new GameplayModules(null, module, null));
            sim.FixedDt = 1f;
            sim.State.Hp = 50;
            return sim;
        }

        static void Tick(RunSimulation sim)
        {
            sim.Tick(1f, InputSnapshot.None, sim.State.Frame + 1, 0);
        }

        static int Count(RunSimulation sim, string rewardId)
        {
            return sim.Presentation.Events.Count(e => e.Type == PresentationEventType.ObjectiveRewardGranted && e.RootSourceId == rewardId);
        }
    }
}
