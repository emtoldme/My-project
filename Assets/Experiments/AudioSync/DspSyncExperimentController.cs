using System;
using UnityEngine;

namespace Reverb.Experiments.AudioSync
{
    // TEST_ONLY: sampling observations do not certify the hardware audio output.
    [DisallowMultipleComponent]
    public class DspSyncExperimentController : MonoBehaviour
    {
        [SerializeField] private AudioClip trackA, trackB;
        [SerializeField] private double scheduleDelaySec = 0.15;
        [Range(0f, 1f)] [SerializeField] private float targetGainA = 1f, targetGainB;
        [SerializeField] private float gainFadeDuration = 1f;
        private AudioSource sourceA, sourceB;
        private bool isPlaying, isPaused;
        private double scheduledStartTime, segmentStartTime, completedPlaySeconds;
        private int pausedSample, loopCount, maxSampleDriftDetected, currentSampleDrift;
        private int validObservations, discardedObservations, observedWraps, lastSample;
        private float currentGainA = 1f, currentGainB;
        private DspStartProbe probeA, probeB;
        public const float TestOutputTrim = 0.5f;

        public bool IsPlaying => isPlaying;
        public bool IsPaused => isPaused;
        public bool IsScheduled => isPlaying && !isPaused && AudioSettings.dspTime < scheduledStartTime;
        public int LoopCount => loopCount;
        public int MaxSampleDriftDetected => maxSampleDriftDetected;
        public int CurrentSampleDrift => currentSampleDrift;
        public int ValidObservations => validObservations;
        public int DiscardedObservations => discardedObservations;
        public int ObservedWraps => observedWraps;
        public int SampleRate => trackA != null ? trackA.frequency : 0;
        public int SamplePositionA => sourceA != null ? sourceA.timeSamples : 0;
        public int SamplePositionB => sourceB != null ? sourceB.timeSamples : 0;
        public double ScheduledStartTime => scheduledStartTime;
        public float GainA => currentGainA;
        public float GainB => currentGainB;
        public bool SourcesRunning => sourceA != null && sourceA.isPlaying && sourceB.isPlaying && !IsScheduled;
        public bool StartProbeReady => probeA != null && probeB != null && probeA.Captured && probeB.Captured;
        public double StartProbeDifferenceSamples => StartProbeReady ? Math.Abs(probeA.FirstSignalDsp - probeB.FirstSignalDsp) * AudioSettings.outputSampleRate : double.NaN;

        private void Awake() => EnsureAudioSources();
        private void EnsureAudioSources()
        {
            if (sourceA == null) sourceA = CreateSource("TEST_ONLY Track A", out probeA);
            if (sourceB == null) sourceB = CreateSource("TEST_ONLY Track B", out probeB);
        }
        private AudioSource CreateSource(string name, out DspStartProbe probe)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            probe = child.AddComponent<DspStartProbe>();
            source.playOnAwake = false; source.loop = true; source.spatialBlend = 0f;
            return source;
        }
        public void ConfigureClips(AudioClip clipA, AudioClip clipB)
        {
            StopAndReset(); trackA = clipA; trackB = clipB;
        }
        public bool ClipsAreCompatible(out string reason)
        {
            if (trackA == null || trackB == null) reason = "Both clips are required.";
            else if (trackA.frequency != trackB.frequency || trackA.samples != trackB.samples || trackA.channels != trackB.channels)
                reason = "Clip sample rate, sample count and channel count must match.";
            else if (trackA.samples <= 0 || trackA.frequency <= 0) reason = "Empty clip.";
            else if (trackA.loadState != AudioDataLoadState.Loaded || trackB.loadState != AudioDataLoadState.Loaded)
                reason = "Preload both clips before scheduling.";
            else { reason = null; return true; }
            return false;
        }
        public void StartSyncPlayback()
        {
            StopAndReset(); EnsureAudioSources();
            if (!ClipsAreCompatible(out var reason))
            {
                Debug.LogWarning("[DspSyncExperiment] " + reason); return;
            }
            sourceA.clip = trackA; sourceB.clip = trackB;
            currentGainA = targetGainA; currentGainB = targetGainB;
            sourceA.volume = currentGainA * TestOutputTrim; sourceB.volume = currentGainB * TestOutputTrim;
            isPlaying = true; ScheduleAtSample(0);
            AudioSettings.GetDSPBufferSize(out int bufferSize, out int buffers);
            Debug.Log($"[DspSyncExperiment] TEST_ONLY scheduled={scheduledStartTime:F6} clipHz={SampleRate} outputHz={AudioSettings.outputSampleRate} buffer={bufferSize}x{buffers}");
        }
        private void ScheduleAtSample(int sample)
        {
            sourceA.Stop(); sourceB.Stop();
            sourceA.timeSamples = sample; sourceB.timeSamples = sample;
            lastSample = sample;
            scheduledStartTime = AudioSettings.dspTime + Math.Max(0.05, scheduleDelaySec);
            probeA.Arm(scheduledStartTime); probeB.Arm(scheduledStartTime);
            segmentStartTime = scheduledStartTime;
            sourceA.PlayScheduled(scheduledStartTime); sourceB.PlayScheduled(scheduledStartTime);
        }
        public void SetLayerGains(float gainA, float gainB)
        {
            targetGainA = Mathf.Clamp01(gainA); targetGainB = Mathf.Clamp01(gainB);
        }
        public void PauseSync()
        {
            if (!isPlaying || isPaused) return;
            bool scheduled = IsScheduled;
            sourceA.Pause(); sourceB.Pause();
            pausedSample = scheduled ? 0 : sourceA.timeSamples;
            completedPlaySeconds += Math.Max(0, AudioSettings.dspTime - segmentStartTime);
            // Cancel future scheduling too; sequential UnPause is not a DSP-synchronized resume.
            sourceA.Stop(); sourceB.Stop(); isPaused = true;
        }
        public void ResumeSync()
        {
            if (!isPlaying || !isPaused) return;
            ScheduleAtSample(pausedSample); isPaused = false;
        }
        public void StopAndReset()
        {
            if (sourceA != null) sourceA.Stop();
            if (sourceB != null) sourceB.Stop();
            isPlaying = isPaused = false;
            pausedSample = loopCount = maxSampleDriftDetected = currentSampleDrift = 0;
            validObservations = discardedObservations = observedWraps = lastSample = 0;
            completedPlaySeconds = scheduledStartTime = segmentStartTime = 0;
        }
        public static int CircularSampleDistance(int a, int b, int length)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
            int difference = Math.Abs(a - b) % length;
            return Math.Min(difference, length - difference);
        }
        private void Update()
        {
            if (!isPlaying || isPaused) return;
            float step = gainFadeDuration > 0.001f ? Time.unscaledDeltaTime / gainFadeDuration : 1f;
            currentGainA = Mathf.MoveTowards(currentGainA, targetGainA, step);
            currentGainB = Mathf.MoveTowards(currentGainB, targetGainB, step);
            sourceA.volume = currentGainA * TestOutputTrim; sourceB.volume = currentGainB * TestOutputTrim;
            if (!SourcesRunning) return;
            // Reject reads crossing an audio-buffer update; do not seek-correct and hide drift.
            int before = sourceA.timeSamples, sampleB = sourceB.timeSamples, after = sourceA.timeSamples;
            if (before != after) { discardedObservations++; return; }
            currentSampleDrift = CircularSampleDistance(before, sampleB, trackA.samples);
            maxSampleDriftDetected = Math.Max(maxSampleDriftDetected, currentSampleDrift);
            if (before < lastSample && lastSample > trackA.samples / 2) observedWraps++;
            lastSample = before;
            validObservations++;
            double played = completedPlaySeconds + Math.Max(0, AudioSettings.dspTime - segmentStartTime);
            loopCount = (int)(played * trackA.frequency / trackA.samples);
        }
        private void OnDisable() => StopAndReset();
        private void OnDestroy() => StopAndReset();
    }
}
