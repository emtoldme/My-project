using UnityEngine;
using Waffle;
using Waffle.RunContent;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>
    /// 只读成长界面。没有宿主就显示等待并禁用按钮。
    /// 不写 timeScale、HP、XP，不监听 Esc，也不自己出牌。
    /// </summary>
    public class RC01GrowthHud : MonoBehaviour
    {
        public GameLoop loop;
        string lastResult = "";

        void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
        }

        void OnGUI()
        {
            ImguiScale.Apply();
            var source = GetComponent<IRunContentViewSource>();
            var commands = GetComponent<IRunContentCommands>();
            bool preview = source is RC01GrowthPreviewSource;
            var view = source?.ReadView();
            var sim = loop?.Sim;
            bool selecting = preview || sim?.State.Phase == RunPhase.LevelUp;
            bool wrap = GUI.skin.label.wordWrap;
            GUI.skin.label.wordWrap = true;
            if (sim != null)
            {
                var st = sim.State;
                GUILayout.BeginArea(ImguiScale.Fit(new Rect(16,16,360,selecting ? 108 : 148)), GUI.skin.box);
                GUILayout.Label("秋收之路 · 战斗草稿    " + RunHud.FormatClock(st.Now) + " / 10:00");
                GUILayout.Label("HP " + st.Hp + "/" + st.MaxHp + "    Lv " + st.Level + "    击杀 " + st.Kills);
                GUILayout.Label("经验 " + st.Xp + "/" + st.XpToNext + "    护盾 " + sim.ShieldCharges);
                if (!selecting)
                {
                    float cd = Mathf.Max(0,sim.DodgeReadyAt-st.Now);
                    GUILayout.Label(cd > 0 ? "闪避 " + cd.ToString("F1") + "s" : "闪避就绪 · Space");
                    GUILayout.Label("WASD移动 · 自动攻击 · Esc暂停 · F11全屏");
                }
                GUILayout.EndArea();
            }
            if (view != null)
            {
                GUILayout.BeginArea(ImguiScale.Fit(new Rect(16,ImguiScale.Height-110,ImguiScale.Width-32,94)),GUI.skin.box);
                GUILayout.Label("武器  " + Slots(view.Weapons,4));
                GUILayout.Label("被动  " + Slots(view.Passives,6));
                GUILayout.Label("测试拾取：红方块回复 · 蓝菱形磁吸 · 橙菱形清场 · 金箱精英奖励");
                GUILayout.EndArea();
            }
            if (selecting)
            {
                Rect panel = ImguiScale.Fit(new Rect((ImguiScale.Width-760)/2,(ImguiScale.Height-440)/2,760,440));
                GUILayout.BeginArea(panel,GUI.skin.box);
                GUILayout.Label(preview ? "TEST_ONLY 排版预览" : "选择成长 · 1 / 2 / 3 或点击卡片");
                if (view == null) GUILayout.Label("等待成长接线");
                else
                {
                    DrawEvolutions(view);
                    DrawChoices(view,commands,preview || sim?.State.Phase == RunPhase.LevelUp);
                }
                if (!string.IsNullOrEmpty(lastResult)) GUILayout.Label(lastResult);
                GUILayout.EndArea();
            }
            if (sim != null && (sim.State.Phase == RunPhase.Paused || sim.State.Phase.IsTerminal()))
            {
                GUILayout.BeginArea(ImguiScale.Fit(new Rect((ImguiScale.Width-380)/2,(ImguiScale.Height-200)/2,380,200)),GUI.skin.box);
                GUILayout.Label(sim.State.Phase == RunPhase.Paused ? "暂停" : sim.State.Phase == RunPhase.Victory ? "完成本局" : "本局结束");
                GUILayout.Label("时间 " + RunHud.FormatClock(sim.State.Now) + " · 等级 " + sim.State.Level + " · 击杀 " + sim.State.Kills);
                if (sim.State.Phase == RunPhase.Paused && GUILayout.Button("继续",GUILayout.Height(36))) loop.TogglePause();
                if (GUILayout.Button("重新开始",GUILayout.Height(36))) loop.Restart();
                GUILayout.EndArea();
            }
            GUI.skin.label.wordWrap = wrap;
        }

        void DrawEvolutions(RunContentView view)
        {
            if (view.Evolutions == null || view.Evolutions.Count == 0)
            {
                GUILayout.Label("进化  无");
                return;
            }
            for (int i = 0; i < view.Evolutions.Count; i++)
            {
                RunContentEvolutionView item = view.Evolutions[i];
                GUILayout.Label((item.IsEligible ? "可进化  " : "还缺  ") + item.DisplayName + "  " + item.RequirementText);
            }
        }

        void DrawChoices(RunContentView view, IRunContentCommands commands, bool allow)
        {
            bool previous = GUI.enabled;
            GUI.enabled = allow && commands != null;
            if (view.Choices == null || view.Choices.Count == 0)
                GUILayout.Label(allow ? "当前没有三选一" : "三选一等待接线");
            else
            {
                GUILayout.BeginHorizontal();
                for (int i = 0; i < view.Choices.Count && i < 3; i++)
                {
                    RunContentChoiceView card = view.Choices[i];
                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(220f), GUILayout.Height(205f));
                    GUILayout.Label(card.DisplayName);
                    GUILayout.Label(card.CurrentRank + " → " + card.NextRank);
                    GUILayout.Label(card.EffectText);
                    bool choose = GUILayout.Button("选择");
                    bool banish = GUILayout.Button("放逐");
                    if (choose) lastResult = commands.TryChoose(view.OfferToken, card.Id) ? "已选择 " + card.Id : "已拒绝";
                    if (banish) lastResult = commands.TryBanish(view.OfferToken, card.Id) ? "已放逐 " + card.Id : "已拒绝";
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            bool reroll = GUILayout.Button("重抽 " + view.Rerolls);
            bool skip = GUILayout.Button("跳过 " + view.Skips);
            if (reroll) lastResult = commands.TryReroll(view.OfferToken) ? "已请求重抽" : "已拒绝";
            if (skip) lastResult = commands.TrySkip(view.OfferToken) ? "已请求跳过" : "已拒绝";
            GUILayout.EndHorizontal();
            GUI.enabled = previous;
        }

        static string Slots(System.Collections.Generic.IReadOnlyList<RunContentOwnedView> items, int count)
        {
            var text = "";
            for (int i = 0; i < count; i++)
            {
                if (i > 0) text += "    ";
                if (items != null && i < items.Count && !string.IsNullOrEmpty(items[i].Id))
                    text += items[i].DisplayName + " " + items[i].Rank + "/" + items[i].MaxRank;
                else
                    text += "空";
            }
            return text;
        }
    }
}
