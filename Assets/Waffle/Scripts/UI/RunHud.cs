using System.Text;
using UnityEngine;

namespace Waffle
{
    /// <summary>
    /// 灰盒阶段的 IMGUI 界面：HUD（状态/计时/HP/等级/XP）、暂停面板、升级三选一占位面板、结算与重开。
    /// 只显示，不改玩法数值；所有操作都回调 GameLoop → RunSimulation 的唯一入口。
    /// IMGUI 不受 timeScale 影响，所以暂停时按钮照常工作（A-73）。
    /// </summary>
    public class RunHud : MonoBehaviour
    {
        public GameLoop loop;

        /// <summary>本场景里 HUD 收到的"进入 LevelUp"事件次数（A-84：每次升级只触发一次）。</summary>
        public int LevelUpEventsReceived { get; private set; }

        private RunSimulation subscribedSim;
        private GUIStyle big;

        private void Start()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (loop != null && loop.Sim != null)
            {
                subscribedSim = loop.Sim;
                subscribedSim.State.PhaseChanged += OnPhaseChanged;
            }
        }

        private void OnDestroy()
        {
            if (subscribedSim != null) subscribedSim.State.PhaseChanged -= OnPhaseChanged;
            subscribedSim = null;
        }

        private void OnPhaseChanged(RunPhase from, RunPhase to)
        {
            if (to == RunPhase.LevelUp && from != RunPhase.Paused) LevelUpEventsReceived++;
        }

        public static string FormatClock(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return string.Format("{0}:{1:00}", s / 60, s % 60);
        }

        private void OnGUI()
        {
            if (loop == null || loop.Sim == null) return;
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            }

            ImguiScale.Apply();
            RunSimulation sim = loop.Sim;
            RunState st = sim.State;

            // ---- HUD ----
            GUILayout.BeginArea(ImguiScale.Fit(new Rect(10, 10, 360, 180)), GUI.skin.box);
            GUILayout.Label(FormatClock(st.Now) + "   " + st.Phase, big);
            GUILayout.Label(string.Format("HP {0}/{1}   Lv{2}   XP {3}/{4}", st.Hp, st.MaxHp, st.Level, st.Xp, st.XpToNext));
            GUILayout.Label(string.Format("待升级 {0}   击杀 {1}", st.PendingLevelUps, st.Kills));
            // Playing / LevelUp 也能直接重开（Paused、Victory、Defeat 的面板里已有重开按钮）
            if (st.Phase == RunPhase.Playing || st.Phase == RunPhase.LevelUp)
            {
                if (GUILayout.Button("重开", GUILayout.Width(80), GUILayout.Height(24))) loop.Restart();
            }
            GUILayout.EndArea();

            float w = 460f, h = 320f;
            Rect center = ImguiScale.Fit(new Rect((ImguiScale.Width - w) / 2f, (ImguiScale.Height - h) / 2f, w, h));

            switch (st.Phase)
            {
                case RunPhase.Paused:
                    GUILayout.BeginArea(center, GUI.skin.box);
                    GUILayout.Label(st.PhaseBeforePause == RunPhase.LevelUp ? "已暂停（升级面板保留）" : "已暂停", big);
                    if (GUILayout.Button("继续（Esc）", GUILayout.Height(40))) loop.TogglePause();
                    if (GUILayout.Button("重开", GUILayout.Height(40))) loop.Restart();
                    GUILayout.EndArea();
                    break;

                case RunPhase.LevelUp:
                    GUILayout.BeginArea(center, GUI.skin.box);
                    GUILayout.Label(string.Format("升级！Lv{0}（还剩 {1} 次）", st.Level - st.PendingLevelUps + 1, st.PendingLevelUps), big);
                    for (int i = 0; i < 3; i++)
                    {
                        UpgradeKind k = sim.CurrentOptions[i];
                        GUI.enabled = k != UpgradeKind.None;
                        string label = string.Format("{0}. {1}", i + 1, UpgradeRules.DisplayName(k));
                        if (GUILayout.Button(label, GUILayout.Height(44))) loop.Choose(i);
                    }
                    GUI.enabled = true;
                    GUILayout.EndArea();
                    break;

                case RunPhase.Victory:
                case RunPhase.Defeat:
                    GUILayout.BeginArea(center, GUI.skin.box);
                    GUILayout.Label(st.Phase == RunPhase.Victory ? "胜利" : "失败", big);
                    GUILayout.Label(string.Format("存活 {0}（{1:F2} s）  击杀 {2}  等级 {3}", FormatClock(st.Now), st.Now, st.Kills, st.Level));
                    GUILayout.Label("原因：" + st.EndReason);
                    GUILayout.Label("升级：" + ChosenList(st));
                    if (GUILayout.Button("重开", GUILayout.Height(40))) loop.Restart();
                    GUILayout.EndArea();
                    break;
            }
        }

        private static string ChosenList(RunState st)
        {
            if (st.ChosenUpgrades.Count == 0) return "无";
            var sb = new StringBuilder();
            for (int i = 0; i < st.ChosenUpgrades.Count; i++)
            {
                if (i > 0) sb.Append("、");
                sb.Append(UpgradeRules.DisplayName(st.ChosenUpgrades[i]));
            }
            return sb.ToString();
        }
    }
}
