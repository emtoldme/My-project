using System;
using System.Collections.Generic;
using Waffle.RunContent;

namespace Waffle
{
    public partial class RunSimulation
    {
        public RunGrowthState Growth { get; private set; }
        public GrowthStats ContentStats { get; private set; }
        public int ShieldCharges { get; private set; }
        float nextShieldAt = float.PositiveInfinity;
        int growthLevelRewards;
        readonly List<GrowthResult> growthResults = new List<GrowthResult>();
        public static readonly string[] RC01EnabledIds = {
            "W01","W02","W03","W04","P01","P02","P04","P06","P07","P09","P12","P13","P15","P18",
            "E01","E02","E03","D01","D02","D03","D04" };

        void InitGrowth()
        {
            Growth = new RunGrowthState(Config.seed, null, null, Config.runContentAllowList != null && Config.runContentAllowList.Length > 0 ? Config.runContentAllowList : RC01EnabledIds);
            ContentStats = GrowthStats.From(Growth);
            Combat?.EnableRunContent(Growth, Config.seed);
        }
        void PrepareGrowthRewards()
        {
            while (growthLevelRewards < State.PendingLevelUps)
            { Growth.EnqueueLevelUpReward(); growthLevelRewards++; }
        }
        void GrowthPanel()
        {
            PrepareGrowthRewards(); PanelsShown++;
            panelShownRealTime = realTime; chooseLocked = false;
        }
        void TickGrowth()
        {
            int rank = Growth.RankOf("P18");
            if (rank <= 0) return;
            if (State.Now >= nextShieldAt)
            {
                ShieldCharges = 1;
                nextShieldAt = State.Now + Growth.Catalog.Get("P18").RankAt(rank).Get("shieldEvery", 12);
                Emit(PresentationEventType.ShieldReady, PlayerX, PlayerZ, 1, 0);
                Combat?.QueueShieldPulse();
            }
        }
        public RunContentView ReadContentView()
        {
            if (Growth == null) return null;
            Growth.SetViewRunSeconds(State.Now);
            return Growth.ReadView();
        }
        public bool TryChooseContent(long token, string id) => GrowthCommand(token, id, 0);
        public bool TryRerollContent(long token) => GrowthCommand(token, null, 1);
        public bool TryBanishContent(long token, string id) => GrowthCommand(token, id, 2);
        public bool TrySkipContent(long token) => GrowthCommand(token, null, 3);
        bool GrowthCommand(long token, string id, int command)
        {
            if (Growth == null || State.Phase != RunPhase.LevelUp || lastChooseFrame == State.Frame
                || realTime - panelShownRealTime < Config.levelUpInputGuard) return false;
            bool ok = command == 0 ? Growth.TryChoose(token, id) : command == 1 ? Growth.TryReroll(token)
                : command == 2 ? Growth.TryBanish(token, id) : Growth.TrySkip(token);
            if (!ok) return false;
            lastChooseFrame = State.Frame;
            if (command == 1 || command == 2)
            {
                Presentation.Add(new PresentationEvent { Type = command == 1 ? PresentationEventType.GrowthReroll : PresentationEventType.GrowthBanish,
                    SourceId = id, Frame = State.Frame, Now = State.Now });
                return true;
            }
            growthResults.Clear(); Growth.DrainResults(growthResults);
            foreach (var result in growthResults)
            {
                if (result.Source == RewardSource.LevelUp) { State.PendingLevelUps--; growthLevelRewards--; }
                if (result.HealFraction > 0) HealContent(result.HealFraction);
                if (result.Kind == GrowthResultKind.Evolved)
                    Presentation.Add(new PresentationEvent { Type = PresentationEventType.Evolved, Frame = State.Frame, Now = State.Now, SourceId = result.Id, Slot = result.Slot, X = PlayerX, Z = PlayerZ });
            }
            int oldMax = State.MaxHp;
            ContentStats = GrowthStats.From(Growth);
            State.MaxHp = (int)Math.Round(Config.maxHp * ContentStats.MaxHpMul);
            State.Hp = Math.Min(State.MaxHp, State.Hp + Math.Max(0, State.MaxHp - oldMax));
            int shieldRank = Growth.RankOf("P18");
            if (shieldRank > 0 && float.IsPositiveInfinity(nextShieldAt))
                nextShieldAt = State.Now + Growth.Catalog.Get("P18").RankAt(shieldRank).Get("shieldEvery", 12);
            Combat?.RefreshRunContent();
            Presentation.Add(new PresentationEvent { Type = PresentationEventType.LevelUpChosen, Frame = State.Frame,
                Now = State.Now, SourceId = id, X = PlayerX, Z = PlayerZ });
            if (Growth.HasOffer)
            {
                PanelsShown++; panelShownRealTime = realTime;
                State.TryTransition(RunPhase.LevelUp, "下一项成长奖励");
            }
            else State.TryTransition(RunPhase.Playing, "成长选择完成");
            return true;
        }
        void HealContent(float fraction)
        { State.Hp = Math.Min(State.MaxHp, State.Hp + (int)Math.Round(State.MaxHp * fraction)); }
    }
}
