using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Waffle.Combat;
using Waffle.S2;

namespace Waffle.Tests.Integration
{
    public class S2StyleLabTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator RealSculptures_AllPartDodge_MusicChoice_Upgrade_EliteAndRestart()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.Update(); InputSystem.Update();
            yield return SceneManager.LoadSceneAsync("S2_StyleLab");
            yield return null;
            var loop = Object.FindAnyObjectByType<GameLoop>();
            if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
            loop.Sim.FixedDt = 1f / 60f;
            loop.Sim.God = true;
            loop.config.levelUpInputGuard = 0f;
            var combat = loop.Sim.Combat;
            combat.Config.spawningEnabled = false;
            Assert.That(Object.FindObjectsByType<GameLoop>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<RunHud>(), Is.Null);
            var hud = loop.GetComponent<S2ChoiceHud>();
            var audio = loop.GetComponent<AudioDirector>();
            Assert.That(audio.combatLoop, Is.SameAs(hud.musicA));
            Assert.That(hud.musicB, Is.Not.Null);
            Assert.That(audio.shoot.clips.Single().name, Does.StartWith("sfx_s03_"));
            var views = loop.GetComponent<CombatViews>();
            Assert.That(views.tintEnemyMaterials || views.normalizeEnemyVisuals, Is.False);
            var parts = loop.playerView.Find("Visual").GetComponentsInChildren<MeshFilter>();
            Assert.That(parts.Length, Is.EqualTo(5), "Actual A-03 model must replace the placeholder.");
            Assert.That(parts.All(p => p.sharedMesh != null), Is.True);
            int vertices = parts.Sum(p => p.sharedMesh.vertexCount);
            int shots = 0, hits = 0, pickups = 0;
            loop.PresentationDispatched += es => {
                foreach (var e in es)
                {
                    if (e.Type == PresentationEventType.Shot) shots++;
                    if (e.Type == PresentationEventType.EnemyHit) hits++;
                    if (e.Type == PresentationEventType.GemPickup) pickups++;
                }
            };
            combat.DebugSpawnEnemy(new Vec2(3f, 2f), 100000f, 0f);
            for (int i = 0; i < 24; i++) yield return null;
            Assert.That(shots, Is.GreaterThan(0)); Assert.That(hits, Is.GreaterThan(0));
            Capture(loop.gameplayCamera, "combat");
            Assert.That(loop.Controls.GameplayMapEnabled, Is.True, "Old scene cleanup must not disable the new run's maps.");
            Vector3 start = loop.playerView.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
            yield return null; yield return null;
            Assert.That(loop.Sim.DodgeRequests, Is.EqualTo(1));
            Assert.That(Vector3.Distance(start, loop.playerView.position), Is.GreaterThan(0.2f));
            Assert.That(loop.playerView.localScale, Is.EqualTo(Vector3.one));
            var ghosts = loop.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.name == "FX_Afterimage" && r.gameObject.activeSelf).ToArray();
            Assert.That(ghosts.Length, Is.GreaterThan(0));
            foreach (var ghost in ghosts)
            {
                Assert.That(ghost.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(vertices), "All sculpture parts must enter the snapshot.");
                Assert.That(ghost.bounds.size.y, Is.InRange(1.3f, 2.1f), "No repeated scaling.");
            }
            Capture(loop.gameplayCamera, "dodge");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            var motion = loop.playerView.Find("Visual/RS_Player/MotionRoot");
            Assert.That(motion, Is.Not.Null);
            loop.TogglePause();
            var frozen = motion.Cast<Transform>().Select(t => t.localPosition).ToArray();
            hud.UseMusic(hud.musicB);
            Assert.That(audio.combatLoop, Is.SameAs(hud.musicB));
            Assert.That(loop.transform.Find("Music_Combat").GetComponent<AudioSource>().isPlaying, Is.False);
            for (int i = 0; i < 5; i++) yield return null;
            CollectionAssert.AreEqual(frozen, motion.Cast<Transform>().Select(t => t.localPosition).ToArray());
            loop.TogglePause();
            Assert.That(loop.transform.Find("Music_Combat").GetComponent<AudioSource>().isPlaying, Is.True);
            for (int i = 0; i < 20; i++) yield return null;
            combat.DebugSpawnGem(new Vec2(loop.Sim.PlayerX, loop.Sim.PlayerZ), 1);
            yield return null;
            Assert.That(pickups, Is.GreaterThan(0));
            loop.Sim.DebugGiveXp(30);
            yield return null;
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.LevelUp));
            for (int i = 0; i < 5 && loop.Sim.State.Phase == RunPhase.LevelUp; i++)
            { loop.Choose(0); yield return null; }
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.Playing));
            // Use the real 120s elite event, with isolated test-only wave lists.
            combat.Config.pushTimes = Enumerable.Repeat(float.MaxValue, combat.Config.pushTimes.Length).ToArray();
            combat.Config.ringTimes = Enumerable.Repeat(float.MaxValue, combat.Config.ringTimes.Length).ToArray();
            combat.Config.waveCounts = new int[combat.Config.waveCounts.Length];
            combat.Config.spawningEnabled = true;
            loop.config.damage = 1000f;
            loop.Sim.DebugSetTime(119.99f);
            EnemyState elite = null;
            for (int i = 0; i < 4 && elite == null; i++)
            {
                yield return null;
                elite = combat.Enemies.FirstOrDefault(e => e.IsAlive && e.IsElite);
            }
            Assert.That(elite, Is.Not.Null);
            loop.Sim.PlayerX = elite.Position.X - 2f; loop.Sim.PlayerZ = elite.Position.Y;
            bool ringVisible = false;
            for (int i = 0; i < 65; i++)
            {
                yield return null;
                ringVisible = loop.GetComponentsInChildren<LineRenderer>().Any(r => r.name.StartsWith("S2_Ring_") && r.enabled);
                if (ringVisible) break;
            }
            Assert.That(ringVisible, Is.True, "Actual elite kill must reach S2 emphasis.");
            Capture(loop.gameplayCamera, "elite");
            var oldCombat = combat;
            loop.Restart();
            yield return null; yield return null;
            loop = Object.FindAnyObjectByType<GameLoop>();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("S2_StyleLab"));
            Assert.That(loop.Sim.Combat, Is.Not.SameAs(oldCombat));
            Assert.That(loop.Sim.State.Level, Is.EqualTo(1));
            Assert.That(loop.Sim.God, Is.False);
            Assert.That(loop.Controls.GameplayMapEnabled, Is.True, "Restart must own fresh enabled input maps.");
            Assert.That(loop.playerView.localScale, Is.EqualTo(Vector3.one));
            Assert.That(loop.GetComponent<AudioDirector>().combatLoop, Is.SameAs(loop.GetComponent<S2ChoiceHud>().musicA));
            Debug.Log("S2_RUNTIME_SMOKE_PASSED: actual model, all-part dodge, pause, A/B, upgrade, real elite and restart.");
        }

        static void Capture(Camera camera, string name)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var rt = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                rt.Create();
                var request = new RenderPipeline.StandardRequest { destination = rt };
                Assert.That(RenderPipeline.SupportsRenderRequest(camera, request), Is.True);
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                Directory.CreateDirectory("Docs/Verification/S2-StyleLab");
                File.WriteAllBytes("Docs/Verification/S2-StyleLab/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; rt.Release(); Object.Destroy(rt); Object.Destroy(texture);
            }
        }
    }
}
