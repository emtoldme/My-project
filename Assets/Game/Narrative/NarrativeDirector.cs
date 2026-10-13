using System;
using System.Collections.Generic;
using UnityEngine;
using Waffle;

namespace Reverb.Narrative
{
    /// <summary>
    /// 局内叙事总监：单向订阅 GameLoop 与 RunSimulation 状态，
    /// 驱动无线电事件判定、优先级排队、对白时钟推进与事件解耦广播。
    /// </summary>
    public class NarrativeDirector : MonoBehaviour
    {
        [Header("数据资产")]
        [SerializeField] private NarrativeDatabase narrativeDatabase;

        [Header("引用")]
        public GameLoop gameLoop;

        // 运行时状态
        private readonly HashSet<string> triggeredCues = new HashSet<string>();
        private readonly Queue<NarrativeLine> playbackQueue = new Queue<NarrativeLine>();
        private readonly List<string> unlockedTapes = new List<string>();

        private NarrativeLine currentActiveLine = null;
        private float lineTimer = 0f;
        private NarrativePriority currentPlayingPriority = NarrativePriority.Ambient;

        // 解耦对外事件
        public event Action<NarrativeLine> OnNarrativeLineStarted;
        public event Action<NarrativeLine> OnNarrativeLineFinished;
        public event Action<float> OnSignalStaticChanged;
        public event Action<string> OnTapeUnlocked;

        public NarrativeDatabase Database => narrativeDatabase;
        public NarrativeLine CurrentActiveLine => currentActiveLine;
        public IReadOnlyList<string> UnlockedTapes => unlockedTapes;
        public int TriggeredCueCount => triggeredCues.Count;

        private void Awake()
        {
            if (narrativeDatabase == null)
            {
                narrativeDatabase = HarvestRoadNarrativeScript.CreateHarvestRoadDatabase();
            }
            if (gameLoop == null)
            {
                gameLoop = GetComponent<GameLoop>() ?? FindAnyObjectByType<GameLoop>();
            }
        }

        private void Update()
        {
            if (gameLoop == null || gameLoop.Sim == null) return;
            var sim = gameLoop.Sim;

            // 升级选卡与暂停时，冻结对白时钟（保持静止沉浸）
            if (sim.State.Phase == RunPhase.Paused || sim.State.Phase == RunPhase.LevelUp)
            {
                return;
            }

            float dt = Time.deltaTime;
            EvaluateConditions(sim);
            TickPlayback(dt);
        }

        public void EvaluateConditions(RunSimulation sim)
        {
            if (narrativeDatabase == null || sim == null) return;

            float now = sim.State.Now;
            int kills = sim.State.Kills;
            float hpFraction = (float)sim.State.Hp / Mathf.Max(1, sim.State.MaxHp);

            for (int i = 0; i < narrativeDatabase.cues.Count; i++)
            {
                var cue = narrativeDatabase.cues[i];
                if (cue.triggerOnce && triggeredCues.Contains(cue.cueId)) continue;

                var cond = cue.condition;
                if (now < cond.minRunSeconds || now > cond.maxRunSeconds) continue;
                if (kills < cond.minKills) continue;
                if (hpFraction > cond.maxHpFraction) continue;

                // 检查前置装备要求
                if (!string.IsNullOrEmpty(cond.requiredContentId) && sim.Growth != null)
                {
                    if (sim.Growth.RankOf(cond.requiredContentId) <= 0) continue;
                }

                // 检查前置事件要求
                if (!string.IsNullOrEmpty(cond.requiredEventId) && !triggeredCues.Contains(cond.requiredEventId))
                {
                    continue;
                }

                TriggerCue(cue);
                break; // 单帧最多触发一个 Cue
            }
        }

        public void TriggerCue(NarrativeCueDefinition cue)
        {
            if (cue == null) return;
            triggeredCues.Add(cue.cueId);

            // 检查解锁物证
            if (!string.IsNullOrEmpty(cue.unlockTapeId) && !unlockedTapes.Contains(cue.unlockTapeId))
            {
                unlockedTapes.Add(cue.unlockTapeId);
                OnTapeUnlocked?.Invoke(cue.unlockTapeId);
            }

            // 优先级抢占机制
            if (currentActiveLine != null)
            {
                if (cue.priority > currentPlayingPriority)
                {
                    // 高优先级事件立即抢占，清空原有待播队列
                    playbackQueue.Clear();
                    currentActiveLine = null;
                }
                else if (cue.priority < currentPlayingPriority)
                {
                    // 优先级过低则直接丢弃（如环境音），保证战斗紧张感不被低级对白打扰
                    return;
                }
            }

            currentPlayingPriority = cue.priority;
            if (cue.lines != null)
            {
                for (int i = 0; i < cue.lines.Length; i++)
                {
                    playbackQueue.Enqueue(cue.lines[i]);
                }
            }
        }

        public void TickPlayback(float dt)
        {
            if (currentActiveLine != null)
            {
                lineTimer -= dt;
                if (lineTimer <= 0f)
                {
                    var ended = currentActiveLine;
                    currentActiveLine = null;
                    OnNarrativeLineFinished?.Invoke(ended);
                }
            }

            if (currentActiveLine == null && playbackQueue.Count > 0)
            {
                currentActiveLine = playbackQueue.Dequeue();
                lineTimer = currentActiveLine.duration;
                OnNarrativeLineStarted?.Invoke(currentActiveLine);
                OnSignalStaticChanged?.Invoke(currentActiveLine.staticIntensity);
            }
        }

        public bool HasTriggered(string cueId) => triggeredCues.Contains(cueId);
    }
}
