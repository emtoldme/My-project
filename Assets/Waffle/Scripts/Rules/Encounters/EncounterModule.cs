using System;
using System.Collections.Generic;
using Waffle.Combat;

namespace Waffle.Rules.Encounters
{
    /// <summary>
    /// GP-A 遭遇模块（纯规则，GP00 之前编写）。每局新建一个实例；暂停/升级时宿主不调用 Tick。
    /// 不持有 HP/XP、不读 Time/Input、不做碰撞；只输出生成/移动/攻击意图，由宿主执行。
    /// </summary>
    public sealed class EncounterModule
    {
        private enum Phase { Approach, Telegraph, Dash, Recover, Keep }

        private sealed class Actor
        {
            public int Generation;
            public string Archetype;
            public Phase Phase;
            public Vec2 LockedDir;
            public long AttackId = -1;
            public double TelegraphEnd, DashEnd, RecoverEnd, RetryAt, NextShot;
        }

        private sealed class Attack
        {
            public EncounterAttackRequest Req;
            public bool Claimed;
        }

        public readonly EncounterConfig Config;
        public readonly int RunId;
        private readonly EncounterRng rng;
        private readonly Dictionary<int, Actor> actors = new Dictionary<int, Actor>();
        private readonly Dictionary<long, Attack> attacks = new Dictionary<long, Attack>();
        private readonly List<int> scratch = new List<int>();
        private readonly List<long> expired = new List<long>();
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly EncounterOutput output = new EncounterOutput();
        private long nextAttackId = 1, nextSpawnId = 1;
        private double spawnAcc;
        private int newAttacksThisTick, newNormalThisTick;

        public EncounterModule(EncounterConfig config, int runSeed, int runId)
        {
            Config = config ?? new EncounterConfig();
            RunId = runId;
            rng = new EncounterRng(runSeed);
        }

        public int ActiveAttackCount { get { return attacks.Count; } }
        public int TrackedActorCount { get { return actors.Count; } }
        public int CurrentStage { get; private set; }

        public bool TryGetAttack(long id, out EncounterAttackRequest req)
        {
            Attack a; if (attacks.TryGetValue(id, out a)) { req = a.Req; return true; }
            req = default(EncounterAttackRequest); return false;
        }

        /// <summary>T8 前：宿主几何检测命中后调用。仅在生效窗口内且该 AttackId 未用过时返回 true（每招最多一次伤害候选）。</summary>
        public bool TryClaimHit(long attackId, double now)
        {
            Attack a;
            if (!attacks.TryGetValue(attackId, out a) || a.Claimed) return false;
            if (now < a.Req.ActiveStart || now > a.Req.ActiveEnd) return false;
            a.Claimed = true;
            return true;
        }

        /// <summary>T7 死亡结算后调用：撤销该实体未结算攻击并重置行为。返回被撤销的 AttackId（无则 -1）。</summary>
        public long OnEnemyRemoved(int serial, int generation)
        {
            Actor a;
            if (!actors.TryGetValue(serial, out a) || a.Generation != generation) return -1;
            long id = a.AttackId;
            Drop(serial, null);
            return id;
        }

        /// <summary>T3/T4：输入 T2 后玩家位置与敌人快照，输出本 tick 意图。返回对象在下一次 Tick 前有效。</summary>
        public EncounterOutput Tick(EncounterContext ctx, IList<EncounterEnemyView> enemies)
        {
            output.Clear();
            newAttacksThisTick = 0; newNormalThisTick = 0;
            CurrentStage = Config.StageAt(ctx.Now);

            // 结束的攻击回收
            expired.Clear();
            foreach (var kv in attacks) if (ctx.Now > kv.Value.Req.ActiveEnd) expired.Add(kv.Key);
            foreach (long lid in expired)
            {
                attacks.Remove(lid);
                foreach (var ac in actors.Values) if (ac.AttackId == lid) ac.AttackId = -1;
            }

            seen.Clear();
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                bool mine = e.ArchetypeId == EncounterArchetypes.Charger || e.ArchetypeId == EncounterArchetypes.Dropper;
                if (!mine || !e.Alive || e.Boss) continue;
                Actor a;
                if (actors.TryGetValue(e.Serial, out a))
                {
                    bool jumped = Vec2.SqrDistance(e.Pos, e.PrevPos) > Config.relocateJump * Config.relocateJump;
                    if (a.Generation != e.Generation || a.Archetype != e.ArchetypeId || e.Relocated || jumped)
                    { Drop(e.Serial, output.CancelledAttacks); a = null; }
                }
                if (a == null)
                {
                    a = new Actor { Generation = e.Generation, Archetype = e.ArchetypeId };
                    a.Phase = e.ArchetypeId == EncounterArchetypes.Charger ? Phase.Approach : Phase.Keep;
                    if (a.Phase == Phase.Keep) a.NextShot = ctx.Now + rng.Range(Config.dropper.firstShotMin, Config.dropper.firstShotMax);
                    actors[e.Serial] = a;
                }
                seen.Add(e.Serial);
                if (a.Phase == Phase.Keep) TickDropper(ref ctx, e, a); else TickCharger(ref ctx, e, a);
            }

            scratch.Clear();
            foreach (var kv in actors) if (!seen.Contains(kv.Key)) scratch.Add(kv.Key);
            foreach (int s in scratch) Drop(s, output.CancelledAttacks);

            TickSpawns(ref ctx);
            return output;
        }

        private void Drop(int serial, List<long> cancelled)
        {
            Actor a;
            if (!actors.TryGetValue(serial, out a)) return;
            if (a.AttackId >= 0 && attacks.Remove(a.AttackId) && cancelled != null) cancelled.Add(a.AttackId);
            actors.Remove(serial);
        }

        private bool BudgetOk(ref EncounterContext ctx)
        {
            return ctx.ActiveAttacks + newAttacksThisTick < Config.attackCap
                && ctx.ActiveNormalAttacks + newNormalThisTick < Config.normalAttackCap;
        }

        private long Emit(EncounterAttackRequest r)
        {
            r.AttackId = nextAttackId++;
            r.Normal = true;
            attacks[r.AttackId] = new Attack { Req = r };
            output.NewAttacks.Add(r);
            newAttacksThisTick++; newNormalThisTick++;
            return r.AttackId;
        }

        private void Move(EncounterEnemyView e, Vec2 dir, float speed, bool locked)
        {
            output.Moves.Add(new EncounterMoveRequest
            { Serial = e.Serial, Generation = e.Generation, Direction = dir, Speed = speed, Locked = locked, AllowRelocate = !locked });
        }

        private void TickCharger(ref EncounterContext ctx, EncounterEnemyView e, Actor a)
        {
            var c = Config.charger;
            double now = ctx.Now;
            if (a.Phase == Phase.Telegraph && now >= a.TelegraphEnd) a.Phase = Phase.Dash;
            if (a.Phase == Phase.Dash && now >= a.DashEnd) { a.Phase = Phase.Recover; a.RecoverEnd = a.DashEnd + c.recover; }
            if (a.Phase == Phase.Recover && now >= a.RecoverEnd) a.Phase = Phase.Approach;

            if (a.Phase == Phase.Approach)
            {
                Vec2 to = ctx.PlayerPos - e.Pos;
                if (to.SqrLength <= c.triggerRange * c.triggerRange && now >= a.RetryAt && a.AttackId < 0)
                {
                    if (!BudgetOk(ref ctx)) { output.AttacksDeferredByBudget++; a.RetryAt = now + c.budgetRetryDelay; }
                    else
                    {
                        Vec2 dir = to.Normalized; if (dir.SqrLength < 0.5f) dir = new Vec2(1f, 0f);
                        a.LockedDir = dir;
                        a.TelegraphEnd = now + c.telegraph;
                        a.DashEnd = a.TelegraphEnd + c.dashDuration;
                        a.Phase = Phase.Telegraph;
                        a.AttackId = Emit(new EncounterAttackRequest
                        {
                            OwnerSerial = e.Serial, OwnerGeneration = e.Generation, Shape = EncounterShape.Capsule,
                            Start = e.Pos, End = e.Pos + dir * (c.dashSpeed * c.dashDuration), Radius = c.hitHalfWidth,
                            LockedDir = dir, StartTime = now, ActiveStart = a.TelegraphEnd, ActiveEnd = a.DashEnd, Damage = c.damage
                        });
                    }
                }
                if (a.Phase == Phase.Approach) { Move(e, to.Normalized, c.approachSpeed, false); return; }
            }
            if (a.Phase == Phase.Dash) Move(e, a.LockedDir, c.dashSpeed, true);
            else Move(e, a.LockedDir, 0f, true); // 预警/恢复：原地，不转向
        }

        private void TickDropper(ref EncounterContext ctx, EncounterEnemyView e, Actor a)
        {
            var d = Config.dropper;
            double now = ctx.Now;
            Vec2 to = ctx.PlayerPos - e.Pos;
            float dist = to.Length;
            Vec2 dir = to.Normalized;
            if (dist > d.keepMax) Move(e, dir, d.moveSpeed, false);
            else if (dist < d.keepMin) Move(e, dir * -1f, d.moveSpeed, false);
            else Move(e, dir, 0f, false);

            if (now >= a.NextShot && a.AttackId < 0)
            {
                if (!BudgetOk(ref ctx)) { output.AttacksDeferredByBudget++; a.NextShot = now + d.budgetRetryDelay; return; }
                double act = now + d.telegraph;
                a.AttackId = Emit(new EncounterAttackRequest
                {
                    OwnerSerial = e.Serial, OwnerGeneration = e.Generation, Shape = EncounterShape.Circle,
                    Start = ctx.PlayerPos, End = ctx.PlayerPos, Radius = d.radius, LockedDir = Vec2.Zero,
                    StartTime = now, ActiveStart = act, ActiveEnd = act + d.active, Damage = d.damage
                });
                a.NextShot = now + d.interval;
            }
        }

        private void TickSpawns(ref EncounterContext ctx)
        {
            var st = Config.stages[CurrentStage];
            if (st.spawnInterval <= 0f) return;
            spawnAcc += ctx.Dt;
            int cap = Math.Min(st.normalCap, Config.normalEnemyCap);
            int made = 0;
            while (spawnAcc >= st.spawnInterval && made < Config.maxSpawnsPerTick)
            {
                spawnAcc -= st.spawnInterval;
                if (ctx.NormalAlive + made >= cap || ctx.PoolUsed + made >= Config.enemyPoolCap)
                { output.SpawnsSkippedByBudget++; spawnAcc = 0; break; }
                float total = st.wChaser + st.wCharger + st.wDropper;
                float r = rng.Value01() * total;
                string arch = r < st.wChaser ? EncounterArchetypes.Chaser
                    : r < st.wChaser + st.wCharger ? EncounterArchetypes.Charger : EncounterArchetypes.Dropper;
                output.Spawns.Add(new EncounterSpawnRequest
                {
                    RequestId = ((long)RunId << 32) | nextSpawnId++, ArchetypeId = arch, ZoneCenter = ctx.PlayerPos,
                    ZoneMinRadius = Config.spawnRingMin, ZoneMaxRadius = Config.spawnRingMax, WarningTime = 0f, StageIndex = CurrentStage
                });
                made++;
            }
            if (spawnAcc > st.spawnInterval) spawnAcc = st.spawnInterval; // 不积攒补发
        }
    }
}
