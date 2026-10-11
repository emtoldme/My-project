using UnityEngine;
using UnityEngine.InputSystem;

namespace Waffle
{
    /// <summary>
    /// 调试控制台 + Overlay（§2.11 最小集）。按 F1 显示/隐藏；只在编辑器和 Development Build 中启用。
    /// 命令解析在纯 C# 的 DebugCommands 里。
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
        public GameLoop loop;
        public bool visible = true;

        private string command = "";
        private string lastResult = DebugCommands.Help;
        private float fpsSmoothed;

        private void Awake()
        {
            if (!Debug.isDebugBuild) enabled = false;
            if (loop == null) loop = GetComponent<GameLoop>();
        }

        // 只做显示用的帧率统计和 F1 开关，不含玩法逻辑
        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) fpsSmoothed = Mathf.Lerp(fpsSmoothed, 1f / dt, 0.1f);
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || loop == null || loop.Sim == null) return;
            RunSimulation sim = loop.Sim;
            RunState st = sim.State;
            RunHud hud = GetComponent<RunHud>();

            ImguiScale.Apply();
            GUILayout.BeginArea(ImguiScale.Fit(new Rect(ImguiScale.Width - 470, 10, 460, 360)), GUI.skin.box);
            GUILayout.Label(string.Format("状态 {0}（暂停前 {1}）  now {2:F3}  dt {3:F4}", st.Phase, st.PhaseBeforePause, st.Now, sim.LastDt));
            GUILayout.Label(string.Format("FPS {0:F0}  timeScale {1}  frame {2}", fpsSmoothed, Time.timeScale, st.Frame));
            GUILayout.Label(string.Format("Map: Gameplay={0}  UI={1}", loop.Controls.GameplayMapEnabled, loop.Controls.UiMapEnabled));
            GUILayout.Label(string.Format("待升级 {0}  面板数 {1}  LevelUp 事件 {2}", st.PendingLevelUps, sim.PanelsShown, hud != null ? hud.LevelUpEventsReceived : -1));
            GUILayout.Label(string.Format("闪避请求 {0}  无敌 {1}  god {2}  拒绝切换 {3}", sim.DodgeRequests, sim.IsInvulnerable, sim.God, st.RejectedTransitions));
            GUILayout.Label(string.Format("玩家 ({0:F2}, {1:F2})  固定dt {2}  slowmo {3}", sim.PlayerX, sim.PlayerZ, sim.FixedDt, sim.Slowmo));

            GUI.SetNextControlName("waffle_cmd");
            command = GUILayout.TextField(command);
            Event e = Event.current;
            bool submit = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                          && GUI.GetNameOfFocusedControl() == "waffle_cmd";
            if (GUILayout.Button("执行") || submit)
            {
                lastResult = DebugCommands.Execute(sim, command);
                Debug.Log("[Waffle][debug] " + command + " → " + lastResult);
                command = "";
                if (submit) e.Use();
            }
            GUILayout.Label(lastResult);
            GUILayout.EndArea();
        }
    }
}
