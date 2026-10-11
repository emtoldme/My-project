using System;
using System.Collections.Generic;
using NUnit.Framework;
using Waffle.Combat;

namespace Waffle.Tests.Combat
{
    /// <summary>
    /// C-02 战斗边界：精英 / 方向推进 / 包围圈事件的固定 seed 回归用例（纯规则）。
    /// </summary>
    public class CombatEventGuardTests
    {
        private const float Dt = 1f / 60f;
        private const float View = 14f;

        private static CombatInput Input(float now, Vec2 player, float damage)
        {
            return new CombatInput
            {
                Dt = Dt, RunTime = now, PlayerPos = player, ViewRadius = View,
                Stats = new CombatPlayerStats { Damage = damage, FireInterval = 0.6f, Pierce = 1, MagnetRadius = 3f }
            };
        }

        /// <summary>只保留事件刷怪（普通波次数量置 0），玩家不造成伤害，使存活数只由事件决定。</summary>
        private static CombatConfig EventsOnly()
        {
            var c = new CombatConfig();
            c.waveCounts = new[] { 0, 0, 0, 0 };
            return c;
        }

        private sealed class SpawnLog
        {
            public readonly Dictionary<int, int> BySource = new Dictionary<int, int>();
            public readonly Dictionary<int, float> FirstTimeBySource = new Dictionary<int, float>();
            public readonly List<string> Violations = new List<string>();
            public int MaxAlive;
        }

        /// <summary>按 60fps 跑完整 tick（T3–T8、T10、T13），记录每个新生成敌人的来源、时刻与出生点合法性。</summary>
        private static SpawnLog Run(CombatModule m, float until, Func<float, Vec2> playerAt, float damage)
        {
            var log = new SpawnLog();
            var seen = new HashSet<int>();
            float now = 0f;
            while (now < until)
            {
                Vec2 p = playerAt(now);
                m.StepCombat(Input(now, p, damage));
                for (int i = 0; i < m.Enemies.Count; i++)
                {
                    EnemyState e = m.Enemies[i];
                    if (!seen.Add(e.Serial)) continue;
                    // 新生成的敌人：PrevPosition 是出生点（本 tick 已经移动过一次）
                    Vec2 s = e.PrevPosition;
                    int src = e.SpawnSource;
                    log.BySource[src] = (log.BySource.TryGetValue(src, out int c) ? c : 0) + 1;
                    if (!log.FirstTimeBySource.ContainsKey(src)) log.FirstTimeBySource[src] = now;
                    if (Math.Abs(s.X) > m.Config.spawnLegalHalf + 1e-3f || Math.Abs(s.Y) > m.Config.spawnLegalHalf + 1e-3f)
                        log.Violations.Add($"t={now:F2} src={src} 出界 {s.X:F2},{s.Y:F2}");
                    if (Vec2.Distance(s, p) < View + m.Config.spawnRingInner - 1e-3f)
                        log.Violations.Add($"t={now:F2} src={src} 安全距离内 d={Vec2.Distance(s, p):F2}");
                }
                log.MaxAlive = Math.Max(log.MaxAlive, m.AliveCount);
                m.StepGems(Input(now, p, damage));
                m.Release();
                now += Dt;
            }
            return log;
        }

        private static int Get(SpawnLog l, int src) { return l.BySource.TryGetValue(src, out int c) ? c : 0; }

        /// <summary>
        /// 事件时刻与次数：伤害为 0、普通波次关闭时，事件严格按时间表触发一次，
        /// 允许超过普通上限 30，直到 aliveCap+eventOverflow=50 才截断（防止误把事件也夹到 30）。
        /// </summary>
        [Test]
        public void EventsOnly_FireOnceOnSchedule_OverflowTo50_NotClampedTo30()
        {
            var m = new CombatModule(EventsOnly(), 4242);
            SpawnLog log = Run(m, 180f, t => Vec2.Zero, 0f);

            Assert.IsEmpty(log.Violations, string.Join("\n", log.Violations));
            Assert.AreEqual(0, Get(log, 0), "普通波次已关闭");
            // 60s 推进 10 → 10；90s 圈 16 → 26；120s 圈 16 → 42，精英 → 43；150s 精英 → 44；
            // 160s 推进 12 中只剩 6 个名额 → 50（截断 6）；165s 圈 20 全截断；170s 精英截断。
            Assert.AreEqual(10 + 6, Get(log, 1), "方向推进");
            Assert.AreEqual(16 + 16, Get(log, 2), "包围圈");
            Assert.AreEqual(2, Get(log, 3), "精英");
            Assert.AreEqual(6 + 20 + 1, m.Counters.Truncated);
            Assert.AreEqual(50, log.MaxAlive);
            Assert.Greater(log.MaxAlive, m.Config.aliveCap, "事件额外数量必须允许超过普通上限");
            Assert.AreEqual(60f, log.FirstTimeBySource[1], 1.5f * Dt);
            Assert.AreEqual(90f, log.FirstTimeBySource[2], 1.5f * Dt);
            Assert.AreEqual(120f, log.FirstTimeBySource[3], 1.5f * Dt);
            Assert.AreEqual(0, m.Counters.EnemyPoolMiss);
        }

        /// <summary>精英语义：生成参数、只击杀一次、掉 10 XP 宝石、回池后代号 +1 且旧引用失效。</summary>
        [Test]
        public void Elite_SpawnStats_KilledOnce_Drops10Xp_RecycledWithNewGeneration()
        {
            var c = EventsOnly();
            c.pushTimes = new float[0]; c.pushCounts = new int[0];
            c.ringTimes = new float[0]; c.ringCounts = new int[0];
            c.eliteTimes = new[] { 0.5f };
            var m = new CombatModule(c, 7);
            float now = 0f;
            EnemyState elite = null;
            while (elite == null && now < 1f)
            {
                m.StepCombat(Input(now, Vec2.Zero, 0f)); m.StepGems(Input(now, Vec2.Zero, 0f)); m.Release();
                foreach (var e in m.Enemies) if (e.IsElite) elite = e;
                now += Dt;
            }
            Assert.NotNull(elite, "0.5s 精英未生成");
            Assert.AreEqual(3, elite.SpawnSource);
            Assert.AreEqual(15f * 6f, elite.MaxHp, 1e-4f);
            Assert.AreEqual(3.0f * 0.8f, elite.Speed, 1e-4f);
            Assert.AreEqual(0.8f, elite.Radius, 1e-6f);
            Assert.AreEqual(20, elite.ContactDamage);
            Assert.AreEqual(10, elite.GemValue);
            int serial = elite.Serial, gen = elite.Generation;

            // 让它走到射程内，用高伤害一枪击杀，再把宝石吸过来
            int kills = 0, xp = 0, killEvents = 0;
            Vec2 pl = Vec2.Zero;
            for (int i = 0; i < 60 * 15; i++)
            {
                if (kills > 0 && m.Gems.Count > 0) pl = m.Gems[0].Position; // 走到宝石处拾取
                var r = m.StepCombat(Input(now, pl, 1000f));
                kills += r.Kills;
                foreach (var ev in r.Events) if (ev.Type == PresentationEventType.EnemyKilled && ev.Data == serial) killEvents++;
                xp += m.StepGems(Input(now, pl, 1000f)).XpCollected;
                m.Release();
                now += Dt;
            }
            Assert.AreEqual(1, kills);
            Assert.AreEqual(1, killEvents);
            Assert.AreEqual(10, xp);
            Assert.AreEqual(0, m.Enemies.Count);
            Assert.IsFalse(m.TryGetEnemy(serial, gen, out _), "回池后旧引用必须失效");
            Assert.AreEqual(gen + 1, elite.Generation);
        }

        /// <summary>出生点合法：玩家贴角/贴边/中心、多 seed，正常波次与事件都在安全距离外且在场地内；普通波次不越过 30。</summary>
        [TestCase(1, 0f, 0f)]
        [TestCase(99, 26f, 26f)]
        [TestCase(2026, -28f, 5f)]
        public void SpawnPositions_LegalAndOutsideSafeDistance_NormalCapHeld(int seed, float px, float pz)
        {
            var c = new CombatConfig();
            var m = new CombatModule(c, seed);
            int normalOver = 0;
            var p = new Vec2(px, pz);
            float now = 0f;
            var seen = new HashSet<int>();
            var bad = new List<string>();
            while (now < 180f)
            {
                int normalBefore = 0;
                m.StepCombat(Input(now, p, 0f));
                foreach (var e in m.Enemies)
                {
                    if (!seen.Add(e.Serial)) continue;
                    Vec2 s = e.PrevPosition;
                    if (Math.Abs(s.X) > c.spawnLegalHalf + 1e-3f || Math.Abs(s.Y) > c.spawnLegalHalf + 1e-3f) bad.Add($"出界 src={e.SpawnSource} t={now:F2}");
                    if (Vec2.Distance(s, p) < View + c.spawnRingInner - 1e-3f) bad.Add($"过近 src={e.SpawnSource} t={now:F2} d={Vec2.Distance(s, p):F2}");
                }
                foreach (var e in m.Enemies) if (e.IsAlive && e.SpawnSource == 0) normalBefore++;
                if (m.AliveCount > c.aliveCap + c.eventOverflow) normalOver++;
                m.StepGems(Input(now, p, 0f)); m.Release();
                now += Dt;
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
            Assert.AreEqual(0, normalOver);
            Assert.AreEqual(0, m.Counters.EnemyPoolMiss);
        }

        /// <summary>同 seed 两局完全一致；新建模块状态干净（模拟重开），不受上一局影响。</summary>
        [Test]
        public void SameSeed_Deterministic_NewInstanceClean()
        {
            Func<CombatModule, List<string>> trace = mod =>
            {
                var list = new List<string>();
                var seen = new HashSet<int>();
                float now = 0f;
                while (now < 125f)
                {
                    var pl = new Vec2((float)Math.Sin(now * 0.3) * 10f, 0f);
                    mod.StepCombat(Input(now, pl, 10f));
                    foreach (var e in mod.Enemies)
                        if (seen.Add(e.Serial)) list.Add($"{e.Serial}:{e.SpawnSource}:{e.PrevPosition.X:F3},{e.PrevPosition.Y:F3}");
                    mod.StepGems(Input(now, pl, 10f)); mod.Release();
                    now += Dt;
                }
                return list;
            };
            var a = new CombatModule(new CombatConfig(), 31337);
            var ta = trace(a);
            var b = new CombatModule(new CombatConfig(), 31337);
            Assert.AreEqual(0, b.Enemies.Count); Assert.AreEqual(0, b.Gems.Count); Assert.AreEqual(0, b.Counters.EnemiesSpawned);
            var tb = trace(b);
            CollectionAssert.AreEqual(ta, tb);
            Assert.That(ta.Exists(s => s.Split(':')[1] == "3"), "125s 内应出现精英");
        }
    }
}
