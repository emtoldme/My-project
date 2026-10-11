using System.Linq;
using NUnit.Framework;
using Waffle.Combat;

namespace Waffle.Tests
{
    public class CombatIntegrationTests
    {
        private static RunSimulation Sim(bool spawning = false)
        {
            return new RunSimulation(new RunConfig(), null, new CombatConfig { spawningEnabled = spawning });
        }

        [Test]
        public void KillEventsXpAndRecycle_AreWiredIntoFramework()
        {
            var sim = Sim();
            sim.Combat.DebugSpawnEnemy(new Vec2(0.3f, 0f), 5f, 0f);
            sim.Tick(0.05f, InputSnapshot.None, 42, 0);
            Assert.That(sim.State.Kills, Is.EqualTo(1));
            Assert.That(sim.State.Xp, Is.EqualTo(1));
            Assert.That(sim.State.Hp, Is.EqualTo(100));
            Assert.That(sim.Combat.Enemies, Is.Empty);
            Assert.That(sim.Combat.Gems, Is.Empty);
            Assert.That(sim.Presentation.Events.Any(e => e.Type == PresentationEventType.EnemyKilled && e.Frame == 42), Is.True);
            Assert.That(sim.Presentation.Events.Any(e => e.Type == PresentationEventType.GemPickup), Is.True);
        }

        [Test]
        public void DeathSkipsGems_ButReleasesKilledEnemy()
        {
            var sim = Sim();
            sim.State.Hp = 10;
            sim.Combat.DebugSpawnEnemy(new Vec2(0.1f, 0f), 5f, 0f);
            sim.Combat.DebugSpawnEnemy(new Vec2(-0.2f, 0f), 100f, 0f);
            sim.Tick(0.05f, InputSnapshot.None, 1, 0);
            Assert.That(sim.State.Phase, Is.EqualTo(RunPhase.Defeat));
            Assert.That(sim.State.Xp, Is.Zero);
            Assert.That(sim.Combat.Enemies.Count, Is.EqualTo(1));
            Assert.That(sim.Combat.Gems.Count, Is.EqualTo(1));
        }

        [TestCase(false, 90)]
        [TestCase(true, 80)]
        public void ContactSetting_UsesMaxOrSum_ThenRespectsInvulnerability(bool stack, int expectedHp)
        {
            var sim = Sim(); sim.Config.stackContactDamage = stack;
            sim.Combat.DebugSpawnEnemy(new Vec2(0f, 0.2f), 1000f, 0f);
            sim.Combat.DebugSpawnEnemy(new Vec2(0f, -0.2f), 1000f, 0f);
            sim.Tick(0.01f, InputSnapshot.None, 1, 0);
            Assert.That(sim.State.Hp, Is.EqualTo(expectedHp));
            sim.Tick(0.01f, InputSnapshot.None, 2, 0.01);
            Assert.That(sim.State.Hp, Is.EqualTo(expectedHp));
        }

        [Test]
        public void DodgeMovesThreeMeters_BlocksDamage_AndHasCooldown()
        {
            var sim = Sim();
            sim.Combat.DebugSpawnEnemy(new Vec2(0.75f, 0f), 1000f, 0f);
            var input = InputSnapshot.None; input.MoveX = 1; input.DodgePressed = true;
            sim.Tick(0.05f, input, 1, 0);
            Assert.That(sim.State.Hp, Is.EqualTo(100));
            Assert.That(sim.IsDodging, Is.True);
            input.MoveX = 0; input.DodgePressed = false;
            for (int i = 0; i < 3; i++) sim.Tick(0.05f, input, i + 2, (i + 1) * 0.05);
            Assert.That(sim.PlayerX, Is.EqualTo(3f).Within(0.0001f));
            input.DodgePressed = true;
            sim.Tick(0.05f, input, 5, 0.2);
            Assert.That(sim.Presentation.Events.Any(e => e.Type == PresentationEventType.Dodge), Is.False);
            Assert.That(sim.DodgeReadyAt, Is.EqualTo(1.5f));
        }

        [Test]
        public void PauseFreezesDodgeCombatAndClock()
        {
            var sim = Sim(true);
            var input = InputSnapshot.None; input.DodgePressed = true;
            sim.Tick(0.05f, input, 1, 0);
            sim.TogglePause("test");
            float now = sim.State.Now, z = sim.PlayerZ;
            var pos = sim.Combat.Enemies[0].Position;
            sim.Tick(0.05f, InputSnapshot.None, 2, 1);
            Assert.That(sim.State.Now, Is.EqualTo(now));
            Assert.That(sim.PlayerZ, Is.EqualTo(z));
            Assert.That(sim.Combat.Enemies[0].Position.X, Is.EqualTo(pos.X));
            Assert.That(sim.IsDodging, Is.True);
        }

        [Test]
        public void DebugCombatCommandsWork_AndTerminalRejectsThem()
        {
            var sim = Sim();
            DebugCommands.Execute(sim, "spawn 3");
            DebugCommands.Execute(sim, "spawngems 2");
            DebugCommands.Execute(sim, "setcap 12");
            Assert.That(sim.Combat.AliveCount, Is.EqualTo(3));
            Assert.That(sim.Combat.Gems.Count, Is.EqualTo(2));
            Assert.That(sim.Combat.Config.aliveCap, Is.EqualTo(12));
            sim.State.Hp = 0; sim.Tick(0.01f, InputSnapshot.None, 1, 0);
            StringAssert.Contains("已终局", DebugCommands.Execute(sim, "spawn 3"));
        }
    }
}
