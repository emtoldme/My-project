using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Waffle.Tests.Integration
{
    public class AudioLifecycleTests
    {
        [UnityTest]
        public IEnumerator RealScene_PauseUpgradeReenableTerminalAndRestart()
        {
            yield return SceneManager.LoadSceneAsync("S1_Graybox");
            yield return null;
            var loop = Object.FindAnyObjectByType<GameLoop>();
            if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
            loop.Sim.Combat.Config.spawningEnabled = false;
            loop.Sim.God = true;
            loop.config.levelUpInputGuard = 0f;
            var audio = loop.GetComponent<AudioDirector>();
            audio.levelUpCrossfade = audio.resumeFadeIn = audio.victoryFadeOut = audio.defeatCut = 0.03f;
            var combat = loop.transform.Find("Music_Combat").GetComponent<AudioSource>();
            var upgrade = loop.transform.Find("Music_LevelUp").GetComponent<AudioSource>();
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(combat.clip, Is.Not.Null);
            Assert.That(upgrade.clip, Is.Not.Null);
            Assert.That(combat.isPlaying, Is.True);
            Assert.That(upgrade.isPlaying, Is.True, "Silent upgrade loop must stay ready for crossfade.");
            loop.TogglePause();
            Assert.That(combat.isPlaying || upgrade.isPlaying, Is.False);
            float frozenGain = combat.volume;
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(combat.volume, Is.EqualTo(frozenGain));
            // Re-enable in Paused before any upgrade: cannot restart the combat bed.
            audio.enabled = false;
            audio.enabled = true;
            Assert.That(combat.isPlaying || upgrade.isPlaying, Is.False);
            loop.TogglePause();
            Assert.That(combat.isPlaying && upgrade.isPlaying, Is.True);
            loop.Sim.DebugGiveXp(30);
            yield return null;
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.LevelUp));
            Assert.That(upgrade.isPlaying, Is.True);
            Assert.That(upgrade.volume, Is.GreaterThan(0.7f));
            Assert.That(combat.isPlaying, Is.False);
            loop.TogglePause();
            audio.enabled = false;
            audio.enabled = true;
            Assert.That(combat.isPlaying || upgrade.isPlaying, Is.False);
            loop.TogglePause();
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(upgrade.isPlaying, Is.True);
            Assert.That(combat.isPlaying, Is.False, "Resuming paused upgrade must not unlock combat music.");
            for (int i = 0; i < 5 && loop.Sim.State.Phase == RunPhase.LevelUp; i++)
            {
                loop.Choose(0);
                yield return null;
                if (loop.Sim.State.PendingLevelUps > 0)
                    Assert.That(combat.volume, Is.LessThan(0.01f));
            }
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(combat.isPlaying, Is.True);
            Assert.That(combat.volume, Is.GreaterThan(0.99f));
            loop.Sim.DebugSetTime(180f);
            yield return null;
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.Victory));
            Assert.That(combat.isPlaying || upgrade.isPlaying, Is.False);
            audio.enabled = false;
            audio.enabled = true;
            Assert.That(combat.isPlaying || upgrade.isPlaying, Is.False, "Terminal re-enable must stay silent.");
            loop.Restart();
            yield return null;
            loop = Object.FindAnyObjectByType<GameLoop>();
            Assert.That(loop.GetComponent<AudioDirector>(), Is.Not.SameAs(audio));
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(loop.transform.Find("Music_Combat").GetComponent<AudioSource>().isPlaying, Is.True);
            Assert.That(loop.transform.Find("Music_Combat").GetComponent<AudioLowPassFilter>().cutoffFrequency, Is.EqualTo(22000f));
            var freshAudio = loop.GetComponent<AudioDirector>();
            freshAudio.defeatCut = 0.03f;
            loop.Sim.Combat.Config.spawningEnabled = false;
            loop.Sim.DebugSetHp(0);
            yield return null;
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.Defeat));
            combat = loop.transform.Find("Music_Combat").GetComponent<AudioSource>();
            upgrade = loop.transform.Find("Music_LevelUp").GetComponent<AudioSource>();
            Assert.That(combat.isPlaying || upgrade.isPlaying, Is.False);
            Assert.That(combat.GetComponent<AudioLowPassFilter>().cutoffFrequency, Is.EqualTo(200f).Within(0.1f));
        }
    }
}
