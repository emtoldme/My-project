using System.Linq;
using NUnit.Framework;
using Reverb.Experiments.Mechanism.Integration;
using Waffle;
using Waffle.Combat;

namespace Reverb.Experiments.Mechanism.Tests
{
    public class M01CombatBridgeTests
    {
        static CombatConfig Config() => new CombatConfig { spawningEnabled = false, separationRadius = 0f };
        static CombatInput Input(float now) => new CombatInput { Dt = 1f / 60f, RunTime = now, PlayerPos = Vec2.Zero,
            Stats = new CombatPlayerStats { Damage = 10, FireInterval = 100, Pierce = 1, MagnetRadius = 0 } };

        [TestCase(MechanismMode.None, 10, 0, 0)]
        [TestCase(MechanismMode.Echo, 15, 1, 0)]
        [TestCase(MechanismMode.Propagation, 14, 0, 1)]
        [TestCase(MechanismMode.Combined, 23, 1, 2)]
        public void RealProjectileAndHost_FourModes_DoNotDuplicateDirectDamage(MechanismMode mode, int total, int echoes, int propagations)
        {
            var combat = new CombatModule(Config(), 12345);
            var bridge = new M01CombatBridge(combat, mode);
            combat.DebugSpawnEnemy(new Vec2(2, 0), 100, 0);
            combat.DebugSpawnEnemy(new Vec2(4, 1), 100, 0);
            combat.DebugSpawnEnemy(new Vec2(4, -2), 100, 0);
            int derived = 0;
            for (int i = 0; i < 75; i++)
            {
                var result = combat.StepCombat(Input(i / 60f));
                derived += result.Events.Count(e => e.Type == PresentationEventType.EchoHit || e.Type == PresentationEventType.PropagationHit || e.Type == PresentationEventType.EchoPropagationHit);
                combat.Release();
            }
            Assert.That(300 - combat.Enemies.Sum(e => e.Hp), Is.EqualTo(total).Within(.001));
            Assert.That(combat.Counters.Hits, Is.EqualTo(1));
            Assert.That(bridge.Rules.EchoHits, Is.EqualTo(echoes));
            Assert.That(bridge.Rules.PropagationHits, Is.EqualTo(propagations));
            Assert.That(derived, Is.EqualTo(echoes + propagations));
            Assert.That(bridge.Rules.PendingCount, Is.Zero);
            Assert.That(bridge.Rules.ActiveChainCount, Is.Zero);
        }

        [Test]
        public void DerivedKills_UseUniqueT7DeathAndDropPath()
        {
            var combat = new CombatModule(Config(), 12345);
            new M01CombatBridge(combat, MechanismMode.Combined);
            combat.DebugSpawnEnemy(new Vec2(2, 0), 15, 0);
            combat.DebugSpawnEnemy(new Vec2(4, 1), 4, 0);
            combat.DebugSpawnEnemy(new Vec2(4, -2), 4, 0);
            int deaths = 0;
            for (int i = 0; i < 75; i++)
            {
                var result = combat.StepCombat(Input(i / 60f));
                deaths += result.Events.Count(e => e.Type == PresentationEventType.EnemyKilled);
                combat.Release();
            }
            Assert.That(deaths, Is.EqualTo(3));
            Assert.That(combat.Counters.TotalKills, Is.EqualTo(3));
            Assert.That(combat.Counters.TotalXpDropped, Is.EqualTo(3));
            Assert.That(combat.Gems.Sum(g => g.Value), Is.EqualTo(3));
        }

        [Test]
        public void PresentationCannotApplyRuleDamage_AfterStepReturns()
        {
            var combat = new CombatModule(Config(), 12345);
            var enemy = combat.DebugSpawnEnemy(new Vec2(20, 0), 100, 0);
            new M01CombatBridge(combat, MechanismMode.Combined);
            combat.StepCombat(Input(0));
            var result = combat.ApplyRuleDamage(enemy.Serial, enemy.Generation, 99, PresentationEventType.EchoHit, Vec2.Zero);
            Assert.That(result, Is.EqualTo(CombatRuleDamageResult.Invalid));
            Assert.That(enemy.Hp, Is.EqualTo(100));
        }

        [Test]
        public void RunSimulationPause_GatesPendingDerivedDamage()
        {
            var sim = new RunSimulation(new RunConfig(), null, Config());
            var bridge = new M01CombatBridge(sim.Combat, MechanismMode.Combined);
            sim.Combat.DebugSpawnEnemy(new Vec2(2, 0), 100, 0);
            for (int i = 0; i < 12; i++) sim.Tick(1f / 60f, new InputSnapshot { ChoosePressed = -1 }, i, i / 60.0);
            Assert.That(bridge.Rules.PendingCount, Is.GreaterThan(0));
            float hp = sim.Combat.Enemies[0].Hp, time = sim.State.Now;
            int pending = bridge.Rules.PendingCount;
            sim.TogglePause("test");
            sim.Tick(10, new InputSnapshot { ChoosePressed = -1 }, 13, 10);
            Assert.That(sim.State.Now, Is.EqualTo(time));
            Assert.That(sim.Combat.Enemies[0].Hp, Is.EqualTo(hp));
            Assert.That(bridge.Rules.PendingCount, Is.EqualTo(pending));
        }
    }
}
