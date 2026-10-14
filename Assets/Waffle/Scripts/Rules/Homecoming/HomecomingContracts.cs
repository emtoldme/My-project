using System;
using System.Collections.Generic;
using System.Globalization;
using Waffle.Combat;

namespace Waffle.Homecoming
{
    // HC00 v1: pure contracts. The host remains the only writer of combat/run state.
    public static class HomecomingContract
    {
        public const int Version = 1;
        public const int StartingRelicSlots = 2;
        public const int MaximumRelicSlots = 4;

        public static string SiteId(int seed, int cellX, int cellZ, string templateId)
        {
            RequireId(templateId);
            return string.Format(CultureInfo.InvariantCulture, "hc1:{0}:{1}:{2}:{3}", seed, cellX, cellZ, templateId);
        }

        public static void RequireId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 64)
                throw new ArgumentException("Content IDs must contain 1..64 ASCII letters, digits, '.', '_' or '-'.", nameof(id));
            foreach (char c in id)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '.' && c != '_' && c != '-')
                    throw new ArgumentException("Invalid content ID.", nameof(id));
        }

        internal static IReadOnlyList<T> Copy<T>(IEnumerable<T> values)
            => new List<T>(values ?? Array.Empty<T>()).AsReadOnly();
    }

    public enum NarrativeKind { Place, Item, Goal, Upgrade, Combination }
    public enum PoiStatus { Unseen, Discovered, Available, Claimed }
    public enum RelicEffectKind { MaxHealth, MoveSpeed, PickupRadius, DodgeCooldown, DodgeEcho }
    public enum HomecomingEventKind { SiteDiscovered, SiteClaimed, ItemEquipped, SpecialEffectTriggered, RunEnded, NarrativeViewed }

    public sealed class NarrativeEntry
    {
        public string Id { get; }
        public string Title { get; }
        public string Body { get; }
        public NarrativeKind Kind { get; }
        public NarrativeEntry(string id, string title, string body, NarrativeKind kind)
        {
            HomecomingContract.RequireId(id);
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Every inspectable entry needs a title and narrative text.");
            Id = id; Title = title; Body = body; Kind = kind;
        }
    }

    public readonly struct ExplorationContext
    {
        public readonly Guid RunId;
        public readonly int WorldSeed;
        public readonly float Now, DeltaTime;
        public readonly Vec2 Player;
        public readonly RunPhase Phase;
        public readonly bool PlayerAlive;
        public bool CanInteract => RunId != Guid.Empty && Phase == RunPhase.Playing && PlayerAlive;
        public ExplorationContext(Guid runId, int worldSeed, float now, float deltaTime, Vec2 player, RunPhase phase, bool playerAlive)
        { RunId = runId; WorldSeed = worldSeed; Now = now; DeltaTime = deltaTime; Player = player; Phase = phase; PlayerAlive = playerAlive; }
    }

    public readonly struct PoiSite
    {
        public readonly string SiteId, TemplateId, NarrativeId;
        public readonly Vec2 Position;
        public readonly float InteractionRadius;
        public PoiSite(string siteId, string templateId, string narrativeId, Vec2 position, float interactionRadius)
        { SiteId = siteId; TemplateId = templateId; NarrativeId = narrativeId; Position = position; InteractionRadius = interactionRadius; }
    }

    public readonly struct PoiView
    {
        public readonly PoiSite Site;
        public readonly PoiStatus Status;
        public readonly string RewardDescription;
        public PoiView(PoiSite site, PoiStatus status, string rewardDescription)
        { Site = site; Status = status; RewardDescription = rewardDescription; }
    }

    // A proposes; B atomically accepts slot expansion + item, then the host confirms A.
    // ClaimId is the stable SiteId. RunId scopes all transient claim ledgers.
    public readonly struct PoiReward
    {
        public readonly Guid RunId;
        public readonly string ClaimId, ItemId;
        public readonly int AdditionalSlots;
        public PoiReward(Guid runId, string claimId, string itemId, int additionalSlots)
        { RunId = runId; ClaimId = claimId; ItemId = itemId; AdditionalSlots = additionalSlots; }
    }

    public interface IPoiModule
    {
        void Reset(Guid runId, int worldSeed);
        bool Register(PoiSite site);
        void Step(ExplorationContext context);
        IReadOnlyList<PoiView> ReadNearby();
        bool TryPrepareClaim(ExplorationContext context, string siteId, out PoiReward reward);
        void ConfirmClaim(Guid runId, string siteId, bool accepted);
    }

    public readonly struct RelicSlotView
    {
        public readonly int Slot;
        public readonly string ItemId, NarrativeId;
        public RelicSlotView(int slot, string itemId, string narrativeId)
        { Slot = slot; ItemId = itemId; NarrativeId = narrativeId; }
    }

    public sealed class RelicInventoryView
    {
        public int Capacity { get; }
        public IReadOnlyList<RelicSlotView> Slots { get; }
        public IReadOnlyList<string> StoredItems { get; }
        public RelicInventoryView(int capacity, IEnumerable<RelicSlotView> slots, IEnumerable<string> storedItems)
        { Capacity = capacity; Slots = HomecomingContract.Copy(slots); StoredItems = HomecomingContract.Copy(storedItems); }
    }

    // Additive fractions (0.05 = +5%); cooldown reduction is positive. Host clamps totals.
    public readonly struct HomecomingModifiers
    {
        public readonly float MaxHealth, MoveSpeed, PickupRadius, DodgeCooldownReduction;
        public readonly bool DodgeEcho;
        public HomecomingModifiers(float maxHealth = 0, float moveSpeed = 0, float pickupRadius = 0,
            float dodgeCooldownReduction = 0, bool dodgeEcho = false)
        { MaxHealth = maxHealth; MoveSpeed = moveSpeed; PickupRadius = pickupRadius; DodgeCooldownReduction = dodgeCooldownReduction; DodgeEcho = dodgeEcho; }
    }

    public interface IRelicInventory
    {
        void Reset(Guid runId, int startingSlots);
        bool TryGrant(PoiReward reward);
        bool TryEquip(Guid runId, string itemId, int slot);
        bool TryUnequip(Guid runId, int slot);
        RelicInventoryView Read();
        HomecomingModifiers ReadModifiers();
    }

    public readonly struct HomecomingFact
    {
        public readonly Guid RunId;
        public readonly string EventId, SubjectId;
        public readonly HomecomingEventKind Kind;
        public HomecomingFact(Guid runId, string eventId, HomecomingEventKind kind, string subjectId)
        { RunId = runId; EventId = eventId; Kind = kind; SubjectId = subjectId; }
    }

    public readonly struct GoalView
    {
        public readonly string Id, NarrativeId, ProgressText;
        public readonly bool Completed;
        public GoalView(string id, string narrativeId, string progressText, bool completed)
        { Id = id; NarrativeId = narrativeId; ProgressText = progressText; Completed = completed; }
    }

    public interface IHomecomingGoals
    {
        void Reset(Guid runId);
        void Observe(HomecomingFact fact);
        IReadOnlyList<GoalView> Read();
    }

    // Constructed by the host from terminal RunOutcome + confirmed run ledgers only.
    public sealed class ExplorationSettlement
    {
        public Guid RunId { get; }
        public RunPhase Phase { get; }
        public float Duration { get; }
        public bool BossDefeated { get; }
        public IReadOnlyList<string> DiscoveredNarrativeIds { get; }
        public IReadOnlyList<string> ClaimedTemplateIds { get; }
        public IReadOnlyList<string> CompletedGoalIds { get; }
        public ExplorationSettlement(Guid runId, RunPhase phase, float duration, bool bossDefeated,
            IEnumerable<string> narratives, IEnumerable<string> claimedTemplates, IEnumerable<string> goals)
        {
            RunId = runId; Phase = phase; Duration = duration; BossDefeated = bossDefeated;
            DiscoveredNarrativeIds = HomecomingContract.Copy(narratives);
            ClaimedTemplateIds = HomecomingContract.Copy(claimedTemplates);
            CompletedGoalIds = HomecomingContract.Copy(goals);
        }
    }

    public readonly struct MetaUpgradeView
    {
        public readonly string Id, NarrativeId;
        public readonly int Rank, MaxRank, NextCost;
        public MetaUpgradeView(string id, string narrativeId, int rank, int maxRank, int nextCost)
        { Id = id; NarrativeId = narrativeId; Rank = rank; MaxRank = maxRank; NextCost = nextCost; }
    }

    public sealed class HomecomingProfileView
    {
        public long TotalExperience { get; }
        public int Level { get; }
        public int AvailablePoints { get; }
        public IReadOnlyList<MetaUpgradeView> Upgrades { get; }
        public IReadOnlyList<string> DiscoveredNarrativeIds { get; }
        public HomecomingProfileView(long xp, int level, int points, IEnumerable<MetaUpgradeView> upgrades, IEnumerable<string> narratives)
        { TotalExperience = xp; Level = level; AvailablePoints = points; Upgrades = HomecomingContract.Copy(upgrades); DiscoveredNarrativeIds = HomecomingContract.Copy(narratives); }
    }

    public readonly struct SettlementReceipt
    {
        public readonly Guid RunId;
        public readonly int ExperienceGained, PointsGained;
        public readonly bool AlreadySettled;
        public SettlementReceipt(Guid runId, int xp, int points, bool alreadySettled)
        { RunId = runId; ExperienceGained = xp; PointsGained = points; AlreadySettled = alreadySettled; }
    }

    // C supplies persistence and transactional publication. Failed saves do not commit spending/rewards.
    public interface IHomecomingProfile
    {
        HomecomingProfileView Read();
        HomecomingModifiers ReadStartingModifiers();
        bool TrySettle(ExplorationSettlement settlement, out SettlementReceipt receipt);
        bool TryPurchase(string upgradeId);
    }

    // Implemented by the host after integration; presentation never edits modules or stat fields.
    public interface IHomecomingViewSource
    {
        IReadOnlyList<PoiView> ReadNearby();
        RelicInventoryView ReadRelics();
        IReadOnlyList<GoalView> ReadGoals();
        HomecomingProfileView ReadProfile();
        NarrativeEntry ReadNarrative(string id);
    }
    public interface IHomecomingCommands
    {
        bool TryInteract(Guid runId, string siteId);
        bool TryEquip(Guid runId, string itemId, int slot);
        bool TryUnequip(Guid runId, int slot);
        bool TryPurchaseUpgrade(string upgradeId);
        bool TryOpenJournal();
        void CloseJournal();
    }
}
