using System;
using System.Collections.Generic;
using Waffle.GameplayNext;

namespace Waffle.Combat
{
    public sealed partial class CombatModule
    {
        GameplayNextSettings gameplaySettings;
        GameplayModules gameplayModules;
        IGameplayModule[] gameplaySlots;
        GameplayCommandBuffer[] gameplayBuffers;
        Guid gameplayRunId;
        int gameplaySeed;
        bool gameplayStopped, bossSpawned, bossDefeated;
        EnemyKey bossKey;
        float bossMaxHp;
        readonly long[] lastSpawnSequence = new long[3], lastAttackSequence = new long[3];
        readonly List<GameplayEnemySnapshot> gameplayEnemies = new List<GameplayEnemySnapshot>(64);
        IReadOnlyList<GameplayEnemySnapshot> gameplayEnemyView;
        readonly List<SpawnWarningSnapshot> pendingSpawns = new List<SpawnWarningSnapshot>(8);
        readonly List<LiveAttack> liveAttacks = new List<LiveAttack>(12);
        readonly List<AttackSnapshot> attackViews = new List<AttackSnapshot>(12);
        readonly List<SpawnWarningSnapshot> spawnViews = new List<SpawnWarningSnapshot>(8);
        readonly Dictionary<EnemyKey, MovementIntent> movements = new Dictionary<EnemyKey, MovementIntent>();
        readonly float[] ringScratch = new float[8];
        internal RunGameplayView GameplayView { get; private set; }
        internal GameplayNextSettings GameplaySettings => gameplaySettings;
        internal IObjectiveModule ObjectiveModule => gameplayModules.Objectives;
        internal ObjectiveRewardIntent? PendingObjectiveReward => gameplayBuffers == null || gameplayStopped ? null : gameplayBuffers[1].Reward;
        sealed class LiveAttack
        {
            public GameplayRequestKey Key;
            public HostileAttackIntent Intent;
            public float CreatedAt;
            public bool Spent;
        }

        internal void InstallGameplay(Guid runId, int seed, GameplayNextSettings settings, GameplayModules modules)
        {
            if (hasStepped || GameplayView != null) throw new InvalidOperationException("Install gameplay modules once, before the first combat tick.");
            if (settings == null || !settings.enabled) throw new InvalidOperationException("Enable gameplayNext before installing modules.");
            if (settings.endingMode == RunEndingMode.BossClear && modules.Finale == null) throw new ArgumentException("BossClear requires a finale module.");
            if (ReferenceEquals(modules.Encounters, modules.Objectives) && modules.Encounters != null
                || ReferenceEquals(modules.Encounters, modules.Finale) && modules.Encounters != null
                || ReferenceEquals(modules.Objectives, modules.Finale) && modules.Objectives != null)
                throw new ArgumentException("Each module slot needs its own instance.");
            gameplaySettings = new GameplayNextSettings {
                enabled = true, endingMode = settings.endingMode,
                maxAttacks = Math.Max(1, Math.Min(12, settings.maxAttacks)),
                maxOrdinaryAttacks = Math.Max(0, Math.Min(8, settings.maxOrdinaryAttacks)),
                maxPendingSpawns = Math.Max(1, Math.Min(8, settings.maxPendingSpawns)),
                maxObjectiveRewards = Math.Max(0, Math.Min(2, settings.maxObjectiveRewards)),
                bossOrdinaryCap = Math.Max(0, Math.Min(48, settings.bossOrdinaryCap)),
                maxMovementSpeed = ClampSetting(settings.maxMovementSpeed, 1, 30),
                maxAttackLifetime = ClampSetting(settings.maxAttackLifetime, .1f, 15),
                maxWarningTime = ClampSetting(settings.maxWarningTime, 0, 5)
            };
            gameplayModules = modules; gameplayRunId = runId; gameplaySeed = seed;
            gameplaySlots = new IGameplayModule[] { modules.Encounters, modules.Objectives, modules.Finale };
            gameplayBuffers = new[] { new GameplayCommandBuffer(GameplayModuleKind.Encounters), new GameplayCommandBuffer(GameplayModuleKind.Objectives), new GameplayCommandBuffer(GameplayModuleKind.Finale) };
            gameplayEnemyView = gameplayEnemies.AsReadOnly();
            GameplayView = new RunGameplayView(attackViews, spawnViews) { RunId = runId, Phase = RunPhase.Playing };
        }
        static float ClampSetting(float value, float min, float max) => GameplayGeometry.Finite(value) ? Math.Max(min, Math.Min(max, value)) : min;

        void StepGameplayModules(CombatInput input)
        {
            if (GameplayView == null || gameplayStopped || input.Dt <= 0) return;
            gameplayEnemies.Clear();
            foreach (var enemy in enemies) gameplayEnemies.Add(new GameplayEnemySnapshot(enemy));
            movements.Clear();
            for (int slot = 0; slot < gameplaySlots.Length; slot++)
            {
                var buffer = gameplayBuffers[slot]; buffer.Open();
                try { gameplaySlots[slot]?.Step(new GameplayContext(gameplayRunId, gameplaySeed, (GameplayModuleKind)slot, input, Config, gameplayEnemyView, GameplayView), buffer); }
                finally { buffer.Close(); }
                foreach (var spawn in buffer.Spawns) AcceptSpawn(slot, spawn);
                foreach (var move in buffer.Movements)
                {
                    if (!TryGetEnemy(move.Owner.Serial, move.Owner.Generation, out var enemy) || !OwnsEnemy(slot, enemy)
                        || !GameplayGeometry.Finite(move.Velocity) || move.Velocity.Length > gameplaySettings.maxMovementSpeed) continue;
                    movements[move.Owner] = move;
                }
                foreach (var attack in buffer.Attacks) AcceptAttack(slot, attack);
            }
            GameplayView.EncounterPhase = gameplayBuffers[0].EncounterPhase;
            GameplayView.EncounterLabel = gameplayBuffers[0].EncounterLabel;
            var objective = gameplayBuffers[1].Objective;
            GameplayView.Objective = ValidObjective(objective) ? objective : default;
            for (int i = pendingSpawns.Count - 1; i >= 0; i--)
            {
                var warning = pendingSpawns[i]; if (warning.SpawnAt > now + 1e-6f) continue;
                pendingSpawns.RemoveAt(i);
                SpawnGameplayEnemy(warning);
            }
        }
        bool OwnsEnemy(int slot, EnemyState enemy) => slot == 0 && !enemy.IsBoss || slot == 2 && enemy.IsBoss;
        static bool TextId(string id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 64;
        static bool ValidObjective(ObjectiveSnapshot value)
        {
            return TextId(value.Id) && Enum.IsDefined(typeof(ObjectiveStatus), value.Status)
                && GameplayGeometry.Finite(value.Position) && GameplayGeometry.Finite(value.Radius) && value.Radius > 0
                && GameplayGeometry.Finite(value.Progress) && value.Progress >= 0 && value.Progress <= 1
                && GameplayGeometry.Finite(value.ExpiresAt);
        }
        void AcceptSpawn(int slot, SpawnIntent request)
        {
            var spec = request.Spec;
            if (slot == 1 || request.Sequence <= lastSpawnSequence[slot] || pendingSpawns.Count >= gameplaySettings.maxPendingSpawns
                || !TextId(spec.ArchetypeId) || !GameplayGeometry.Finite(request.Position)
                || !GameplayGeometry.Finite(request.WarningSeconds) || request.WarningSeconds < 0 || request.WarningSeconds > gameplaySettings.maxWarningTime
                || !GameplayGeometry.Finite(spec.MaxHp) || spec.MaxHp <= 0 || spec.MaxHp > 1000000
                || !GameplayGeometry.Finite(spec.Speed) || spec.Speed < 0 || spec.Speed > gameplaySettings.maxMovementSpeed
                || !GameplayGeometry.Finite(spec.Radius) || spec.Radius <= 0 || spec.Radius > 5
                || spec.ContactDamage < 0 || spec.ContactDamage > 10000 || spec.GemValue < 0 || spec.GemValue > 10000
                || spec.IsBoss != (slot == 2) || spec.IsBoss && (bossSpawned || HasPendingBoss())
                || Math.Abs(request.Position.X) > Config.arenaHalfSize - spec.Radius
                || Math.Abs(request.Position.Y) > Config.arenaHalfSize - spec.Radius) return;
            lastSpawnSequence[slot] = request.Sequence;
            pendingSpawns.Add(new SpawnWarningSnapshot(new GameplayRequestKey((GameplayModuleKind)slot, request.Sequence), request, now + request.WarningSeconds));
        }
        bool HasPendingBoss() { foreach (var item in pendingSpawns) if (item.Spawn.Spec.IsBoss) return true; return false; }
        void SpawnGameplayEnemy(SpawnWarningSnapshot warning)
        {
            var spec = warning.Spawn.Spec;
            bool bossActive = bossSpawned && !bossDefeated;
            int cap = spec.IsElite ? Config.aliveCap + Config.eventOverflow : Config.aliveCap;
            if (bossActive) cap = Math.Min(cap, gameplaySettings.bossOrdinaryCap);
            int ordinaryAlive = 0; foreach (var existing in enemies) if (existing.IsAlive && !existing.IsBoss) ordinaryAlive++;
            if (spec.IsBoss && bossSpawned || !spec.IsBoss && (ordinaryAlive >= cap || bossActive && spec.IsElite)) return;
            // Skip a now-occupied birth point instead of appearing directly on the player.
            if (Vec2.Distance(warning.Spawn.Position, player) < spec.Radius + Config.playerRadius + .5f) return;
            var enemy = AcquireEnemy(warning.Spawn.Position, Config.PhaseAt(now), spec.IsElite, 10);
            if (enemy == null) return;
            enemy.ArchetypeId = spec.ArchetypeId; enemy.SpawnRequest = warning.Key; enemy.IsBoss = spec.IsBoss;
            enemy.Hp = enemy.MaxHp = spec.MaxHp; enemy.Speed = spec.Speed; enemy.Radius = spec.Radius;
            enemy.ContactDamage = spec.ContactDamage; enemy.GemValue = spec.GemValue;
            if (spec.IsBoss)
            {
                bossSpawned = true; bossKey = new EnemyKey(enemy.Serial, enemy.Generation); bossMaxHp = enemy.MaxHp;
                Result.Events.Add(new PresentationEvent { Type = PresentationEventType.BossSpawned, Now = now,
                    X = enemy.Position.X, Z = enemy.Position.Y, Data = enemy.Serial, Generation = enemy.Generation, SourceId = spec.ArchetypeId });
            }
        }
        void AcceptAttack(int slot, HostileAttackIntent request)
        {
            if (request.Sequence <= lastAttackSequence[slot] || liveAttacks.Count >= gameplaySettings.maxAttacks
                || !TryGetEnemy(request.Owner.Serial, request.Owner.Generation, out var owner) || !OwnsEnemy(slot, owner)
                || !GameplayGeometry.Valid(request.Shape) || request.Damage <= 0 || request.Damage > 10000
                || !GameplayGeometry.Finite(request.ActivateAt) || !GameplayGeometry.Finite(request.EndAt)
                || request.ActivateAt < now - 1e-6f || request.ActivateAt - now > gameplaySettings.maxWarningTime
                || request.EndAt <= request.ActivateAt || request.EndAt - request.ActivateAt > gameplaySettings.maxAttackLifetime
                || request.SweepOwnerMotion && request.Shape.Kind != AttackShapeKind.Capsule) return;
            int ordinary = 0, ownerShapes = 0;
            foreach (var attack in liveAttacks)
            {
                if (attack.Intent.Owner.Equals(request.Owner))
                {
                    ownerShapes++;
                    // A boss cast can contain three locked circles; do not overlap different patterns.
                    if (!owner.IsBoss || attack.Intent.Shape.Kind != request.Shape.Kind) return;
                }
                if (attack.Key.Module == GameplayModuleKind.Encounters) ordinary++;
            }
            if (ownerShapes >= (request.Shape.Kind == AttackShapeKind.Circle && owner.IsBoss ? 3 : 1)) return;
            if (slot == 0 && ordinary >= gameplaySettings.maxOrdinaryAttacks) return;
            lastAttackSequence[slot] = request.Sequence;
            liveAttacks.Add(new LiveAttack { Key = new GameplayRequestKey((GameplayModuleKind)slot, request.Sequence), Intent = request, CreatedAt = now });
        }

        void ResolveGameplayAttacks(CombatInput input)
        {
            if (GameplayView == null || gameplayStopped) return;
            for (int i = liveAttacks.Count - 1; i >= 0; i--)
            {
                var live = liveAttacks[i]; var attack = live.Intent;
                if (!TryGetEnemy(attack.Owner.Serial, attack.Owner.Generation, out var owner) || now >= attack.EndAt)
                { liveAttacks.RemoveAt(i); continue; }
                float fromTime = Math.Max(now, attack.ActivateAt), toTime = Math.Min(now + input.Dt, attack.EndAt);
                if (live.Spent || input.Dt <= 0 || toTime <= fromTime) continue;
                float a = (fromTime - now) / input.Dt, b = (toTime - now) / input.Dt;
                Vec2 playerFrom = input.PlayerPreviousPos + (input.PlayerPos - input.PlayerPreviousPos) * a;
                Vec2 playerTo = input.PlayerPreviousPos + (input.PlayerPos - input.PlayerPreviousPos) * b;
                bool hit;
                if (attack.SweepOwnerMotion)
                {
                    Vec2 ownerFrom = owner.PrevPosition + (owner.Position - owner.PrevPosition) * a;
                    Vec2 ownerTo = owner.PrevPosition + (owner.Position - owner.PrevPosition) * b;
                    hit = SweptEntry(ownerFrom - playerFrom, ownerTo - playerTo, attack.Shape.Radius + Config.playerRadius, out _);
                }
                else hit = GameplayGeometry.Hits(attack.Shape, playerFrom, playerTo, Config.playerRadius,
                    fromTime - attack.ActivateAt, toTime - attack.ActivateAt, ringScratch);
                if (!hit) continue;
                live.Spent = true; // A dodged/shielded candidate is still consumed for this attack.
                Result.ContactDamageSum = (int)Math.Min(int.MaxValue, (long)Result.ContactDamageSum + attack.Damage);
                if (attack.Damage > Result.ContactDamage) { Result.ContactDamage = attack.Damage; Result.ContactSourceSerial = owner.Serial; }
            }
            CaptureGameplayView(now + input.Dt);
        }

        void CaptureGameplayView(float time)
        {
            for (int i = liveAttacks.Count - 1; i >= 0; i--)
                if (!TryGetEnemy(liveAttacks[i].Intent.Owner.Serial, liveAttacks[i].Intent.Owner.Generation, out _)) liveAttacks.RemoveAt(i);
            attackViews.Clear(); spawnViews.Clear();
            foreach (var live in liveAttacks)
            {
                var intent = live.Intent;
                if (intent.SweepOwnerMotion && now >= intent.ActivateAt && TryGetEnemy(intent.Owner.Serial, intent.Owner.Generation, out var owner))
                    intent = new HostileAttackIntent(intent.Sequence, intent.Owner,
                        new AttackShape(AttackShapeKind.Capsule, owner.PrevPosition, intent.Shape.Radius, owner.Position),
                        intent.ActivateAt, intent.EndAt, intent.Damage, true);
                attackViews.Add(new AttackSnapshot(live.Key, intent, live.CreatedAt, live.Spent));
            }
            spawnViews.AddRange(pendingSpawns);
            float hp = 0;
            foreach (var enemy in enemies) if (enemy.Serial == bossKey.Serial && enemy.Generation == bossKey.Generation)
            {
                hp = enemy.Hp;
                if (bossSpawned && !enemy.IsAlive && !bossDefeated)
                {
                    bossDefeated = true;
                    Result.Events.Add(new PresentationEvent { Type = PresentationEventType.BossDefeated, Now = now,
                        X = enemy.Position.X, Z = enemy.Position.Y, Data = enemy.Serial, Generation = enemy.Generation, SourceId = enemy.ArchetypeId });
                }
                break;
            }
            GameplayView.Now = time;
            GameplayView.Finale = new FinaleSnapshot(bossKey, bossSpawned, bossDefeated, hp, bossMaxHp, Config.runDuration, gameplayBuffers[2].FinaleAttack);
        }
        void CancelGameplayOwner(EnemyState enemy)
        {
            if (GameplayView == null) return;
            var key = new EnemyKey(enemy.Serial, enemy.Generation);
            for (int i = liveAttacks.Count - 1; i >= 0; i--) if (liveAttacks[i].Intent.Owner.Equals(key)) liveAttacks.RemoveAt(i);
        }
        internal void ClearGameplay()
        {
            if (GameplayView == null || gameplayStopped) return;
            gameplayStopped = true; liveAttacks.Clear(); pendingSpawns.Clear(); movements.Clear(); attackViews.Clear(); spawnViews.Clear();
            GameplayView.Objective = default;
            foreach (var module in gameplaySlots) module?.Clear();
        }
        internal int MagnetizeFreeGems()
        {
            int count = 0;
            foreach (var gem in gems) if (gem.Life == GemLife.Free)
            { gem.Life = GemLife.Magnet; gem.MagnetSpeed = Config.magnetStartSpeed; count++; }
            return count;
        }
    }
}
