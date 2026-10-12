using System;
using System.Collections.Generic;

namespace Waffle.RunContent
{
    // RC01 shared view contract. Codex owns this file; gameplay state stays in the rules host.
    public readonly struct RunContentChoiceView
    {
        public readonly string Id, DisplayName, EffectText;
        public readonly int CurrentRank, NextRank;
        public RunContentChoiceView(string id, string displayName, string effectText, int currentRank, int nextRank)
        { Id = id; DisplayName = displayName; EffectText = effectText; CurrentRank = currentRank; NextRank = nextRank; }
    }

    public readonly struct RunContentOwnedView
    {
        public readonly string Id, DisplayName;
        public readonly int Rank, MaxRank;
        public RunContentOwnedView(string id, string displayName, int rank, int maxRank)
        { Id = id; DisplayName = displayName; Rank = rank; MaxRank = maxRank; }
    }

    public sealed class RunContentView
    {
        public readonly long OfferToken;
        public readonly int Rerolls, Banishes, Skips;
        public readonly IReadOnlyList<RunContentChoiceView> Choices;
        public readonly IReadOnlyList<RunContentOwnedView> Weapons, Passives;
        public RunContentView(long offerToken, int rerolls, int banishes, int skips,
            RunContentChoiceView[] choices, RunContentOwnedView[] weapons, RunContentOwnedView[] passives)
        {
            OfferToken = offerToken; Rerolls = rerolls; Banishes = banishes; Skips = skips;
            Choices = Array.AsReadOnly((RunContentChoiceView[])(choices ?? Array.Empty<RunContentChoiceView>()).Clone());
            Weapons = Array.AsReadOnly((RunContentOwnedView[])(weapons ?? Array.Empty<RunContentOwnedView>()).Clone());
            Passives = Array.AsReadOnly((RunContentOwnedView[])(passives ?? Array.Empty<RunContentOwnedView>()).Clone());
        }
    }

    public interface IRunContentViewSource { RunContentView ReadView(); }
    public interface IRunContentCommands
    {
        bool TryChoose(long offerToken, string definitionId);
        bool TryReroll(long offerToken);
        bool TryBanish(long offerToken, string definitionId);
        bool TrySkip(long offerToken);
    }
}
