using System.Collections;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Rendering;

namespace Waffle.Tests.Integration
{
    public class DodgeSceneTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator SpaceInput_MovesRealPlayer_AndAfterimagesMatchVisualSize()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.Update();
            InputSystem.Update();
            yield return SceneManager.LoadSceneAsync("S1_Graybox");
            yield return null;
            var loop = Object.FindAnyObjectByType<GameLoop>();
            if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
            loop.Sim.Combat.Config.spawningEnabled = false;
            loop.Sim.FixedDt = 1f / 60f;
            loop.Sim.God = true;
            // Real scene component, normal player loop and real Space/D bindings.
            Vector3 start = loop.playerView.position;
            Vector3 scale = loop.playerView.Find("Visual").localScale;
            Capture(loop.gameplayCamera, "before-dodge");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
            yield return null;
            yield return null;
            Assert.That(loop.Sim.DodgeRequests, Is.EqualTo(1), "Actual Space binding must reach GameLoop.");
            Assert.That(Vector3.Distance(start, loop.playerView.position), Is.GreaterThan(0.2f));
            Assert.That(loop.playerView.Find("Visual").localScale, Is.EqualTo(scale), "Rigid model must keep its scale during dodge.");
            Assert.That(Mathf.Abs(loop.gameplayCamera.WorldToViewportPoint(loop.playerView.position).x - 0.5f), Is.GreaterThan(0.005f), "Camera must allow a visible short movement before catching up.");
            var renderers = loop.playerView.Find("Visual").GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var ghosts = loop.GetComponentsInChildren<MeshRenderer>().Where(r => r.gameObject.name == "FX_Afterimage").ToArray();
            Assert.That(ghosts.Length, Is.GreaterThan(0));
            foreach (var ghost in ghosts)
            {
                Debug.Log("DODGE_SIZE_CHECK: visual=" + bounds.size + " ghost=" + ghost.bounds.size + " scale=" + ghost.transform.lossyScale);
                Assert.That(ghost.bounds.size.magnitude, Is.LessThanOrEqualTo(bounds.size.magnitude * 1.15f));
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            for (int i = 0; i < 4; i++) yield return null;
            ghosts = loop.GetComponentsInChildren<MeshRenderer>().Where(r => r.gameObject.name == "FX_Afterimage").ToArray();
            Assert.That(ghosts.Length, Is.EqualTo(2));
            Assert.That(Vector3.Distance(ghosts[0].transform.position, ghosts[1].transform.position), Is.GreaterThan(0.5f), "Second snapshot must be further along the dodge.");
            Vector3 frozenPosition = ghosts[0].transform.position;
            Capture(loop.gameplayCamera, "during-dodge");
            loop.TogglePause();
            float pausedAt = loop.Sim.State.Now;
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(loop.Sim.State.Now, Is.EqualTo(pausedAt));
            Assert.That(ghosts[0].gameObject.activeSelf, Is.True);
            Assert.That(ghosts[0].transform.position, Is.EqualTo(frozenPosition));
            loop.TogglePause();
            for (int i = 0; i < 13; i++)
            {
                yield return null;
            }
            Assert.That(Vector3.Distance(start, loop.playerView.position), Is.EqualTo(3f).Within(0.01f));
            Assert.That(loop.GetComponentsInChildren<MeshRenderer>().Count(r => r.gameObject.name == "FX_Afterimage"), Is.Zero, "Trail must return to the pool.");
            Debug.Log("DODGE_DISTANCE_CHECK: " + Vector3.Distance(start, loop.playerView.position));
            Capture(loop.gameplayCamera, "after-dodge");
        }

        [UnityTearDown]
        public IEnumerator CleanupScene()
        {
            var scene = SceneManager.GetSceneByName("S1_Graybox");
            if (scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("DodgeTestCleanup"));
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            Time.timeScale = 1f;
        }

        private static void Capture(Camera camera, string name)
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
                Directory.CreateDirectory("Docs/Verification/DodgeFix");
                File.WriteAllBytes("Docs/Verification/DodgeFix/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release(); Object.Destroy(target);
                if (pixels != null) Object.Destroy(pixels);
            }
        }
    }
}
