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
        public AudioClip musicA, musicB;
        float intro;
        GUIStyle titleStyle, bodyStyle;
        Font uiFont;

        void EnsureStyles()
        {
            if (titleStyle != null) return;
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 18);
            titleStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 24, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 16, wordWrap = true };
        }

        static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

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
            EnsureStyles();
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
            Rect bar = ImguiScale.Fit(new Rect(24f, 24f, 520f, 120f));
            Fill(bar, new Color(0.07f, 0.09f, 0.12f, 0.92f));
            GUILayout.BeginArea(new Rect(bar.x + 12f, bar.y + 8f, bar.width - 24f, bar.height - 16f));
            GUILayout.Label(RunHud.FormatClock(state.Now) + "    HP " + state.Hp + "/" + state.MaxHp + "    Lv." + state.Level, titleStyle);
            GUILayout.Label("WASD 移动   Space 闪避   Esc 暂停", bodyStyle);
            GUILayout.Label("经验 " + state.Xp + "/" + state.XpToNext + "    击杀 " + state.Kills, bodyStyle);
            if (state.Phase == RunPhase.Playing && GUILayout.Button("重开", GUILayout.Width(72f), GUILayout.Height(22f)))
                loop.Restart();
            GUILayout.EndArea();
        }

        void DrawCards(RunState state)
        {
            const float panelW = 920f;
            const float cardW = 280f;
            const float gap = 24f;
            const float cardH = 280f;
            float x0 = (ImguiScale.Width - panelW) * 0.5f;
            float y = (ImguiScale.Height - cardH) * 0.5f + (1f - intro) * 28f;
            GUILayout.BeginArea(ImguiScale.Fit(new Rect(x0, y - 36f, panelW, 32f)));
            GUILayout.Label("选择共振强化    ·    待选择 " + state.PendingLevelUps, titleStyle);
            GUILayout.EndArea();
            var options = loop.Sim.CurrentOptions;
            if (options == null || options.Length < 3) return;
            for (int i = 0; i < 3; i++)
            {
                Rect card = ImguiScale.Fit(new Rect(x0 + 16f + i * (cardW + gap), y, cardW, cardH));
                Fill(card, new Color(0.07f, 0.09f, 0.12f, 0.97f));
                GUILayout.BeginArea(new Rect(card.x + 16f, card.y + 12f, card.width - 32f, card.height - 24f));
                Color stripe = i == 0 ? new Color(0.16f, 0.28f, 0.72f) : i == 1 ? new Color(1f, 0.27f, 0f) : new Color(0.93f, 0.74f, 0.32f);
                Color prev = GUI.color;
                GUI.color = stripe;
                GUILayout.Box("", GUILayout.Height(cardH * 0.18f));
                GUI.color = prev;
                UpgradeKind kind = options[i];
                GUI.enabled = kind != UpgradeKind.None && state.Phase == RunPhase.LevelUp;
                GUILayout.Space(14f);
                GUILayout.Label((i + 1) + ". " + UpgradeRules.DisplayName(kind), titleStyle);
                GUILayout.Label(Description(kind), bodyStyle);
                GUILayout.FlexibleSpace();
                bool chosen = GUILayout.Button("选择  [ " + (i + 1) + " ]", GUILayout.Height(36f));
                GUI.enabled = true;
                GUILayout.EndArea();
                if (chosen) { loop.Choose(i); return; }
            }
        }

        void DrawPause(RunState state)
        {
            Rect box = ImguiScale.Fit(new Rect((ImguiScale.Width - 360f) * 0.5f, 160f, 360f, 260f));
            GUILayout.BeginArea(box, GUI.skin.box);
            GUILayout.Label(state.PhaseBeforePause == RunPhase.LevelUp ? "已暂停 · 升级卡保留" : "已暂停");
            if (GUILayout.Button("继续", GUILayout.Height(32f))) loop.TogglePause();
            if (GUILayout.Button("重开", GUILayout.Height(32f))) loop.Restart();
            GUILayout.Label("音乐试听（重开恢复 A）", bodyStyle);
            if (GUILayout.Button("A · 拨弦 / 错位重音", GUILayout.Height(28f))) UseMusic(musicA);
            if (GUILayout.Button("B · 空间 / 电子脉冲", GUILayout.Height(28f))) UseMusic(musicB);
            if (state.PhaseBeforePause == RunPhase.Playing && state.Now < 119f && GUILayout.Button("预览精英波次", GUILayout.Height(28f)))
            {
                loop.Sim.DebugSetTime(119f);
                loop.TogglePause();
            }
            GUILayout.EndArea();
        }

        public void UseMusic(AudioClip clip)
        {
            if (clip == null || loop == null || loop.Sim.State.Phase != RunPhase.Paused) return;
            var audio = loop.GetComponent<AudioDirector>();
            audio.combatLoop = clip;
            audio.BeginFreshRun(); // Audio only; phase-aware restart leaves gameplay paused.
        }

        static string Description(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.Damage: return "增强每次自动攻击的伤害。";
                case UpgradeKind.FireRate: return "缩短攻击间隔，更频繁地释放脉冲。";
                case UpgradeKind.Pierce: return "让投射物穿过更多目标。";
                case UpgradeKind.MoveSpeed: return "提高移动速度，拉开与敌群的距离。";
                case UpgradeKind.MaxHp: return "提高生命上限，强化生存能力。";
                case UpgradeKind.PickupRange: return "扩大经验吸附范围。";
                case UpgradeKind.DodgeCooldown: return "缩短闪避冷却，更快恢复机动能力。";
                case UpgradeKind.Heal: return "恢复生命，继续战斗。";
                default: return "暂无可用强化。";
            }
        }

        void OnDestroy() { if (uiFont != null) Destroy(uiFont); }

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
