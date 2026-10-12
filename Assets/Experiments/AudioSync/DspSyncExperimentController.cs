using System;
using UnityEngine;

namespace Reverb.Experiments.AudioSync
{
    /// <summary>
    /// E-03 同步混音技术实验控制器：
    /// 验证双轨（或多轨）同采样率/长度音频通过 AudioSettings.dspTime 计划启动同步播放，
    /// 支持独立垂直分层增益平滑过渡、暂停/恢复采样对齐、重开重置与采样漂移测量。
    /// 独立实验组件，不与正式 AudioDirector 冲突，不参与默认战斗逻辑。
    /// </summary>
    [DisallowMultipleComponent]
    public class DspSyncExperimentController : MonoBehaviour
    {
        [Header("Audio Clips (TEST_ONLY Stems)")]
        [Tooltip("基础节奏/底床轨道 (Track A)")]
        [SerializeField] private AudioClip trackA;
        [Tooltip("动态氛围/高频分层轨道 (Track B)")]
        [SerializeField] private AudioClip trackB;

        [Header("DSP Scheduling Parameters")]
        [Tooltip("DSP 启动预调度延迟时间（秒），防止音频线程调度抖动导致跳音")]
        [SerializeField] private double scheduleDelaySec = 0.15;

        [Header("Vertical Layering Gains")]
        [Range(0f, 1f)]
        [SerializeField] private float targetGainA = 1.0f;
        [Range(0f, 1f)]
        [SerializeField] private float targetGainB = 0.0f;
        [Tooltip("分层增益平滑过渡时间（秒）")]
        [SerializeField] private float gainFadeDuration = 1.0f;

        [Header("Playback Status (Read Only)")]
        [SerializeField] private bool isPlaying = false;
        [SerializeField] private bool isPaused = false;
        [SerializeField] private int loopCount = 0;
        [SerializeField] private int maxSampleDriftDetected = 0;
        [SerializeField] private int currentSampleDrift = 0;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private double scheduledStartTime = 0.0;
        private float currentGainA = 1.0f;
        private float currentGainB = 0.0f;
        private int lastSampleA = 0;

        public bool IsPlaying => isPlaying;
        public bool IsPaused => isPaused;
        public int LoopCount => loopCount;
        public int MaxSampleDriftDetected => maxSampleDriftDetected;
        public int CurrentSampleDrift => currentSampleDrift;
        public float GainA => currentGainA;
        public float GainB => currentGainB;

        private void Awake()
        {
            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            if (sourceA == null)
            {
                sourceA = gameObject.AddComponent<AudioSource>();
                sourceA.playOnAwake = false;
                sourceA.loop = true;
                sourceA.spatialBlend = 0.0f; // 2D 纯立体声
                sourceA.bypassEffects = true;
                sourceA.bypassListenerEffects = true;
                sourceA.bypassReverbZones = true;
            }

            if (sourceB == null)
            {
                sourceB = gameObject.AddComponent<AudioSource>();
                sourceB.playOnAwake = false;
                sourceB.loop = true;
                sourceB.spatialBlend = 0.0f; // 2D 纯立体声
                sourceB.bypassEffects = true;
                sourceB.bypassListenerEffects = true;
                sourceB.bypassReverbZones = true;
            }
        }

        /// <summary>
        /// 配置测试音轨
        /// </summary>
        public void ConfigureClips(AudioClip clipA, AudioClip clipB)
        {
            trackA = clipA;
            trackB = clipB;
        }

        /// <summary>
        /// 双轨 DSP 预定同步启动
        /// </summary>
        public void StartSyncPlayback()
        {
            EnsureAudioSources();
            if (trackA == null || trackB == null)
            {
                Debug.LogWarning("[DspSyncExperiment] Clips not configured!");
                return;
            }

            sourceA.clip = trackA;
            sourceB.clip = trackB;

            currentGainA = targetGainA;
            currentGainB = targetGainB;
            sourceA.volume = currentGainA;
            sourceB.volume = currentGainB;

            // 采样点绝对归零
            sourceA.timeSamples = 0;
            sourceB.timeSamples = 0;

            // 核心：利用 AudioSettings.dspTime 预定精确启动时间
            scheduledStartTime = AudioSettings.dspTime + scheduleDelaySec;
            sourceA.PlayScheduled(scheduledStartTime);
            sourceB.PlayScheduled(scheduledStartTime);

            isPlaying = true;
            isPaused = false;
            loopCount = 0;
            maxSampleDriftDetected = 0;
            currentSampleDrift = 0;
            lastSampleA = 0;

            Debug.Log($"[DspSyncExperiment] Scheduled dual tracks to start at dspTime={scheduledStartTime:F4} (Delay={scheduleDelaySec*1000:F1}ms)");
        }

        /// <summary>
        /// 设置分层目标增益（垂直混音）
        /// </summary>
        public void SetLayerGains(float gainA, float gainB)
        {
            targetGainA = Mathf.Clamp01(gainA);
            targetGainB = Mathf.Clamp01(gainB);
        }

        /// <summary>
        /// 暂停播放（冻结时间并保持对齐）
        /// </summary>
        public void PauseSync()
        {
            if (!isPlaying || isPaused) return;

            sourceA.Pause();
            sourceB.Pause();
            isPaused = true;
            Debug.Log($"[DspSyncExperiment] Paused at sample A={sourceA.timeSamples}, B={sourceB.timeSamples}");
        }

        /// <summary>
        /// 恢复播放（重对齐并同步解冻）
        /// </summary>
        public void ResumeSync()
        {
            if (!isPlaying || !isPaused) return;

            // 恢复时强制以 Track A 当前采样点为基准对齐 Track B，杜绝驱动唤醒不同步
            int resumeSample = sourceA.timeSamples;
            sourceB.timeSamples = resumeSample;

            sourceA.UnPause();
            sourceB.UnPause();
            isPaused = false;
            Debug.Log($"[DspSyncExperiment] Resumed aligned at sample={resumeSample}");
        }

        /// <summary>
        /// 重置与停止播放
        /// </summary>
        public void StopAndReset()
        {
            if (sourceA != null) sourceA.Stop();
            if (sourceB != null) sourceB.Stop();

            isPlaying = false;
            isPaused = false;
            currentSampleDrift = 0;
            Debug.Log("[DspSyncExperiment] Stopped and reset all tracks.");
        }

        private void Update()
        {
            if (!isPlaying || isPaused) return;

            // 1. 平滑增益插值（垂直分层渐入渐出）
            float dt = Time.unscaledDeltaTime;
            if (gainFadeDuration > 0.001f)
            {
                currentGainA = Mathf.MoveTowards(currentGainA, targetGainA, dt / gainFadeDuration);
                currentGainB = Mathf.MoveTowards(currentGainB, targetGainB, dt / gainFadeDuration);
            }
            else
            {
                currentGainA = targetGainA;
                currentGainB = targetGainB;
            }

            sourceA.volume = currentGainA;
            sourceB.volume = currentGainB;

            // 2. 采样漂移与循环监控
            if (AudioSettings.dspTime >= scheduledStartTime)
            {
                int sampleA = sourceA.timeSamples;
                int sampleB = sourceB.timeSamples;

                // 统计循环次数（以 Track A 回绕为准）
                if (sampleA < lastSampleA && lastSampleA > (trackA.samples / 2))
                {
                    loopCount++;
                    Debug.Log($"[DspSyncExperiment] Completed loop #{loopCount}, drift at wrap={Mathf.Abs(sampleA - sampleB)} samples");
                }
                lastSampleA = sampleA;

                // 测量两轨当前采样偏差
                currentSampleDrift = Mathf.Abs(sampleA - sampleB);
                if (currentSampleDrift > maxSampleDriftDetected)
                {
                    maxSampleDriftDetected = currentSampleDrift;
                }

                // 若因驱动极端抖动漂移超过容差（如 128 samples / 约 2.6ms），执行硬锁步校正
                if (currentSampleDrift > 128)
                {
                    sourceB.timeSamples = sampleA;
                }
            }
        }
    }
}
