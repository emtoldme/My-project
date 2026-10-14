using System;
using Waffle.Combat;

namespace Waffle
{
    /// <summary>
    /// 一局的逻辑核心：按 §0.3 的固定顺序 T0→T13 推进一个 tick。
    /// 纯 C#，不依赖 UnityEngine：GameLoop 每帧调用一次 <see cref="Tick"/>，测试里也可以直接调用。
    /// 战斗模块只返回结果；此处统一决定伤害、经验、升级与终局优先级。
    /// </summary>
    public partial class RunSimulation
    {
        public readonly RunConfig Config;
        public readonly RunState State;
        private readonly IRunLog log;
        private readonly Random random;
        public CombatModule Combat { get; private set; }
        public float ViewRadius = 14f;
        public float DodgeReadyAt { get; private set; }
        private float dodgeLeft, dodgeX, dodgeZ;
        private float facingX, facingZ = 1f;
        public bool IsDodging { get { return dodgeLeft > 0f; } }

        // ---- 玩家：位置、统一无敌截止、闪避状态 ----
        public float PlayerX;
        public float PlayerZ;
        /// <summary>唯一的无敌截止时刻（§1.8 C-1），判定 now &lt; InvulnerableUntil。</summary>
        public float InvulnerableUntil;
        /// <summary>闪避缓冲截止时刻；&lt; 0 表示没有缓冲。离开 Playing 时清空（§1.1）。</summary>
        public float DodgeBufferUntil = -1f;
        /// <summary>T1收到的闪避请求次数，区别于实际发动次数。</summary>
        public int DodgeRequests;

        // 相机相对的移动基向量（XZ 平面），由 GameLoop 开局时根据相机算一次（§8.2）。
        private float rightX = 1f, rightZ = 0f, forwardX = 0f, forwardZ = 1f;

        // ---- 升级面板 ----
        /// <summary>当前面板上的 3 个选项（只有 LevelUp / 从 LevelUp 暂停时有意义）。</summary>
        public UpgradeKind[] CurrentOptions = { UpgradeKind.None, UpgradeKind.None, UpgradeKind.None };
        /// <summary>一共显示过多少张升级面板（A-84：每次升级只弹一次）。</summary>
        public int PanelsShown;
        private double panelShownRealTime = double.NegativeInfinity;
        private long lastChooseFrame = -1;
        private bool chooseLocked;
        private double realTime;

        // ---- 调试钩子（§2.11 最小集） ----
        public bool God;
        /// <summary>&gt; 0 时固定逻辑 dt（setdt），否则用真实帧时间。</summary>
        public float FixedDt;
        public float Slowmo = 1f;
        /// <summary>tracetick：&gt; 0 表示还要追踪几个 tick，-1 表示一直追踪。</summary>
        public int TraceTicks;
        private int queuedXp;
        private int queuedDamage;
        /// <summary>已排队、等待 T10 结算的调试 XP（只读，测试与 Overlay 用）。</summary>
        public int QueuedXp { get { return queuedXp; } }
        /// <summary>已排队、等待 T8 结算的调试伤害（只读）。</summary>
        public int QueuedDamage { get { return queuedDamage; } }

        /// <summary>本帧的表现事件（只读给音效/特效；T0 清空，GameLoop 分发后也清空）。</summary>
        public readonly PresentationEventBuffer Presentation = new PresentationEventBuffer();
        private bool restartEmitted;

        /// <summary>最近一次 tick 实际用的 dt（调试 Overlay 显示）。</summary>
        public float LastDt;

        public RunSimulation(RunConfig config, IRunLog log, CombatConfig combatConfig = null)
        {
            Config = config ?? new RunConfig();
            if (Config.upgrades == null) Config.upgrades = new UpgradeConfig();
            this.log = log ?? new NullRunLog();
            State = new RunState(Config.maxHp, this.log, Config.extraXpStepAfterFour);
            random = new Random(Config.seed);
            State.PhaseChanged += OnPhaseChanged;
            // Rules-only callers may omit combat; the real GameLoop always supplies it.
            if (combatConfig != null)
            {
                combatConfig.runDuration = Config.runDuration;
                combatConfig.unboundedWorld = Config.unboundedWorld;
                combatConfig.arenaHalfSize = Config.arenaHalfExtent + combatConfig.playerRadius;
                combatConfig.spawnLegalHalf = Math.Max(0f, combatConfig.arenaHalfSize - 1f);
                combatConfig.gemLifetime = Config.gemLifetime;
                Combat = new CombatModule(combatConfig, Config.seed);
            }
            if (Config.useRunContent) InitGrowth();
        }

        /// <summary>设置相机相对移动方向（已投影到 XZ 并归一化）。</summary>
        public void SetMoveBasis(float rx, float rz, float fx, float fz)
        {
            rightX = rx; rightZ = rz; forwardX = fx; forwardZ = fz;
            facingX = fx; facingZ = fz;
        }

        /// <summary>
        /// 记录当前帧号和 unscaled 时间。Tick 会自动调用；UI 在同一帧里提交选择前也依赖它，
        /// 用来实现"同一帧多次提交只算一次"和 0.3 s 防误触。
        /// </summary>
        public void BeginFrame(long frame, double realTimeNow)
        {
            State.Frame = frame;
            realTime = realTimeNow;
        }

        /// <summary>按当前升级等级、从 RunConfig 算出的数值（例如当前移速、最大 HP）。</summary>
        public float CurrentValue(UpgradeKind kind)
        {
            if (Growth != null)
            {
                if (kind == UpgradeKind.MoveSpeed) return Config.moveSpeed * ContentStats.MoveSpeedMul * HomecomingFactor(kind);
                if (kind == UpgradeKind.PickupRange) return Config.magnetRadius * ContentStats.PickupMul * HomecomingFactor(kind);
                if (kind == UpgradeKind.MaxHp) return Config.maxHp * ContentStats.MaxHpMul * HomecomingFactor(kind);
            }
            int level = kind >= 0 && (int)kind < UpgradeRules.RealKindCount ? State.UpgradeLevels[(int)kind] : 0;
            return UpgradeRules.Value(kind, level, Config) * HomecomingFactor(kind);
        }

        public bool IsInvulnerable { get { return God || State.Now < InvulnerableUntil; } }

        // =====================================================================
        // 每帧一次的 tick
        // =====================================================================

        /// <summary>
        /// 推进一帧。frameDt = Time.deltaTime（真实帧时间），frame = 帧号，realTimeNow = unscaled 时间。
        /// </summary>
        public void Tick(float frameDt, InputSnapshot input, long frame, double realTimeNow)
        {
            gameplayTickStarted = true;
            BeginFrame(frame, realTimeNow);
            Presentation.Clear(); // T0：上一帧没分发的事件不再保留
            bool trace = TraceTicks != 0;

            // ---- T0 输入快照与暂停 ----
            Trace(trace, "T0 输入快照");
            if (input.PausePressed) TogglePause("暂停键");
            if (State.Phase == RunPhase.LevelUp && input.ChoosePressed >= 0)
                ChooseUpgrade(input.ChoosePressed);

            // 门控：只有 Playing 才执行 T1–T13（§0.1 规则 4）
            if (State.Phase != RunPhase.Playing)
            {
                EndTrace(trace);
                return;
            }

            float dt = FixedDt > 0f ? FixedDt : Math.Min(Math.Max(frameDt, 0f), Config.maxDt);
            dt *= Slowmo;
            LastDt = dt;
            float now = State.Now;
            if (Growth != null) TickGrowth();

            // ---- T1 闪避请求/缓冲，只有真正发动才产生表现事件 ----
            Trace(trace, "T1 闪避");
            if (input.DodgePressed)
            {
                DodgeRequests++;
                DodgeBufferUntil = now + Config.dodgeBuffer;
            }
            if (DodgeBufferUntil >= 0f && now > DodgeBufferUntil) DodgeBufferUntil = -1f;
            if (DodgeBufferUntil >= 0f && !IsDodging && now >= DodgeReadyAt)
            {
                float x = rightX * input.MoveX + forwardX * input.MoveY;
                float z = rightZ * input.MoveX + forwardZ * input.MoveY;
                float length = (float)Math.Sqrt(x * x + z * z);
                dodgeX = length > 0.001f ? x / length : facingX;
                dodgeZ = length > 0.001f ? z / length : facingZ;
                dodgeLeft = Math.Max(0.001f, Config.dodgeDuration);
                DodgeReadyAt = now + CurrentValue(UpgradeKind.DodgeCooldown);
                InvulnerableUntil = Math.Max(InvulnerableUntil, now + dodgeLeft);
                DodgeBufferUntil = -1f;
                Combat?.NotifyRunDodge(new Vec2(PlayerX, PlayerZ));
                QueueHomecomingDodge();
                Emit(PresentationEventType.Dodge, PlayerX, PlayerZ, dodgeLeft, 0);
            }

            // ---- T2 玩家移动（相机相对，长度 ≤ 1，夹在场地内） ----
            Trace(trace, "T2 玩家移动");
            Vec2 previousPlayer = new Vec2(PlayerX, PlayerZ);
            MovePlayer(input.MoveX, input.MoveY, dt);

            Trace(trace, "T3 刷怪");
            CombatInput combatInput = MakeCombatInput(dt, now);
            combatInput.PlayerPreviousPos = previousPlayer;
            CombatStepResult combatResult = Combat != null ? Combat.StepCombat(combatInput) : null;
            if (combatResult != null)
            {
                State.Kills += combatResult.Kills;
                ForwardCombatEvents(combatResult);
            }
            Trace(trace, "T4 敌人移动");
            Trace(trace, "T5 武器");
            Trace(trace, "T6 子弹与命中");
            Trace(trace, "T7 敌人死亡结算");

            // ---- T8：敌人接触与调试伤害都经唯一伤害入口 ----
            Trace(trace, "T8 接触伤害");
            int contact = combatResult == null ? 0 : (Config.stackContactDamage ? combatResult.ContactDamageSum : combatResult.ContactDamage);
            int damage = Math.Max(queuedDamage, contact);
            queuedDamage = 0;
            if (damage > 0) ApplyDamage(damage, now);

            // ---- T9 失败判定 ----
            Trace(trace, "T9 失败判定");
            bool diedThisTick = State.Hp <= 0;
            if (diedThisTick && (Config.defeatWinsSameTick || IsBossClearMode))
            {
                // Defeat 的时钟固定在死亡那个 tick 开始时的 now
                EnterTerminal(RunPhase.Defeat, "hp<=0");
                Recycle(trace);
                return;
            }

            // ---- T10：拾取与调试XP均走正常升级流程 ----
            Trace(trace, "T10 宝石/XP");
            ApplyObjectiveReward();
            if (Combat != null)
            {
                CombatStepResult gems = Combat.StepGems(combatInput);
                ForwardCombatEvents(gems);
                if (gems.XpCollected > 0) { AddXp(gems.XpCollected); CollectFieldFragments(gems.XpCollected); }
                if (Growth != null)
                {
                    State.Kills += gems.Kills;
                    if (gems.HealFraction > 0) HealContent(gems.HealFraction);
                    foreach (long token in gems.EliteRewards) Growth.TryEnqueueEliteReward(token, now);
                }
            }
            if (queuedXp > 0)
            {
                int gained = queuedXp;
                queuedXp = 0;
                AddXp(gained);
            }

            // ---- T11 计时 ----
            Trace(trace, "T11 计时");
            if (ResolveGameplayEnding(now, dt)) { Recycle(trace); return; }
            if (now + dt >= Config.runDuration)
            {
                State.Now = Config.runDuration;
                // §5-1 的另一种选择（defeatWinsSameTick = false）：同 tick 死亡也算胜利
                EnterTerminal(RunPhase.Victory, diedThisTick ? "timeout（同 tick 死亡，按设置判胜）" : "timeout");
                Recycle(trace);
                return;
            }
            State.Now = now + dt;

            if (diedThisTick)
            {
                // 只会在 defeatWinsSameTick = false 且没到 3:00 时走到这里；时钟固定在本 tick 开始时
                State.Now = now;
                EnterTerminal(RunPhase.Defeat, "hp<=0");
                Recycle(trace);
                return;
            }

            // ---- T12 待升级 → LevelUp ----
            Trace(trace, "T12 升级判定");
            if (State.PendingLevelUps > 0 || (Growth != null && Growth.HasOffer))
            {
                DrawPanel();
                State.TryTransition(RunPhase.LevelUp, "待升级 " + State.PendingLevelUps);
            }

            Recycle(trace);
        }

        private void Recycle(bool trace)
        {
            // ---- T13：失败/胜利帧也必须回收 ----
            if (Combat != null) Combat.Release();
            Trace(trace, "T13 回收");
            EndTrace(trace);
        }

        private void Trace(bool on, string step)
        {
            if (on) log.Info(string.Format("[Waffle][trace] frame={0} {1}", State.Frame, step));
        }

        private void EndTrace(bool on)
        {
            if (on && TraceTicks > 0) TraceTicks--;
        }

        private void MovePlayer(float ix, float iy, float dt)
        {
            float len = (float)Math.Sqrt(ix * ix + iy * iy);
            if (len > 1f) { ix /= len; iy /= len; }
            float speed = CurrentValue(UpgradeKind.MoveSpeed);
            float mx = rightX * ix + forwardX * iy, mz = rightZ * ix + forwardZ * iy;
            float moveLength = (float)Math.Sqrt(mx * mx + mz * mz);
            if (moveLength > 0.001f) { facingX = mx / moveLength; facingZ = mz / moveLength; }
            float dodgeDt = Math.Min(dt, dodgeLeft);
            float dx = dodgeX * Config.dodgeSpeed * dodgeDt + mx * speed * (dt - dodgeDt);
            float dz = dodgeZ * Config.dodgeSpeed * dodgeDt + mz * speed * (dt - dodgeDt);
            dodgeLeft = Math.Max(0f, dodgeLeft - dt);
            float limit = Config.arenaHalfExtent;
            PlayerX = Config.unboundedWorld ? PlayerX + dx : Math.Max(-limit, Math.Min(limit, PlayerX + dx));
            PlayerZ = Config.unboundedWorld ? PlayerZ + dz : Math.Max(-limit, Math.Min(limit, PlayerZ + dz));
        }

        private void ApplyDamage(int amount, float now)
        {
            if (God || now < InvulnerableUntil)
            {
                log.Info(string.Format("[Waffle] 受击被无敌挡下 dmg={0} now={1:F3}", amount, now));
                return;
            }
            if (Growth != null && ShieldCharges > 0)
            { ShieldCharges--; Emit(PresentationEventType.ShieldBlock, PlayerX, PlayerZ, amount, State.Hp); return; }
            if (BlockWithHearth(now, amount)) return;
            if (Growth != null) amount = (int)Math.Ceiling(ContentStats.FinalIncoming(amount));
            State.Hp = Math.Max(0, State.Hp - amount);
            InvulnerableUntil = Math.Max(InvulnerableUntil, now + Config.hitIFrame);
            Emit(PresentationEventType.PlayerHurt, PlayerX, PlayerZ, amount, State.Hp);
            LoseFieldFragment();
            log.Info(string.Format("[Waffle] 受击 now={0:F3} 伤害={1} 剩余HP={2}", now, amount, State.Hp));
        }

        private CombatInput MakeCombatInput(float dt, float now)
        {
            return new CombatInput
            {
                Dt = dt, RunTime = now, PlayerPos = new Vec2(PlayerX, PlayerZ), ViewRadius = ViewRadius,
                MaxLegalMovementStep = Math.Max(CurrentValue(UpgradeKind.MoveSpeed), Config.dodgeSpeed) * dt * 1.25f,
                Stats = new CombatPlayerStats
                {
                    Damage = CurrentValue(UpgradeKind.Damage), FireInterval = CurrentValue(UpgradeKind.FireRate),
                    Pierce = (int)CurrentValue(UpgradeKind.Pierce), MagnetRadius = CurrentValue(UpgradeKind.PickupRange)
                }
            };
        }

        private void ForwardCombatEvents(CombatStepResult result)
        {
            for (int i = 0; i < result.Events.Count; i++)
            {
                PresentationEvent e = result.Events[i];
                e.Frame = State.Frame;
                Presentation.Add(e);
            }
        }

        public bool DebugSpawn(int count, bool gems)
        {
            if (RejectIfTerminal(gems ? "spawngems" : "spawn") || Combat == null || count < 1) return false;
            count = Math.Min(count, 200);
            for (int i = 0; i < count; i++)
            {
                double a = i * Math.PI * 2 / count;
                var p = new Vec2(PlayerX + (float)Math.Cos(a) * (gems ? 2f : 6f), PlayerZ + (float)Math.Sin(a) * (gems ? 2f : 6f));
                if(!Config.unboundedWorld)p = new Vec2(Math.Max(-Config.arenaHalfExtent, Math.Min(Config.arenaHalfExtent, p.X)),
                    Math.Max(-Config.arenaHalfExtent, Math.Min(Config.arenaHalfExtent, p.Y)));
                if (gems) Combat.DebugSpawnGem(p, 1); else Combat.DebugSpawnEnemy(p);
            }
            return true;
        }

        public bool DebugSetCap(int count)
        {
            if (RejectIfTerminal("setcap") || Combat == null || count < 1) return false;
            Combat.Config.aliveCap = Math.Min(count, Math.Max(1, Combat.Config.enemyPoolSize - Combat.Config.eventOverflow));
            return true;
        }

        private void AddXp(int amount)
        {
            int level = State.Level, xp = State.Xp;
            int ups = XpRules.AddXp(ref level, ref xp, amount, State.ExtraXpStepAfterFour);
            State.Level = level;
            State.Xp = xp;
            State.PendingLevelUps += ups;
            log.Info(string.Format("[Waffle] +{0} XP → Lv{1} {2}/{3}，本次升级 {4} 次，待升级 {5}",
                amount, level, xp, State.XpToNext, ups, State.PendingLevelUps));
        }

        private void EnterTerminal(RunPhase phase, string reason)
        {
            ClearResonanceForEnding();
            if (State.PendingLevelUps > 0)
                log.Info("[Waffle] 终局，丢弃待升级 " + State.PendingLevelUps);
            State.PendingLevelUps = 0;
            Growth?.CloseForRunEnd(); Combat?.ClearRunContent();
            queuedXp = 0;
            queuedDamage = 0;
            State.EndReason = reason;
            if (State.TryTransition(phase, reason))
            {
                CaptureRunOutcome();
                log.Info(string.Format("[Waffle] 终局 {0} now={1:F3} 原因={2} 等级={3} 击杀={4}",
                    phase, State.Now, reason, State.Level, State.Kills));
            }
        }

        // =====================================================================
        // 表现事件（只写入 Presentation，不改玩法状态）
        // =====================================================================

        internal void Emit(PresentationEventType type, float x, float z, float value, int data)
        {
            Presentation.Add(new PresentationEvent
            {
                Type = type, Frame = State.Frame, Now = State.Now, X = x, Z = z, Value = value, Data = data
            });
        }

        private void EmitPhaseEvent(RunPhase from, RunPhase to)
        {
            switch (to)
            {
                case RunPhase.Paused:
                    Emit(PresentationEventType.Paused, PlayerX, PlayerZ, 0f, (int)from);
                    break;
                case RunPhase.Victory:
                    Emit(PresentationEventType.Victory, PlayerX, PlayerZ, 0f, 0);
                    break;
                case RunPhase.Defeat:
                    Emit(PresentationEventType.Defeat, PlayerX, PlayerZ, 0f, 0);
                    break;
                case RunPhase.LevelUp:
                    if (from == RunPhase.Paused) Emit(PresentationEventType.Resumed, PlayerX, PlayerZ, 0f, (int)to);
                    else Emit(PresentationEventType.LevelUpShown, PlayerX, PlayerZ, 0f, State.PendingLevelUps);
                    break;
                case RunPhase.Playing:
                    if (from == RunPhase.Paused) Emit(PresentationEventType.Resumed, PlayerX, PlayerZ, 0f, (int)to);
                    break;
            }
        }

        /// <summary>
        /// 重开前由 GameLoop 调用：发出一次 Restart 事件（同一局重复调用只发一次），让音效/特效清场。
        /// 不改玩法状态；真正的重开是重新加载场景。
        /// </summary>
        public void NotifyRestart()
        {
            if (restartEmitted) return;
            restartEmitted = true;
            Combat?.ClearGameplay();
            Emit(PresentationEventType.Restart, PlayerX, PlayerZ, 0f, (int)State.Phase);
        }

        // =====================================================================
        // 暂停
        // =====================================================================

        /// <summary>暂停键：Playing/LevelUp → Paused；Paused → 暂停前的状态。终态里什么都不做。</summary>
        public void TogglePause(string reason)
        {
            if (State.Phase == RunPhase.Paused)
                State.TryTransition(State.PhaseBeforePause, reason + "（恢复）");
            else if (State.Phase == RunPhase.Playing || State.Phase == RunPhase.LevelUp)
                State.TryTransition(RunPhase.Paused, reason);
        }

        /// <summary>只暂停、不切换（失去焦点时用，§0.1、A-74）。</summary>
        public void RequestPause(string reason)
        {
            if (State.Phase == RunPhase.Playing || State.Phase == RunPhase.LevelUp)
                State.TryTransition(RunPhase.Paused, reason);
        }

        private void OnPhaseChanged(RunPhase from, RunPhase to)
        {
            EmitPhaseEvent(from, to);
            if (from == RunPhase.Playing && to != RunPhase.Playing)
                DodgeBufferUntil = -1f; // 离开 Playing 清空闪避缓冲
            if (from == RunPhase.Paused && to == RunPhase.LevelUp)
            {
                // 回到同一张面板（选项不重抽），重新计算防误触
                panelShownRealTime = realTime;
                chooseLocked = false;
            }
        }

        // =====================================================================
        // 升级面板
        // =====================================================================

        private void DrawPanel()
        {
            if (Growth != null) { GrowthPanel(); return; }
            CurrentOptions = UpgradeRules.Draw(State.UpgradeLevels, Config, random);
            PanelsShown++;
            panelShownRealTime = realTime;
            chooseLocked = false;
            log.Info(string.Format("[Waffle] 升级面板 #{0}：Lv{1} 选项 [{2}, {3}, {4}] 待升级 {5}",
                PanelsShown, State.Level, CurrentOptions[0], CurrentOptions[1], CurrentOptions[2], State.PendingLevelUps));
        }

        /// <summary>
        /// UI 提交选择（按钮或数字键）。同一帧多次提交只算第一次；面板显示后 0.3 s 内忽略。
        /// 返回 true 表示这次提交被接受。
        /// </summary>
        public bool ChooseUpgrade(int index)
        {
            if (Growth != null)
            {
                var view = ReadContentView();
                return index >= 0 && index < view.Choices.Count && TryChooseContent(view.OfferToken, view.Choices[index].Id);
            }
            if (State.Phase != RunPhase.LevelUp) return false;
            if (index < 0 || index > 2) return false;
            if (chooseLocked || lastChooseFrame == State.Frame) return false;
            if (realTime - panelShownRealTime < Config.levelUpInputGuard) return false;
            UpgradeKind kind = CurrentOptions[index];
            if (kind == UpgradeKind.None) return false;

            chooseLocked = true;
            lastChooseFrame = State.Frame;
            ApplyUpgrade(kind);
            Emit(PresentationEventType.LevelUpChosen, PlayerX, PlayerZ, 0f, (int)kind);
            State.PendingLevelUps--;
            log.Info(string.Format("[Waffle] 选择升级 {0}（第 {1} 次），剩余待升级 {2}",
                kind, State.ChosenUpgrades.Count, State.PendingLevelUps));

            if (State.PendingLevelUps > 0)
            {
                DrawPanel();
                State.TryTransition(RunPhase.LevelUp, "连续升级，重抽");
            }
            else
            {
                CurrentOptions = new[] { UpgradeKind.None, UpgradeKind.None, UpgradeKind.None };
                State.TryTransition(RunPhase.Playing, "升级完成");
            }
            return true;
        }

        private void ApplyUpgrade(UpgradeKind kind)
        {
            State.ChosenUpgrades.Add(kind);
            if (kind == UpgradeKind.Heal)
            {
                State.Hp = Math.Min(State.MaxHp, State.Hp + (int)Math.Round(State.MaxHp * Config.upgrades.healFraction));
                return;
            }
            int i = (int)kind;
            State.UpgradeLevels[i] = Math.Min(State.UpgradeLevels[i] + 1, UpgradeRules.MaxLevel(kind, Config));
            if (kind == UpgradeKind.MaxHp)
            {
                State.MaxHp = (int)Math.Round(CurrentValue(UpgradeKind.MaxHp));
                State.Hp = Math.Min(State.MaxHp, State.Hp + Config.upgrades.maxHpUpgradeHeal);
            }
        }

        // =====================================================================
        // 调试钩子（§2.11 最小集）：终态里一律拒绝并写日志（A-79b）
        // =====================================================================

        private bool RejectIfTerminal(string command)
        {
            if (!State.Phase.IsTerminal()) return false;
            State.RejectedTransitions++;
            log.Error(string.Format("[Waffle] 终态 {0} 拒绝调试命令 {1}", State.Phase, command));
            return true;
        }

        /// <summary>givexp n：排队，在下一个 Playing tick 的 T10 走正常加 XP 流程。</summary>
        public bool DebugGiveXp(int amount)
        {
            if (RejectIfTerminal("givexp " + amount) || amount <= 0) return false;
            queuedXp += amount;
            return true;
        }

        /// <summary>sethp n：立即设置当前 HP（0..MaxHp）；在下一个 tick 的 T9 判定。</summary>
        public bool DebugSetHp(int hp)
        {
            if (RejectIfTerminal("sethp " + hp)) return false;
            State.Hp = Math.Max(0, Math.Min(State.MaxHp, hp));
            return true;
        }

        /// <summary>settime s：立即设置游戏时钟（0..时长）。</summary>
        public bool DebugSetTime(float seconds)
        {
            if (RejectIfTerminal("settime " + seconds)) return false;
            State.Now = Math.Max(0f, Math.Min(Config.runDuration, seconds));
            return true;
        }

        /// <summary>hurt n：在下一个 tick 的 T8 结算一次伤害（代替敌人接触；尊重 god 和无敌）。</summary>
        public bool DebugHurt(int amount)
        {
            if (RejectIfTerminal("hurt " + amount) || amount <= 0) return false;
            queuedDamage = Math.Max(queuedDamage, amount); // 同 tick 不叠加，取最大（C-3）
            return true;
        }
    }
}
