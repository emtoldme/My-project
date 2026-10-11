using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Waffle.Tests
{
    /// <summary>P-01：表现事件通道的顺序与“只发一次”。</summary>
    public class PresentationEventTests
    {
        private static List<PresentationEventType> Types(SimDriver d)
        {
            return d.Sim.Presentation.Events.Select(e => e.Type).ToList();
        }

        /// <summary>模拟 GameLoop：每次操作后分发并清空，累计到 all。</summary>
        private static void Drain(SimDriver d, List<PresentationEvent> all)
        {
            all.AddRange(d.Sim.Presentation.Events);
            d.Sim.Presentation.Clear();
        }

        [Test]
        public void T0_ClearsPreviousFrameEvents()
        {
            var d = new SimDriver();
            var s = InputSnapshot.None; s.DodgePressed = true;
            d.Tick(1f / 60f, s);
            CollectionAssert.AreEqual(new[] { PresentationEventType.Dodge }, Types(d));
            d.Tick();
            Assert.AreEqual(0, d.Sim.Presentation.Count);
        }

        [Test]
        public void SameTick_DodgeThenHurtThenLevelUp_InTickOrder()
        {
            var d = new SimDriver();
            d.Sim.DebugHurt(10);
            d.Sim.DebugGiveXp(5);
            var s = InputSnapshot.None; s.DodgePressed = true;
            d.Tick(1f / 60f, s);
            CollectionAssert.AreEqual(new[]
            {
                PresentationEventType.Dodge,        // T1
                PresentationEventType.PlayerHurt,   // T8
                PresentationEventType.LevelUpShown  // T12
            }, Types(d));
            var hurt = d.Sim.Presentation.Events[1];
            Assert.AreEqual(10f, hurt.Value);
            Assert.AreEqual(90, hurt.Data);
            Assert.AreEqual(d.Frame, hurt.Frame);
        }

        [Test]
        public void HurtBlockedByInvulnerability_EmitsNothing()
        {
            var d = new SimDriver();
            d.Sim.God = true;
            d.Sim.DebugHurt(10);
            d.Tick();
            Assert.AreEqual(0, d.Sim.Presentation.Count);
        }

        [Test]
        public void Pause_EmitsPausedOnly_AndNoGameplayEventsWhilePaused()
        {
            var d = new SimDriver();
            d.PressPause();
            CollectionAssert.AreEqual(new[] { PresentationEventType.Paused }, Types(d));

            var s = InputSnapshot.None; s.DodgePressed = true; s.MoveX = 1f;
            d.Sim.DebugHurt(10);
            d.Sim.DebugGiveXp(5);
            for (int i = 0; i < 10; i++)
            {
                d.Tick(1f / 60f, s);
                Assert.AreEqual(0, d.Sim.Presentation.Count, "暂停中不发任何事件");
            }

            d.PressPause(); // 恢复：同一帧 T1–T13 继续执行，排队的伤害和 XP 在恢复帧结算
            CollectionAssert.AreEqual(new[]
            {
                PresentationEventType.Resumed,
                PresentationEventType.PlayerHurt,
                PresentationEventType.LevelUpShown
            }, Types(d));
        }

        [Test]
        public void ChainedLevelUp_ShownChosenPairs()
        {
            var d = new SimDriver();
            var all = new List<PresentationEvent>();
            d.Sim.DebugGiveXp(30);
            d.Tick(); Drain(d, all);
            for (int i = 0; i < 3; i++) { d.ChooseAfterGuard(0); Drain(d, all); }
            CollectionAssert.AreEqual(new[]
            {
                PresentationEventType.LevelUpShown,
                PresentationEventType.LevelUpChosen, PresentationEventType.LevelUpShown,
                PresentationEventType.LevelUpChosen, PresentationEventType.LevelUpShown,
                PresentationEventType.LevelUpChosen
            }, all.Select(e => e.Type).ToArray());
            Assert.AreEqual(new[] { 3, 2, 1 }, all.Where(e => e.Type == PresentationEventType.LevelUpShown).Select(e => e.Data).ToArray());
        }

        [Test]
        public void PauseDuringLevelUp_ResumedNotShownAgain()
        {
            var d = new SimDriver();
            var all = new List<PresentationEvent>();
            d.Sim.DebugGiveXp(5);
            d.Tick(); Drain(d, all);
            d.PressPause(); Drain(d, all);
            d.PressPause(); Drain(d, all);
            CollectionAssert.AreEqual(new[]
            {
                PresentationEventType.LevelUpShown, PresentationEventType.Paused, PresentationEventType.Resumed
            }, all.Select(e => e.Type).ToArray());
            Assert.AreEqual((int)RunPhase.LevelUp, all[2].Data);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Terminal_EmitsExactlyOnce(bool victory)
        {
            var d = new SimDriver();
            var all = new List<PresentationEvent>();
            if (victory) d.Sim.DebugSetTime(179.99f); else d.Sim.DebugHurt(100);
            d.Sim.FixedDt = 0.05f;
            for (int i = 0; i < 20; i++) { d.Tick(); Drain(d, all); }
            d.PressPause(); Drain(d, all);
            DebugCommands.Execute(d.Sim, "givexp 30"); Drain(d, all);
            var expected = victory ? PresentationEventType.Victory : PresentationEventType.Defeat;
            Assert.AreEqual(1, all.Count(e => e.Type == expected));
            Assert.AreEqual(0, all.Count(e => e.Type == PresentationEventType.Paused || e.Type == PresentationEventType.LevelUpShown));
        }

        [Test]
        public void SameTick_LevelUpAndTimeout_VictoryWithoutShown()
        {
            var d = new SimDriver();
            d.Sim.FixedDt = 0.05f;
            d.Sim.DebugSetTime(179.99f);
            d.Sim.DebugGiveXp(5);
            d.Tick();
            CollectionAssert.AreEqual(new[] { PresentationEventType.Victory }, Types(d));
        }

        [Test]
        public void Restart_EmitsOnce_FromAnyPhase()
        {
            foreach (var setup in new System.Action<SimDriver>[]
            {
                d => { },
                d => d.PressPause(),
                d => { d.Sim.DebugGiveXp(5); d.Tick(); },
                d => { d.Sim.DebugHurt(100); d.Tick(); },
                d => { d.Sim.DebugSetTime(180f); d.Tick(); }
            })
            {
                var d = new SimDriver();
                setup(d);
                d.Sim.Presentation.Clear();
                d.Sim.NotifyRestart();
                d.Sim.NotifyRestart();
                CollectionAssert.AreEqual(new[] { PresentationEventType.Restart }, Types(d));
            }
        }

        [Test]
        public void ReservedCombatTypes_NotEmittedByFramework()
        {
            var d = new SimDriver();
            var all = new List<PresentationEvent>();
            var s = InputSnapshot.None; s.DodgePressed = true; s.MoveX = 1f;
            d.Sim.DebugGiveXp(30);
            for (int i = 0; i < 300; i++) { d.Tick(1f / 60f, s); Drain(d, all); if (d.Sim.State.Phase == RunPhase.LevelUp) { d.ChooseAfterGuard(0); Drain(d, all); } }
            var reserved = new[] { PresentationEventType.Shot, PresentationEventType.EnemyHit, PresentationEventType.EnemyKilled, PresentationEventType.GemPickup };
            Assert.IsFalse(all.Any(e => reserved.Contains(e.Type)));
        }
    }
}
