using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
            var loop = GetComponent<GameLoop>(); var controls = GetComponent<M01PlaytestController>();
            for (int i = 0; i < 3; i++) yield return null;
            if (loop.Sim.State.Phase == RunPhase.Paused) loop.TogglePause();
            var start = loop.playerView.position;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W));
            yield return new WaitForSecondsRealtime(0.25f);
            var beforeDodge = loop.playerView.position;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W, Key.Space));
            yield return new WaitForSecondsRealtime(0.12f);
            var afterDodge = loop.playerView.position;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            yield return new WaitForSecondsRealtime(0.1f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape));
            yield return new WaitForSecondsRealtime(0.1f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            float pauseTime = loop.Sim.State.Now;
            yield return new WaitForSecondsRealtime(0.2f);
            bool paused = loop.Sim.State.Phase == RunPhase.Paused && loop.Sim.State.Now == pauseTime;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Escape));
            yield return new WaitForSecondsRealtime(0.1f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            var fx = GetComponent<M01DerivedFx>();
            bool capturedEcho = false, capturedLink = false;
            float deadline = Time.realtimeSinceStartup + 7f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (!capturedEcho && fx.ActiveEchoes > 0)
                { ScreenCapture.CaptureScreenshot(Path.Combine(output, "echo.png")); capturedEcho = true; }
                if (!capturedLink && fx.ActiveLinks > 0)
                { ScreenCapture.CaptureScreenshot(Path.Combine(output, "propagation.png")); capturedLink = true; }
                yield return null;
            }
            bool moved = Vector3.Distance(start, beforeDodge) > 0.2f;
            bool dodged = loop.Sim.DodgeRequests > 0 && Vector3.Distance(beforeDodge, afterDodge) > 1f;
            bool rootScale = loop.playerView.localScale == Vector3.one;
            bool echo = controls.Mode != MechanismMode.Echo && controls.Mode != MechanismMode.Combined || controls.EchoHits > 0;
            bool propagation = controls.Mode != MechanismMode.Propagation && controls.Mode != MechanismMode.Combined || controls.PropagationHits > 0;
            bool none = controls.Mode != MechanismMode.None || controls.EchoHits == 0 && controls.PropagationHits == 0;
            bool passed = moved && dodged && paused && rootScale && echo && propagation && none;
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "screen.png"));
            for (int i = 0; i < 5; i++) yield return null;
            string text = $"TEST_ONLY mode={controls.Mode} passed={passed} movement={moved} dodge={dodged} pause={paused} rootScale={rootScale} echo={controls.EchoHits} propagation={controls.PropagationHits} kills={loop.Sim.State.Kills} time={loop.Sim.State.Now:F3} seed={loop.config.seed}\n";
            File.WriteAllText(Path.Combine(output, "smoke.txt"), text);
            Debug.Log("M01_STANDALONE_SMOKE " + text);
            Application.Quit(passed ? 0 : 2);
        }
    }
}
