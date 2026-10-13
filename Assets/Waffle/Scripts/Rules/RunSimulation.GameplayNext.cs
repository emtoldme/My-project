using System;
using System.Collections.Generic;
using Waffle.GameplayNext;

namespace Waffle
{
    public partial class RunSimulation
    {
        public Guid RunId { get; } = Guid.NewGuid();
        public RunOutcome Outcome { get; private set; }
        readonly HashSet<string> objectiveClaims = new HashSet<string>(StringComparer.Ordinal);
        bool gameplayTickStarted;

        public void InstallGameplayModules(GameplayModules modules)
        {
            if (gameplayTickStarted || Combat == null) throw new InvalidOperationException("Install gameplay before Tick, with a combat host.");
            Combat.InstallGameplay(RunId, Config.seed, Config.gameplayNext, modules);
        }
        public RunGameplayView ReadGameplayView()
        {
            var view = Combat?.GameplayView;
            if (view == null) return null;
            view.Now = State.Now; view.Phase = State.Phase; view.Outcome = Outcome;
            return view;
        }
        bool IsBossClearMode => Combat?.GameplaySettings?.endingMode == RunEndingMode.BossClear;

        void ApplyObjectiveReward()
        {
            var pending = Combat?.PendingObjectiveReward;
            if (!pending.HasValue) return;
            var request = pending.Value;
            var view = Combat.GameplayView;
            var objective = view.Objective;
            bool valid = State.Hp > 0 && request.RunId == RunId && !string.IsNullOrWhiteSpace(request.ObjectiveId)
                && request.ObjectiveId.Length <= 64 && request.ObjectiveId == objective.Id
                && request.RewardId == objective.RewardId && objective.Status == ObjectiveStatus.Completed && objective.Progress >= 1
                && (request.RewardId == "D02" || request.RewardId == "D03");
            bool granted = valid && objectiveClaims.Contains(request.ObjectiveId);
            if (valid && !granted && objectiveClaims.Count < Combat.GameplaySettings.maxObjectiveRewards)
            {
                objectiveClaims.Add(request.ObjectiveId); granted = true;
                int value;
                if (request.RewardId == "D02") { int before = State.Hp; HealContent(.2f); value = State.Hp - before; }
                else value = Combat.MagnetizeFreeGems();
                view.CompletedObjectives = objectiveClaims.Count;
                Presentation.Add(new PresentationEvent { Type = PresentationEventType.ObjectiveRewardGranted,
                    Frame = State.Frame, Now = State.Now, X = objective.Position.X, Z = objective.Position.Y,
                    SourceId = request.ObjectiveId, RootSourceId = request.RewardId, Value = value, Data = objectiveClaims.Count });
            }
            Combat.ObjectiveModule?.ConfirmReward(RunId, request.ObjectiveId, granted);
        }
        bool ResolveGameplayEnding(float now, float dt)
        {
            if (!IsBossClearMode) return false;
            var finale = Combat.GameplayView.Finale;
            if (!finale.Defeated && now + dt < Config.runDuration) return false;
            State.Now = Math.Min(Config.runDuration, now + dt);
            EnterTerminal(finale.Defeated ? RunPhase.Victory : RunPhase.Defeat,
                finale.Defeated ? "boss-defeated" : "boss-deadline");
            return true;
        }
        void CaptureRunOutcome()
        {
            if (Outcome == null) Outcome = new RunOutcome(RunId, State, Combat?.GameplayView, ReadContentView());
            Combat?.ClearGameplay();
        }
    }
}
