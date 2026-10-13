using System;
using System.Collections.Generic;
using Waffle.RunContent;

namespace Waffle.Combat
{
    internal sealed class RunShotGroup
    {
        readonly Dictionary<long, int> hits = new Dictionary<long, int>();
        readonly int cap;
        public RunShotGroup(int cap) { this.cap = Math.Max(1, cap); }
        public bool Accept(long key)
        {
            hits.TryGetValue(key, out int count);
            if (count >= cap) return false;
            hits[key] = count + 1; return true;
        }
    }

    public sealed class RunZone
    {
        public Vec2 Position { get; internal set; }
        public float Radius { get; internal set; }
        public float StartsAt { get; internal set; }
        public float EndsAt { get; internal set; }
        internal float NextTick, Tick, Damage, Burst;
        internal string Source; internal int Slot;
    }
    public sealed class RunPickup
    {
        public long Serial { get; internal set; }
        public string Id { get; internal set; }
        public Vec2 Position { get; internal set; }
        public bool IsAttracted { get; internal set; }
        internal float AttractionSpeed;
        internal float ExpiresAt;
    }
    public readonly struct RunEchoCue
    {
        public readonly int Serial, Generation;
        public readonly float ScheduledAt, DueAt;
        public RunEchoCue(int serial, int generation, float scheduled, float due)
        { Serial = serial; Generation = generation; ScheduledAt = scheduled; DueAt = due; }
    }

    // RC01 extends the existing rule module: all hits, deaths, XP and recycling use its original paths.
    public sealed partial class CombatModule
    {
        RunGrowthState runGrowth;
        GrowthStats runStats;
        List<WeaponRuntimeParams> runWeapons = new List<WeaponRuntimeParams>();
        readonly float[] runTimers = new float[4];
        readonly Dictionary<long, float>[] orbitHits = {
            new Dictionary<long,float>(), new Dictionary<long,float>(), new Dictionary<long,float>(), new Dictionary<long,float>() };
        readonly Dictionary<long, float> knockReady = new Dictionary<long, float>();
        readonly List<RunZone> zones = new List<RunZone>(16);
        readonly List<RunPickup> pickups = new List<RunPickup>(12);
        readonly List<PendingRunHit> pendingHits = new List<PendingRunHit>(128);
        readonly List<RunEchoCue> echoCues = new List<RunEchoCue>(32);
        CombatRng pickupRng;
        long nextPickupSerial = 1;
        float magnetDropAt, pulseDropAt;
        bool shieldPulseQueued;
        struct PendingRunHit
        {
            public int Serial, Generation, Slot;
            public float At, Scheduled, Damage, ComboCoef, ComboRadius;
            public string Source, Root;
            public PresentationEventType Type;
            public Vec2 From;
        }
        public IReadOnlyList<WeaponRuntimeParams> RunWeapons => runWeapons;
        public IReadOnlyList<RunZone> Zones => zones;
        public IReadOnlyList<RunPickup> Pickups => pickups;
        public IReadOnlyList<RunEchoCue> EchoCues => echoCues;
        public int RunEchoHits { get; private set; }
        public int RunPropagationHits { get; private set; }
        public int RunPickupsCollected { get; private set; }

        public void EnableRunContent(RunGrowthState growth, int seed)
        {
            if (hasStepped) throw new InvalidOperationException("Enable growth before the first step.");
            runGrowth = growth; pickupRng = new CombatRng(seed ^ 0x4c031);
            components = new Waffle.RunContent.Components.RunComponentRuntime(growth);
            components.RebaseMovement(player);
            RefreshRunContent();
        }
        public void RefreshRunContent()
        { runStats = GrowthStats.From(runGrowth); runWeapons = WeaponRuntimeParams.From(runGrowth); components?.Refresh(); }
        public void QueueShieldPulse() { shieldPulseQueued = true; }
        public void ClearRunContent()
        { pendingHits.Clear(); echoCues.Clear(); zones.Clear(); pickups.Clear(); components?.Clear(); }

        public Vec2 OrbitPosition(WeaponRuntimeParams w, int index, float time, Vec2 center)
        {
            var d = w.RankData;
            int count = (int)d.Get("count", 2);
            float radius = w.Radius;
            if (d.Get("pulseEvery") > 0 && time % d.Get("pulseEvery") < d.Get("pulseDur"))
                radius = d.Get("pulseRadius") * runStats.AreaMul;
            float angle = Deg(time * d.Get("angSpeed", 180) + index * 360f / count);
            return center + new Vec2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
        }

        void RunWeaponStep(float dt)
        {
            foreach (var w in runWeapons)
            {
                if (components != null && components.Owns(w.Id)) continue;
                var d = w.RankData;
                if (w.Id == "W03" || w.Id == "E03")
                {
                    int count = (int)d.Get("count", 2);
                    foreach (var e in enemies)
                    {
                        if (!e.IsAlive || (orbitHits[w.Slot].TryGetValue(Key(e), out float at) && now < at)) continue;
                        for (int i = 0; i < count; i++)
                        {
                            Vec2 pos = OrbitPosition(w, i, now, player);
                            Vec2 prev = OrbitPosition(w, i, now - dt, player);
                            if (!SweptEntry(prev - e.PrevPosition, pos - e.Position, .4f + e.Radius, out _)) continue;
                            orbitHits[w.Slot][Key(e)] = now + d.Get("hitCd", .5f) / runStats.RateMul;
                            Counters.Hits++;
                            ApplyEnemyDamage(e, w.Damage, PresentationEventType.EnemyHit, pos, w.Id, w.Slot);
                            break;
                        }
                    }
                    runTimers[w.Slot] -= dt;
                    if (runTimers[w.Slot] <= 0)
                    { runTimers[w.Slot] = .6f; EmitRun(PresentationEventType.WeaponOrbit, player, w.Id, w.Slot, w.Damage, w.Radius); }
                    continue;
                }
                runTimers[w.Slot] -= dt;
                if (runTimers[w.Slot] > 0) continue;
                var target = FindNearestTarget();
                if (target == null) { runTimers[w.Slot] = 0; continue; }
                runTimers[w.Slot] = Math.Max(.15f, w.Cooldown);
                if (w.Id == "W04")
                {
                    Vec2 first = target.Position;
                    for (int i = 0; i < (int)d.Get("count", 1) && zones.Count < 16; i++)
                    {
                        Vec2 at = first;
                        if (i > 0)
                        {
                            EnemyState second = null; float best = float.MaxValue;
                            foreach (var e in enemies)
                            { float dist = Vec2.SqrDistance(e.Position, player); if (e.IsAlive && e != target && dist < best) { second = e; best = dist; } }
                            at = second != null ? second.Position : first + new Vec2(w.Radius, 0);
                        }
                        float start = now + d.Get("delay", .6f);
                        zones.Add(new RunZone { Position = at, Radius = w.Radius, StartsAt = start,
                            EndsAt = start + d.Get("duration", 2) * runStats.DurationMul, NextTick = start,
                            Tick = d.Get("tick", .5f) / runStats.RateMul, Damage = w.Damage,
                            Burst = d.Get("burst") * runStats.DamageMul, Source = w.Id, Slot = w.Slot });

                    }
                    continue;
                }
                bool spread = w.Id == "W02" || w.Id == "E02";
                int shots = (int)d.Get("count", 1);
                var group = new RunShotGroup(spread ? (int)d.Get("sameTargetHits", 1) : shots);
                Vec2 dir = (target.Position - player).Normalized;
                if (dir.SqrLength < .001f) dir = new Vec2(1, 0);
                for (int i = 0; i < shots; i++)
                {
                    if (projectileFree.Count == 0) { Counters.ProjectilePoolMiss++; break; }
                    float a = spread ? Deg((i / (float)Math.Max(1, shots - 1) - .5f) * d.Get("arc", 45)) : 0;
                    Vec2 heading = new Vec2(dir.X * (float)Math.Cos(a) - dir.Y * (float)Math.Sin(a), dir.X * (float)Math.Sin(a) + dir.Y * (float)Math.Cos(a));
                    var p = projectilePool[projectileFree.Pop()];
                    p.Serial = nextProjectileSerial++; p.Position = player; p.Direction = heading;
                    p.Age = 0; p.Damage = w.Damage; p.PierceLeft = (int)d.Get("pierce", 1);
                    p.Finished = false; p.BornThisTick = true; p.HitKeys.Clear();
                    p.SourceId = w.Id; p.Slot = w.Slot; p.Group = group;
                    p.Speed = d.Get("speed", 14) * runStats.ProjSpeedMul;
                    p.MaxDistance = spread ? d.Get("range", 5) * runStats.AreaMul : Config.targetRange + 2;
                    p.ReturnAllowed = spread && (w.Id == "E02" || d.Get("returnShots") > 0) && (i == 0 || i == shots - 1);
                    p.Returning = false; p.Travelled = 0;
                    p.Lifetime = p.MaxDistance / p.Speed * (p.ReturnAllowed ? 2.5f : 1.1f);
                    projectiles.Add(p); Counters.ShotsFired++;
                    Result.Events.Add(new PresentationEvent { Type = PresentationEventType.Shot, Now = now,
                        X = player.X, Z = player.Y, DirectionX = heading.X, DirectionZ = heading.Y,
                        Value = w.Damage, Data = target.Serial, SourceId = w.Id, RootSourceId = w.Id, Slot = w.Slot });
                }
            }
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                var z = zones[i];
                if (now < z.StartsAt) continue;
                if (now >= z.EndsAt)
                { if (z.Burst > 0) DamageArea(z.Position, z.Radius, z.Burst, z.Source, z.Slot, 48); zones.RemoveAt(i); continue; }
                if (now >= z.NextTick)
                { if (z.NextTick == z.StartsAt) EmitRun(PresentationEventType.WeaponZone,z.Position,z.Source,z.Slot,z.Damage,z.Radius,z.EndsAt-z.StartsAt);
                    z.NextTick = now + Math.Max(.15f, z.Tick); DamageArea(z.Position, z.Radius, z.Damage, z.Source, z.Slot, 48); }
            }
            if (shieldPulseQueued)
            {
                shieldPulseQueued = false;
                foreach (var w in runWeapons) if (w.Id == "E03")
                { DamageArea(player, 3.5f * runStats.AreaMul, w.RankData.Get("shieldPulseDmg", 20) * runStats.DamageMul, "E03", w.Slot, 24); EmitRun(PresentationEventType.ShieldReady, player, "E03", w.Slot, 0, 3.5f); }
            }
        }

        void DamageArea(Vec2 at, float radius, float damage, string source, int slot, int budget, float eliteMul = 1, bool allowMechanisms = true)
        {
            foreach (var e in enemies)
                if (e.IsAlive && Vec2.SqrDistance(at, e.Position) <= (radius + e.Radius) * (radius + e.Radius))
                { ApplyEnemyDamage(e, damage * (e.IsElite ? eliteMul : 1), PresentationEventType.EnemyHit, at, source, slot, allowMechanisms: allowMechanisms); if (--budget <= 0) break; }
        }

        void OnRunDirectHit(EnemyState e, float damage, string source, int slot)
        {
            var push = runGrowth.Catalog.Get("P15")?.RankAt(runGrowth.RankOf("P15"));
            float amount = push?.Get("knockback") ?? 0;
            if (source == "E02") amount += 1;
            if (amount > 0 && (!knockReady.TryGetValue(Key(e), out float ready) || now >= ready))
            {
                knockReady[Key(e)] = now + .5f;
                Vec2 dir = (e.Position - player).Normalized;
                e.Position = ClampSquare(e.Position + dir * (amount * (e.IsElite ? .35f : 1)), Config.spawnLegalHalf);
            }
            float echo = runStats.EchoCoef + (source == "E01" ? .8f : 0);
            if (echo > 0 && e.Hp > 0) ScheduleRunHit(e, damage * echo, .5f, PresentationEventType.EchoHit, "P12", source, slot, e.Position, runStats.PropCoef, runStats.PropRadius);
            if (runStats.PropCoef <= 0) return;
            int budget = runStats.PropBudget;
            var chosen = new HashSet<long> { Key(e) };
            while (budget-- > 0)
            {
                EnemyState found = null; float best = runStats.PropRadius * runStats.PropRadius;
                foreach (var other in enemies)
                {
                    if (!other.IsAlive || chosen.Contains(Key(other))) continue;
                    float dist = Vec2.SqrDistance(e.Position, other.Position);
                    if (dist < best) { best = dist; found = other; }
                }
                if (found == null) break;
                chosen.Add(Key(found));
                ScheduleRunHit(found, damage * runStats.PropCoef, .1f, PresentationEventType.PropagationHit, "P13", source, slot, e.Position);
            }
        }
        void ScheduleRunHit(EnemyState e, float damage, float delay, PresentationEventType type, string source, string root, int slot, Vec2 from, float comboCoef = 0, float comboRadius = 0)
        {
            if (pendingHits.Count >= 128) return;
            pendingHits.Add(new PendingRunHit { Serial = e.Serial, Generation = e.Generation, At = now + delay,
                Scheduled = now, Damage = damage, Type = type, Source = source, Root = root, Slot = slot, From = from, ComboCoef = comboCoef, ComboRadius = comboRadius });
        }
        void ResolveRunPending()
        {
            for (int i = 0; i < pendingHits.Count;)
            {
                var p = pendingHits[i];
                if (!TryGetEnemy(p.Serial, p.Generation, out var target)) { pendingHits.RemoveAt(i); continue; }
                if (now < p.At) { i++; continue; }
                pendingHits.RemoveAt(i);
                ApplyEnemyDamage(target, p.Damage, p.Type, p.From, p.Source, p.Slot, p.Root);
                if (p.Type == PresentationEventType.EchoHit)
                {
                    RunEchoHits++;
                    // The established combination has one explicit secondary hop. No other derived hit schedules children.
                    if (p.ComboCoef > 0)
                    {
                        EnemyState next = null; float best = p.ComboRadius * p.ComboRadius;
                        foreach (var e in enemies)
                        {
                            if (!e.IsAlive || e == target) continue;
                            float dist = Vec2.SqrDistance(e.Position,target.Position);
                            if (dist < best) { best = dist; next = e; }
                        }
                        if (next != null) ScheduleRunHit(next,p.Damage*p.ComboCoef,.1f,PresentationEventType.EchoPropagationHit,
                            "P12_P13",p.Root,p.Slot,target.Position);
                    }
                }
                else RunPropagationHits++;
            }
            echoCues.Clear();
            foreach (var p in pendingHits) if (p.Type == PresentationEventType.EchoHit)
                echoCues.Add(new RunEchoCue(p.Serial, p.Generation, p.Scheduled, p.At));
        }

        void DropRunPickups(EnemyState e)
        {
            foreach (var map in orbitHits) map.Remove(Key(e));
            knockReady.Remove(Key(e));
            if (e.IsElite) { SpawnRunPickup("ELITE", e.Position); return; }
            float r = pickupRng.Range(0, 1);
            if (r < .02f) SpawnRunPickup("D02", e.Position);
            else if (r < .024f && now >= magnetDropAt) { if (SpawnRunPickup("D03", e.Position)) magnetDropAt = now + 45; }
            else if (r < .027f && now >= pulseDropAt) { if (SpawnRunPickup("D04", e.Position)) pulseDropAt = now + 60; }
        }
        bool SpawnRunPickup(string id, Vec2 at)
        {
            int count = 0; foreach (var p in pickups) if (p.Id == id) count++;
            int cap = id == "ELITE" ? 8 : id == "D02" ? 3 : 1;
            if (count >= cap) return false;
            pickups.Add(new RunPickup { Id = id, Position = at, Serial = nextPickupSerial++, ExpiresAt = now + (id == "ELITE" ? 600 : 20) });
            return true;
        }
        public bool DebugSpawnPickup(string id, Vec2 at)
        { return runGrowth != null && (id == "D02" || id == "D03" || id == "D04" || id == "ELITE") && SpawnRunPickup(id, at); }
        void CollectRunPickups(CombatInput input)
        {
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                var p = pickups[i];
                if (!p.IsAttracted && now >= p.ExpiresAt) { pickups.RemoveAt(i); continue; }
                // Ordinary drops share the player's attraction range. Elite rewards remain deliberate contact pickups.
                float range = Math.Max(0f, input.Stats.MagnetRadius);
                if (p.Id != "ELITE" && !p.IsAttracted && Vec2.SqrDistance(player, p.Position) <= range * range)
                { p.IsAttracted = true; p.AttractionSpeed = Config.magnetStartSpeed; }
                if (p.IsAttracted)
                    p.Position = AdvanceAttraction(p.Position, player, ref p.AttractionSpeed, Math.Max(0f, input.Dt));
                if (Vec2.SqrDistance(player, p.Position) > Config.pickupRadius * Config.pickupRadius) continue;
                pickups.RemoveAt(i); RunPickupsCollected++;
                if (p.Id == "D02") Result.HealFraction += .2f;
                else if (p.Id == "D03")
                { MagnetizeFreeGems(); }
                else if (p.Id == "D04") DamageArea(player, 100, 30, "D04", -1, 40, .3f);
                else if (p.Id == "ELITE") Result.EliteRewards.Add(p.Serial);
                EmitRun(p.Id == "ELITE" ? PresentationEventType.EliteReward : PresentationEventType.PickupCollected, p.Position, p.Id, -1, 0, 0);
            }
        }
        void EmitRun(PresentationEventType type, Vec2 at, string id, int slot, float value, float radius, float duration = 0)
        { Result.Events.Add(new PresentationEvent { Type = type, Now = now, X = at.X, Z = at.Y, SourceId = id, RootSourceId = id, Slot = slot, Value = value, Radius = radius, Duration = duration }); }
    }
}
