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
            bool ready = source != null && commands != null && !preview;
            RunContentView view = source != null ? source.ReadView() : null;

            Rect box = ImguiScale.Fit(new Rect(16f, 16f, 760f, 430f));
            GUILayout.BeginArea(box, GUI.skin.box);
            if (source == null || commands == null)
                GUILayout.Label("等待成长接线");
            else if (preview)
                GUILayout.Label("TEST_ONLY 排版预览    不是本局成长");
            else
                GUILayout.Label("局内成长");

            if (loop != null && loop.Sim != null)
            {
                var state = loop.Sim.State;
                GUILayout.Label(state.Phase + "    HP " + state.Hp + "/" + state.MaxHp + "    " + RunHud.FormatClock(state.Now));
            }

            if (view == null)
            {
                GUILayout.Label("武器  空    空    空    空");
                GUILayout.Label("被动  空    空    空    空    空    空");
                GUILayout.Label("重抽 —    放逐 —    跳过 —");
                bool previous = GUI.enabled;
                GUI.enabled = false;
                GUILayout.Button("选择");
                GUILayout.Button("重抽");
                GUILayout.Button("跳过");
                GUI.enabled = previous;
            }
            else
            {
                GUILayout.Label("武器  " + Slots(view.Weapons, 4));
                GUILayout.Label("被动  " + Slots(view.Passives, 6));
                DrawEvolutions(view);
                GUILayout.Label("重抽 " + view.Rerolls + "    放逐 " + view.Banishes + "    跳过 " + view.Skips);
                DrawChoices(view, commands, preview || ready);
            }
            if (!string.IsNullOrEmpty(lastResult)) GUILayout.Label(lastResult);
            GUILayout.EndArea();
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
                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(220f), GUILayout.Height(130f));
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
