using System.Collections.Generic;
using UnityEngine;
using Waffle.RunContent;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>
    /// 只给独立排版场景用。不生成下一手牌，不改 HP / XP / timeScale。
    /// </summary>
    public class RC01GrowthPreviewSource : MonoBehaviour, IRunContentViewSource, IRunContentCommands
    {
        public bool previewEnabled;
        readonly List<RunContentOwnedView> weapons = new List<RunContentOwnedView>();
        readonly List<RunContentOwnedView> passives = new List<RunContentOwnedView>();
        readonly List<RunContentChoiceView> choices = new List<RunContentChoiceView>();
        RunContentEvolutionView[] evolutions = System.Array.Empty<RunContentEvolutionView>();
        int rerolls = 3;
        int banishes = 2;
        int skips = 2;
        long token = 1;
        RunContentView cached;
        bool dirty = true;

        void Awake()
        {
            if (!previewEnabled) return;
            weapons.Add(new RunContentOwnedView("W01", "定向脉冲", 2, 5));
            weapons.Add(new RunContentOwnedView("W02", "扇形散射", 1, 5));
            passives.Add(new RunContentOwnedView("P06", "轻步", 1, 3));
            passives.Add(new RunContentOwnedView("P12", "回声模块", 1, 3));
            passives.Add(new RunContentOwnedView("P01", "输出增幅", 1, 3));
            choices.Add(new RunContentChoiceView("W03", "环绕护卫", "新武器", 0, 1));
            choices.Add(new RunContentChoiceView("P07", "生命储备", "被动 1→2", 1, 2));
            choices.Add(new RunContentChoiceView("W01", "定向脉冲", "武器 2→3", 2, 3));
            evolutions = new[]
            {
                new RunContentEvolutionView("E01", "W01", "延迟重奏", "要 P12 至少 2 阶，且局内至少 4 分钟", false)
            };
        }

        public RunContentView ReadView()
        {
            if (!previewEnabled) return null;
            if (dirty || cached == null)
            {
                cached = new RunContentView(token, rerolls, banishes, skips,
                    choices.ToArray(), weapons.ToArray(), passives.ToArray(), evolutions);
                dirty = false;
            }
            return cached;
        }

        public bool TryChoose(long offerToken, string definitionId)
        {
            if (!Accept(offerToken) || string.IsNullOrEmpty(definitionId)) return false;
            int index = FindChoice(definitionId);
            if (index < 0) return false;
            RunContentChoiceView card = choices[index];
            Upsert(card.Id.StartsWith("P") ? passives : weapons, card.Id, card.DisplayName, card.NextRank, card.Id.StartsWith("P") ? 3 : 5);
            choices.Clear();
            Touch();
            return true;
        }

        public bool TryReroll(long offerToken)
        {
            if (!Accept(offerToken) || rerolls <= 0 || choices.Count == 0) return false;
            rerolls--;
            Touch();
            return true;
        }

        public bool TryBanish(long offerToken, string definitionId)
        {
            if (!Accept(offerToken) || banishes <= 0) return false;
            int index = FindChoice(definitionId);
            if (index < 0) return false;
            banishes--;
            choices.RemoveAt(index);
            Touch();
            return true;
        }

        public bool TrySkip(long offerToken)
        {
            if (!Accept(offerToken) || skips <= 0 || choices.Count == 0) return false;
            skips--;
            choices.Clear();
            Touch();
            return true;
        }

        bool Accept(long offerToken)
        {
            return previewEnabled && offerToken == token;
        }

        void Touch()
        {
            token++;
            dirty = true;
        }

        int FindChoice(string id)
        {
            for (int i = 0; i < choices.Count; i++)
                if (choices[i].Id == id) return i;
            return -1;
        }

        static void Upsert(List<RunContentOwnedView> list, string id, string name, int rank, int maxRank)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Id != id) continue;
                list[i] = new RunContentOwnedView(id, name, rank, maxRank);
                return;
            }
            if (list.Count < (maxRank == 5 ? 4 : 6)) list.Add(new RunContentOwnedView(id, name, rank, maxRank));
        }
    }
}
