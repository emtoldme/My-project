using System;
using System.Linq;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.GameplayNext;

namespace Waffle.Tests
{
    public sealed class GameplayNextHostTests
    {
        sealed class Module : IEncounterModule, IObjectiveModule, IFinaleModule
        {
            public Action<GameplayContext, GameplayCommandBuffer> Action;
            public int Calls, Clears, Rewards;
            public bool Granted;
            public GameplayContext LastContext;
            public GameplayCommandBuffer Retained;
            public void Step(GameplayContext context, GameplayCommandBuffer commands)
            { LastContext = context; Retained = commands; Action?.Invoke(context, commands); Calls++; }
            public void Clear() { Clears++; }
            public void ConfirmReward(Guid runId, string objectiveId, bool granted) { Rewards++; Granted = granted; }
        }
        static RunSimulation Make(Module encounters = null, Module objectives = null, Module finale = null,
            bool boss = false, float duration = 10, int maxAttacks = 12)
        {
            var config = new RunConfig { runDuration = duration, damage = 0, hitIFrame = .01f,
                gameplayNext = new GameplayNextSettings { enabled = true, maxAttacks = maxAttacks,
                    endingMode = boss ? RunEndingMode.BossClear : RunEndingMode.SurvivalOnly } };
            var combat = new CombatConfig { spawningEnabled = false, contactDamage = 0, projectileSpeed = 1000,
                phaseSpeed = new[] { 0f, 0f, 0f, 0f } };
            var sim = new RunSimulation(config, null, combat);
            sim.InstallGameplayModules(new GameplayModules(encounters, objectives, finale));
            return sim;
        }
        static void Tick(RunSimulation sim, float dt = .05f) => sim.Tick(dt, InputSnapshot.None, sim.State.Frame + 1, sim.State.Frame * .05);
        static void Attack(Module module, float warning = .1f, int damage = 10)
        {
            module.Action = (c, b) => {
                if (module.Calls != 0) return;
                b.TryAttack(new HostileAttackIntent(1, c.Enemies[0].Key,
                    new AttackShape(AttackShapeKind.Circle, Vec2.Zero, 1), c.Now + warning, c.Now + warning + .5f, damage));
            };
        }
        static void Objective(Module module, string rewardId = "D02", Guid? runOverride = null)
        {
            module.Action = (c, b) => {
                b.PublishObjective(new ObjectiveSnapshot("node-1", Vec2.Zero, 2, ObjectiveStatus.Completed, 1, 60, rewardId));
                b.TryReward(new ObjectiveRewardIntent(runOverride ?? c.RunId, "node-1", rewardId));
            };
        }
        static void Boss(Module module, float hp = 1)
        {
            module.Action = (c, b) => {
                if (module.Calls == 0) b.TrySpawn(new SpawnIntent(1, new Vec2(2, 0),
                    new EnemySpawnSpec("test-boss", hp, 0, .3f, 0, 0, true, true), 0));
            };
        }

        [Test]
        public void FeatureOffDoesNotChangeLegacyVictoryAndHasNoGameplayView()
        {
            var sim = new RunSimulation(new RunConfig { runDuration = .05f }, null,
                new CombatConfig { spawningEnabled = false });
            Assert.Throws<InvalidOperationException>(() => sim.InstallGameplayModules(new GameplayModules()));
            Tick(sim);
            Assert.That(sim.State.Phase, Is.EqualTo(RunPhase.Victory));
            Assert.That(sim.ReadGameplayView(), Is.Null);
            Assert.That(sim.Outcome.Reason, Is.EqualTo("timeout"));
        }
        [Test]
        public void ModulesInstallOnceAndBeforeAnyTick()
        {
            var sim = Make();
            Assert.Throws<InvalidOperationException>(() => sim.InstallGameplayModules(new GameplayModules()));
            Tick(sim);
            Assert.Throws<InvalidOperationException>(() => sim.InstallGameplayModules(new GameplayModules()));
        }
        [Test]
        public void ModuleSeedsAreStableAndSeparatedAndRunsHaveDistinctIdentity()
        {
            var a = new Module(); var b = new Module(); var c = new Module(); var sim = Make(a, b, c); Tick(sim);
            Assert.That(a.LastContext.ModuleSeed, Is.Not.EqualTo(b.LastContext.ModuleSeed));
            Assert.That(b.LastContext.ModuleSeed, Is.Not.EqualTo(c.LastContext.ModuleSeed));
            var other = new Module(); var repeat = Make(other); Tick(repeat);
            Assert.That(other.LastContext.ModuleSeed, Is.EqualTo(a.LastContext.ModuleSeed));
            Assert.That(repeat.RunId, Is.Not.EqualTo(sim.RunId));
        }
        [Test]
        public void PausedAndLevelUpDoNotCallModules()
        {
            var module = new Module(); var sim = Make(module); Tick(sim); sim.TogglePause("test"); Tick(sim);
            Assert.That(module.Calls, Is.EqualTo(1));
            sim.TogglePause("test"); sim.DebugGiveXp(5); Tick(sim);
            Assert.That(sim.State.Phase, Is.EqualTo(RunPhase.LevelUp)); int calls = module.Calls; Tick(sim);
            Assert.That(module.Calls, Is.EqualTo(calls));
        }
        [Test]
        public void CommandsCannotBeSubmittedOutsideModuleCallback()
        {
            var module = new Module(); var sim = Make(module); Tick(sim);
            Assert.That(module.Retained.TrySpawn(new SpawnIntent(1, new Vec2(3, 0), new EnemySpawnSpec("late", 10, 0, .3f, 0, 0), 0)), Is.False);
            module.Retained.PublishEncounter(99, "late mutation");
            Assert.That(sim.ReadGameplayView().EncounterPhase, Is.Zero);
        }
        [Test]
        public void PrewarningDoesNotDamageAndAttackCandidateIsConsumedOnce()
        {
            var module = new Module(); Attack(module); var sim = Make(module);
            sim.Combat.DebugSpawnEnemy(new Vec2(5, 0), 1000); Tick(sim); Tick(sim);
            Assert.That(sim.State.Hp, Is.EqualTo(100)); Tick(sim);
            Assert.That(sim.State.Hp, Is.EqualTo(90));
            sim.InvulnerableUntil = 0; for (int i = 0; i < 6; i++) Tick(sim);
            Assert.That(sim.State.Hp, Is.EqualTo(90));
        }
        [Test]
        public void DodgeConsumesAttackWithoutDelayedDamage()
        {
            var module = new Module(); Attack(module, 0); var sim = Make(module);
            sim.Combat.DebugSpawnEnemy(new Vec2(5, 0), 1000); sim.InvulnerableUntil = 1; Tick(sim);
            Assert.That(sim.State.Hp, Is.EqualTo(100)); sim.InvulnerableUntil = 0; Tick(sim);
            Assert.That(sim.State.Hp, Is.EqualTo(100));
        }
        [Test]
        public void OwnerKilledAtT7CannotDamagePlayerAtT8()
        {
            var module = new Module(); Attack(module, 0); var sim = Make(module); sim.Config.damage = 100;
            sim.Combat.DebugSpawnEnemy(new Vec2(5, 0), 1); Tick(sim);
            Assert.That(sim.State.Kills, Is.EqualTo(1)); Assert.That(sim.State.Hp, Is.EqualTo(100));
            Assert.That(sim.ReadGameplayView().Attacks, Is.Empty);
        }
        [Test]
        public void AttackBudgetsAndNonFiniteGeometryAreRejected()
        {
            var module = new Module(); module.Action = (c, b) => {
                if (module.Calls != 0) return;
                b.TryAttack(new HostileAttackIntent(1, c.Enemies[0].Key,
                    new AttackShape(AttackShapeKind.Circle, new Vec2(float.NaN, 0), 1), 0, 1, 10));
                for (int i = 0; i < c.Enemies.Count; i++) b.TryAttack(new HostileAttackIntent(i + 2, c.Enemies[i].Key,
                    new AttackShape(AttackShapeKind.Circle, Vec2.Zero, 1), .5f, 1, 10));
            };
            var sim = Make(module, maxAttacks: 1); sim.Combat.DebugSpawnEnemy(new Vec2(5, 0), 1000); sim.Combat.DebugSpawnEnemy(new Vec2(6, 0), 1000); Tick(sim);
            Assert.That(sim.ReadGameplayView().Attacks.Count, Is.EqualTo(1));
        }
        [Test]
        public void SpawnPrewarningDeduplicatesAndBossUsesOneHostEntity()
        {
            var finale = new Module(); finale.Action = (c, b) => b.TrySpawn(new SpawnIntent(1,
                new Vec2(5, 0), new EnemySpawnSpec("boss", 120, 0, .5f, 5, 10, true, true), .1f));
            var sim = Make(finale: finale, boss: true); Tick(sim); Tick(sim);
            Assert.That(sim.Combat.Enemies, Is.Empty); Assert.That(sim.ReadGameplayView().SpawnWarnings.Count, Is.EqualTo(1));
            Tick(sim); Tick(sim);
            Assert.That(sim.Combat.Enemies.Count, Is.EqualTo(1));
            Assert.That(sim.ReadGameplayView().Finale.Hp, Is.EqualTo(sim.Combat.Enemies[0].Hp));
            Assert.That(sim.Combat.Enemies[0].IsBoss, Is.True);
        }
        [Test]
        public void ObjectiveRewardIsIdempotentAndUsesNormalHealLimit()
        {
            var objective = new Module(); Objective(objective); var sim = Make(objectives: objective); sim.State.Hp = 85;
            Tick(sim); Assert.That(sim.State.Hp, Is.EqualTo(100));
            Assert.That(sim.Presentation.Events.Single(e => e.Type == PresentationEventType.ObjectiveRewardGranted).Value, Is.EqualTo(15));
            sim.State.Hp = 50; Tick(sim);
            Assert.That(sim.State.Hp, Is.EqualTo(50)); Assert.That(sim.ReadGameplayView().CompletedObjectives, Is.EqualTo(1));
            Assert.That(objective.Granted, Is.True);
            Assert.That(sim.Presentation.Events.Any(e => e.Type == PresentationEventType.ObjectiveRewardGranted), Is.False);
        }
        [Test]
        public void StaleRunRewardDoesNotGrant()
        {
            var objective = new Module(); Objective(objective, runOverride: Guid.NewGuid()); var sim = Make(objectives: objective); sim.State.Hp = 50;
            Tick(sim); Assert.That(sim.State.Hp, Is.EqualTo(50)); Assert.That(objective.Granted, Is.False);
        }
        [Test]
        public void ObjectiveRewardCannotResurrectSameTickDeath()
        {
            var objective = new Module(); Objective(objective); var sim = Make(objectives: objective); sim.DebugHurt(100);
            Tick(sim); Assert.That(sim.State.Phase, Is.EqualTo(RunPhase.Defeat));
            Assert.That(sim.ReadGameplayView().CompletedObjectives, Is.Zero); Assert.That(objective.Rewards, Is.Zero);
        }
        [Test]
        public void ObjectiveMagnetPreservesExistingXp()
        {
            var objective = new Module(); Objective(objective, "D03"); var sim = Make(objectives: objective);
            sim.Combat.DebugSpawnGem(new Vec2(10, 0), 3); sim.Combat.DebugSpawnGem(new Vec2(11, 0), 4);
            Tick(sim); Assert.That(sim.Combat.Gems.All(g => g.Life == GemLife.Magnet), Is.True);
            Assert.That(sim.Combat.Gems.Sum(g => g.Value) + sim.State.Xp, Is.EqualTo(7));
        }
        [TestCase(false, RunPhase.Victory)]
        [TestCase(true, RunPhase.Defeat)]
        public void BossKillAtDeadlineUsesPlayerDeathPriority(bool killPlayer, RunPhase expected)
        {
            var finale = new Module(); Boss(finale); var sim = Make(finale: finale, boss: true, duration: .05f); sim.Config.damage = 100;
            sim.Config.defeatWinsSameTick = false; if (killPlayer) sim.DebugHurt(100);
            Tick(sim); Assert.That(sim.State.Phase, Is.EqualTo(expected));
            Assert.That(sim.Outcome.BossDefeated, Is.True);
            Assert.That(sim.ReadGameplayView().Attacks, Is.Empty);
            Assert.That(finale.Clears, Is.EqualTo(1));
        }
        [Test]
        public void MissingBossAtDeadlineCannotWinThroughFakeResolutionRequest()
        {
            var finale = new Module { Action = (c, b) => b.RequestFinaleResolution(FinaleResolution.BossDefeated) };
            var sim = Make(finale: finale, boss: true, duration: .05f); Tick(sim);
            Assert.That(sim.State.Phase, Is.EqualTo(RunPhase.Defeat)); Assert.That(sim.Outcome.Reason, Is.EqualTo("boss-deadline"));
        }
        [Test]
        public void RestartClearsOnceAndFreshRunHasNoClaimsOrThreats()
        {
            var module = new Module(); Attack(module); var sim = Make(module); sim.Combat.DebugSpawnEnemy(new Vec2(5, 0), 1000); Tick(sim);
            sim.NotifyRestart(); sim.NotifyRestart(); Assert.That(module.Clears, Is.EqualTo(1)); Assert.That(sim.ReadGameplayView().Attacks, Is.Empty);
            var next = Make(); Assert.That(next.RunId, Is.Not.EqualTo(sim.RunId)); Assert.That(next.ReadGameplayView().CompletedObjectives, Is.Zero);
        }
        [Test]
        public void GeometrySweepsCapsuleAndFastExpandingRingContinuously()
        {
            var scratch = new float[8];
            Assert.That(GameplayGeometry.Hits(new AttackShape(AttackShapeKind.Capsule, new Vec2(-2, 0), .1f, new Vec2(2, 0)),
                new Vec2(0, -2), new Vec2(0, 2), .2f, 0, .05f, scratch), Is.True);
            var ring = new AttackShape(AttackShapeKind.Annulus, Vec2.Zero, 1.1f, innerRadius: 1, expansionSpeed: 30);
            Assert.That(GameplayGeometry.Hits(ring, new Vec2(2, 0), new Vec2(2, 0), .05f, 0, .1f, scratch), Is.True);
            Assert.That(GameplayGeometry.Hits(ring, Vec2.Zero, Vec2.Zero, .05f, 0, .1f, scratch), Is.False);
        }
        [Test]
        public void BossCastAllowsThreeCirclesButRejectsMixedPatterns()
        {
            var finale = new Module(); Boss(finale, 1000); var original = finale.Action;
            finale.Action = (c, b) => {
                original(c, b); if (finale.Calls != 1) return;
                for (int i = 1; i <= 4; i++) b.TryAttack(new HostileAttackIntent(i, c.Enemies[0].Key,
                    new AttackShape(AttackShapeKind.Circle, Vec2.Zero, 1), .5f, 1, 10));
                b.TryAttack(new HostileAttackIntent(5, c.Enemies[0].Key,
                    new AttackShape(AttackShapeKind.Annulus, Vec2.Zero, 2, innerRadius: 1), .5f, 1, 10));
            };
            var sim = Make(finale: finale, boss: true); Tick(sim); Tick(sim);
            Assert.That(sim.ReadGameplayView().Attacks.Count, Is.EqualTo(3));
            Assert.That(sim.ReadGameplayView().Attacks.All(a => a.Attack.Shape.Kind == AttackShapeKind.Circle), Is.True);
        }
        [Test]
        public void RelocationCancelsOwnedThreatAndChangesSnapshotVersion()
        {
            var module = new Module(); Attack(module, 1); var sim = Make(module);
            sim.ViewRadius = 1; sim.Combat.Config.relocateExtra = 0;
            sim.Combat.DebugSpawnEnemy(new Vec2(25, 0), 1000); Tick(sim);
            Assert.That(sim.ReadGameplayView().Attacks, Is.Empty);
            Assert.That(sim.Combat.Enemies[0].RelocationVersion, Is.EqualTo(1));
        }
        [Test]
        public void SweptOwnerAttackPreservesLockedWarningPath()
        {
            var module = new Module { Action = (c, b) => {
                b.TryAttack(new HostileAttackIntent(1, c.Enemies[0].Key,
                    new AttackShape(AttackShapeKind.Capsule, new Vec2(5, 0), .3f, Vec2.Zero), 1, 2, 10, true));
            } };
            var sim = Make(module); sim.Combat.DebugSpawnEnemy(new Vec2(5, 0), 1000); Tick(sim);
            var shape = sim.ReadGameplayView().Attacks[0].Attack.Shape;
            Assert.That(shape.Center.X, Is.EqualTo(5)); Assert.That(shape.End.X, Is.Zero);
        }
        [Test]
        public void BossOrdinaryBudgetCountsOrdinaryEnemiesSeparately()
        {
            var finale = new Module(); Boss(finale, 1000); var encounters = new Module();
            encounters.Action = (c, b) => { if (encounters.Calls == 1) b.TrySpawn(new SpawnIntent(1,
                new Vec2(6, 0), new EnemySpawnSpec("ordinary", 1000, 0, .3f, 0, 0), 0)); };
            var config = new RunConfig { damage = 0, gameplayNext = new GameplayNextSettings { enabled = true,
                endingMode = RunEndingMode.BossClear, bossOrdinaryCap = 1 } };
            var sim = new RunSimulation(config, null, new CombatConfig { spawningEnabled = false });
            sim.InstallGameplayModules(new GameplayModules(encounters, finale: finale)); Tick(sim); Tick(sim);
            Assert.That(sim.Combat.Enemies.Count(e => !e.IsBoss), Is.EqualTo(1));
            Assert.That(sim.Combat.Enemies.Count(e => e.IsBoss), Is.EqualTo(1));
        }
    }
}
