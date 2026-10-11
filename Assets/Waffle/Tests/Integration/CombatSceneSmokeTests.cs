using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using Waffle.Combat;

namespace Waffle.Tests.Integration
{
    public class CombatSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator ActualScene_AttacksCollectsPausesUpgradesAndRestarts()
        {
            yield return SceneManager.LoadSceneAsync("S1_Graybox");
            yield return null;
            var loop = Object.FindAnyObjectByType<GameLoop>();
            var views = loop.GetComponent<CombatViews>();
            Assert.That(views, Is.Not.Null);
            Assert.That(views.enemyVisualPrefab, Is.Not.Null);
            Assert.That(loop.CalculateViewRadius(), Is.GreaterThan(9f));
            if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
            loop.Sim.God = true;
            var combat = loop.Sim.Combat;
            combat.DebugSpawnEnemy(new Vec2(3f, 0f), 5f, 0f);
            int shots = 0, deaths = 0, pickups = 0;
            loop.PresentationDispatched += events =>
            {
                foreach (var e in events)
                {
                    if (e.Type == PresentationEventType.Shot) shots++;
                    if (e.Type == PresentationEventType.EnemyKilled) deaths++;
                    if (e.Type == PresentationEventType.GemPickup) pickups++;
                }
            };
            float deadline = Time.realtimeSinceStartup + 6f;
            while (deaths == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(shots, Is.GreaterThan(0));
            Assert.That(deaths, Is.GreaterThan(0));
            Assert.That(loop.Sim.State.Kills, Is.GreaterThan(0));
            Assert.That(views.VisibleEnemies, Is.EqualTo(combat.AliveCount));
            loop.Sim.DebugSpawn(8, false);
            yield return null;
            CapturePreview(loop.gameplayCamera);

            loop.TogglePause();
            float now = loop.Sim.State.Now;
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(loop.Sim.State.Now, Is.EqualTo(now));
            loop.TogglePause();
            combat.DebugSpawnGem(new Vec2(loop.Sim.PlayerX, loop.Sim.PlayerZ), 5);
            yield return null;
            Assert.That(pickups, Is.GreaterThan(0));
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.LevelUp));
            yield return new WaitForSecondsRealtime(0.32f);
            loop.Choose(0);
            Assert.That(loop.Sim.State.Phase, Is.EqualTo(RunPhase.Playing));
            loop.Restart();
            yield return null;
            loop = Object.FindAnyObjectByType<GameLoop>();
            Assert.That(loop.Sim.Combat, Is.Not.SameAs(combat));
            Assert.That(loop.Sim.State.Level, Is.EqualTo(1));
            Assert.That(loop.Sim.State.Kills, Is.Zero);
            Assert.That(loop.Sim.God, Is.False);
        }

        // Real engine render of a deliberately spawned smoke-test situation, without IMGUI HUD.
        private static void CapturePreview(Camera camera)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var target = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                target.Create();
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request)) return;
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Docs/Verification/C-01");
                File.WriteAllBytes("Docs/Verification/C-01/combat-preview.png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; target.Release(); Object.Destroy(target);
                if (pixels != null) Object.Destroy(pixels);
            }
        }
    }
}
