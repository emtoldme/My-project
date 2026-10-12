using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Waffle;
using Waffle.Combat;
using Waffle.Experiments.VisualPipeline;
using Reverb.Experiments.AudioSync;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Reverb.Experiments.Verification
{
    public class ExperimentPlayTests
    {
        const string Evidence = "Docs/Verification/Reverb-Followup-02";

        [UnityTest]
        public IEnumerator Visual_RealEvents_Freeze_Clear_AndRenderedRing()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/Experiments/VisualPipeline/Scenes/E01_VisualPipeline.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Editor experiment scene loader required.");
#endif
            yield return null;
            var loop = Object.FindAnyObjectByType<GameLoop>();
            if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
            loop.Sim.God = true;
            loop.Sim.FixedDt = 1f / 60f;
            var wave = loop.GetComponent<E01WavePlayer>();
            int shots = 0, hits = 0;
            loop.PresentationDispatched += events =>
            {
                foreach (var item in events)
                {
                    if (item.Type == PresentationEventType.Shot) shots++;
                    if (item.Type == PresentationEventType.EnemyHit) hits++;
                }
            };
            var enemy = loop.Sim.Combat.DebugSpawnEnemy(new Vec2(loop.Sim.PlayerX + 2f, loop.Sim.PlayerZ), 500, 0);
            for (int frame = 0; frame < 120 && hits == 0; frame++) yield return null;
            Assert.That(shots, Is.GreaterThan(0));
            Assert.That(hits, Is.GreaterThan(0));
            Assert.That(enemy.Hp, Is.LessThan(500));
            Assert.That(wave.ActiveRings, Is.GreaterThan(0));
            Assert.That(wave.ActiveSparks, Is.GreaterThan(0));
            var particles = wave.GetComponentInChildren<ParticleSystem>();
            Assert.That(particles, Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<VfxDirector>(), Is.Null);
            Assert.That(loop.playerView.localScale, Is.EqualTo(Vector3.one));
            loop.TogglePause();
            var rings = wave.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("E01_Wave_")).ToArray();
            var block = new MaterialPropertyBlock();
            rings[0].GetPropertyBlock(block);
            float radius = block.GetFloat("_Radius");
            int frozenParticles = particles.particleCount;
            yield return new WaitForSecondsRealtime(0.2f);
            rings[0].GetPropertyBlock(block);
            Assert.That(block.GetFloat("_Radius"), Is.EqualTo(radius));
            Assert.That(particles.particleCount, Is.EqualTo(frozenParticles));
            Directory.CreateDirectory(Evidence);
            var on = Capture(loop.gameplayCamera, Evidence + "/e01-wave-on.png");
            foreach (var ring in rings) ring.enabled = false;
            var off = Capture(loop.gameplayCamera, Evidence + "/e01-wave-off.png");
            int different = 0;
            for (int i = 0; i < on.Length; i++)
                if (Mathf.Abs(on[i].r - off[i].r) + Mathf.Abs(on[i].g - off[i].g) + Mathf.Abs(on[i].b - off[i].b) > 30) different++;
            Assert.That(different, Is.GreaterThan(20), "The actual ShaderGraph ring must change rendered pixels.");
            wave.waveEnabled = false;
            wave.enabled = false;
            Assert.That(wave.ActiveRings, Is.Zero);
            Assert.That(wave.ActiveSparks, Is.Zero);
            File.WriteAllText(Evidence + "/e01-runtime.txt", $"Real shots={shots}; hits={hits}; rendered ring pixels={different}; pause and disable cleanup passed. Camera-only captures, no IMGUI. TEST_ONLY art.\n");
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("S1_Graybox");
        }

        [UnityTest]
        public IEnumerator Audio_ActualStems_TenWraps_Pause_Resume_Restart_Disable()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/Experiments/AudioSync/E03_AudioSync.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Editor experiment scene loader required.");
#endif
            yield return null;
            Time.timeScale = 1f;
            var controller = Object.FindAnyObjectByType<DspSyncExperimentController>();
            Assert.That(Object.FindAnyObjectByType<AudioDirector>(), Is.Null);
            Assert.That(controller.ClipsAreCompatible(out var reason), Is.True, reason);
            controller.SetLayerGains(0.7f, 0.6f);
            controller.StartSyncPlayback();
            // Cancel while still scheduled; it must stay silent beyond the canceled start.
            controller.PauseSync();
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(controller.SourcesRunning, Is.False);
            controller.ResumeSync();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(controller.SourcesRunning, Is.True, "An active audio device is required for this test.");
            Assert.That(controller.SamplePositionA, Is.GreaterThan(0));
            Assert.That(controller.StartProbeReady, Is.True);
            Assert.That(controller.StartProbeDifferenceSamples, Is.LessThan(64));
            controller.SetLayerGains(0.7f, 0.6f);
            for (int i = 0; i < 3; i++)
            {
                controller.PauseSync();
                yield return new WaitForSecondsRealtime(0.1f);
                Assert.That(controller.SourcesRunning, Is.False);
                controller.ResumeSync();
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.That(controller.SourcesRunning, Is.True);
                Assert.That(controller.StartProbeReady, Is.True);
                Assert.That(controller.StartProbeDifferenceSamples, Is.LessThan(64));
            }
            float deadline = Time.realtimeSinceStartup + 175f;
            while (controller.ObservedWraps < 10 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(controller.ObservedWraps, Is.GreaterThanOrEqualTo(10));
            Assert.That(controller.ValidObservations, Is.GreaterThan(100));
            AudioSettings.GetDSPBufferSize(out int buffer, out int buffers);
            Assert.That(controller.MaxSampleDriftDetected, Is.LessThanOrEqualTo(buffer * 2), "Main-thread cursors are quantized; audio-thread onset is checked separately.");
            Assert.That(controller.GainA, Is.EqualTo(0.7f).Within(0.01f));
            Assert.That(controller.GainB, Is.EqualTo(0.6f).Within(0.01f));
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + "/e03-runtime.txt", $"TEST_ONLY actual 16-second stems; observed wraps={controller.ObservedWraps}; DSP estimated loops={controller.LoopCount}; stable reads={controller.ValidObservations}; rejected reads={controller.DiscardedObservations}; max main-thread circular cursor distance={controller.MaxSampleDriftDetected}; last audio-thread first-signal difference={controller.StartProbeDifferenceSamples:F3} output samples; clipHz={controller.SampleRate}; outputHz={AudioSettings.outputSampleRate}; buffer={buffer}x{buffers}; output trim={DspSyncExperimentController.TestOutputTrim}. Start and three resumes checked separately. Main-thread cursor does not certify sub-buffer phase; source filter onset is not hardware output capture. No listening, no hard seek correction.\n");
            controller.StopAndReset();
            Assert.That(controller.LoopCount, Is.Zero);
            controller.StartSyncPlayback();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(controller.SourcesRunning, Is.True);
            controller.enabled = false;
            Assert.That(controller.IsPlaying, Is.False);
            Assert.That(controller.SourcesRunning, Is.False);
        }

        static Color32[] Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                target.Create();
                var request = new RenderPipeline.StandardRequest { destination = target };
                Assert.That(RenderPipeline.SupportsRenderRequest(camera, request), Is.True);
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                return texture.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(texture);
            }
        }
    }
}
