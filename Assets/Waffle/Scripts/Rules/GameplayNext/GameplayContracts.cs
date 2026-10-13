using System;
using System.Collections.Generic;
using Waffle.Combat;
using Waffle.RunContent;

namespace Waffle.GameplayNext
{
    public enum GameplayModuleKind { Encounters, Objectives, Finale }
    public enum RunEndingMode { SurvivalOnly, BossClear }
    public enum AttackShapeKind { Circle, Capsule, Annulus }
    public enum ObjectiveStatus { None, Available, Channeling, Completed, Expired }
    public enum FinaleResolution { None, BossDefeated, DeadlineReached }

    [Serializable]
    public sealed class GameplayNextSettings
    {
        public bool enabled;
        public RunEndingMode endingMode = RunEndingMode.SurvivalOnly;
        public int maxAttacks = 12, maxOrdinaryAttacks = 8, maxPendingSpawns = 8;
        public int maxObjectiveRewards = 2;
        public int bossOrdinaryCap = 16;
        public float maxMovementSpeed = 30f, maxAttackLifetime = 15f, maxWarningTime = 5f;
    }

    public readonly struct EnemyKey : IEquatable<EnemyKey>
    {
        public readonly int Serial, Generation;
        public EnemyKey(int serial, int generation) { Serial = serial; Generation = generation; }
        public bool Equals(EnemyKey other) => Serial == other.Serial && Generation == other.Generation;
        public override bool Equals(object obj) => obj is EnemyKey key && Equals(key);
        public override int GetHashCode() => unchecked(Serial * 397 ^ Generation);
    }

    public readonly struct GameplayRequestKey : IEquatable<GameplayRequestKey>
    {
        public readonly GameplayModuleKind Module;
        public readonly long Sequence;
        public GameplayRequestKey(GameplayModuleKind module, long sequence) { Module = module; Sequence = sequence; }
        public bool Equals(GameplayRequestKey other) => Module == other.Module && Sequence == other.Sequence;
        public override bool Equals(object obj) => obj is GameplayRequestKey key && Equals(key);
        public override int GetHashCode() => unchecked((int)Module * 397 ^ Sequence.GetHashCode());
    }

    public readonly struct GameplayEnemySnapshot
    {
        public readonly EnemyKey Key;
        public readonly string ArchetypeId;
        public readonly Vec2 Position, PreviousPosition;
        public readonly float Hp, MaxHp, Speed, Radius;
        public readonly bool IsAlive, IsElite, IsBoss;
        public readonly int RelocationVersion;
        public readonly GameplayRequestKey SpawnRequest;
        internal GameplayEnemySnapshot(EnemyState enemy)
        {
            Key = new EnemyKey(enemy.Serial, enemy.Generation); ArchetypeId = enemy.ArchetypeId;
            Position = enemy.Position; PreviousPosition = enemy.PrevPosition;
            Hp = enemy.Hp; MaxHp = enemy.MaxHp; Speed = enemy.Speed; Radius = enemy.Radius;
            IsAlive = enemy.IsAlive; IsElite = enemy.IsElite; IsBoss = enemy.IsBoss;
            RelocationVersion = enemy.RelocationVersion; SpawnRequest = enemy.SpawnRequest;
        }
    }

    public readonly struct GameplayContext
    {
        public readonly Guid RunId;
        public readonly int Seed, ModuleSeed;
        public readonly float Now, Dt, RunDuration, ArenaHalfSize, PlayerRadius, ViewRadius;
        public readonly Vec2 PlayerPreviousPosition, PlayerPosition;
        public readonly IReadOnlyList<GameplayEnemySnapshot> Enemies;
        public readonly IReadOnlyList<AttackSnapshot> ActiveAttacks;
        public readonly IReadOnlyList<SpawnWarningSnapshot> SpawnWarnings;
        internal GameplayContext(Guid runId, int seed, GameplayModuleKind kind, CombatInput input,
            CombatConfig config, IReadOnlyList<GameplayEnemySnapshot> enemies, RunGameplayView view)
        {
            RunId = runId; Seed = seed; ModuleSeed = unchecked(seed ^ ((int)kind + 1) * 0x2c9277b5);
            Now = input.RunTime; Dt = input.Dt; RunDuration = config.runDuration;
            ArenaHalfSize = config.arenaHalfSize; PlayerRadius = config.playerRadius; ViewRadius = input.ViewRadius;
            PlayerPreviousPosition = input.PlayerPreviousPos; PlayerPosition = input.PlayerPos; Enemies = enemies;
            ActiveAttacks = view.Attacks; SpawnWarnings = view.SpawnWarnings;
        }
    }

    public readonly struct EnemySpawnSpec
    {
        public readonly string ArchetypeId;
        public readonly float MaxHp, Speed, Radius;
        public readonly int ContactDamage, GemValue;
        public readonly bool IsElite, IsBoss;
        public EnemySpawnSpec(string archetypeId, float maxHp, float speed, float radius,
            int contactDamage, int gemValue, bool isElite = false, bool isBoss = false)
        { ArchetypeId = archetypeId; MaxHp = maxHp; Speed = speed; Radius = radius;
          ContactDamage = contactDamage; GemValue = gemValue; IsElite = isElite; IsBoss = isBoss; }
    }
    public readonly struct SpawnIntent
    {
        public readonly long Sequence;
        public readonly Vec2 Position;
        public readonly float WarningSeconds;
        public readonly EnemySpawnSpec Spec;
        public SpawnIntent(long sequence, Vec2 position, EnemySpawnSpec spec, float warningSeconds)
        { Sequence = sequence; Position = position; Spec = spec; WarningSeconds = warningSeconds; }
    }
    public readonly struct MovementIntent
    {
        public readonly EnemyKey Owner;
        public readonly Vec2 Velocity;
        public readonly bool UseSeparation, AllowRelocation;
        public MovementIntent(EnemyKey owner, Vec2 velocity, bool useSeparation = true, bool allowRelocation = true)
        { Owner = owner; Velocity = velocity; UseSeparation = useSeparation; AllowRelocation = allowRelocation; }
    }
    public readonly struct AttackShape
    {
        public readonly AttackShapeKind Kind;
        public readonly Vec2 Center, End;
        public readonly float Radius, InnerRadius, ExpansionSpeed;
        public AttackShape(AttackShapeKind kind, Vec2 center, float radius, Vec2 end = default,
            float innerRadius = 0, float expansionSpeed = 0)
        { Kind = kind; Center = center; Radius = radius; End = end; InnerRadius = innerRadius; ExpansionSpeed = expansionSpeed; }
    }
    public readonly struct HostileAttackIntent
    {
        public readonly long Sequence;
        public readonly EnemyKey Owner;
        public readonly AttackShape Shape;
        public readonly float ActivateAt, EndAt;
        public readonly int Damage;
        public readonly bool SweepOwnerMotion;
        public HostileAttackIntent(long sequence, EnemyKey owner, AttackShape shape, float activateAt,
            float endAt, int damage, bool sweepOwnerMotion = false)
        { Sequence = sequence; Owner = owner; Shape = shape; ActivateAt = activateAt; EndAt = endAt;
          Damage = damage; SweepOwnerMotion = sweepOwnerMotion; }
    }
    public readonly struct AttackSnapshot
    {
        public readonly GameplayRequestKey Key;
        public readonly HostileAttackIntent Attack;
        public readonly float CreatedAt;
        public readonly bool Spent;
        internal AttackSnapshot(GameplayRequestKey key, HostileAttackIntent attack, float createdAt, bool spent)
        { Key = key; Attack = attack; CreatedAt = createdAt; Spent = spent; }
    }
    public readonly struct SpawnWarningSnapshot
    {
        public readonly GameplayRequestKey Key;
        public readonly SpawnIntent Spawn;
        public readonly float SpawnAt;
        internal SpawnWarningSnapshot(GameplayRequestKey key, SpawnIntent spawn, float spawnAt)
        { Key = key; Spawn = spawn; SpawnAt = spawnAt; }
    }
    public readonly struct ObjectiveSnapshot
    {
        public readonly string Id, RewardId;
        public readonly Vec2 Position;
        public readonly float Radius, Progress, ExpiresAt;
        public readonly ObjectiveStatus Status;
        public ObjectiveSnapshot(string id, Vec2 position, float radius, ObjectiveStatus status,
            float progress, float expiresAt, string rewardId)
        { Id = id; Position = position; Radius = radius; Status = status; Progress = progress;
          ExpiresAt = expiresAt; RewardId = rewardId; }
    }
    public readonly struct ObjectiveRewardIntent
    {
        public readonly Guid RunId;
        public readonly string ObjectiveId, RewardId;
        public ObjectiveRewardIntent(Guid runId, string objectiveId, string rewardId)
        { RunId = runId; ObjectiveId = objectiveId; RewardId = rewardId; }
    }
    public readonly struct FinaleSnapshot
    {
        public readonly EnemyKey Boss;
        public readonly bool Spawned, Defeated;
        public readonly float Hp, MaxHp, Deadline;
        public readonly string AttackLabel;
        internal FinaleSnapshot(EnemyKey boss, bool spawned, bool defeated, float hp, float maxHp, float deadline, string label)
        { Boss = boss; Spawned = spawned; Defeated = defeated; Hp = hp; MaxHp = maxHp; Deadline = deadline; AttackLabel = label; }
    }

    public interface IGameplayModule { void Step(GameplayContext context, GameplayCommandBuffer commands); void Clear(); }
    public interface IEncounterModule : IGameplayModule { }
    public interface IObjectiveModule : IGameplayModule { void ConfirmReward(Guid runId, string objectiveId, bool granted); }
    public interface IFinaleModule : IGameplayModule { }
    public readonly struct GameplayModules
    {
        public readonly IEncounterModule Encounters;
        public readonly IObjectiveModule Objectives;
        public readonly IFinaleModule Finale;
        public GameplayModules(IEncounterModule encounters = null, IObjectiveModule objectives = null, IFinaleModule finale = null)
        { Encounters = encounters; Objectives = objectives; Finale = finale; }
    }
    // A scene adapter implements this interface and constructs fresh pure-rule instances per run.
    public interface IGameplayModuleProvider { GameplayModules CreateGameplayModules(); }
    public interface IGameplayViewSource { RunGameplayView ReadGameplayView(); RunOutcome ReadRunOutcome(); }

    public sealed class GameplayCommandBuffer
    {
        internal readonly List<SpawnIntent> Spawns = new List<SpawnIntent>(8);
        internal readonly List<MovementIntent> Movements = new List<MovementIntent>(64);
        internal readonly List<HostileAttackIntent> Attacks = new List<HostileAttackIntent>(12);
        internal ObjectiveRewardIntent? Reward;
        internal ObjectiveSnapshot Objective;
        internal string EncounterLabel, FinaleAttack;
        internal int EncounterPhase;
        internal FinaleResolution Resolution;
        readonly GameplayModuleKind kind;
        bool open;
        internal GameplayCommandBuffer(GameplayModuleKind kind) { this.kind = kind; }
        internal void Open()
        { Spawns.Clear(); Movements.Clear(); Attacks.Clear(); Reward = null; Objective = default;
          EncounterLabel = FinaleAttack = null; EncounterPhase = 0; Resolution = FinaleResolution.None; open = true; }
        internal void Close() { open = false; }
        public bool TrySpawn(SpawnIntent intent)
        { if (!open || kind == GameplayModuleKind.Objectives || Spawns.Count >= 8) return false; Spawns.Add(intent); return true; }
        public bool TryMove(MovementIntent intent)
        { if (!open || kind == GameplayModuleKind.Objectives || Movements.Count >= 64) return false; Movements.Add(intent); return true; }
        public bool TryAttack(HostileAttackIntent intent)
        { if (!open || kind == GameplayModuleKind.Objectives || Attacks.Count >= 12) return false; Attacks.Add(intent); return true; }
        public bool TryReward(ObjectiveRewardIntent intent)
        { if (!open || kind != GameplayModuleKind.Objectives || Reward.HasValue) return false; Reward = intent; return true; }
        public void PublishObjective(ObjectiveSnapshot objective) { if (open && kind == GameplayModuleKind.Objectives) Objective = objective; }
        public void PublishEncounter(int phase, string label) { if (open && kind == GameplayModuleKind.Encounters) { EncounterPhase = phase; EncounterLabel = label; } }
        public void PublishFinaleAttack(string label) { if (open && kind == GameplayModuleKind.Finale) FinaleAttack = label; }
        public void RequestFinaleResolution(FinaleResolution resolution) { if (open && kind == GameplayModuleKind.Finale) Resolution = resolution; }
    }

    /// <summary>Borrowed, read-only current frame. Copy values if keeping a historical frame.</summary>
    public sealed class RunGameplayView
    {
        public Guid RunId { get; internal set; }
        public float Now { get; internal set; }
        public RunPhase Phase { get; internal set; }
        public int EncounterPhase { get; internal set; }
        public string EncounterLabel { get; internal set; }
        public ObjectiveSnapshot Objective { get; internal set; }
        public int CompletedObjectives { get; internal set; }
        public FinaleSnapshot Finale { get; internal set; }
        public RunOutcome Outcome { get; internal set; }
        public IReadOnlyList<AttackSnapshot> Attacks { get; }
        public IReadOnlyList<SpawnWarningSnapshot> SpawnWarnings { get; }
        internal RunGameplayView(List<AttackSnapshot> attacks, List<SpawnWarningSnapshot> spawns)
        { Attacks = attacks.AsReadOnly(); SpawnWarnings = spawns.AsReadOnly(); }
    }
    public sealed class RunOutcome
    {
        public readonly Guid RunId;
        public readonly RunPhase Phase;
        public readonly string Reason;
        public readonly float Duration;
        public readonly int Kills, Level, CompletedObjectives;
        public readonly bool BossDefeated;
        public readonly IReadOnlyList<RunContentOwnedView> Weapons, Passives;
        internal RunOutcome(Guid runId, RunState state, RunGameplayView gameplay, RunContentView content)
        {
            RunId = runId; Phase = state.Phase; Reason = state.EndReason; Duration = state.Now;
            Kills = state.Kills; Level = state.Level; CompletedObjectives = gameplay?.CompletedObjectives ?? 0;
            BossDefeated = gameplay?.Finale.Defeated ?? false;
            Weapons = new List<RunContentOwnedView>(content?.Weapons ?? Array.Empty<RunContentOwnedView>()).AsReadOnly();
            Passives = new List<RunContentOwnedView>(content?.Passives ?? Array.Empty<RunContentOwnedView>()).AsReadOnly();
        }
    }
}
