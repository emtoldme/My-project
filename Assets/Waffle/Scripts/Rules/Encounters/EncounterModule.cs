using System;
using System.Collections.Generic;
using Waffle.Combat;
using Waffle.GameplayNext;

namespace Waffle.Rules.Encounters
{
    /// <summary>
    /// GP-A 遭遇模块：实现 GP00 <see cref="IEncounterModule"/>。宿主每个 Playing tick 在 T3 前调用 Step；
    /// 暂停/升级不调用。只提交生成/移动/攻击请求；命中检测、候选去重、死亡/挪位撤销与预算硬上限由宿主执行。
    /// 工厂每局 new 一个实例；Clear 幂等。
    /// </summary>
    public sealed class EncounterModule : IEncounterModule
    {
        /// <summary>模块盐：在 GP00 ModuleSeed 上再分流，保证与其他 CombatRng 序列独立。</summary>
        public const int ModuleSalt = unchecked((int)0xA11CE5E7u);

        private enum Phase { Approach, Telegraph, Dash, Recover, Keep }

        private sealed class Actor
        {
            public int RelocationVersion;
            public string Archetype;
            public Phase Phase;
            public Vec2 LockedDir;
            public float TelegraphEnd, DashEnd, RecoverEnd, RetryAt, NextShot, AttackEnd = float.NegativeInfinity;
        }

        public readonly EncounterConfig Config;
        private CombatRng rng;
        private bool seeded;
        private readonly Dictionary<int, Actor> actors = new Dictionary<int, Actor>();      // key: Serial
        private readonly Dictionary<int, int> actorGen = new Dictionary<int, int>();
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly List<int> scratch = new List<int>();
        private long spawnSeq, attackSeq;
        private float spawnAcc;
        private int nextElite;
        private float eliteRetryAt;
        private long eliteSeq = -1;
        private int liveOrdinaryAttacks, liveAttacks, pendingMine, ordinaryAlive;

        public EncounterModule(EncounterConfig config = null) { Config = config ?? new EncounterConfig(); }

        public int CurrentStage { get; private set; }
        public int TrackedActorCount { get { return actors.Count; } }
        public long AttacksRequested { get { return attackSeq; } }
        public long SpawnsRequested { get { return spawnSeq; } }

        public void Clear()
        {
            actors.Clear(); actorGen.Clear(); seen.Clear();
            spawnSeq = attackSeq = 0; spawnAcc = 0; nextElite = 0; eliteRetryAt = 0; eliteSeq = -1;
            seeded = false; rng = null; CurrentStage = 0;
        }

        public void Step(GameplayContext ctx, GameplayCommandBuffer cmd)
        {
            if (!seeded) { rng = new CombatRng(unchecked(ctx.ModuleSeed ^ ModuleSalt)); seeded = true; }
            CurrentStage = Config.StageAt(ctx.Now);
            cmd.PublishEncounter(CurrentStage, StageLabel(CurrentStage));

            liveAttacks = ctx.ActiveAttacks.Count; liveOrdinaryAttacks = 0;
            foreach (var a in ctx.ActiveAttacks) if (a.Key.Module == GameplayModuleKind.Encounters) liveOrdinaryAttacks++;
            pendingMine = 0;
            foreach (var w in ctx.SpawnWarnings) pendingMine++;
            ordinaryAlive = 0;

            seen.Clear();
            for (int i = 0; i < ctx.Enemies.Count; i++)
            {
                var e = ctx.Enemies[i];
                if (!e.IsAlive || e.IsBoss) continue;
                ordinaryAlive++;
                bool mine = e.ArchetypeId == EncounterArchetypes.Charger || e.ArchetypeId == EncounterArchetypes.Dropper;
                if (!mine) continue;
                Actor a;
                int gen;
                if (actors.TryGetValue(e.Key.Serial, out a) && actorGen.TryGetValue(e.Key.Serial, out gen)
                    && (gen != e.Key.Generation || a.RelocationVersion != e.RelocationVersion || a.Archetype != e.ArchetypeId))
                    a = null; // 世代复用或挪位：丢弃旧状态（宿主已撤销旧攻击）
                if (a == null)
                {
                    a = new Actor { RelocationVersion = e.RelocationVersion, Archetype = e.ArchetypeId };
                    a.Phase = e.ArchetypeId == EncounterArchetypes.Charger ? Phase.Approach : Phase.Keep;
                    if (a.Phase == Phase.Keep) a.NextShot = ctx.Now + rng.Range(Config.dropper.firstShotMin, Config.dropper.firstShotMax);
                    actors[e.Key.Serial] = a; actorGen[e.Key.Serial] = e.Key.Generation;
                }
                seen.Add(e.Key.Serial);
                if (a.Phase == Phase.Keep) StepDropper(ctx, cmd, e, a); else StepCharger(ctx, cmd, e, a);
            }
            scratch.Clear();
            foreach (var kv in actors) if (!seen.Contains(kv.Key)) scratch.Add(kv.Key);
            foreach (int s in scratch) { actors.Remove(s); actorGen.Remove(s); } // 死亡/回池：宿主已在 T7 撤销攻击

            StepElites(ctx, cmd);
            StepSpawns(ctx, cmd);
        }

        private static string StageLabel(int i) { return "stage-" + i; }

        private bool AttackBudgetOk()
        {
            return liveAttacks < Config.attackCap && liveOrdinaryAttacks < Config.normalAttackCap;
        }

        private bool SubmitAttack(GameplayCommandBuffer cmd, HostileAttackIntent proto)
        {
            var intent = new HostileAttackIntent(++attackSeq, proto.Owner, proto.Shape, proto.ActivateAt, proto.EndAt, proto.Damage, proto.SweepOwnerMotion);
            if (!cmd.TryAttack(intent)) return false;
            liveAttacks++; liveOrdinaryAttacks++;
            return true;
        }

        private void StepCharger(GameplayContext ctx, GameplayCommandBuffer cmd, GameplayEnemySnapshot e, Actor a)
        {
            var c = Config.charger;
            float now = ctx.Now, dt = ctx.Dt;
            if (a.Phase == Phase.Telegraph && now + dt > a.TelegraphEnd) a.Phase = Phase.Dash;
            if (a.Phase == Phase.Dash && now >= a.DashEnd) { a.Phase = Phase.Recover; a.RecoverEnd = a.DashEnd + c.recover; }
            if (a.Phase == Phase.Recover && now >= a.RecoverEnd) a.Phase = Phase.Approach;

            if (a.Phase == Phase.Approach)
            {
                Vec2 to = ctx.PlayerPosition - e.Position;
                if (to.SqrLength <= c.triggerRange * c.triggerRange && now >= a.RetryAt)
                {
                    Vec2 dir = to.Normalized; if (dir.SqrLength < 0.5f) dir = new Vec2(1f, 0f);
                    float tEnd = now + c.telegraph, dEnd = tEnd + c.dashDuration;
                    var shape = new AttackShape(AttackShapeKind.Capsule, e.Position, c.hitHalfWidth, e.Position + dir * (c.dashSpeed * c.dashDuration));
                    if (AttackBudgetOk() && SubmitAttack(cmd, new HostileAttackIntent(0, e.Key, shape, tEnd, dEnd, c.damage, true)))
                    {
                        a.LockedDir = dir; a.TelegraphEnd = tEnd; a.DashEnd = dEnd; a.Phase = Phase.Telegraph;
                        if (now + dt > tEnd) a.Phase = Phase.Dash;
                    }
                    else a.RetryAt = now + c.budgetRetryDelay; // 不排队、不补发
                }
                if (a.Phase == Phase.Approach) { cmd.TryMove(new MovementIntent(e.Key, to.Normalized * c.approachSpeed, true, true)); return; }
            }
            if (a.Phase == Phase.Dash)
            {
                // 只在与 [TelegraphEnd, DashEnd] 重叠的时段内位移，冲锋长度与 tick 粒度无关
                float overlap = Math.Min(now + dt, a.DashEnd) - Math.Max(now, a.TelegraphEnd);
                float k = dt > 0 ? Math.Max(0f, overlap) / dt : 0f;
                cmd.TryMove(new MovementIntent(e.Key, a.LockedDir * (c.dashSpeed * k), false, false));
            }
            else cmd.TryMove(new MovementIntent(e.Key, Vec2.Zero, false, false)); // 预警/恢复：原地、不推挤、不挪位
        }

        private void StepDropper(GameplayContext ctx, GameplayCommandBuffer cmd, GameplayEnemySnapshot e, Actor a)
        {
            var d = Config.dropper;
            float now = ctx.Now;
            Vec2 to = ctx.PlayerPosition - e.Position;
            float dist = to.Length;
            Vec2 dir = to.Normalized;
            Vec2 vel = dist > d.keepMax ? dir * d.moveSpeed : dist < d.keepMin ? dir * -d.moveSpeed : Vec2.Zero;
            cmd.TryMove(new MovementIntent(e.Key, vel, true, true));

            if (now >= a.NextShot && now >= a.AttackEnd)
            {
                float act = now + d.telegraph;
                var shape = new AttackShape(AttackShapeKind.Circle, ctx.PlayerPosition, d.radius);
                if (AttackBudgetOk() && SubmitAttack(cmd, new HostileAttackIntent(0, e.Key, shape, act, act + d.active, d.damage)))
                { a.AttackEnd = act + d.active; a.NextShot = now + d.interval; }
                else a.NextShot = now + d.budgetRetryDelay;
            }
        }

        private void StepElites(GameplayContext ctx, GameplayCommandBuffer cmd)
        {
            var times = Config.eliteTimes;
            if (times == null) return;
            while (nextElite < times.Length)
            {
                float t = times[nextElite];
                if (ctx.Now < t) return;
                // 已出生？
                if (eliteSeq > 0)
                {
                    foreach (var e in ctx.Enemies)
                        if (e.SpawnRequest.Module == GameplayModuleKind.Encounters && e.SpawnRequest.Sequence == eliteSeq)
                        { nextElite++; eliteSeq = -1; goto next; }
                    foreach (var w in ctx.SpawnWarnings)
                        if (w.Key.Module == GameplayModuleKind.Encounters && w.Key.Sequence == eliteSeq) return; // 仍在预警
                }
                if (ctx.Now > t + Config.eliteRetryWindow) { nextElite++; eliteSeq = -1; continue; } // 放弃，不补发
                if (ctx.Now < eliteRetryAt) return;
                eliteRetryAt = ctx.Now + Config.eliteRetryDelay;
                long seq;
                if (TrySubmitSpawn(ctx, cmd, Config.eliteSpec, out seq)) eliteSeq = seq;
                return;
            next:;
            }
        }

        private void StepSpawns(GameplayContext ctx, GameplayCommandBuffer cmd)
        {
            var st = Config.stages[CurrentStage];
            if (st.spawnInterval <= 0f) return;
            spawnAcc += ctx.Dt;
            int cap = Math.Min(st.normalCap, Config.normalEnemyCap);
            int made = 0;
            while (spawnAcc >= st.spawnInterval && made < Config.maxSpawnsPerTick)
            {
                spawnAcc -= st.spawnInterval;
                if (ordinaryAlive + pendingMine >= cap || pendingMine >= Config.pendingSpawnCap) { spawnAcc = 0; break; }
                float total = st.wChaser + st.wCharger + st.wDropper;
                float r = rng.Next01() * total;
                var spec = r < st.wChaser ? Config.chaserSpec : r < st.wChaser + st.wCharger ? Config.chargerSpec : Config.dropperSpec;
                long seq;
                if (!TrySubmitSpawn(ctx, cmd, spec, out seq)) { spawnAcc = 0; break; }
                made++;
            }
            if (spawnAcc > st.spawnInterval) spawnAcc = st.spawnInterval; // 不积攒补发
        }

        private bool TrySubmitSpawn(GameplayContext ctx, GameplayCommandBuffer cmd, EncounterArchetypeSpec s, out long seq)
        {
            seq = -1;
            if (pendingMine >= Config.pendingSpawnCap) return false;
            float lim = ctx.ArenaHalfSize - s.radius;
            Vec2 p = ctx.PlayerPosition;
            for (int tries = 0; tries < 6; tries++)
            {
                p = ctx.PlayerPosition + Vec2.FromAngle(rng.Range(0f, (float)(Math.PI * 2))) * rng.Range(Config.spawnRingMin, Config.spawnRingMax);
                if (Math.Abs(p.X) <= lim && Math.Abs(p.Y) <= lim) break;
            }
            p = new Vec2(Math.Max(-lim, Math.Min(lim, p.X)), Math.Max(-lim, Math.Min(lim, p.Y)));
            var spec = new EnemySpawnSpec(s.id, s.maxHp, s.speed, s.radius, s.contactDamage, s.gemValue, s.elite, false);
            var intent = new SpawnIntent(spawnSeq + 1, p, spec, Config.spawnWarning);
            if (!cmd.TrySpawn(intent)) return false;
            seq = ++spawnSeq; pendingMine++;
            return true;
        }
    }
}
