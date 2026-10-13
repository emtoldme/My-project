using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Waffle;
using Waffle.Combat;
using Waffle.GameplayNext;
using Waffle.Rules.Encounters;

// 通过真实 GP00 宿主（RunSimulation + CombatModule.GameplayNext）驱动 EncounterModule。
public class EncounterTests
{
    const float Dt = 0.05f;

    static EncounterConfig OnlyCfg(float wCh, float wCg, float wDr, float interval = 0.5f, int cap = 1)
    {
        var c = new EncounterConfig();
        c.stages = new[] { new EncounterStage(0f, interval, cap, wCh, wCg, wDr) };
        c.eliteTimes = new float[0];
        c.chargerSpec.contactDamage = 0; c.dropperSpec.contactDamage = 0; c.chaserSpec.contactDamage = 0;
        c.chargerSpec.maxHp = 1e5f; c.dropperSpec.maxHp = 1e5f;
        c.spawnRingMin = 7f; c.spawnRingMax = 7.5f;
        return c;
    }

    static RunSimulation Make(EncounterModule m, int seed = 12345, float duration = 600f, float weaponDamage = 0f)
    {
        var config = new RunConfig { runDuration = duration, damage = weaponDamage, hitIFrame = .01f, seed = seed,
            gameplayNext = new GameplayNextSettings { enabled = true } };
        var combat = new CombatConfig { contactDamage = 0, aliveCap = 48, eventOverflow = 8 };
        var sim = new RunSimulation(config, null, combat);
        sim.InstallGameplayModules(new GameplayModules(m));
        return sim;
    }

    static void Tick(RunSimulation sim, float mx = 0, float my = 0)
    {
        var input = InputSnapshot.None; input.MoveX = mx; input.MoveY = my;
        sim.Tick(Dt, input, sim.State.Frame + 1, sim.State.Frame * Dt);
        if (sim.State.Phase == RunPhase.LevelUp) sim.Tick(Dt, new InputSnapshot { ChoosePressed = 1 }, sim.State.Frame + 1, sim.State.Frame * Dt);
    }

    static IEnumerable<AttackSnapshot> Attacks(RunSimulation sim) => sim.ReadGameplayView().Attacks;

    static AttackSnapshot WaitAttack(RunSimulation sim, AttackShapeKind kind, int max = 2000)
    {
        for (int i = 0; i < max; i++)
        {
            Tick(sim);
            foreach (var a in Attacks(sim)) if (a.Attack.Shape.Kind == kind && !a.Spent) return a;
        }
        Assert.Fail("no attack " + kind); return default;
    }

    static string Trace(int seed)
    {
        var m = new EncounterModule(); var sim = Make(m, seed, weaponDamage: 10f); sim.God = true;
        var sb = new StringBuilder();
        for (int i = 0; i < 3000; i++) // 150 s
        {
            Tick(sim, (float)Math.Sin(i * 0.01), 0);
            foreach (var w in sim.ReadGameplayView().SpawnWarnings) sb.Append('W').Append(w.Key.Sequence).Append(w.Spawn.Spec.ArchetypeId).Append(w.Spawn.Position);
            foreach (var a in Attacks(sim)) sb.Append('A').Append(a.Key.Sequence).Append(a.Attack.Shape.Center).Append(a.Attack.ActivateAt.ToString("R"));
        }
        return sb.ToString();
    }

    [Test] public void Deterministic_SameSeedSameResult()
    {
        string a = Trace(42);
        Assert.AreEqual(a, Trace(42));
        Assert.AreNotEqual(a, Trace(43));
        StringAssert.Contains("charger", a);
        StringAssert.Contains("dropper", a);
    }

    [Test] public void Charger_TelegraphNoDamage_LockNoTracking_SingleCandidate()
    {
        var m = new EncounterModule(OnlyCfg(0, 1, 0)); var sim = Make(m);
        var atk = WaitAttack(sim, AttackShapeKind.Capsule);
        Assert.True(atk.Attack.SweepOwnerMotion);
        Assert.AreEqual(0.8f, atk.Attack.ActivateAt - atk.CreatedAt, 1e-3);
        Assert.AreEqual(0.35f, atk.Attack.EndAt - atk.Attack.ActivateAt, 1e-3);
        var lockedEnd = atk.Attack.Shape.End; var key = atk.Key;
        int hp0 = sim.State.Hp, hits = 0, prev = hp0;
        // 玩家先横移 0.3 s（预警期），再站回冲锋线上 —— 冲锋路径不得跟随
        for (int i = 0; i < 60; i++)
        {
            float now = sim.State.Now;
            bool before = now + Dt <= atk.Attack.ActivateAt;
            Tick(sim);
            sim.InvulnerableUntil = 0;
            if (before) Assert.AreEqual(hp0, sim.State.Hp, "damage during telegraph");
            if (sim.State.Hp < prev) hits++;
            prev = sim.State.Hp;
            foreach (var a in Attacks(sim))
                if (a.Key.Equals(key) && sim.State.Now < atk.Attack.ActivateAt)
                { Assert.AreEqual(lockedEnd.X, a.Attack.Shape.End.X, 1e-4); Assert.AreEqual(lockedEnd.Y, a.Attack.Shape.End.Y, 1e-4); }
        }
        Assert.AreEqual(1, hits, "exactly one candidate");
        Assert.AreEqual(hp0 - 10, sim.State.Hp);
    }

    [Test] public void Charger_SidestepDodges_FromTelegraphStart()
    {
        var cfg = OnlyCfg(0, 1, 0);
        Assert.True(cfg.SidestepFeasible(5f, 0.5f, out float need, out float have), need + " vs " + have);
        var m = new EncounterModule(cfg); var sim = Make(m);
        var atk = WaitAttack(sim, AttackShapeKind.Capsule);
        Vec2 d = (atk.Attack.Shape.End - atk.Attack.Shape.Center).Normalized;
        Vec2 side = new Vec2(-d.Y, d.X);
        int hp0 = sim.State.Hp;
        for (int i = 0; i < 6; i++) Tick(sim); // 0.3 s 反应
        for (int i = 0; i < 30; i++) { Tick(sim, side.X, side.Y); sim.InvulnerableUntil = 0; }
        Assert.AreEqual(hp0, sim.State.Hp, "walking sideways must dodge the dash");
    }

    // 测试专用：在 T6 后用规则伤害入口击杀目标，让宿主走真实 T7 死亡结算
    sealed class Killer : ICombatRuleExtension
    {
        public CombatModule Host; public int Serial = -1, Generation;
        public void OnDirectHit(float t, int s, int g, Vec2 p, float d) { }
        public void AfterProjectiles(float t)
        { if (Serial >= 0) { Host.ApplyRuleDamage(Serial, Generation, 1e9f, PresentationEventType.EchoHit, Vec2.Zero); Serial = -1; } }
    }

    [Test] public void OwnerDeath_CancelsAttack_And_ModuleDropsState()
    {
        var m = new EncounterModule(OnlyCfg(0, 1, 0)); var sim = Make(m);
        var killer = new Killer { Host = sim.Combat }; sim.Combat.InstallRuleExtension(killer);
        var atk = WaitAttack(sim, AttackShapeKind.Capsule);
        Assert.AreEqual(1, m.TrackedActorCount);
        var owner = sim.Combat.Enemies.First(e => e.IsAlive && e.ArchetypeId == "charger");
        killer.Serial = owner.Serial; killer.Generation = owner.Generation;
        int hp0 = sim.State.Hp;
        Tick(sim);
        Assert.False(owner.IsAlive && owner.Generation == atk.Attack.Owner.Generation, "charger should be dead");
        Assert.False(Attacks(sim).Any(a => a.Key.Equals(atk.Key)), "attack must be cancelled at T7");
        for (int i = 0; i < 30; i++) { Tick(sim); sim.InvulnerableUntil = 0; }
        Assert.False(Attacks(sim).Any(a => a.Attack.Owner.Equals(atk.Attack.Owner)));
        Assert.AreEqual(hp0, sim.State.Hp, "no damage from cancelled dash");
    }

    [Test] public void Relocation_ResetsBehavior_NoInheritedAttack()
    {
        var c = OnlyCfg(0, 0, 1);
        c.dropper.moveSpeed = 0f; c.dropper.firstShotMin = c.dropper.firstShotMax = 3f;
        var m = new EncounterModule(c); var sim = Make(m);
        WaitAttack(sim, AttackShapeKind.Circle);
        var e0 = sim.Combat.Enemies.First(e => e.IsAlive && e.ArchetypeId == "dropper");
        int rv = e0.RelocationVersion;
        int guard = 0;
        while (e0.RelocationVersion == rv && guard++ < 400) Tick(sim, 1, 0); // 跑远触发挪位
        Assert.Greater(e0.RelocationVersion, rv);
        float tReloc = sim.State.Now;
        Assert.False(Attacks(sim).Any(a => a.Attack.Owner.Serial == e0.Serial), "relocated owner keeps no attack");
        // 行为已重置：首发重新计时（≥3 s），不会继承上一轮冷却立即开火
        for (int i = 0; i < 50; i++) { Tick(sim); Assert.False(Attacks(sim).Any(a => a.Attack.Owner.Serial == e0.Serial && a.CreatedAt < tReloc + 2.9f)); }
    }

    [Test] public void Budget_NoBurst_AndRestartClean()
    {
        var c = OnlyCfg(0, 0, 1, 0.05f, 48); // 落波者每 tick 一只，很快顶满普通攻击预算
        c.dropper.firstShotMin = c.dropper.firstShotMax = 0.1f;
        var m = new EncounterModule(c); var sim = Make(m); sim.God = true;
        int maxAtk = 0, maxWarn = 0;
        for (int i = 0; i < 600; i++)
        {
            Tick(sim);
            var v = sim.ReadGameplayView();
            maxAtk = Math.Max(maxAtk, v.Attacks.Count(a => a.Key.Module == GameplayModuleKind.Encounters));
            maxWarn = Math.Max(maxWarn, v.SpawnWarnings.Count);
            Assert.LessOrEqual(v.SpawnWarnings.Count - 0, 8);
        }
        Assert.LessOrEqual(maxAtk, 8);
        Assert.LessOrEqual(sim.Combat.Enemies.Count(e => e.IsAlive && !e.IsBoss), 48);
        // 生成请求数受每 tick 上限约束：600 tick 最多 600 个
        Assert.LessOrEqual(m.SpawnsRequested, 600);
        // 暂停期间不推进
        long before = m.SpawnsRequested; sim.TogglePause("t"); for (int i = 0; i < 100; i++) Tick(sim);
        Assert.AreEqual(before, m.SpawnsRequested); sim.TogglePause("t"); Tick(sim);
        Assert.LessOrEqual(m.SpawnsRequested - before, 1, "no catch-up after pause");
        // Clear 幂等 + 新实例干净
        m.Clear(); m.Clear();
        Assert.AreEqual(0, m.TrackedActorCount); Assert.AreEqual(0, m.SpawnsRequested);
        var fresh = new EncounterModule(c); var sim2 = Make(fresh); Tick(sim2);
        Assert.AreEqual(0, sim2.ReadGameplayView().Attacks.Count);
    }

    [Test] public void Stages_SixWithChargerAt60_DropperAt120_ElitesAt240And480()
    {
        var cfg = new EncounterConfig();
        Assert.AreEqual(6, cfg.stages.Length);
        float[] ts = { 0, 60, 120, 240, 360, 480 };
        for (int i = 0; i < 6; i++) { Assert.AreEqual(ts[i], cfg.stages[i].startTime); Assert.LessOrEqual(cfg.stages[i].normalCap, 48); }
        Assert.AreEqual(0f, cfg.stages[0].wCharger); Assert.AreEqual(0f, cfg.stages[1].wDropper);
        CollectionAssert.AreEqual(new[] { 240f, 480f }, cfg.eliteTimes);

        var m = new EncounterModule(); var sim = Make(m, 11, weaponDamage: 10f); sim.God = true;
        var first = new Dictionary<string, float>(); int elites = 0;
        var seenSerials = new HashSet<long>();
        for (int i = 0; i < (int)(500 / Dt); i++)
        {
            Tick(sim);
            foreach (var e in sim.Combat.Enemies)
            {
                if (!e.IsAlive || !seenSerials.Add(((long)e.Serial << 20) | (uint)e.Generation)) continue;
                if (!first.ContainsKey(e.ArchetypeId)) first[e.ArchetypeId] = sim.State.Now;
                if (e.IsElite) elites++;
            }
        }
        Assert.GreaterOrEqual(first["charger"], 60f); Assert.Less(first["charger"], 120f);
        Assert.GreaterOrEqual(first["dropper"], 120f);
        Assert.AreEqual(2, elites);
        Assert.False(first.ContainsKey("legacy-chaser"), "A installed ⇒ old spawning off");
    }
}
