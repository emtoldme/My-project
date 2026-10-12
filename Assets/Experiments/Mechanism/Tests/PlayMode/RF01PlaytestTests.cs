using System.Collections;
using System.IO;
using NUnit.Framework;
using Reverb.Experiments.Mechanism.Integration;
using ReverbInTheRye.VFX;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Waffle;
using Waffle.Experiments.Mechanism.Presentation;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Reverb.Experiments.Mechanism.Tests
{
    public class RF01PlaytestTests
    {
        [UnityTest]
        public IEnumerator RF01_FourModes_RealEvents_ParticlesWindPause_TerminalAndReload()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/Experiments/RyeField/Scenes/RF01_MechanismPlaytest.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Editor loader required.");
#endif
            yield return null;
            int previousRate = Application.targetFrameRate; Application.targetFrameRate = 60;
            var observations = new System.Text.StringBuilder();
            try
            {
                foreach (var mode in new[] { MechanismMode.Combined, MechanismMode.None, MechanismMode.Echo, MechanismMode.Propagation })
                {
                    var old = Object.FindAnyObjectByType<M01PlaytestController>();
                    old.RestartWithMode(mode);
                    M01PlaytestController controller;
                    float deadline = Time.realtimeSinceStartup + 6f;
                    do { yield return null; controller = Object.FindAnyObjectByType<M01PlaytestController>(); }
                    while ((controller == null || controller == old) && Time.realtimeSinceStartup < deadline);
                    Assert.That(controller, Is.Not.Null); Assert.That(controller, Is.Not.EqualTo(old));
                    var loop = controller.GetComponent<GameLoop>();
                    var feedback = controller.GetComponent<RF01FeedbackBridge>();
                    var sonic = controller.GetComponent<SonicAttackVFX>();
                    var fx = controller.GetComponent<M01DerivedFx>();
                    Assert.That(loop.config.seed, Is.EqualTo(12345));
                    Assert.That(controller.PendingCount, Is.Zero);
                    // The new scene may have completed its first Update before this coroutine resumes.
                    Assert.That(sonic.FireCount, Is.LessThanOrEqualTo(1));
                    Assert.That(sonic.FireCount, Is.EqualTo(loop.Sim.Combat.Counters.ShotsFired));
                    Assert.That(sonic.PoolSize, Is.EqualTo(18));
                    Assert.That(Object.FindObjectsByType<GameLoop>().Length, Is.EqualTo(1));
                    Assert.That(Object.FindObjectsByType<AudioDirector>().Length, Is.EqualTo(1));
                    loop.combatConfig.spawningEnabled = false; loop.Sim.God = true; loop.Sim.FixedDt = 1f / 60f;
                    if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
                    bool cueSeen = false;
                    for (int i = 0; i < 90; i++) { yield return null; cueSeen |= fx.ActiveCues > 0; }
                    bool hasEcho = mode == MechanismMode.Echo || mode == MechanismMode.Combined;
                    bool hasPropagation = mode == MechanismMode.Propagation || mode == MechanismMode.Combined;
                    Assert.That(cueSeen, Is.EqualTo(hasEcho));
                    Assert.That(controller.EchoHits, hasEcho ? Is.GreaterThan(0) : Is.Zero);
                    Assert.That(controller.PropagationHits, hasPropagation ? Is.GreaterThan(0) : Is.Zero);
                    Assert.That(sonic.FireCount, Is.EqualTo(feedback.Shots).And.GreaterThan(0));
                    Assert.That(sonic.HitCount, Is.EqualTo(feedback.DirectHits).And.GreaterThan(0));
                    Assert.That(sonic.DeathCount, Is.EqualTo(feedback.Deaths));
                    Assert.That(sonic.PoolSize, Is.EqualTo(18));
                    // Pause while visual effects are active so a no-op empty-pool check cannot pass.
                    for (int i = 0; i < 60 && sonic.ActiveEffects == 0; i++) yield return null;
                    Assert.That(sonic.ActiveEffects, Is.GreaterThan(0));
                    loop.TogglePause();
                    // Let LateUpdate deliver the final already-simulated tick before recording the paused view.
                    yield return null;
                    float now = loop.Sim.State.Now, visual = sonic.VisualTime, wind = feedback.WindTime;
                    int active = sonic.ActiveEffects, cues = fx.ActiveCues;
                    var systems = sonic.GetComponentsInChildren<ParticleSystem>(true);
                    var particleTimes = new float[systems.Length];
                    for (int i = 0; i < systems.Length; i++) particleTimes[i] = systems[i].time;
                    yield return new WaitForSecondsRealtime(.25f);
                    Assert.That(loop.Sim.State.Now, Is.EqualTo(now));
                    Assert.That(sonic.VisualTime, Is.EqualTo(visual)); Assert.That(feedback.WindTime, Is.EqualTo(wind));
                    Assert.That(sonic.ActiveEffects, Is.EqualTo(active)); Assert.That(fx.ActiveCues, Is.EqualTo(cues));
                    for (int i = 0; i < systems.Length; i++) Assert.That(systems[i].time, Is.EqualTo(particleTimes[i]));
                    Assert.That(loop.playerView.localScale, Is.EqualTo(Vector3.one));
                    loop.TogglePause(); loop.Sim.God = false; Assert.That(loop.Sim.DebugHurt(99999), Is.True);
                    yield return null; yield return null;
                    Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.Defeat));
                    Assert.That(fx.ActiveCues, Is.Zero); Assert.That(controller.PendingCount, Is.Zero); Assert.That(sonic.ActiveEffects, Is.Zero);
                    observations.AppendLine($"mode={mode} cueSeen={cueSeen} echo={controller.EchoHits} propagation={controller.PropagationHits} shot={feedback.Shots} direct={feedback.DirectHits} deaths={feedback.Deaths} pool=18 pause/terminal/reload=passed");
                }
                Directory.CreateDirectory("Docs/Verification/RF01");
                File.WriteAllText("Docs/Verification/RF01/playmode-observations.txt", observations.ToString());
            }
            finally { Application.targetFrameRate = previousRate; Time.timeScale = 1f; }
        }
    }
}
