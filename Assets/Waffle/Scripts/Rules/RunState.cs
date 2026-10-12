using System;
using System.Collections.Generic;

namespace Waffle
{
    /// <summary>
    /// 一局的全部可变数据 + 状态机（§0.1、§8.6）。纯 C#，不依赖 UnityEngine。
    /// 所有状态切换都必须走 <see cref="TryTransition"/> 这一个入口。
    /// </summary>
    public class RunState
    {
        private readonly IRunLog log;
        public readonly float ExtraXpStepAfterFour;

        public RunPhase Phase { get; private set; } = RunPhase.Playing;
        /// <summary>进入 Paused 之前的状态（只会是 Playing 或 LevelUp）。</summary>
        public RunPhase PhaseBeforePause { get; private set; } = RunPhase.Playing;

        /// <summary>游戏时钟（s）：本 tick 开始时的时间。</summary>
        public float Now;
        public int Hp;
        public int MaxHp;
        public int Level = 1;
        public int Xp;
        /// <summary>待升级数：T10 每升一级 +1，每选一张卡 −1。</summary>
        public int PendingLevelUps;
        public int Kills;
        /// <summary>当前帧号（只用于日志）。</summary>
        public long Frame;
        /// <summary>终局原因标签（§2.12 P-07），例如 "timeout"、"hp<=0"。</summary>
        public string EndReason = "";

        /// <summary>各升级选项的等级，下标 = UpgradeKind 0..6。</summary>
        public readonly int[] UpgradeLevels = new int[UpgradeRules.RealKindCount];
        /// <summary>本局按顺序选择过的升级（结算界面显示）。</summary>
        public readonly List<UpgradeKind> ChosenUpgrades = new List<UpgradeKind>();

        /// <summary>被拒绝的切换次数（调试 Overlay 显示，A-79b）。</summary>
        public int RejectedTransitions;

        /// <summary>状态切换事件（实例事件，不是 static）。参数：旧状态、新状态。</summary>
        public event Action<RunPhase, RunPhase> PhaseChanged;

        public RunState(int maxHp, IRunLog log, float extraXpStepAfterFour = 0)
        {
            this.log = log ?? new NullRunLog();
            ExtraXpStepAfterFour = float.IsNaN(extraXpStepAfterFour) || float.IsInfinity(extraXpStepAfterFour) ? 0 : Math.Max(0, extraXpStepAfterFour);
            MaxHp = maxHp;
            Hp = maxHp;
        }

        public int XpToNext { get { return XpRules.XpToNext(Level, ExtraXpStepAfterFour); } }

        /// <summary>§8.6 状态切换表。LevelUp → LevelUp 表示"重抽下一张面板"。</summary>
        public bool CanTransition(RunPhase to)
        {
            switch (Phase)
            {
                case RunPhase.Playing:
                    return to == RunPhase.Paused || to == RunPhase.LevelUp
                        || to == RunPhase.Victory || to == RunPhase.Defeat;
                case RunPhase.Paused:
                    return to == PhaseBeforePause; // 只能回到暂停前的状态
                case RunPhase.LevelUp:
                    return to == RunPhase.Playing || to == RunPhase.Paused || to == RunPhase.LevelUp;
                default:
                    return false; // Victory / Defeat：终态吸收，只能重开
            }
        }

        /// <summary>
        /// 唯一的状态切换入口。允许就切换并写日志，返回 true；
        /// 禁止就拒绝、写 Error 日志，返回 false。
        /// </summary>
        public bool TryTransition(RunPhase to, string reason)
        {
            RunPhase from = Phase;
            if (!CanTransition(to))
            {
                RejectedTransitions++;
                log.Error(string.Format("[Waffle] 拒绝状态切换 {0}→{1}（{2}） now={3:F3} frame={4}",
                    from, to, reason, Now, Frame));
                return false;
            }

            if (to == RunPhase.Paused) PhaseBeforePause = from;
            Phase = to;
            log.Info(string.Format("[Waffle] 状态 {0}→{1}（{2}） now={3:F3} frame={4} timeScale={5}",
                from, to, reason, Now, Frame, TimeScaleFor(to)));

            var handler = PhaseChanged;
            if (handler != null) handler(from, to);
            return true;
        }

        /// <summary>每个状态对应的 Time.timeScale（§0.1）：只有 Playing 是 1。</summary>
        public static float TimeScaleFor(RunPhase phase)
        {
            return phase == RunPhase.Playing ? 1f : 0f;
        }
    }
}
