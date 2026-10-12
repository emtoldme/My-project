using System.Collections;
using System.IO;
using NUnit.Framework;
using Reverb.Experiments.Mechanism.Integration;
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
    public class M01PlaytestTests
    {
        [UnityTest]
        public IEnumerator FourModes_RealScene_ReloadSameSeed_FreezeAndClean()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/Experiments/Mechanism/Scenes/M01_MechanismPlaytest.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Editor loader required.");
#endif
            yield return null;
            int oldRate = Application.targetFrameRate; Application.targetFrameRate = 60;
            var results = new System.Text.StringBuilder();
            try
            {
                foreach (var mode in new[] { MechanismMode.Combined, MechanismMode.None, MechanismMode.Echo, MechanismMode.Propagation })
                {
                    var controller = Object.FindAnyObjectByType<M01PlaytestController>();
                    var old = controller;
                    controller.RestartWithMode(mode);
                    float deadline = Time.realtimeSinceStartup + 5;
                    do { yield return null; controller = Object.FindAnyObjectByType<M01PlaytestController>(); }
                    while ((controller == null || controller == old) && Time.realtimeSinceStartup < deadline);
                    Assert.That(controller, Is.Not.Null);
                    Assert.That(controller, Is.Not.EqualTo(old));
                    var loop = controller.GetComponent<GameLoop>();
                    Assert.That(controller.IsReady, Is.True);
                    Assert.That(controller.Mode, Is.EqualTo(mode));
                    Assert.That(loop.config.seed, Is.EqualTo(12345));
                    Assert.That(controller.EchoHits, Is.Zero);
                    Assert.That(controller.PropagationHits, Is.Zero);
                    loop.combatConfig.spawningEnabled = false; loop.Sim.God = true; loop.Sim.FixedDt = 1f / 60f;
                    if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
                    for (int i = 0; i < 100; i++) yield return null;
                    Assert.That(controller.EchoHits, mode == MechanismMode.Echo || mode == MechanismMode.Combined ? Is.GreaterThan(0) : Is.Zero);
                    Assert.That(controller.PropagationHits, mode == MechanismMode.Propagation || mode == MechanismMode.Combined ? Is.GreaterThan(0) : Is.Zero);
                    var fx = controller.GetComponent<M01DerivedFx>();
                    loop.TogglePause();
                    float now = loop.Sim.State.Now;
                    int echoes = controller.EchoHits, propagation = controller.PropagationHits, pending = controller.PendingCount;
                    int activeEcho = fx.ActiveEchoes, activeLinks = fx.ActiveLinks;
                    yield return new WaitForSecondsRealtime(.2f);
                    Assert.That(loop.Sim.State.Now, Is.EqualTo(now));
                    Assert.That(controller.EchoHits, Is.EqualTo(echoes));
                    Assert.That(controller.PropagationHits, Is.EqualTo(propagation));
                    Assert.That(controller.PendingCount, Is.EqualTo(pending));
                    Assert.That(fx.ActiveEchoes, Is.EqualTo(activeEcho)); Assert.That(fx.ActiveLinks, Is.EqualTo(activeLinks));
                    Assert.That(loop.playerView.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(Object.FindObjectsByType<GameLoop>().Length, Is.EqualTo(1));
                    Assert.That(Object.FindObjectsByType<AudioDirector>().Length, Is.EqualTo(1));
                    results.AppendLine($"mode={mode} seed={loop.config.seed} echo={echoes} propagation={propagation} pending={pending} pause/reload/root=passed");
                    loop.TogglePause();
                }
                Directory.CreateDirectory("Docs/Verification/M01");
                File.WriteAllText("Docs/Verification/M01/playmode-observations.txt", results.ToString());
            }
            finally { Application.targetFrameRate = oldRate; Time.timeScale = 1; }
        }
    }
}
