using System;
using System.Collections.Generic;

namespace Waffle.Homecoming
{
    /// <summary>
    /// HC-D: Pure rules implementation of IHomecomingGoals.
    /// Tracks three limited narrative homecoming goals:
    /// 1. goal.homeward: View old home (SiteDiscovered or NarrativeViewed) and claim sewing-tin (SiteClaimed).
    /// 2. goal.harvest-memory: View old silo and claim work-gloves, then equip it or confirm harvest footprint.
    /// 3. goal.unfinished-song: Claim old-guitar, equip it, and trigger relic resonance effect (SpecialEffectTriggered).
    /// Consumes only host-confirmed HomecomingFact records, deduplicates by EventId, rejects stale RunIds.
    /// Pure observer: computes completion status, does not grant items or XP.
    /// </summary>
    public sealed class HomecomingGoalsModule : IHomecomingGoals
    {
        public const string GoalHomeward = "goal.homeward";
        public const string GoalHarvestMemory = "goal.harvest-memory";
        public const string GoalUnfinishedSong = "goal.unfinished-song";

        private Guid _currentRunId = Guid.Empty;
        private readonly HashSet<string> _observedEventIds = new HashSet<string>(StringComparer.Ordinal);

        // State tracking for Goal 1: goal.homeward
        private bool _homewardHomeViewed;
        private bool _homewardTinClaimed;
        private bool _homewardCompleted;

        // State tracking for Goal 2: goal.harvest-memory
        private bool _harvestSiloViewed;
        private bool _harvestGlovesClaimed;
        private bool _harvestGlovesEquipped;
        private bool _harvestCompleted;

        // State tracking for Goal 3: goal.unfinished-song
        private bool _songGuitarClaimed;
        private bool _songGuitarEquipped;
        private bool _songEchoTriggered;
        private bool _songCompleted;

        public HomecomingGoalsModule()
        {
            Reset(Guid.Empty);
        }

        public void Reset(Guid runId)
        {
            _currentRunId = runId;
            _observedEventIds.Clear();

            _homewardHomeViewed = false;
            _homewardTinClaimed = false;
            _homewardCompleted = false;

            _harvestSiloViewed = false;
            _harvestGlovesClaimed = false;
            _harvestGlovesEquipped = false;
            _harvestCompleted = false;

            _songGuitarClaimed = false;
            _songGuitarEquipped = false;
            _songEchoTriggered = false;
            _songCompleted = false;
        }

        public void Observe(HomecomingFact fact)
        {
            // 1. RunId guard: reject facts with mismatched or empty RunId
            if (_currentRunId == Guid.Empty || fact.RunId != _currentRunId)
                return;

            // 2. EventId guard: deduplicate strictly by EventId
            if (string.IsNullOrWhiteSpace(fact.EventId) || fact.EventId.Length > 128 || _observedEventIds.Count >= 512 || !_observedEventIds.Add(fact.EventId))
                return;

            string subject = fact.SubjectId ?? string.Empty;

            // Process based on fact kind
            switch (fact.Kind)
            {
                case HomecomingEventKind.NarrativeViewed:
                    if (subject == "poi.old-home")
                        _homewardHomeViewed = true;
                    if (subject == "poi.old-silo")
                        _harvestSiloViewed = true;
                    break;

                case HomecomingEventKind.SiteClaimed:
                    if (subject == "poi.old-home" || subject == "item.sewing-tin")
                    {
                        _homewardTinClaimed = true;
                    }
                    if (subject == "poi.old-silo" || subject == "item.work-gloves")
                    {
                        _harvestGlovesClaimed = true;
                    }
                    if (subject == "poi.old-guitar" || subject == "item.old-guitar")
                    {
                        _songGuitarClaimed = true;
                    }
                    break;

                case HomecomingEventKind.ItemEquipped:
                    if (subject == "item.work-gloves")
                    {
                        _harvestGlovesEquipped = true;
                    }
                    if (subject == "item.old-guitar")
                    {
                        _songGuitarEquipped = true;
                    }
                    break;

                case HomecomingEventKind.SpecialEffectTriggered:
                    if (subject == "item.old-guitar" || subject == "item.old-guitar.echo")
                    {
                        _songEchoTriggered = true;
                    }
                    break;

                case HomecomingEventKind.RunEnded:
                    // Terminal event noted, goals retain status
                    break;
            }

            EvaluateCompletion();
        }

        private void EvaluateCompletion()
        {
            if (!_homewardCompleted && _homewardHomeViewed && _homewardTinClaimed)
                _homewardCompleted = true;

            // Goal 2: Silo viewed + gloves claimed + (gloves equipped or step confirmed)
            if (!_harvestCompleted && _harvestSiloViewed && _harvestGlovesClaimed && _harvestGlovesEquipped)
                _harvestCompleted = true;

            // Goal 3: Guitar claimed + equipped + relic resonance echo triggered
            if (!_songCompleted && _songGuitarClaimed && _songGuitarEquipped && _songEchoTriggered)
                _songCompleted = true;
        }

        public IReadOnlyList<GoalView> Read()
        {
            var list = new List<GoalView>(3);

            // Goal 1: Homeward
            string progress1;
            if (_homewardCompleted)
                progress1 = "已达成：在老屋窗台认出了针线盒";
            else if (_homewardTinClaimed)
                progress1 = "已拾取针线盒，待确认老屋回忆 (1/2)";
            else if (_homewardHomeViewed)
                progress1 = "已发现老房子，寻觅窗台针线盒 (1/2)";
            else
                progress1 = "尚未发现老屋 (0/2)";
            list.Add(new GoalView(GoalHomeward, GoalHomeward, progress1, _homewardCompleted));

            // Goal 2: Harvest Memory
            string progress2;
            if (_harvestCompleted)
                progress2 = "已达成：戴上粗布手套，认得田里每种分量";
            else if (_harvestGlovesClaimed)
                progress2 = "已取得工作手套，待装备入槽 (2/3)";
            else if (_harvestSiloViewed)
                progress2 = "已找到老筒仓，寻取粗布手套 (1/3)";
            else
                progress2 = "尚未抵达老筒仓 (0/3)";
            list.Add(new GoalView(GoalHarvestMemory, GoalHarvestMemory, progress2, _harvestCompleted));

            // Goal 3: Unfinished Song
            string progress3;
            if (_songCompleted)
                progress3 = "已达成：在断续的琴弦中接回了那个节拍";
            else if (_songGuitarEquipped)
                progress3 = "旧吉他已装备，闪避时触发一次残响 (2/3)";
            else if (_songGuitarClaimed)
                progress3 = "已拾起旧吉他，待装备于遗物槽 (1/3)";
            else
                progress3 = "寻觅旧秋千旁的残缺吉他 (0/3)";
            list.Add(new GoalView(GoalUnfinishedSong, GoalUnfinishedSong, progress3, _songCompleted));

            return list.AsReadOnly();
        }
    }
}
