using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.Rules.Encounters;

public class EncounterTests
{
    const float Dt = 0.05f;

    static EncounterContext Ctx(double now, Vec2 player, int normal = 0, int pool = 0, int atk = 0, int natk = 0)
        => new EncounterContext { RunId = 1, Now = now, Dt = Dt, PlayerPos = player, PlayerPrevPos = player, PlayerRadius = 0.5f,
            NormalAlive = normal, PoolUsed = pool, ActiveAttacks = atk, ActiveNormalAttacks = natk };

    static EncounterEnemyView E(int serial, string arch, Vec2 pos, int gen = 0)
        => new EncounterEnemyView { Serial = serial, Generation = gen, ArchetypeId = arch, Pos = pos, PrevPos = pos, Radius = 0.5f, Alive = true };

    static void ApplyMoves(EncounterOutput o, List<EncounterEnemyView> es)
    {
        foreach (var m in o.Moves)
            for (int i = 0; i < es.Count; i++) if (es[i].Serial == m.Serial)
            { var e = es[i]; e.PrevPos = e.Pos; e.Pos = e.Pos + m.Direction * (m.Speed * Dt); es[i] = e; }
    }

    // 跑到冲锋者开始预警，返回攻击
    static EncounterAttackRequest RunToTelegraph(EncounterModule m, List<EncounterEnemyView> es, Vec2 player, ref double t)
    {
        for (int k = 0; k < 400; k++)
        {
            var o = m.Tick(Ctx(t, player), es); ApplyMoves(o, es); t += Dt;
            if (o.NewAttacks.Count > 0) return o.NewAttacks[0];
        }
        Assert.Fail("no telegraph"); return default;
    }

    static string Trace(int seed)
    {
        var m = new EncounterModule(new EncounterConfig(), seed, 1);
        var es = new List<EncounterEnemyView> { E(1, "charger", new Vec2(10, 0)), E(2, "dropper", new Vec2(-12, 3)) };
        var sb = new StringBuilder(); double t = 0;
        for (int k = 0; k < 2400; k++)
        {
            var o = m.Tick(Ctx(t, new Vec2((float)System.Math.Sin(t), 0), normal: 10, pool: 10), es); ApplyMoves(o, es);
            foreach (var s in o.Spawns) sb.Append('S').Append(s.ArchetypeId).Append(s.RequestId);
            foreach (var a in o.NewAttacks) sb.Append('A').Append(a.AttackId).Append(a.Start).Append(a.End).Append(a.ActiveStart.ToString("R"));
            t += Dt;
        }
        return sb.ToString();
    }

    [Test] public void Deterministic_SameSeedSameOutput_DifferentSeedDiffers()
    {
        Assert.AreEqual(Trace(42), Trace(42));
        Assert.AreNotEqual(Trace(42), Trace(43));
    }

    [Test] public void Charger_TelegraphNoDamage_LockNoTracking_SingleCandidate()
    {
        var m = new EncounterModule(new EncounterConfig(), 1, 1);
        var es = new List<EncounterEnemyView> { E(1, "charger", new Vec2(8, 0)) };
        double t = 0; var player = Vec2.Zero;
        var atk = RunToTelegraph(m, es, player, ref t);
        Assert.AreEqual(EncounterShape.Capsule, atk.Shape);
        Assert.AreEqual(0.8, atk.ActiveStart - atk.StartTime, 1e-6);
        Assert.AreEqual(0.35, atk.ActiveEnd - atk.ActiveStart, 1e-6);
        Vec2 lockDir = atk.LockedDir;
        int claims = 0;
        // 玩家在预警中横移；冲锋方向和胶囊不得改变
        for (int k = 0; k < 60; k++)
        {
            player = new Vec2(0, (float)(k * 0.25));
            var o = m.Tick(Ctx(t, player), es); ApplyMoves(o, es);
            foreach (var mv in o.Moves) { Assert.True(mv.Locked || t > atk.ActiveEnd); if (mv.Locked) { Assert.AreEqual(lockDir.X, mv.Direction.X, 1e-6); Assert.AreEqual(lockDir.Y, mv.Direction.Y, 1e-6); } }
            if (t < atk.ActiveStart) Assert.False(m.TryClaimHit(atk.AttackId, t), "telegraph damage");
            else if (m.TryClaimHit(atk.AttackId, t)) claims++;
            if (m.TryGetAttack(atk.AttackId, out var cur)) Assert.AreEqual(atk.End.Y, cur.End.Y, 1e-6);
            t += Dt;
        }
        Assert.AreEqual(1, claims);
    }

    [Test] public void Cancel_OnDeath_Generation_Relocation()
    {
        foreach (int mode in new[] { 0, 1, 2, 3 })
        {
            var m = new EncounterModule(new EncounterConfig(), 7, 1);
            var es = new List<EncounterEnemyView> { E(1, "charger", new Vec2(8, 0)) };
            double t = 0; var atk = RunToTelegraph(m, es, Vec2.Zero, ref t);
            Assert.AreEqual(1, m.ActiveAttackCount);
            EncounterOutput o;
            if (mode == 0) { Assert.AreEqual(atk.AttackId, m.OnEnemyRemoved(1, 0)); o = m.Tick(Ctx(t, Vec2.Zero), new List<EncounterEnemyView>()); }
            else
            {
                var e = es[0];
                if (mode == 1) e.Generation = 1;
                if (mode == 2) e.Relocated = true;
                if (mode == 3) { e.PrevPos = e.Pos; e.Pos = new Vec2(-20, 0); }
                if (mode == 1 || mode == 2) e.Pos = new Vec2(30, 30); // 回池复用/挪位后都在远处
                es[0] = e;
                o = m.Tick(Ctx(t, Vec2.Zero), es);
                CollectionAssert.Contains(o.CancelledAttacks, atk.AttackId, "mode " + mode);
                foreach (var mv in o.Moves) Assert.False(mv.Locked, "behavior must reset, mode " + mode);
            }
            Assert.False(m.TryGetAttack(atk.AttackId, out _));
            Assert.False(m.TryClaimHit(atk.AttackId, atk.ActiveStart + 0.01));
            Assert.AreEqual(0, m.ActiveAttackCount);
        }
    }

    [Test] public void Budget_Full_NoBurstAfterRecovery()
    {
        var cfg = new EncounterConfig();
        var m = new EncounterModule(cfg, 3, 1);
        var es = new List<EncounterEnemyView>();
        for (int i = 0; i < 6; i++) es.Add(E(i + 1, "charger", new Vec2(4, i * 0.1f)));
        double t = 0;
        var full = m.Tick(Ctx(t, Vec2.Zero, normal: 48, pool: 56, atk: 12, natk: 8), es);
        Assert.AreEqual(0, full.NewAttacks.Count); Assert.AreEqual(0, full.Spawns.Count);
        for (int k = 0; k < 200; k++) { t += Dt; m.Tick(Ctx(t, Vec2.Zero, normal: 48, pool: 56, atk: 12, natk: 8), es); }
        t += Dt;
        var rec = m.Tick(Ctx(t, Vec2.Zero, normal: 0, pool: 0, atk: 6, natk: 6), es);
        Assert.LessOrEqual(rec.NewAttacks.Count, 2, "normal budget 8 - 6");
        Assert.LessOrEqual(rec.Spawns.Count, cfg.maxSpawnsPerTick);
        // 大 dt 也不补发
        var big = new EncounterContext { Now = t + 30, Dt = 30f, NormalAlive = 0, PoolUsed = 0 };
        Assert.LessOrEqual(m.Tick(big, new List<EncounterEnemyView>()).Spawns.Count, cfg.maxSpawnsPerTick);
    }

    [Test] public void Restart_NewInstanceClean()
    {
        var a = new EncounterModule(new EncounterConfig(), 5, 1);
        var es = new List<EncounterEnemyView> { E(1, "charger", new Vec2(8, 0)) };
        double t = 0; RunToTelegraph(a, es, Vec2.Zero, ref t);
        var b = new EncounterModule(new EncounterConfig(), 5, 2);
        Assert.AreEqual(0, b.ActiveAttackCount); Assert.AreEqual(0, b.TrackedActorCount);
        var o = b.Tick(Ctx(0, Vec2.Zero), new List<EncounterEnemyView>());
        Assert.AreEqual(0, o.NewAttacks.Count);
    }

    [Test] public void Sidestep_Feasible_GeometryAndSim()
    {
        var cfg = new EncounterConfig();
        Assert.True(cfg.SidestepFeasible(5f, 0.5f, out float need, out float have), need + " vs " + have);
        // 仿真：玩家反应 0.25s 后以 5m/s 垂直于冲锋方向行走，生效窗口内距胶囊中线始终 > 半宽+玩家半径
        var m = new EncounterModule(cfg, 9, 1);
        var es = new List<EncounterEnemyView> { E(1, "charger", new Vec2(8, 0)) };
        double t = 0; var atk = RunToTelegraph(m, es, Vec2.Zero, ref t);
        double startMove = atk.StartTime + cfg.reactionAllowance;
        for (double tt = atk.ActiveStart; tt <= atk.ActiveEnd; tt += 0.01)
        {
            float lateral = (float)(5.0 * (tt - startMove));
            Assert.Greater(lateral, cfg.charger.hitHalfWidth + 0.5f);
        }
    }

    [Test] public void Dropper_KeepsRange_CircleAtLockedPlayerPos_StageMix()
    {
        var cfg = new EncounterConfig();
        var m = new EncounterModule(cfg, 11, 1);
        var es = new List<EncounterEnemyView> { E(1, "dropper", new Vec2(3, 0)) };
        double t = 0; EncounterAttackRequest? atk = null;
        for (int k = 0; k < 200 && atk == null; k++)
        { var o = m.Tick(Ctx(t, Vec2.Zero), es); ApplyMoves(o, es); if (o.NewAttacks.Count > 0) atk = o.NewAttacks[0]; t += Dt; }
        Assert.NotNull(atk);
        var a = atk.Value;
        Assert.AreEqual(EncounterShape.Circle, a.Shape); Assert.AreEqual(1.1f, a.Radius, 1e-6);
        Assert.AreEqual(1.0, a.ActiveStart - a.StartTime, 1e-6); Assert.AreEqual(0.2, a.ActiveEnd - a.ActiveStart, 1e-6);
        Assert.AreEqual(0f, a.Start.X, 1e-6);
        for (int k = 0; k < 200; k++) { var o = m.Tick(Ctx(t, Vec2.Zero), es); ApplyMoves(o, es); t += Dt; }
        float d = es[0].Pos.Length; Assert.That(d, Is.InRange(cfg.dropper.keepMin - 0.2f, cfg.dropper.keepMax + 0.2f));
        // 五段：时间点、首段无落波者、末段两种新敌人都有
        Assert.AreEqual(6, cfg.stages.Length);
        float[] ts = { 0, 60, 120, 240, 360, 480 };
        for (int i = 0; i < 6; i++) { Assert.AreEqual(ts[i], cfg.stages[i].startTime); Assert.LessOrEqual(cfg.stages[i].normalCap, 48); }
        Assert.AreEqual(0f, cfg.stages[0].wCharger); Assert.AreEqual(0f, cfg.stages[1].wDropper); Assert.Greater(cfg.stages[5].wCharger, 0); Assert.Greater(cfg.stages[5].wDropper, 0);
        // 0–60 s 无冲锋者，60–120 s 出现冲锋者且无落波者
        var early = new EncounterModule(cfg, 11, 1); int c0 = 0, c1 = 0, d1 = 0;
        for (double tt = 0; tt < 120; tt += Dt)
            foreach (var s in early.Tick(new EncounterContext { Now = tt, Dt = Dt }, new List<EncounterEnemyView>()).Spawns)
            { if (s.ArchetypeId == "charger") { if (tt < 60) c0++; else c1++; } if (s.ArchetypeId == "dropper") d1++; }
        Assert.AreEqual(0, c0); Assert.Greater(c1, 0); Assert.AreEqual(0, d1);
        var mix = new EncounterModule(cfg, 11, 1); var counts = new Dictionary<string, int>();
        for (double tt = 480; tt < 600; tt += Dt)
            foreach (var s in mix.Tick(new EncounterContext { Now = tt, Dt = Dt }, new List<EncounterEnemyView>()).Spawns)
                counts[s.ArchetypeId] = counts.TryGetValue(s.ArchetypeId, out var c) ? c + 1 : 1;
        Assert.Greater(counts["charger"], 0); Assert.Greater(counts["dropper"], 0);
    }
}
