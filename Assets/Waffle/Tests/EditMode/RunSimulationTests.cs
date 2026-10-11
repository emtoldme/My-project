using System.Linq;
using NUnit.Framework;

namespace Waffle.Tests
{
    /// <summary>GameLoop 逻辑核心：tick 顺序、同 tick 优先级、终态锁、暂停、升级队列、重开。</summary>
    public class RunSimulationTests
    {
        private const float Dt = 1f / 60f;

        // ---------------- A-05：tick 顺序 ----------------

        [Test]
        public void A05_TraceOrder_T0ToT13_WhenPlaying()
        {
            var d = new SimDriver();
            d.Sim.TraceTicks = 1;
            d.Tick();
            var steps = d.Log.Infos.Where(s => s.Contains("[trace]"))
                .Select(s => s.Substring(s.IndexOf(" T") + 1).Split(' ')[0]).ToList();
            CollectionAssert.AreEqual(
                new[] { "T0", "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12", "T13" }, steps);
            Assert.AreEqual(0, d.Sim.TraceTicks, "tracetick 1 只追踪 1 个 tick");
        }

        [Test]
        public void A05_TraceOnlyT0_WhenPausedOrLevelUp()
        {
            var d = new SimDriver();
            d.PressPause();
            d.Log.Infos.Clear();
            d.Sim.TraceTicks = -1;
            d.Tick();
            var steps = d.Log.Infos.Where(s => s.Contains("[trace]")).ToList();
            Assert.AreEqual(1, steps.Count);
            StringAssert.Contains("T0", steps[0]);

            var d2 = new SimDriver();
            d2.Sim.DebugGiveXp(5);
            d2.Tick();
            Assert.AreEqual(RunPhase.LevelUp, d2.Sim.State.Phase);
            d2.Log.Infos.Clear();
            d2.Sim.TraceTicks = -1;
            d2.Tick();
            Assert.AreEqual(1, d2.Log.Infos.Count(s => s.Contains("[trace]")));
        }

        // ---------------- §0.2 同 tick / 不同 tick ----------------

        [Test]
        public void A76_TimeoutGivesVictory_ClockExactly180()
        {
            var d = new SimDriver();
            d.Sim.DebugSetTime(179f);
            for (int i = 0; i < 200 && d.Sim.State.Phase == RunPhase.Playing; i++) d.Tick();
            Assert.AreEqual(RunPhase.Victory, d.Sim.State.Phase);
            Assert.AreEqual(180f, d.Sim.State.Now);
        }

        [Test]
        public void F7_SameTick_DeathAndTimeout_IsDefeat_WithSetHp0()
        {
            var d = new SimDriver();
            d.Sim.FixedDt = 0.05f;
            d.Sim.DebugSetHp(0);
            d.Sim.DebugSetTime(179.99f);
            d.Tick();
            Assert.AreEqual(RunPhase.Defeat, d.Sim.State.Phase);
            Assert.AreEqual(179.99f, d.Sim.State.Now, 1e-4, "Defeat 的时钟固定在死亡 tick 开始时");
        }

        [Test]
        public void A77b_SameTick_ContactDeathAndTimeout_IsDefeat()
        {
            var d = new SimDriver();
            d.Sim.FixedDt = 0.05f;
            d.Sim.DebugSetHp(10);
            d.Sim.DebugSetTime(179.99f);
            d.Sim.DebugHurt(10); // T8 结算，代替敌人接触
            d.Tick();
            Assert.AreEqual(RunPhase.Defeat, d.Sim.State.Phase);
            Assert.AreEqual(0, d.Sim.State.Hp);
        }

        [Test]
        public void A77c_SameSetupWithGod_IsVictory()
        {
            var d = new SimDriver();
            d.Sim.FixedDt = 0.05f;
            d.Sim.God = true;
            d.Sim.DebugSetHp(10);
            d.Sim.DebugSetTime(179.99f);
            d.Sim.DebugHurt(10);
            d.Tick();
            Assert.AreEqual(RunPhase.Victory, d.Sim.State.Phase);
            Assert.AreEqual(10, d.Sim.State.Hp);
        }

        [Test]
        public void Decision51_Alternative_SameTickDeathCountsAsVictory()
        {
            var d = new SimDriver(new RunConfig { defeatWinsSameTick = false });
            d.Sim.FixedDt = 0.05f;
            d.Sim.DebugSetHp(0);
            d.Sim.DebugSetTime(179.99f);
            d.Tick();
            Assert.AreEqual(RunPhase.Victory, d.Sim.State.Phase);

            // 没到 3:00 时仍然是 Defeat，时钟停在 tick 开始
            var d2 = new SimDriver(new RunConfig { defeatWinsSameTick = false });
            d2.Sim.FixedDt = 0.05f;
            d2.Sim.DebugSetTime(10f);
            d2.Sim.DebugSetHp(0);
            d2.Tick();
            Assert.AreEqual(RunPhase.Defeat, d2.Sim.State.Phase);
            Assert.AreEqual(10f, d2.Sim.State.Now, 1e-4);
        }

        [Test]
        public void A77a_DeathOnEarlierTick_LaterTimeoutCannotOverride()
        {
            var d = new SimDriver();
            d.Sim.FixedDt = 0.05f;
            d.Sim.DebugSetTime(179.9f);
            d.Sim.DebugSetHp(10);
            d.Sim.DebugHurt(10);
            d.Tick(); // 179.9：死亡
            Assert.AreEqual(RunPhase.Defeat, d.Sim.State.Phase);
            for (int i = 0; i < 10; i++) d.Tick();
            Assert.AreEqual(RunPhase.Defeat, d.Sim.State.Phase);
            Assert.AreEqual(179.9f, d.Sim.State.Now, 1e-4);
        }

        [Test]
        public void A79_SameTick_LevelUpAndTimeout_IsVictoryWithoutPanel()
        {
            var d = new SimDriver();
            d.Sim.FixedDt = 0.05f;
            d.Sim.DebugSetTime(179.99f);
            d.Sim.DebugGiveXp(5);
            d.Tick();
            Assert.AreEqual(RunPhase.Victory, d.Sim.State.Phase);
            Assert.AreEqual(0, d.Sim.PanelsShown);
            Assert.AreEqual(0, d.Sim.State.PendingLevelUps);
            Assert.AreEqual(2, d.Sim.State.Level, "XP 已结算，只是不弹面板");
            Assert.IsTrue(d.Log.Infos.Any(s => s.Contains("丢弃待升级 1")));
        }

        [Test]
        public void A78_LevelUpFreezesClock_ThenVictoryAt180()
        {
            var d = new SimDriver();
            d.Sim.FixedDt = 0.05f;
            d.Sim.DebugSetTime(179.9f);
            d.Sim.DebugGiveXp(5);
            d.Tick();
            Assert.AreEqual(RunPhase.LevelUp, d.Sim.State.Phase);
            float frozen = d.Sim.State.Now;
            for (int i = 0; i < 100; i++) d.Tick();
            Assert.AreEqual(frozen, d.Sim.State.Now, "面板期间计时冻结");
            Assert.IsTrue(d.ChooseAfterGuard(0));
            Assert.AreEqual(RunPhase.Playing, d.Sim.State.Phase);
            for (int i = 0; i < 10 && d.Sim.State.Phase == RunPhase.Playing; i++) d.Tick();
            Assert.AreEqual(RunPhase.Victory, d.Sim.State.Phase);
            Assert.AreEqual(180f, d.Sim.State.Now);
        }

        // ---------------- A-79b / §8.6 终态锁 ----------------

        [Test]
        public void A79b_TerminalState_RejectsDebugCommandsAndTransitions()
        {
            var d = new SimDriver();
            d.Sim.DebugSetTime(180f);
            d.Tick();
            Assert.AreEqual(RunPhase.Victory, d.Sim.State.Phase);
            int errors = d.Log.Errors.Count;

            d.Sim.DebugGiveXp(100);
            d.Sim.DebugSetHp(0);
            d.Sim.DebugSetTime(0f);
            d.Sim.DebugHurt(50);
            d.PressPause();
            for (int i = 0; i < 5; i++) d.Tick();

            Assert.AreEqual(RunPhase.Victory, d.Sim.State.Phase);
            Assert.AreEqual(180f, d.Sim.State.Now);
            Assert.AreEqual(100, d.Sim.State.Hp);
            Assert.AreEqual(1, d.Sim.State.Level);
            Assert.AreEqual(errors + 4, d.Log.Errors.Count, "4 条调试命令各写 1 条 Error");

            foreach (RunPhase to in new[] { RunPhase.Playing, RunPhase.Paused, RunPhase.LevelUp, RunPhase.Defeat })
                Assert.IsFalse(d.Sim.State.TryTransition(to, "test"));
            Assert.AreEqual(RunPhase.Victory, d.Sim.State.Phase);
        }

        [Test]
        public void TransitionTable_MatchesSection86()
        {
            var log = new ListLog();
            // Playing → 全部允许
            foreach (RunPhase to in new[] { RunPhase.Paused, RunPhase.LevelUp, RunPhase.Victory, RunPhase.Defeat })
                Assert.IsTrue(new RunState(100, log).TryTransition(to, "t"), "Playing→" + to);

            // Paused（来自 Playing）→ 只能回 Playing
            var s = new RunState(100, log);
            s.TryTransition(RunPhase.Paused, "t");
            Assert.IsFalse(s.CanTransition(RunPhase.LevelUp));
            Assert.IsFalse(s.CanTransition(RunPhase.Victory));
            Assert.IsFalse(s.CanTransition(RunPhase.Defeat));
            Assert.IsTrue(s.CanTransition(RunPhase.Playing));

            // Paused（来自 LevelUp）→ 只能回 LevelUp
            var s2 = new RunState(100, log);
            s2.TryTransition(RunPhase.LevelUp, "t");
            s2.TryTransition(RunPhase.Paused, "t");
            Assert.IsFalse(s2.CanTransition(RunPhase.Playing));
            Assert.IsTrue(s2.CanTransition(RunPhase.LevelUp));

            // LevelUp → Playing / Paused / LevelUp，不能直接终局
            var s3 = new RunState(100, log);
            s3.TryTransition(RunPhase.LevelUp, "t");
            Assert.IsTrue(s3.CanTransition(RunPhase.Playing));
            Assert.IsTrue(s3.CanTransition(RunPhase.Paused));
            Assert.IsTrue(s3.CanTransition(RunPhase.LevelUp));
            Assert.IsFalse(s3.CanTransition(RunPhase.Victory));
            Assert.IsFalse(s3.CanTransition(RunPhase.Defeat));

            int before = s.RejectedTransitions;
            Assert.IsFalse(s.TryTransition(RunPhase.Defeat, "forbidden"));
            Assert.AreEqual(before + 1, s.RejectedTransitions);
            Assert.IsTrue(log.Errors.Any(e => e.Contains("拒绝状态切换")));
        }

        // ---------------- 暂停（A-70、A-74、A-75） ----------------

        [Test]
        public void A70_PauseFreezesClock_NoBacklogAfterResume()
        {
            var d = new SimDriver();
            d.Sim.DebugSetTime(60f);
            d.PressPause();
            Assert.AreEqual(RunPhase.Paused, d.Sim.State.Phase);
            float frozen = d.Sim.State.Now;
            for (int i = 0; i < 1800; i++) d.Tick(); // 30 s 现实时间
            Assert.AreEqual(frozen, d.Sim.State.Now);

            d.PressPause(); // 恢复（这一帧 T0 恢复后继续执行 T1–T13）
            Assert.AreEqual(RunPhase.Playing, d.Sim.State.Phase);
            d.Tick(10f); // 一个超长帧
            Assert.LessOrEqual(d.Sim.State.Now - frozen, 2 * Dt + 0.05f + 1e-4f, "恢复后没有积压，单帧 dt 有上限");
        }

        [Test]
        public void DtIsCappedAt005()
        {
            var d = new SimDriver();
            d.Tick(1.0f);
            Assert.AreEqual(0.05f, d.Sim.State.Now, 1e-6);
        }

        [Test]
        public void A75_PauseToggled20Times_StateAndTimeScaleConsistent()
        {
            var d = new SimDriver();
            for (int i = 0; i < 20; i++) d.PressPause();
            Assert.AreEqual(RunPhase.Playing, d.Sim.State.Phase);
            Assert.AreEqual(1f, RunState.TimeScaleFor(d.Sim.State.Phase));
            d.PressPause();
            Assert.AreEqual(RunPhase.Paused, d.Sim.State.Phase);
            Assert.AreEqual(0f, RunState.TimeScaleFor(d.Sim.State.Phase));
        }

        [Test]
        public void A74_FocusLoss_PausesAndStaysPaused()
        {
            var d = new SimDriver();
            d.Sim.RequestPause("失去焦点");
            Assert.AreEqual(RunPhase.Paused, d.Sim.State.Phase);
            d.Sim.RequestPause("失去焦点"); // 重复失焦不会出错
            for (int i = 0; i < 10; i++) d.Tick();
            Assert.AreEqual(RunPhase.Paused, d.Sim.State.Phase);
        }

        [Test]
        public void F4_DodgePressWhilePaused_DoesNotLeak_AndBufferCleared()
        {
            var d = new SimDriver();
            var dodge = InputSnapshot.None; dodge.DodgePressed = true;
            d.Tick(Dt, dodge);
            Assert.AreEqual(1, d.Sim.DodgeRequests);
            Assert.GreaterOrEqual(d.Sim.DodgeBufferUntil, 0f);

            d.PressPause();
            Assert.AreEqual(-1f, d.Sim.DodgeBufferUntil, "离开 Playing 清空闪避缓冲");
            d.Tick(Dt, dodge); // 暂停时按闪避（实际中 Gameplay Map 已关闭，这里再验证门控）
            d.PressPause();
            d.Tick();
            Assert.AreEqual(1, d.Sim.DodgeRequests, "暂停期间的按键不会在恢复后补出");
        }

        // ---------------- 升级队列（A-60~A-63） ----------------

        [Test]
        public void A61_Give30_ThreePanels_EndsAt4_6of14()
        {
            var d = new SimDriver();
            d.Sim.DebugGiveXp(30);
            d.Tick();
            Assert.AreEqual(RunPhase.LevelUp, d.Sim.State.Phase);
            Assert.AreEqual(3, d.Sim.State.PendingLevelUps);
            Assert.AreEqual(4, d.Sim.State.Level);
            Assert.AreEqual(6, d.Sim.State.Xp);

            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(RunPhase.LevelUp, d.Sim.State.Phase);
                Assert.IsTrue(d.ChooseAfterGuard(0));
            }
            Assert.AreEqual(RunPhase.Playing, d.Sim.State.Phase);
            Assert.AreEqual(3, d.Sim.PanelsShown);
            Assert.AreEqual(0, d.Sim.State.PendingLevelUps);
            Assert.AreEqual(3, d.Sim.State.ChosenUpgrades.Count);
            Assert.AreEqual(14, d.Sim.State.XpToNext);
        }

        [TestCase(5, 2, 0, 1)]
        [TestCase(38, 5, 0, 4)]
        [TestCase(300, 14, 1, 13)]
        public void F5_GiveXp_PanelsAndResult(int gain, int level, int xp, int panels)
        {
            var d = new SimDriver();
            d.Sim.DebugGiveXp(gain);
            d.Tick();
            int guard = 0;
            while (d.Sim.State.Phase == RunPhase.LevelUp && guard++ < 50) Assert.IsTrue(d.ChooseAfterGuard(0));
            Assert.AreEqual(level, d.Sim.State.Level);
            Assert.AreEqual(xp, d.Sim.State.Xp);
            Assert.AreEqual(panels, d.Sim.PanelsShown);
            Assert.AreEqual(RunPhase.Playing, d.Sim.State.Phase);
        }

        [Test]
        public void A61c_FromLevel2With3Xp_Give13()
        {
            var d = new SimDriver();
            d.Sim.DebugGiveXp(8); // 1 级 0 → 2 级 3/8
            d.Tick();
            Assert.IsTrue(d.ChooseAfterGuard(0));
            Assert.AreEqual(2, d.Sim.State.Level);
            Assert.AreEqual(3, d.Sim.State.Xp);
            d.Sim.DebugGiveXp(13);
            d.Tick();
            Assert.IsTrue(d.ChooseAfterGuard(0));
            Assert.AreEqual(3, d.Sim.State.Level);
            Assert.AreEqual(8, d.Sim.State.Xp);
            Assert.AreEqual(2, d.Sim.PanelsShown);
        }

        [Test]
        public void A62_SameFrameDoubleSubmit_CountsOnce_AndGuardBlocksEarlyInput()
        {
            var d = new SimDriver();
            d.Sim.DebugGiveXp(30);
            d.Tick();
            // 0.3 s 防误触：面板刚显示时提交被忽略
            Assert.IsFalse(d.Sim.ChooseUpgrade(0));

            Assert.IsTrue(d.ChooseAfterGuard(0));
            // 同一帧再次提交（例如按钮 + 数字键）：被忽略
            Assert.IsFalse(d.Sim.ChooseUpgrade(1));
            Assert.IsFalse(d.Sim.ChooseUpgrade(0));
            Assert.AreEqual(2, d.Sim.State.PendingLevelUps);
            Assert.AreEqual(1, d.Sim.State.ChosenUpgrades.Count);
        }

        [Test]
        public void A63_PauseDuringLevelUp_ReturnsToSamePanel()
        {
            var d = new SimDriver();
            d.Sim.DebugGiveXp(30);
            d.Tick();
            var options = (UpgradeKind[])d.Sim.CurrentOptions.Clone();
            int pending = d.Sim.State.PendingLevelUps;

            d.PressPause();
            Assert.AreEqual(RunPhase.Paused, d.Sim.State.Phase);
            Assert.IsFalse(d.Sim.ChooseUpgrade(0), "暂停中不能选");
            d.PressPause();
            Assert.AreEqual(RunPhase.LevelUp, d.Sim.State.Phase);
            CollectionAssert.AreEqual(options, d.Sim.CurrentOptions);
            Assert.AreEqual(pending, d.Sim.State.PendingLevelUps);
            Assert.AreEqual(1, d.Sim.PanelsShown);
        }

        [Test]
        public void ChooseInput_FromSnapshot_Works()
        {
            var d = new SimDriver();
            d.Sim.DebugGiveXp(5);
            d.Tick();
            d.RealTime += 0.5;
            var s = InputSnapshot.None; s.ChoosePressed = 1;
            d.Tick(Dt, s);
            Assert.AreEqual(RunPhase.Playing, d.Sim.State.Phase);
            Assert.AreEqual(1, d.Sim.State.ChosenUpgrades.Count);
        }

        // ---------------- 升级抽卡与效果 ----------------

        [Test]
        public void Draw_FallbackHeal_WhenFewerThanThree()
        {
            var levels = new int[UpgradeRules.RealKindCount];
            for (int i = 0; i < 5; i++) levels[i] = UpgradeRules.MaxLevel((UpgradeKind)i);
            var opts = UpgradeRules.Draw(levels, new System.Random(1));
            Assert.AreEqual(1, opts.Count(o => o == UpgradeKind.Heal));
            Assert.AreEqual(2, opts.Count(o => o == UpgradeKind.PickupRange || o == UpgradeKind.DodgeCooldown));

            for (int i = 0; i < levels.Length; i++) levels[i] = UpgradeRules.MaxLevel((UpgradeKind)i);
            opts = UpgradeRules.Draw(levels, new System.Random(1));
            CollectionAssert.AreEqual(new[] { UpgradeKind.Heal, UpgradeKind.None, UpgradeKind.None }, opts);
        }

        [Test]
        public void Draw_NoDuplicates_AndDeterministicBySeed()
        {
            var levels = new int[UpgradeRules.RealKindCount];
            for (int seed = 0; seed < 50; seed++)
            {
                var a = UpgradeRules.Draw(levels, new System.Random(seed));
                var b = UpgradeRules.Draw(levels, new System.Random(seed));
                CollectionAssert.AreEqual(a, b);
                Assert.AreEqual(3, a.Distinct().Count());
                Assert.IsFalse(a.Contains(UpgradeKind.Heal));
            }
        }

        // ---------------- 重开（A-80/81/85） ----------------

        [Test]
        public void Restart_NewSimulationIsFresh_NoSharedStaticState()
        {
            var first = new SimDriver();
            first.Sim.DebugGiveXp(30);
            first.Sim.DebugSetTime(100f);
            first.Sim.God = true;
            first.Tick();
            first.ChooseAfterGuard(0);
            first.Sim.DebugSetHp(1);

            // GameLoop 重开 = 重新加载场景 = 新建 RunSimulation
            var second = new SimDriver();
            var st = second.Sim.State;
            Assert.AreEqual(RunPhase.Playing, st.Phase);
            Assert.AreEqual(0f, st.Now);
            Assert.AreEqual(1, st.Level);
            Assert.AreEqual(0, st.Xp);
            Assert.AreEqual(100, st.Hp);
            Assert.AreEqual(100, st.MaxHp);
            Assert.AreEqual(0, st.Kills);
            Assert.AreEqual(0, st.PendingLevelUps);
            Assert.AreEqual(0, st.ChosenUpgrades.Count);
            Assert.IsTrue(st.UpgradeLevels.All(l => l == 0));
            Assert.IsFalse(second.Sim.God);
            Assert.AreEqual(0, second.Sim.PanelsShown);
            Assert.AreEqual(0f, second.Sim.PlayerX);

            // 第一局的状态没有被第二局改动（两局互不影响）
            Assert.AreEqual(4, first.Sim.State.Level);
        }

        [Test]
        public void A84_SameSeed_SamePanelOptions_AcrossRestarts()
        {
            UpgradeKind[] Run()
            {
                var d = new SimDriver();
                d.Sim.DebugGiveXp(5);
                d.Tick();
                return (UpgradeKind[])d.Sim.CurrentOptions.Clone();
            }
            CollectionAssert.AreEqual(Run(), Run());
        }

        [Test]
        public void MaxHpUpgrade_RaisesMaxAndHeals25()
        {
            var d = new SimDriver();
            d.Sim.DebugSetHp(50);
            d.Sim.DebugGiveXp(5);
            d.Tick();
            // 不依赖随机抽卡结果：直接把面板第 1 格设成生命强化
            d.Sim.CurrentOptions[0] = UpgradeKind.MaxHp;
            Assert.IsTrue(d.ChooseAfterGuard(0));
            Assert.AreEqual(125, d.Sim.State.MaxHp);
            Assert.AreEqual(75, d.Sim.State.Hp);
        }

        // ---------------- 受击无敌 ----------------

        [Test]
        public void Hurt_SetsHitIFrame_SecondHitBlockedWithin05s()
        {
            var d = new SimDriver();
            d.Sim.FixedDt = 0.125f; // 二进制精确，避免浮点累加误差
            d.Sim.DebugHurt(10);
            d.Tick(); // now 0 → 受伤，无敌到 0.5
            Assert.AreEqual(90, d.Sim.State.Hp);
            d.Sim.DebugHurt(10);
            d.Tick(); // now 0.125 → 无敌
            Assert.AreEqual(90, d.Sim.State.Hp);
            for (int i = 0; i < 2; i++) d.Tick(); // now → 0.5
            d.Sim.DebugHurt(10);
            d.Tick(); // now 0.5 ≥ 0.5 → 可受伤
            Assert.AreEqual(80, d.Sim.State.Hp);
        }

        // ---------------- 移动 ----------------

        [Test]
        public void Move_DiagonalNotFaster_AndClampedToArena()
        {
            var d = new SimDriver();
            d.Sim.FixedDt = 0.05f;
            var s = InputSnapshot.None; s.MoveX = 1f; s.MoveY = 1f;
            d.Tick(0.05f, s);
            double dist = System.Math.Sqrt(d.Sim.PlayerX * d.Sim.PlayerX + d.Sim.PlayerZ * d.Sim.PlayerZ);
            Assert.AreEqual(5f * 0.05f, dist, 1e-4);

            for (int i = 0; i < 400; i++) d.Tick(0.05f, s);
            Assert.AreEqual(29.5f, d.Sim.PlayerX, 1e-4);
            Assert.AreEqual(29.5f, d.Sim.PlayerZ, 1e-4);
        }
    }
}
