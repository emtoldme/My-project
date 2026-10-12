using System;
using System.Collections.Generic;

namespace Waffle.Combat
{
    /// <summary>
    /// S1 最小战斗模块（C-01A）：一种追踪敌人 + 一把直线弹自动武器 + 经验宝石。
    /// 纯 C#，不依赖 UnityEngine，没有 static 局内状态；新一局 new 一个新实例。
    ///
    /// 框架在 Playing 的每个 tick 按顺序调用（§0.3）：
    ///   StepCombat(input)  = T3 刷怪 → T4 敌人移动 → T5 武器 → T6 子弹/命中 → T7 死亡结算 → T8 接触伤害
    ///   （框架：T8 结算无敌并扣血、T9 失败判定；失败就跳到 T13）
    ///   StepGems(input)    = T10 宝石寿命/磁吸/拾取 → 返回 XP（框架加 XP、升级）
    ///   Release()          = T13 回收（不管是否提前跳出都必须调用）
    /// 暂停/升级面板/终局后不调用任何步骤。模块不读写 RunState，不调用调试命令。
    /// </summary>
    public sealed partial class CombatModule
    {
        public readonly CombatConfig Config;
        public readonly CombatCounters Counters = new CombatCounters();
        /// <summary>最近一个步骤的输出（StepCombat 或 StepGems 调用时清空重写）。</summary>
        public readonly CombatStepResult Result = new CombatStepResult();

        private readonly CombatRng spawnRng;

        // 池 + 活动列表（活动列表按出池顺序 = Serial 递增）
        private readonly EnemyState[] enemyPool;
        private readonly Stack<int> enemyFree = new Stack<int>();
        private readonly List<EnemyState> enemies = new List<EnemyState>();
        private readonly ProjectileState[] projectilePool;
        private readonly Stack<int> projectileFree = new Stack<int>();
        private readonly List<ProjectileState> projectiles = new List<ProjectileState>();
        private readonly GemState[] gemPool;
        private readonly Stack<int> gemFree = new Stack<int>();
        private readonly List<GemState> gems = new List<GemState>();

        private readonly List<EnemyState> newlyDead = new List<EnemyState>();
        private readonly List<EnemyState> enemyRecycle = new List<EnemyState>();
        private readonly List<ProjectileState> projectileRecycle = new List<ProjectileState>();
        private readonly List<GemState> gemRecycle = new List<GemState>();

        // 命中候选（避免每 tick 分配）
        private struct HitCandidate { public float S; public EnemyState Enemy; }
        private readonly List<HitCandidate> candidates = new List<HitCandidate>(16);
        private Vec2[] sepBuffer = new Vec2[0];

        private int nextEnemySerial = 1;
        private int nextProjectileSerial = 1;
        private int nextGemSerial = 1;

        private float weaponTimer;
        private float nextWaveAt;
        private bool wavesDone;
        private readonly bool[] pushFired, ringFired, eliteFired;
        private GemState bigGem;
        private int pendingDirectXp;

        // 玩家近 0.5 s 速度
        private readonly List<float> sampleTimes = new List<float>();
        private readonly List<Vec2> samplePos = new List<Vec2>();
        private Vec2 playerVelocity;

        private float now;
        private Vec2 player;
        private float viewRadius = 14f;
        private ICombatRuleExtension ruleExtension;
        private bool hasStepped, resolvingRuleExtension;

        public void InstallRuleExtension(ICombatRuleExtension extension)
        {
            if (hasStepped) throw new InvalidOperationException("Install rule extension before the first combat step.");
            ruleExtension = extension;
        }

        // Synchronous rule-only entry: presentation callbacks cannot use this after StepCombat.
        public CombatRuleDamageResult ApplyRuleDamage(int serial, int generation, float damage,
            PresentationEventType type, Vec2 from)
        {
            if (!resolvingRuleExtension || float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f
                || (type != PresentationEventType.EchoHit && type != PresentationEventType.PropagationHit
                    && type != PresentationEventType.EchoPropagationHit)
                || !TryGetEnemy(serial, generation, out var enemy)) return CombatRuleDamageResult.Invalid;
            bool killed = ApplyEnemyDamage(enemy, damage, type, from);
            return killed ? CombatRuleDamageResult.Killed : CombatRuleDamageResult.Applied;
        }

        public IReadOnlyList<EnemyState> Enemies { get { return enemies; } }
        public IReadOnlyList<ProjectileState> Projectiles { get { return projectiles; } }
        public IReadOnlyList<GemState> Gems { get { return gems; } }

        /// <summary>当前 Alive 敌人数。</summary>
        public int AliveCount
        {
            get { int n = 0; for (int i = 0; i < enemies.Count; i++) if (enemies[i].IsAlive) n++; return n; }
        }

        /// <summary>武器冷却计时（s，≤ 0 表示就绪）。</summary>
        public float WeaponTimer { get { return weaponTimer; } }

        public CombatModule(CombatConfig config, int seed)
        {
            if (config == null) throw new ArgumentNullException("config");
            Config = config;
            spawnRng = new CombatRng(seed);

            enemyPool = new EnemyState[Math.Max(1, config.enemyPoolSize)];
            for (int i = enemyPool.Length - 1; i >= 0; i--) { enemyPool[i] = new EnemyState { PoolIndex = i }; enemyFree.Push(i); }
            projectilePool = new ProjectileState[Math.Max(1, config.projectilePoolSize)];
            for (int i = projectilePool.Length - 1; i >= 0; i--) { projectilePool[i] = new ProjectileState { PoolIndex = i }; projectileFree.Push(i); }
            gemPool = new GemState[Math.Max(1, config.gemPoolSize)];
            for (int i = gemPool.Length - 1; i >= 0; i--) { gemPool[i] = new GemState { PoolIndex = i }; gemFree.Push(i); }

            pushFired = new bool[config.pushTimes.Length];
            ringFired = new bool[config.ringTimes.Length];
            eliteFired = new bool[config.eliteTimes.Length];
        }

        // =====================================================================
        // 公共步骤
        // =====================================================================

        /// <summary>T3–T8。返回 Result（ContactDamage、Kills、Events）。</summary>
        public CombatStepResult StepCombat(CombatInput input)
        {
            Result.Clear();
            hasStepped = true;
            BeginTick(input);
            float dt = Math.Max(0f, input.Dt);

            SpawnStep();                 // T3
            EnemyMoveStep(dt);           // T4
            WeaponStep(dt, input.Stats); // T5
            ProjectileStep(dt);          // T6
            if (ruleExtension != null)
            {
                resolvingRuleExtension = true;
                try { ruleExtension.AfterProjectiles(now); }
                finally { resolvingRuleExtension = false; }
            }
            if (runGrowth != null) ResolveRunPending();
            DeathStep();                 // T7
            ContactStep();               // T8
            return Result;
        }

        /// <summary>T10。返回 Result（XpCollected、GemPickup 事件）。T9 判定失败时不要调用。</summary>
        public CombatStepResult StepGems(CombatInput input)
        {
            Result.Clear();
            now = input.RunTime;
            player = input.PlayerPos;
            float dt = Math.Max(0f, input.Dt);
            float magnet = input.Stats.MagnetRadius;
            float magnetSqr = magnet * magnet;
            float pickSqr = Config.pickupRadius * Config.pickupRadius;

            if (runGrowth != null) CollectRunPickups(input);
            int xp = pendingDirectXp;
            pendingDirectXp = 0;

            for (int i = 0; i < gems.Count; i++)
            {
                GemState g = gems[i];
                if (g.Life != GemLife.Free && g.Life != GemLife.Magnet) continue;

                if (g.Life == GemLife.Free)
                {
                    if (Vec2.SqrDistance(g.Position, player) <= magnetSqr)
                    {
                        g.Life = GemLife.Magnet;
                        g.MagnetSpeed = Config.magnetStartSpeed;
                    }
                    else if (!g.IsBig)
                    {
                        g.Age += dt;
                        if (Config.gemLifetime > 0f && g.Age >= Config.gemLifetime)
                        {
                            g.Life = GemLife.Expired;
                            gemRecycle.Add(g);
                            Counters.GemsExpired++;
                            if (g == bigGem) bigGem = null;
                            continue;
                        }
                    }
                }

                if (g.Life == GemLife.Magnet)
                {
                    g.Position = AdvanceAttraction(g.Position, player, ref g.MagnetSpeed, dt);
                }

                if (Vec2.SqrDistance(g.Position, player) <= pickSqr)
                {
                    g.Life = GemLife.Collected; // 只转换一次
                    gemRecycle.Add(g);
                    xp += g.Value;
                    if (g == bigGem) bigGem = null;
                    Emit(PresentationEventType.GemPickup, g.Position, g.Value, g.Serial);
                }
            }

            if (runGrowth != null) DeathStep();
            Result.XpCollected = xp;
            Counters.TotalXpCollected += xp;
            return Result;
        }

        // One attraction motion for XP and ordinary drops; collection/rewards stay in their existing T10 paths.
        private Vec2 AdvanceAttraction(Vec2 position, Vec2 target, ref float speed, float dt)
        {
            Vec2 to = target - position;
            float distance = to.Length;
            float step = Math.Max(0f, speed) * dt;
            Vec2 next = distance <= step || distance <= .00001f ? target : position + to * (step / distance);
            speed = Math.Min(Config.magnetMaxSpeed, speed + Config.magnetAcceleration * dt);
            return next;
        }

        /// <summary>T13：把本 tick 标记回收的对象放回池（代号 +1）。每个 tick 都要调用，包括失败/胜利那一 tick。</summary>
        public void Release()
        {
            for (int i = 0; i < enemyRecycle.Count; i++)
            {
                EnemyState e = enemyRecycle[i];
                enemies.Remove(e);
                e.Generation++;
                enemyFree.Push(e.PoolIndex);
            }
            enemyRecycle.Clear();
            for (int i = 0; i < projectileRecycle.Count; i++)
            {
                ProjectileState p = projectileRecycle[i];
                projectiles.Remove(p);
                p.HitKeys.Clear();
            p.SourceId = null; p.Slot = 0; p.Speed = Config.projectileSpeed; p.Lifetime = Config.projectileLifetime;
            p.MaxDistance = float.MaxValue; p.Travelled = 0; p.Returning = false; p.ReturnAllowed = false; p.Group = null;
                p.Generation++;
                projectileFree.Push(p.PoolIndex);
            }
            projectileRecycle.Clear();
            for (int i = 0; i < gemRecycle.Count; i++)
            {
                GemState g = gemRecycle[i];
                gems.Remove(g);
                g.Generation++;
                gemFree.Push(g.PoolIndex);
            }
            gemRecycle.Clear();
        }

        /// <summary>
        /// 引用校验（§1.9 第 7 步）：保存的 (serial, generation) 是否仍指向一个 Alive 敌人。
        /// </summary>
        public bool TryGetEnemy(int serial, int generation, out EnemyState enemy)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyState e = enemies[i];
                if (e.Serial == serial && e.Generation == generation && e.IsAlive) { enemy = e; return true; }
            }
            enemy = null;
            return false;
        }

        // =====================================================================
        // 调试 / 测试入口（不经过上限；只受池约束）
        // =====================================================================

        /// <summary>在指定位置生成一个普通敌人（调试 spawn / 测试用）。hp/speed ≤ 0 时用当前阶段值。池空返回 null。</summary>
        public EnemyState DebugSpawnEnemy(Vec2 pos, float hp = -1f, float speed = -1f)
        {
            int phase = Config.PhaseAt(now);
            EnemyState e = AcquireEnemy(pos, phase, false, 9);
            if (e == null) return null;
            if (hp > 0f) { e.Hp = hp; e.MaxHp = hp; }
            if (speed >= 0f) e.Speed = speed;
            return e;
        }

        /// <summary>通过正式创建入口生成一颗宝石（满上限时合并，XP 守恒）。</summary>
        public void DebugSpawnGem(Vec2 pos, int value) { CreateGem(pos, value); }

        // =====================================================================
        // T3 刷怪
        // =====================================================================

        private void BeginTick(CombatInput input)
        {
            now = input.RunTime;
            player = input.PlayerPos;
            viewRadius = input.ViewRadius > 0f ? input.ViewRadius : 14f;

            sampleTimes.Add(now);
            samplePos.Add(player);
            while (sampleTimes.Count > 1 && sampleTimes[0] < now - Config.playerVelocityWindow)
            {
                sampleTimes.RemoveAt(0);
                samplePos.RemoveAt(0);
            }
            float span = now - sampleTimes[0];
            playerVelocity = span > 1e-4f ? (player - samplePos[0]) * (1f / span) : Vec2.Zero;
        }

        private void SpawnStep()
        {
            if (!Config.spawningEnabled || now >= Config.runDuration) return;

            // 普通波次：每个阶段第一波在阶段开始时刷，之后按间隔累加（保留余数）
            while (!wavesDone && nextWaveAt <= now)
            {
                int phase = Config.PhaseAt(nextWaveAt);
                int count = Config.waveCounts[phase];
                for (int i = 0; i < count; i++)
                {
                    if (AliveCount >= Config.aliveCap) { Counters.Truncated++; continue; }
                    AcquireEnemy(PickSpawnPoint(true), Config.PhaseAt(now), false, 0);
                }
                float next = nextWaveAt + Config.waveIntervals[phase];
                if (phase + 1 < Config.phaseStarts.Length && next >= Config.phaseStarts[phase + 1])
                    next = Config.phaseStarts[phase + 1];
                nextWaveAt = next;
                if (nextWaveAt >= Config.runDuration) wavesDone = true;
            }

            int eventCap = Config.aliveCap + Config.eventOverflow;

            for (int k = 0; k < pushFired.Length; k++)
            {
                if (pushFired[k] || now < Config.pushTimes[k]) continue;
                pushFired[k] = true;
                Vec2 dir = MoveDirOrRandom();
                float half = Deg(Config.pushHalfAngleDeg);
                float baseAng = (float)Math.Atan2(dir.Y, dir.X);
                for (int i = 0; i < Config.pushCounts[k]; i++)
                {
                    if (AliveCount >= eventCap) { Counters.Truncated++; continue; }
                    Vec2 p = player + Vec2.FromAngle(baseAng + spawnRng.Range(-half, half)) * (viewRadius + Config.pushRadiusExtra);
                    if (!IsLegal(p)) p = PickSpawnPoint(false);
                    AcquireEnemy(p, Config.PhaseAt(now), false, 1);
                }
            }

            for (int k = 0; k < ringFired.Length; k++)
            {
                if (ringFired[k] || now < Config.ringTimes[k]) continue;
                ringFired[k] = true;
                int n = Config.ringCounts[k];
                float rot = spawnRng.Range(0f, (float)(2 * Math.PI));
                for (int i = 0; i < n; i++)
                {
                    Vec2 p = player + Vec2.FromAngle(rot + (float)(2 * Math.PI) * i / n) * (viewRadius + Config.ringRadiusExtra);
                    if (!IsLegal(p)) { Counters.RingSkipped++; continue; }
                    if (AliveCount >= eventCap) { Counters.Truncated++; continue; }
                    AcquireEnemy(p, Config.PhaseAt(now), false, 2);
                }
            }

            for (int k = 0; k < eliteFired.Length; k++)
            {
                if (eliteFired[k] || now < Config.eliteTimes[k]) continue;
                eliteFired[k] = true;
                if (AliveCount >= eventCap) { Counters.Truncated++; continue; }
                AcquireEnemy(PickSpawnPoint(false), Config.PhaseAt(now), true, 3);
            }
        }

        private EnemyState AcquireEnemy(Vec2 pos, int phase, bool elite, int source)
        {
            if (enemyFree.Count == 0) { Counters.EnemyPoolMiss++; return null; }
            EnemyState e = enemyPool[enemyFree.Pop()];
            float hp = Config.phaseHp[phase];
            float speed = Config.phaseSpeed[phase];
            e.Serial = nextEnemySerial++;
            e.Life = EnemyLife.Alive;
            e.Position = pos;
            e.PrevPosition = pos; // 本 tick 新生成：prevPos = 生成点
            e.Phase = phase;
            e.SpawnSource = source;
            e.IsElite = elite;
            e.MaxHp = elite ? hp * Config.eliteHpMultiplier : hp;
            e.Hp = e.MaxHp;
            e.Speed = elite ? speed * Config.eliteSpeedMultiplier : speed;
            e.Radius = elite ? Config.eliteRadius : Config.enemyRadius;
            e.ContactDamage = elite ? Config.eliteContactDamage : Config.contactDamage;
            e.GemValue = elite ? Config.eliteGemValue : Config.gemValue;
            enemies.Add(e);
            Counters.EnemiesSpawned++;
            return e;
        }

        /// <summary>§1.4 取点流程。allowForward：普通波次可以用前方扇区。</summary>
        private Vec2 PickSpawnPoint(bool allowForward)
        {
            float rMin = viewRadius + Config.spawnRingInner, rMax = viewRadius + Config.spawnRingOuter;
            float ang;
            Vec2 v = playerVelocity;
            if (allowForward && now >= Config.forwardSectorStart && v.Length >= Config.forwardSectorMinSpeed
                && spawnRng.Next01() < Config.forwardSectorChance)
            {
                float half = Deg(Config.forwardSectorHalfAngleDeg);
                ang = (float)Math.Atan2(v.Y, v.X) + spawnRng.Range(-half, half);
            }
            else ang = spawnRng.Range(0f, (float)(2 * Math.PI));

            Vec2 p = player + Vec2.FromAngle(ang) * spawnRng.Range(rMin, rMax);
            for (int retry = 0; retry < 3 && !IsLegal(p); retry++)
                p = player + Vec2.FromAngle(spawnRng.Range(0f, (float)(2 * Math.PI))) * spawnRng.Range(rMin, rMax);
            if (IsLegal(p)) return p;

            // ③ 朝场地中心 ±45°
            Vec2 toCenter = (Vec2.Zero - player).Normalized;
            float baseAng = toCenter.SqrLength > 0f ? (float)Math.Atan2(toCenter.Y, toCenter.X) : spawnRng.Range(0f, (float)(2 * Math.PI));
            p = player + Vec2.FromAngle(baseAng + spawnRng.Range(-Deg(45f), Deg(45f))) * spawnRng.Range(rMin, rMax);
            return ClampSquare(p, Config.spawnLegalHalf); // 防御：R_view 过大时仍保证在场地内
        }

        private bool IsLegal(Vec2 p) { return Math.Abs(p.X) <= Config.spawnLegalHalf && Math.Abs(p.Y) <= Config.spawnLegalHalf; }

        private Vec2 MoveDirOrRandom()
        {
            if (playerVelocity.Length >= Config.forwardSectorMinSpeed) return playerVelocity.Normalized;
            return Vec2.FromAngle(spawnRng.Range(0f, (float)(2 * Math.PI)));
        }

        // =====================================================================
        // T4 敌人移动：追踪 + 分离（O(n²)）+ 夹边界 + 远离挪位
        // =====================================================================

        private void EnemyMoveStep(float dt)
        {
            int n = enemies.Count;
            if (sepBuffer.Length < n) sepBuffer = new Vec2[Math.Max(n, sepBuffer.Length * 2)];
            float sr = Config.separationRadius;

            // 先用移动前的位置算分离，结果与遍历顺序无关
            for (int i = 0; i < n; i++)
            {
                Vec2 push = Vec2.Zero;
                EnemyState a = enemies[i];
                if (a.IsAlive)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (i == j) continue;
                        EnemyState b = enemies[j];
                        if (!b.IsAlive) continue;
                        Vec2 d = a.Position - b.Position;
                        float dist = d.Length;
                        if (dist >= sr) continue;
                        Vec2 dir = dist > 1e-5f ? d * (1f / dist) : Vec2.FromAngle(a.Serial < b.Serial ? 0f : (float)Math.PI);
                        push = push + dir * (1f - dist / sr);
                    }
                    float len = push.Length;
                    push = len > 1e-6f ? push * (Config.separationMaxSpeed * Math.Min(1f, len) / len) : Vec2.Zero;
                }
                sepBuffer[i] = push;
            }

            float relocateDist = viewRadius + Config.relocateExtra;
            for (int i = 0; i < n; i++)
            {
                EnemyState e = enemies[i];
                if (!e.IsAlive) continue;
                e.PrevPosition = e.Position;
                Vec2 chase = (player - e.Position).Normalized * e.Speed;
                Vec2 vel = chase + sepBuffer[i];
                float lim = Config.arenaHalfSize - e.Radius;
                e.Position = ClampSquare(e.Position + vel * dt, lim);

                if (Vec2.Distance(e.Position, player) > relocateDist)
                {
                    Vec2 dir = MoveDirOrRandom();
                    float half = Deg(Config.forwardSectorHalfAngleDeg);
                    Vec2 p = player + Vec2.FromAngle((float)Math.Atan2(dir.Y, dir.X) + spawnRng.Range(-half, half))
                             * spawnRng.Range(viewRadius + Config.spawnRingInner, viewRadius + Config.spawnRingOuter);
                    if (!IsLegal(p)) p = PickSpawnPoint(false);
                    e.Position = p;
                    e.PrevPosition = p; // 瞬移不做扫掠
                    Counters.Relocated++;
                }
            }
        }

        // =====================================================================
        // T5 武器
        // =====================================================================

        private void WeaponStep(float dt, CombatPlayerStats stats)
        {
            if (runGrowth != null) { RunWeaponStep(dt); return; }
            weaponTimer -= dt;
            EnemyState target = FindNearestTarget();
            if (target == null)
            {
                if (weaponTimer < 0f) weaponTimer = 0f; // 没目标：停在就绪
                return;
            }
            if (weaponTimer > 0f) return;

            float interval = Math.Max(0.01f, stats.FireInterval);
            weaponTimer += interval; // 保留余数
            if (weaponTimer <= 0f) weaponTimer = interval * 0.5f; // 防御：dt 远大于间隔时不连发补射

            if (projectileFree.Count == 0) { Counters.ProjectilePoolMiss++; return; }
            Vec2 dir = (target.Position - player).Normalized;
            if (dir.SqrLength == 0f) dir = new Vec2(1f, 0f); // 目标和玩家重合
            ProjectileState p = projectilePool[projectileFree.Pop()];
            p.Serial = nextProjectileSerial++;
            p.Position = player;
            p.Direction = dir;
            p.Age = 0f;
            p.PierceLeft = Math.Max(1, stats.Pierce);
            p.Damage = stats.Damage;
            p.Finished = false;
            p.BornThisTick = true;
            p.HitKeys.Clear();
            p.SourceId = null; p.Slot = 0; p.Speed = Config.projectileSpeed; p.Lifetime = Config.projectileLifetime;
            p.MaxDistance = float.MaxValue; p.Travelled = 0; p.Returning = false; p.ReturnAllowed = false; p.Group = null;
            projectiles.Add(p);
            Counters.ShotsFired++;
            Result.Events.Add(new PresentationEvent { Type = PresentationEventType.Shot, Now = now,
                X = player.X, Z = player.Y, Value = stats.Damage, Data = target.Serial,
                DirectionX = dir.X, DirectionZ = dir.Y });
        }

        /// <summary>最近的 Alive 敌人（水平距离 ≤ targetRange；距离相同取 Serial 小的）。</summary>
        public EnemyState FindNearestTarget()
        {
            float best = Config.targetRange * Config.targetRange;
            EnemyState found = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyState e = enemies[i];
                if (!e.IsAlive) continue;
                float d = Vec2.SqrDistance(e.Position, player);
                if (d < best || (d == best && found == null)) { best = d; found = e; }
            }
            return found;
        }

        // =====================================================================
        // T6 子弹移动 + 相对运动扫掠命中
        // =====================================================================

        private void ProjectileStep(float dt)
        {
            float lim = Config.arenaHalfSize;
            for (int pi = 0; pi < projectiles.Count; pi++)
            {
                ProjectileState p = projectiles[pi];
                if (p.Finished) continue;
                Vec2 b0 = p.Position;
                if (p.Returning) p.Direction = (player - b0).Normalized;
                float travel = Math.Min(p.Speed * dt, Math.Max(0, p.MaxDistance - p.Travelled));
                Vec2 b1 = b0 + p.Direction * travel;
                p.Travelled += travel;

                candidates.Clear();
                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyState e = enemies[i];
                    if (!e.IsAlive) continue; // 已死亡的直接穿过，不消耗穿透
                    if (p.HitKeys.Contains(Key(e))) continue;
                    Vec2 e1 = e.Position;
                    Vec2 e0 = p.BornThisTick ? e1 : e.PrevPosition;
                    float s;
                    if (SweptEntry(b0 - e0, b1 - e1, Config.projectileRadius + e.Radius, out s))
                        candidates.Add(new HitCandidate { S = s, Enemy = e });
                }
                candidates.Sort((x, y) => x.S != y.S ? x.S.CompareTo(y.S) : x.Enemy.Serial.CompareTo(y.Enemy.Serial));

                for (int c = 0; c < candidates.Count && p.PierceLeft > 0; c++)
                {
                    EnemyState e = candidates[c].Enemy;
                    if (p.Group != null && !p.Group.Accept(Key(e))) continue;
                    p.HitKeys.Add(Key(e));
                    p.PierceLeft--;
                    Counters.Hits++;
                    ApplyEnemyDamage(e, p.Damage, PresentationEventType.EnemyHit, e.Position, p.SourceId, p.Slot);
                    ruleExtension?.OnDirectHit(now, e.Serial, e.Generation, e.Position, p.Damage);
                }

                p.Position = b1;
                p.Age += dt;
                p.BornThisTick = false;
                if (p.ReturnAllowed && !p.Returning && p.Travelled >= p.MaxDistance)
                { p.Returning = true; p.Travelled = 0; p.HitKeys.Clear(); p.PierceLeft = 1; }
                if ((p.PierceLeft <= 0 && (!p.ReturnAllowed || p.Returning)) || p.Age >= p.Lifetime || (!p.ReturnAllowed && p.Travelled >= p.MaxDistance)
                    || (p.Returning && Vec2.SqrDistance(p.Position, player) < .25f)
                    || Math.Abs(b1.X) > lim || Math.Abs(b1.Y) > lim)
                {
                    p.Finished = true;
                    projectileRecycle.Add(p);
                }
            }
        }

        /// <summary>
        /// 相对运动扫掠：相对坐标线段 r0 → r1 进入半径 R 圆的最小 s∈[0,1]；起点已在圆内 s = 0。
        /// </summary>
        public static bool SweptEntry(Vec2 r0, Vec2 r1, float radius, out float s)
        {
            float rr = radius * radius;
            float c = r0.SqrLength - rr;
            if (c <= 0f) { s = 0f; return true; }
            Vec2 d = r1 - r0;
            float a = d.SqrLength;
            s = 0f;
            if (a < 1e-12f) return false;
            float b = 2f * Vec2.Dot(r0, d);
            float disc = b * b - 4f * a * c;
            if (disc < 0f) return false;
            float t = (-b - (float)Math.Sqrt(disc)) / (2f * a);
            if (t < 0f || t > 1f) return false;
            s = t;
            return true;
        }

        private static long Key(EnemyState e) { return ((long)e.Serial << 32) | (uint)e.Generation; }

        private bool ApplyEnemyDamage(EnemyState enemy, float damage, PresentationEventType type, Vec2 from, string sourceId = null, int slot = 0, string rootSourceId = null)
        {
            if (!enemy.IsAlive || damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage)) return false;
            enemy.Hp -= damage;
            enemy.LastSourceId = sourceId; enemy.LastSlot = slot;
            Result.Events.Add(new PresentationEvent { Type = type, Now = now, X = enemy.Position.X,
                Z = enemy.Position.Y, Value = damage, Data = enemy.Serial, Generation = enemy.Generation,
                FromX = from.X, FromZ = from.Y, SourceId = sourceId, RootSourceId = rootSourceId ?? sourceId, Slot = slot });
            if (runGrowth != null && type == PresentationEventType.EnemyHit && sourceId != null && (sourceId.StartsWith("W") || sourceId.StartsWith("E")))
                OnRunDirectHit(enemy, damage, sourceId, slot);
            if (enemy.Hp > 0f || enemy.Life != EnemyLife.Alive) return false;
            enemy.Life = EnemyLife.Dead;
            newlyDead.Add(enemy);
            return true;
        }

        // =====================================================================
        // T7 死亡结算
        // =====================================================================

        private void DeathStep()
        {
            for (int i = 0; i < newlyDead.Count; i++)
            {
                EnemyState e = newlyDead[i];
                CreateGem(e.Position, e.GemValue);
                Result.Kills++;
                Counters.TotalKills++;
                enemyRecycle.Add(e);
                Result.Events.Add(new PresentationEvent { Type = PresentationEventType.EnemyKilled, Now = now, X = e.Position.X, Z = e.Position.Y, Data = e.Serial, Generation = e.Generation, Value = e.GemValue, SourceId = e.LastSourceId, Slot = e.LastSlot });
                if (runGrowth != null) DropRunPickups(e);
            }
            newlyDead.Clear();
        }

        /// <summary>唯一的宝石创建入口（§1.5）：占用满时在创建时合并，XP 守恒。</summary>
        private void CreateGem(Vec2 pos, int value)
        {
            if (value <= 0) return;
            Counters.TotalXpDropped += value;
            if (gems.Count < Config.maxGems)
            {
                if (gemFree.Count == 0) { Counters.GemPoolMiss++; pendingDirectXp += value; Counters.XpDirectCredited += value; return; }
                GemState g = gemPool[gemFree.Pop()];
                g.Serial = nextGemSerial++;
                g.Life = GemLife.Free;
                g.Position = pos;
                g.Value = value;
                g.Age = 0f;
                g.IsBig = false;
                g.MagnetSpeed = 0f;
                gems.Add(g);
                return;
            }
            if (bigGem != null && (bigGem.Life == GemLife.Free || bigGem.Life == GemLife.Magnet))
            {
                bigGem.Value += value;
                Counters.GemsMerged++;
                return;
            }
            for (int i = 0; i < gems.Count; i++) // gems 按出池顺序，第一颗 Free 普通宝石就是最老的
            {
                GemState g = gems[i];
                if (g.Life == GemLife.Free && !g.IsBig)
                {
                    g.IsBig = true;
                    g.Value += value;
                    bigGem = g;
                    Counters.GemsMerged++;
                    return;
                }
            }
            pendingDirectXp += value; // 本 tick T10 直接计入
            Counters.XpDirectCredited += value;
        }

        // =====================================================================
        // T8 接触伤害（原始值；无敌由框架判定）
        // =====================================================================

        private void ContactStep()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyState e = enemies[i];
                if (!e.IsAlive) continue; // 本 tick 被打死的不再造成伤害
                float r = Config.playerRadius + e.Radius;
                if (Vec2.SqrDistance(e.Position, player) > r * r) continue;
                Result.ContactCount++;
                Result.ContactDamageSum += e.ContactDamage;
                if (e.ContactDamage > Result.ContactDamage)
                {
                    Result.ContactDamage = e.ContactDamage;
                    Result.ContactSourceSerial = e.Serial;
                }
            }
        }

        // =====================================================================

        private void Emit(PresentationEventType type, Vec2 pos, float value, int data)
        {
            Result.Events.Add(new PresentationEvent { Type = type, Now = now, X = pos.X, Z = pos.Y, Value = value, Data = data });
        }

        private static float Deg(float d) { return d * (float)Math.PI / 180f; }

        private static Vec2 ClampSquare(Vec2 p, float h)
        {
            return new Vec2(Math.Max(-h, Math.Min(h, p.X)), Math.Max(-h, Math.Min(h, p.Y)));
        }
    }
}
