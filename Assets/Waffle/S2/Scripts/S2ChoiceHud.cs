using UnityEngine;

namespace Waffle.S2
{
    /// <summary>
    /// 样板三选一。只调用 GameLoop.Choose / TogglePause / Restart。
    /// 本场景不要再挂 RunHud，否则会出现两套升级面板。
    /// 卡片进入用 unscaled 时间；暂停时停住，不另写升级规则。
    /// </summary>
    public class S2ChoiceHud : MonoBehaviour
    {
        public GameLoop loop;
        float intro;

        void Update()
        {
            if (loop == null || loop.Sim == null) return;
            RunState state = loop.Sim.State;
            if (state.Phase == RunPhase.LevelUp)
                intro = Mathf.MoveTowards(intro, 1f, Time.unscaledDeltaTime / 0.16f);
            else if (state.Phase == RunPhase.Paused && state.PhaseBeforePause == RunPhase.LevelUp)
                return;
            else
                intro = 0f;
        }

        void OnGUI()
        {
            if (loop == null || loop.Sim == null) return;
            ImguiScale.Apply();
            RunState state = loop.Sim.State;
            DrawStatus(state);
            if (state.Phase == RunPhase.LevelUp || (state.Phase == RunPhase.Paused && state.PhaseBeforePause == RunPhase.LevelUp))
                DrawCards(state);
            if (state.Phase == RunPhase.Paused)
                DrawPause(state);
            if (state.Phase == RunPhase.Victory || state.Phase == RunPhase.Defeat)
                DrawEnd(state);
        }

        void DrawStatus(RunState state)
        {
            Rect bar = ImguiScale.Fit(new Rect(16f, 16f, 520f, 64f));
            GUILayout.BeginArea(bar, GUI.skin.box);
            GUILayout.Label(RunHud.FormatClock(state.Now) + "   " + state.Phase + "   HP " + state.Hp + "/" + state.MaxHp);
            GUILayout.Label("样板占位 UI · WASD 移动 · Space 闪避 · Esc 暂停 · 自动攻击");
            if (state.Phase == RunPhase.Playing && GUILayout.Button("重开", GUILayout.Width(72f), GUILayout.Height(22f)))
                loop.Restart();
            GUILayout.EndArea();
        }

        void DrawCards(RunState state)
        {
            const float panelW = 920f;
            const float cardW = 280f;
            const float gap = 24f;
            const float cardH = 168f;
            float x0 = (ImguiScale.Width - panelW) * 0.5f;
            float y = (ImguiScale.Height - cardH) * 0.5f + (1f - intro) * 28f;
            GUILayout.BeginArea(ImguiScale.Fit(new Rect(x0, y - 36f, panelW, 32f)));
            GUILayout.Label("升级  还剩 " + state.PendingLevelUps + "  色条是提案，不是已验收色卡");
            GUILayout.EndArea();
            for (int i = 0; i < 3; i++)
            {
                Rect card = ImguiScale.Fit(new Rect(x0 + 16f + i * (cardW + gap), y, cardW, cardH));
                GUILayout.BeginArea(card, GUI.skin.box);
                Color stripe = i == 0 ? new Color(0.16f, 0.28f, 0.72f) : i == 1 ? new Color(1f, 0.27f, 0f) : new Color(0.93f, 0.74f, 0.32f);
                Color prev = GUI.color;
                GUI.color = stripe;
                GUILayout.Box("", GUILayout.Height(cardH * 0.18f));
                GUI.color = prev;
                UpgradeKind kind = loop.Sim.CurrentOptions[i];
                GUI.enabled = kind != UpgradeKind.None && state.Phase == RunPhase.LevelUp;
                GUILayout.Label((i + 1) + ". " + UpgradeRules.DisplayName(kind));
                GUILayout.Label("占位卡 · 现有规则");
                if (GUILayout.Button("选择", GUILayout.Height(36f))) loop.Choose(i);
                GUI.enabled = true;
                GUILayout.EndArea();
            }
        }

        void DrawPause(RunState state)
        {
            Rect box = ImguiScale.Fit(new Rect((ImguiScale.Width - 360f) * 0.5f, 72f, 360f, 120f));
            GUILayout.BeginArea(box, GUI.skin.box);
            GUILayout.Label(state.PhaseBeforePause == RunPhase.LevelUp ? "已暂停 · 升级卡保留" : "已暂停");
            if (GUILayout.Button("继续", GUILayout.Height(32f))) loop.TogglePause();
            if (GUILayout.Button("重开", GUILayout.Height(32f))) loop.Restart();
            GUILayout.EndArea();
        }

        void DrawEnd(RunState state)
        {
            Rect box = ImguiScale.Fit(new Rect((ImguiScale.Width - 420f) * 0.5f, (ImguiScale.Height - 160f) * 0.5f, 420f, 160f));
            GUILayout.BeginArea(box, GUI.skin.box);
            GUILayout.Label(state.Phase == RunPhase.Victory ? "胜利" : "失败");
            GUILayout.Label(RunHud.FormatClock(state.Now) + "  击杀 " + state.Kills + "  等级 " + state.Level);
            GUILayout.Label(state.EndReason);
            if (GUILayout.Button("重开", GUILayout.Height(36f))) loop.Restart();
            GUILayout.EndArea();
        }
    }
}
