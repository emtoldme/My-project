using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Waffle.Tests.Integration
{
    public class PresentationSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator PresentationScene_StartPauseUpgradeAndRestart()
        {
            yield return SceneManager.LoadSceneAsync("S1_Graybox");
            yield return null;
            var loop = Object.FindAnyObjectByType<GameLoop>();
            Assert.That(loop, Is.Not.Null);
            Assert.That(loop.GetComponent<AudioDirector>().combatLoop, Is.Not.Null);
            Assert.That(loop.GetComponent<VfxDirector>().afterimageMaterial, Is.Not.Null);
            // A headless editor may correctly auto-pause on its initial focus notification.
            if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.Playing));

            loop.TogglePause();
            float stoppedAt = loop.Sim.State.Now;
            yield return new WaitForSecondsRealtime(0.06f);
            Assert.That(loop.Sim.State.Now, Is.EqualTo(stoppedAt));
            loop.TogglePause();

            loop.Sim.DebugGiveXp(5);
            yield return null;
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.LevelUp));
            yield return new WaitForSecondsRealtime(0.32f);
            loop.Choose(0);
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.Playing));

            var previous = loop;
            loop.Restart();
            yield return null;
            loop = Object.FindAnyObjectByType<GameLoop>();
            Assert.That(loop, Is.Not.Null);
            Assert.That(loop != previous, Is.True);
            Assert.That(loop.Sim.State.Level, Is.EqualTo(1));
            Assert.That(loop.Sim.State.PendingLevelUps, Is.Zero);
            Assert.That(loop.Sim.PanelsShown, Is.Zero);
        }
    }
}
