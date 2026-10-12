using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Waffle;
using Waffle.Combat;
using Waffle.Experiments.VisualPipeline;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Reverb.Experiments.Verification
{
    // TEST_ONLY: whole Editor frame observations, not isolated effect cost or a shipping benchmark.
    public class SmallSceneProfileTests
    {
        [Serializable] class Metric
        {
            public string name, unit;
            public bool available;
            public int samples;
            public double median, p95, mean;
        }
        [Serializable] class Phase
        {
            public string mode;
            public int activeRings, activeParticles, enemyCount;
            public Metric[] metrics;
        }
        [Serializable] class Report
        {
            public string unity, cpu, gpu, graphicsApi, os, scope;
            public int width = 1280, height = 720;
            public Phase[] phases;
            public string[] availableCounters;
        }

        [UnityTest]
        public IEnumerator Visual_SmallScene_ProfilerCounters_OnOff()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/Experiments/VisualPipeline/Scenes/E01_VisualPipeline.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Editor scene required.");
#endif
            yield return null;
            var loop = UnityEngine.Object.FindAnyObjectByType<GameLoop>();
            if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
            loop.Sim.God = true;
            loop.Sim.FixedDt = 1f / 60f;
            var wave = loop.GetComponent<E01WavePlayer>();
            loop.Sim.Combat.DebugSpawnEnemy(new Vec2(loop.Sim.PlayerX + 2, loop.Sim.PlayerZ), 500, 0);
            for (int i = 0; i < 120 && wave.ActiveSparks == 0; i++) yield return null;
            Assert.That(wave.ActiveRings, Is.GreaterThan(0));
            Assert.That(wave.ActiveSparks, Is.GreaterThan(0));
            loop.TogglePause(); // Freeze transforms and effects, vary renderer visibility only.
            foreach (var script in UnityEngine.Object.FindObjectsByType<MonoBehaviour>())
                if (script.GetType().Name == "RunHud" || script.GetType().Name == "DebugConsole") script.enabled = false;
            var renderers = wave.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("E01_")).ToArray();
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            var camera = loop.gameplayCamera;
            var oldTarget = camera.targetTexture;
            camera.targetTexture = target;
            int oldRate = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount;
            Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0;
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var descriptions = handles.Select(ProfilerRecorderHandle.GetDescription).ToArray();
            string[] wanted = { "Main Thread", "PlayerLoop", "Render Thread", "Draw Calls Count", "Batches Count", "SetPass Calls Count", "GC Allocated In Frame", "GPU Frame Time" };
            var recorders = new ProfilerRecorder[wanted.Length];
            var units = new string[wanted.Length];
            var phases = new List<Phase>();
            try
            {
                for (int i = 0; i < wanted.Length; i++)
                    foreach (var description in descriptions)
                        if (description.Name == wanted[i])
                        {
                            recorders[i] = ProfilerRecorder.StartNew(description.Category, description.Name, 1);
                            units[i] = description.UnitType.ToString();
                            break;
                        }
                foreach (bool enabled in new[] { false, true, true, false })
                {
                    foreach (var renderer in renderers) renderer.enabled = enabled;
                    for (int i = 0; i < 30; i++) yield return null;
                    var values = wanted.Select(_ => new List<long>(120)).ToArray();
                    for (int frame = 0; frame < 120; frame++)
                    {
                        yield return null;
                        for (int i = 0; i < recorders.Length; i++)
                        {
                            if (recorders[i].Valid && recorders[i].Count > 0) values[i].Add(recorders[i].LastValue);
#if UNITY_EDITOR
                            else if (wanted[i] == "Draw Calls Count")
                            { values[i].Add(UnityEditor.UnityStats.drawCalls); units[i] = "Count (UnityEditor.UnityStats)"; }
#endif
                        }
                    }
                    var metrics = new Metric[wanted.Length];
                    for (int i = 0; i < wanted.Length; i++)
                    {
                        values[i].Sort();
                        // All-zero GPU timings are unavailable rather than zero-cost rendering.
                        bool available = values[i].Count > 0 && (wanted[i] != "GPU Frame Time" || values[i].Any(v => v > 0));
                        metrics[i] = new Metric { name = wanted[i], unit = units[i] ?? "unavailable", available = available, samples = values[i].Count };
                        if (available)
                        {
                            metrics[i].median = values[i][values[i].Count / 2];
                            metrics[i].p95 = values[i][(int)((values[i].Count - 1) * .95)];
                            metrics[i].mean = values[i].Average(v => (double)v);
                        }
                    }
                    phases.Add(new Phase { mode = enabled ? "on" : "off", activeRings = wave.ActiveRings, activeParticles = wave.ActiveSparks,
                        enemyCount = loop.Sim.Combat.Enemies.Count(e => e.IsAlive), metrics = metrics });
                }
                var report = new Report { unity = Application.unityVersion, cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName,
                    graphicsApi = SystemInfo.graphicsDeviceType.ToString(), os = SystemInfo.operatingSystem, phases = phases.ToArray(),
                    availableCounters = descriptions.Select(d => d.Category.Name + "/" + d.Name + " [" + d.UnitType + "]").Where(s => System.Text.RegularExpressions.Regex.IsMatch(s, "Draw|Batch|Render|GPU|PlayerLoop|Main Thread|GC Allocated", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).ToArray(),
                    scope = "Windows Editor PlayMode / batch D3D11. Same paused scene, camera and objects; E01 renderer visibility off/on/on/off. 30 warmup + 120 observed frames per phase, target 60Hz, camera target 1280x720. Whole frame CPU includes Editor, waits and test harness; GC is whole-frame allocation, not just effects. GPU unavailable if absent/all zero. ProfilerRecorder plus explicitly labeled UnityStats fallback for draw calls, not a Frame Debugger trace. No stress or shipping performance acceptance." };
                Directory.CreateDirectory("Docs/Verification/Reverb-Followup-02");
                File.WriteAllText("Docs/Verification/Reverb-Followup-02/e01-profiler.json", JsonUtility.ToJson(report, true) + "\n");
                Assert.That(phases.Select(p => p.activeRings).Distinct().Count(), Is.EqualTo(1));
                Assert.That(phases.Select(p => p.activeParticles).Distinct().Count(), Is.EqualTo(1));
                Assert.That(phases.Select(p => p.enemyCount).Distinct().Count(), Is.EqualTo(1));
            }
            finally
            {
                foreach (var recorder in recorders) if (recorder.Valid) recorder.Dispose();
                foreach (var renderer in renderers) if (renderer != null) renderer.enabled = true;
                camera.targetTexture = oldTarget;
                target.Release(); UnityEngine.Object.DestroyImmediate(target);
                Application.targetFrameRate = oldRate; QualitySettings.vSyncCount = oldVsync;
                Time.timeScale = 1;
            }
        }
    }
}
