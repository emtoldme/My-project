using NUnit.Framework;

namespace Waffle.Tests.Integration
{
    /// <summary>
    /// V-01 里不依赖 Unity 播放器的规则层用例。
    /// 直接调用 RunSimulation，不经过 GameLoop、输入资源和场景。
    /// </summary>
    public class V01RulesTests
    {
        static RunSimulation NewSim()
        {
            return new RunSimulation(new RunConfig(), new NullRunLog());
        }

        static void Tick(RunSimulation sim, float dt, InputSnapshot input, long frame, double realTime)
        {
            sim.Tick(dt, input, frame, realTime);
        }

        static InputSnapshot Press(bool pause, bool dodge, float moveX, int choose)
        {
            return new InputSnapshot
            {
                PausePressed = pause,
                DodgePressed = dodge,
                MoveX = moveX,
                MoveY = 0f,
                ChoosePressed = choose
            };
        }

        [Test]
        public void PausedInput_DoesNotMoveOrDodge_AndDoesNotReplayAfterResume()
        {
            RunSimulation sim = NewSim();
            Tick(sim, 0.02f, Press(true, true, 1f, -1), 1, 0.02);
            Assert.AreEqual(RunPhase.Paused, sim.State.Phase);
            Assert.AreEqual(0, sim.DodgeRequests, "暂停键同一帧的闪避不应进入 T1");
            Assert.AreEqual(0f, sim.PlayerX, 0.0001f);
            Assert.AreEqual(0f, sim.State.Now, 0.0001f);

            Tick(sim, 0.02f, Press(false, true, 1f, -1), 2, 0.04);
            Assert.AreEqual(RunPhase.Paused, sim.State.Phase);
            Assert.AreEqual(0, sim.DodgeRequests);
            Assert.AreEqual(0f, sim.PlayerX, 0.0001f);
            Assert.AreEqual(0f, sim.State.Now, 0.0001f);

            Tick(sim, 0.02f, Press(true, false, 0f, -1), 3, 0.06);
            Assert.AreEqual(RunPhase.Playing, sim.State.Phase);
            Assert.AreEqual(0, sim.DodgeRequests, "恢复帧按 GameInput 的暂停态快照，不应带上闪避");

            Tick(sim, 0.02f, Press(false, false, 0f, -1), 4, 0.08);
            Assert.AreEqual(RunPhase.Playing, sim.State.Phase);
            Assert.AreEqual(0, sim.DodgeRequests);
            Assert.AreEqual(0.04f, sim.State.Now, 0.0001f);
            Assert.AreEqual(0f, sim.PlayerX, 0.0001f);
        }

        [Test]
        public void GiveXp30_OpensThreePanels_AndFinishesAtLevel4With6Of14()
        {
            RunSimulation sim = NewSim();
            DebugCommands.Execute(sim, "givexp 30");
            Tick(sim, 0.02f, InputSnapshot.None, 1, 1.0);

            Assert.AreEqual(RunPhase.LevelUp, sim.State.Phase);
            Assert.AreEqual(4, sim.State.Level);
            Assert.AreEqual(6, sim.State.Xp);
            Assert.AreEqual(14, sim.State.XpToNext);
            Assert.AreEqual(3, sim.State.PendingLevelUps);
            Assert.AreEqual(1, sim.PanelsShown);
            UpgradeKind[] first = (UpgradeKind[])sim.CurrentOptions.Clone();

            sim.BeginFrame(2, 2.0);
            sim.TogglePause("测试暂停");
            Assert.AreEqual(RunPhase.Paused, sim.State.Phase);
            Assert.AreEqual(RunPhase.LevelUp, sim.State.PhaseBeforePause);
            CollectionAssert.AreEqual(first, sim.CurrentOptions);

            Tick(sim, 0.02f, Press(true, false, 0f, 0), 3, 2.1);
            Assert.AreEqual(RunPhase.LevelUp, sim.State.Phase, "恢复后仍是升级，不应直接回 Playing");
            Assert.AreEqual(3, sim.State.PendingLevelUps, "恢复同一帧的选择应被 0.3 秒防误触挡住");
            Assert.AreEqual(1, sim.PanelsShown);
            CollectionAssert.AreEqual(first, sim.CurrentOptions);
            Assert.AreEqual(0.02f, sim.State.Now, 0.0001f, "进入升级的那一帧已经走完 T11；之后暂停不再走时");

            long frame = 10;
            double time = 2.1;
            for (int i = 0; i < 3; i++)
            {
                frame++;
                time += 0.4;
                sim.BeginFrame(frame, time);
                Assert.IsTrue(sim.ChooseUpgrade(0), "第 " + (i + 1) + " 次选择应被接受");
            }

            Assert.AreEqual(RunPhase.Playing, sim.State.Phase);
            Assert.AreEqual(0, sim.State.PendingLevelUps);
            Assert.AreEqual(4, sim.State.Level);
            Assert.AreEqual(6, sim.State.Xp);
            Assert.AreEqual(14, sim.State.XpToNext);
            Assert.AreEqual(3, sim.PanelsShown, "第一张加两次重抽，第三次选完不再抽");
            Assert.AreEqual(3, sim.State.ChosenUpgrades.Count);
        }

        [Test]
        public void SameTick_DeathAndTimeout_IsDefeat()
        {
            RunSimulation sim = NewSim();
            DebugCommands.Execute(sim, "settime 179.99");
            DebugCommands.Execute(sim, "hurt 100");
            Tick(sim, 0.05f, InputSnapshot.None, 1, 1.0);

            Assert.AreEqual(RunPhase.Defeat, sim.State.Phase);
            Assert.AreEqual("hp<=0", sim.State.EndReason);
            Assert.AreEqual(179.99f, sim.State.Now, 0.0001f);
            Assert.AreEqual(0, sim.State.Hp);
        }

        [Test]
        public void DeathWithoutTimeout_IsDefeat_AndClockStaysAtTickStart()
        {
            RunSimulation sim = NewSim();
            DebugCommands.Execute(sim, "hurt 100");
            Tick(sim, 0.02f, InputSnapshot.None, 1, 1.0);

            Assert.AreEqual(RunPhase.Defeat, sim.State.Phase);
            Assert.AreEqual(0f, sim.State.Now, 0.0001f);
        }

        [Test]
        public void TimeoutWithoutDeath_IsVictoryAtDuration()
        {
            RunSimulation sim = NewSim();
            DebugCommands.Execute(sim, "settime 179.96");
            Tick(sim, 0.05f, InputSnapshot.None, 1, 1.0);

            Assert.AreEqual(RunPhase.Victory, sim.State.Phase);
            Assert.AreEqual("timeout", sim.State.EndReason);
            Assert.AreEqual(180f, sim.State.Now, 0.0001f);
            Assert.AreEqual(100, sim.State.Hp);
        }

        [Test]
        public void LevelUpAndTimeoutSameTick_IsVictory_AndDoesNotOpenPanel()
        {
            RunSimulation sim = NewSim();
            DebugCommands.Execute(sim, "givexp 30");
            DebugCommands.Execute(sim, "settime 179.96");
            Tick(sim, 0.05f, InputSnapshot.None, 1, 1.0);

            Assert.AreEqual(RunPhase.Victory, sim.State.Phase);
            Assert.AreEqual(180f, sim.State.Now, 0.0001f);
            Assert.AreEqual(4, sim.State.Level);
            Assert.AreEqual(6, sim.State.Xp);
            Assert.AreEqual(0, sim.State.PendingLevelUps);
            Assert.AreEqual(0, sim.PanelsShown);
            Assert.AreEqual(UpgradeKind.None, sim.CurrentOptions[0]);
        }

        [Test]
        public void Terminal_RejectsGiveXp_AndDoesNotChangeProgress()
        {
            RunSimulation sim = NewSim();
            DebugCommands.Execute(sim, "hurt 100");
            Tick(sim, 0.02f, InputSnapshot.None, 1, 1.0);
            int rejected = sim.State.RejectedTransitions;

            DebugCommands.Execute(sim, "givexp 30");
            Tick(sim, 0.02f, InputSnapshot.None, 2, 1.1);

            Assert.AreEqual(RunPhase.Defeat, sim.State.Phase);
            Assert.AreEqual(1, sim.State.Level);
            Assert.AreEqual(0, sim.State.Xp);
            Assert.AreEqual(0, sim.State.PendingLevelUps);
            Assert.Greater(sim.State.RejectedTransitions, rejected);
        }

        [Test]
        public void NewSimulation_DoesNotKeepPreviousRun()
        {
            RunSimulation first = NewSim();
            DebugCommands.Execute(first, "givexp 30");
            Tick(first, 0.02f, InputSnapshot.None, 1, 1.0);
            Assert.AreEqual(4, first.State.Level);

            RunSimulation second = NewSim();
            Assert.AreEqual(RunPhase.Playing, second.State.Phase);
            Assert.AreEqual(1, second.State.Level);
            Assert.AreEqual(0, second.State.Xp);
            Assert.AreEqual(0, second.State.PendingLevelUps);
            Assert.AreEqual(100, second.State.Hp);
            Assert.AreEqual(100, second.State.MaxHp);
            Assert.AreEqual(0f, second.State.Now, 0.0001f);
            Assert.AreEqual(0, second.PanelsShown);
            Assert.AreEqual(0, second.DodgeRequests);
            Assert.AreEqual(1f, RunState.TimeScaleFor(second.State.Phase));
        }
    }
}
