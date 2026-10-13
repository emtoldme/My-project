using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Waffle;

namespace Reverb.Experiments.Mechanism.Integration
{
    // M01 owns its display controls; ordinary scenes and PlayerSettings stay unchanged.
    public sealed class M01DisplayControls : MonoBehaviour
    {
        static bool sessionInitialized;
        static int windowWidth = 1280, windowHeight = 720;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { sessionInitialized = false; windowWidth = 1280; windowHeight = 720; }

        void Start()
        {
            if (Application.isEditor || sessionInitialized) return;
            sessionInitialized = true;
            var args = Environment.GetCommandLineArgs();
            // Explicit windowed launches and automated smoke runs keep their requested mode.
            if (Array.IndexOf(args, "-screen-fullscreen") >= 0 || Array.IndexOf(args, "--m01-smoke-output") >= 0) return;
            SetFullscreen(true);
        }

        void Update()
        {
            if (!Application.isEditor && Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
                SetFullscreen(!Screen.fullScreen);
        }

        public void SetFullscreen(bool fullscreen)
        {
            if (Application.isEditor) return;
            if (fullscreen)
            {
                if (!Screen.fullScreen) { windowWidth = Screen.width; windowHeight = Screen.height; }
                var desktop = Screen.currentResolution;
                Screen.SetResolution(desktop.width, desktop.height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                Screen.SetResolution(windowWidth, windowHeight, FullScreenMode.Windowed);
            }
        }

        void OnGUI()
        {
            if (Application.isEditor) return;
            ImguiScale.Apply();
            var rect = ImguiScale.Fit(new Rect(ImguiScale.Width - 216, 12, 200, 38));
            if (GUI.Button(rect, Screen.fullScreen ? "切换窗口 · F11" : "切换全屏 · F11"))
                SetFullscreen(!Screen.fullScreen);
        }
    }
}
