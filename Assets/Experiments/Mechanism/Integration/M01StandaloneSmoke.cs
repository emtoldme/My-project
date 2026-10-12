using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using Waffle;
using Waffle.Experiments.Mechanism.Presentation;

namespace Reverb.Experiments.Mechanism.Integration
{
    // Opt-in automation only. Normal player launches do not run this coroutine or inject input.
    public sealed class M01StandaloneSmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "--m01-smoke-output");
            if (flag < 0 || flag + 1 >= args.Length) yield break;
            string output = Path.GetFullPath(args[flag + 1]); Directory.CreateDirectory(output);
            Application.runInBackground = true; Application.targetFrameRate = 60;
            // Batch players need not expose a physical keyboard. This device exists only in smoke runs.
            var keyboard = InputSystem.AddDevice<Keyboard>("M01SmokeKeyboard");
            var loop = GetComponent<GameLoop>(); var controls = GetComponent<M01PlaytestController>();
            for (int i = 0; i < 3; i++) yield return null;
            if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
            var start = loop.playerView.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSecondsRealtime(0.25f);
            var beforeDodge = loop.playerView.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space));
            yield return new WaitForSecondsRealtime(0.12f);
            var afterDodge = loop.playerView.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(0.1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return new WaitForSecondsRealtime(0.1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            float pauseTime = loop.Sim.State.Now;
            yield return new WaitForSecondsRealtime(0.2f);
            bool paused = loop.Sim.State.Phase == RunPhase.Paused && loop.Sim.State.Now == pauseTime;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return new WaitForSecondsRealtime(0.1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            bool displayPassed = true;
            if (Array.IndexOf(args, "--m01-display-smoke") >= 0)
            {
                var display = GetComponent<M01DisplayControls>();
                display.SetFullscreen(false);
                yield return new WaitForSecondsRealtime(.5f);
                int windowWidth = Screen.width, windowHeight = Screen.height;
                var desktop = Screen.currentResolution;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F11));
                yield return new WaitForSecondsRealtime(.5f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                bool fullscreen = Screen.fullScreen && Screen.fullScreenMode == FullScreenMode.FullScreenWindow
                    && Screen.width == desktop.width && Screen.height == desktop.height;
                yield return new WaitForSecondsRealtime(.1f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F11));
                yield return new WaitForSecondsRealtime(.5f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                bool windowed = !Screen.fullScreen && Screen.width == windowWidth && Screen.height == windowHeight;
                if (loop.Sim.State.Phase != RunPhase.Paused) loop.TogglePause();
                float frozen = loop.Sim.State.Now;
                yield return new WaitForSecondsRealtime(.1f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F11));
                yield return new WaitForSecondsRealtime(.5f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                bool pausedFullscreen = Screen.fullScreen && loop.Sim.State.Phase == RunPhase.Paused && loop.Sim.State.Now == frozen;
                display.SetFullscreen(false);
                yield return new WaitForSecondsRealtime(.5f);
                displayPassed = fullscreen && windowed && pausedFullscreen && !Screen.fullScreen;
                File.WriteAllText(Path.Combine(output, "display.txt"), $"fullscreen={fullscreen} restoredWindow={windowed} pausedFullscreen={pausedFullscreen} passed={displayPassed} desktop={desktop.width}x{desktop.height} window={windowWidth}x{windowHeight}\n");
                if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
            }
            var fx = GetComponent<M01DerivedFx>();
            bool capturedEcho = false, capturedLink = false;
            float deadline = Time.realtimeSinceStartup + 7f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (!capturedEcho && fx.ActiveEchoes > 0)
                { capturedEcho = CaptureWorld(loop.gameplayCamera, Path.Combine(output, "echo.png")); }
                if (!capturedLink && fx.ActiveLinks > 0)
                { capturedLink = CaptureWorld(loop.gameplayCamera, Path.Combine(output, "propagation.png")); }
                yield return null;
            }
            bool moved = Vector3.Distance(start, beforeDodge) > 0.2f;
            bool dodged = loop.Sim.DodgeRequests > 0 && Vector3.Distance(beforeDodge, afterDodge) > 1f;
            bool rootScale = loop.playerView.localScale == Vector3.one;
            bool echo = controls.Mode != MechanismMode.Echo && controls.Mode != MechanismMode.Combined || controls.EchoHits > 0;
            bool propagation = controls.Mode != MechanismMode.Propagation && controls.Mode != MechanismMode.Combined || controls.PropagationHits > 0;
            bool none = controls.Mode != MechanismMode.None || controls.EchoHits == 0 && controls.PropagationHits == 0;
            bool rendered = CaptureWorld(loop.gameplayCamera, Path.Combine(output, "world.png"));
            bool passed = moved && dodged && paused && rootScale && echo && propagation && none && rendered && displayPassed;
            for (int i = 0; i < 5; i++) yield return null;
            string text = $"TEST_ONLY mode={controls.Mode} passed={passed} movement={moved} dodge={dodged} pause={paused} rootScale={rootScale} rendered={rendered} echo={controls.EchoHits} propagation={controls.PropagationHits} kills={loop.Sim.State.Kills} time={loop.Sim.State.Now:F3} seed={loop.config.seed}\n";
            File.WriteAllText(Path.Combine(output, "smoke.txt"), text);
            Debug.Log("M01_STANDALONE_SMOKE " + text);
            Application.Quit(passed ? 0 : 2);
        }

        // Hidden Windows do not reliably present the backbuffer. Capture the actual URP camera.
        // This records the 3D world only; IMGUI text/buttons still need normal-window acceptance.
        static bool CaptureWorld(Camera camera, string path)
        {
            if (camera == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return false;
            var target = RenderTexture.GetTemporary(1280, 720, 24);
            var previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request)) return false;
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                var colors = pixels.GetPixels32(); int min = 765, max = 0;
                for (int i = 0; i < colors.Length; i += 31)
                { int value = colors[i].r + colors[i].g + colors[i].b; min = Math.Min(min, value); max = Math.Max(max, value); }
                return max - min > 30;
            }
            finally
            {
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
                if (pixels != null) Destroy(pixels);
            }
        }
    }
}
