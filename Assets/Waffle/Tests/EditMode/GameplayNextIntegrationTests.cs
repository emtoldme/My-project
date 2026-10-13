using System.Linq;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.Finale;
using Waffle.GameplayNext;
using Waffle.Objectives;
using Waffle.Rules.Encounters;

namespace Waffle.Tests
{
    public sealed class GameplayNextIntegrationTests
    {
        static RunSimulation Make(bool encounters = true, float damage = 0f)
        {
            var config = new RunConfig { runDuration = 600f, seed = 11, damage = damage, fireInterval = 1000f,
                gameplayNext = new GameplayNextSettings { enabled = true, endingMode = RunEndingMode.BossClear } };
            var combat = new CombatConfig { spawningEnabled = false, aliveCap = 48, targetRange = 30f, projectileSpeed = 1000f };
            var sim = new RunSimulation(config, null, combat);
            sim.InstallGameplayModules(new GameplayModules(encounters ? new EncounterModule() : null,
                new TuningObjectives(Vec2.Zero, Vec2.Zero), new FinaleBossModule(config.seed)));
            sim.God = true;
            return sim;
        }
        static void Advance(RunSimulation sim, float seconds)
        {
            for (int i = 0; i < (int)(seconds / .05f); i++)
            {
                if (sim.State.Phase == RunPhase.LevelUp)
                    sim.Tick(.05f, new InputSnapshot { ChoosePressed = 1 }, sim.State.Frame + 1, sim.State.Frame + 100);
                sim.Tick(.05f, InputSnapshot.None, sim.State.Frame + 1, sim.State.Frame + 100);
            }
        }

        [Test]
        public void ActualModules_GrantTwoRewardsShowBossAndEnforceDeadline()
        {
            var sim = Make(); sim.DebugSetHp(50); sim.DebugSetTime(150f);
            Advance(sim, 4.2f);
            Assert.That(sim.State.Hp, Is.EqualTo(70));
            Assert.That(sim.ReadGameplayView().CompletedObjectives, Is.EqualTo(1));
            Assert.That(sim.Combat.Enemies.Any(e => e.ArchetypeId == "legacy-chaser"), Is.False);
            sim.Combat.DebugSpawnGem(new Vec2(20f, 0f), 1);
            sim.DebugSetTime(390f); Advance(sim, 4.2f);
            Assert.That(sim.ReadGameplayView().CompletedObjectives, Is.EqualTo(2));
            Assert.That(sim.Combat.Gems.Any(g => g.Life == GemLife.Magnet), Is.True);
            sim.DebugSetTime(540f); Advance(sim, 6f);
            Assert.That(sim.ReadGameplayView().Finale.Spawned, Is.True);
            Assert.That(sim.Combat.Enemies.Count(e => e.IsBoss && e.IsAlive), Is.EqualTo(1));
            Assert.That(sim.ReadGameplayView().Attacks.Any(a => a.Key.Module == GameplayModuleKind.Finale), Is.True);
            sim.DebugSetTime(599.95f); Advance(sim, .1f);
            Assert.That(sim.Outcome.Reason, Is.EqualTo("boss-deadline"));
            Assert.That(sim.Outcome.CompletedObjectives, Is.EqualTo(2));
            Assert.That(sim.ReadGameplayView().Attacks, Is.Empty);
        }

        [Test]
        public void ActualBossKilledByExistingWeaponProducesVictory()
        {
            var sim = Make(false, 1000f); sim.DebugSetTime(540f); Advance(sim, 4f);
            Assert.That(sim.State.Phase, Is.EqualTo(RunPhase.Victory));
            Assert.That(sim.Outcome.BossDefeated, Is.True);
            Assert.That(sim.Outcome.Reason, Is.EqualTo("boss-defeated"));
        }

        [Test]
        public void EnragedBossNeverOverlapsCircleAndRing()
        {
            var sim = Make(false, 450f); sim.DebugSetTime(540f); Advance(sim, 3.2f);
            var boss = sim.Combat.Enemies.Single(e => e.IsBoss);
            Assert.That(boss.Hp, Is.EqualTo(350f));
            sim.Config.damage = 0f;
            bool sawCircle = false, sawRing = false;
            for (int i = 0; i < 600; i++)
            {
                Advance(sim, .05f);
                var attacks = sim.ReadGameplayView().Attacks.Where(a => a.Key.Module == GameplayModuleKind.Finale).ToArray();
                bool circle = attacks.Any(a => a.Attack.Shape.Kind == AttackShapeKind.Circle);
                bool ring = attacks.Any(a => a.Attack.Shape.Kind == AttackShapeKind.Annulus);
                Assert.That(circle && ring, Is.False);
                sawCircle |= circle; sawRing |= ring;
            }
            Assert.That(sawCircle && sawRing, Is.True);
        }
    }
}
