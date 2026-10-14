using System;
using System.Collections.Generic;

namespace Waffle.Combat
{
    public sealed partial class CombatModule
    {
        bool homecomingEchoQueued;
        Vec2 homecomingEchoAt;
        float homecomingEchoRadius, homecomingEchoDamage;
        int homecomingEchoCap;
        public int HomecomingEchoHits { get; private set; }
        readonly HashSet<long> homecomingLured = new HashSet<long>();
        public Vec2 HomecomingLurePosition { get; private set; }
        public float HomecomingLureUntil { get; private set; }
        public int HomecomingMagnetizedGems { get; private set; }
        public bool HomecomingLureActive => now < HomecomingLureUntil && homecomingLured.Count > 0;
        public void SetHomecomingLure(Vec2 position, float startAt, float duration, float radius, int cap)
        {
            homecomingLured.Clear(); HomecomingLurePosition = position;
            HomecomingLureUntil = startAt + Math.Max(0, Math.Min(2.5f, duration));
            radius = Math.Max(0, Math.Min(6, radius)); cap = Math.Max(0, Math.Min(12, cap));
            if (cap == 0) return;
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive || enemy.IsBoss || enemy.IsElite || (enemy.ArchetypeId != "legacy-chaser" && enemy.ArchetypeId != "chaser")
                    || Vec2.SqrDistance(position, enemy.Position) > radius * radius) continue;
                homecomingLured.Add(Key(enemy)); if (homecomingLured.Count >= cap) break;
            }
        }
        public void ClearHomecomingLure() { homecomingLured.Clear(); HomecomingLureUntil = 0; }
        public void RebaseHomecomingReturn(Vec2 at)
        { RebaseRunMovement(at); sampleTimes.Clear(); samplePos.Clear(); playerVelocity = Vec2.Zero; }
        bool TryHomecomingLure(EnemyState enemy, out Vec2 velocity)
        {
            velocity = Vec2.Zero;
            if (!HomecomingLureActive || !homecomingLured.Contains(Key(enemy)) || enemy.IsBoss || enemy.IsElite) return false;
            Vec2 offset = HomecomingLurePosition - enemy.Position;
            velocity = offset.Length < .45f ? Vec2.Zero : offset.Normalized * enemy.Speed;
            return true;
        }
        public int MagnetizeHomecomingGems(Vec2 center, float radius, int cap)
        {
            radius = Math.Max(0, Math.Min(8, radius)); cap = Math.Max(0, Math.Min(12, cap));
            int count = 0; if (cap == 0) return 0;
            foreach (var gem in gems)
            {
                if (gem.Life != GemLife.Free || Vec2.SqrDistance(gem.Position, center) > radius * radius) continue;
                gem.Life = GemLife.Magnet; gem.MagnetSpeed = Config.magnetStartSpeed;
                if (++count >= cap) break;
            }
            HomecomingMagnetizedGems += count; return count;
        }
        public void QueueHomecomingEcho(Vec2 at, float radius, float damage, int maxTargets)
        {
            homecomingEchoQueued = true; homecomingEchoAt = at;
            homecomingEchoRadius = Math.Max(0, Math.Min(3, radius));
            homecomingEchoDamage = Math.Max(0, damage); homecomingEchoCap = Math.Max(0, Math.Min(12, maxTargets));
        }
        void ResolveHomecomingEcho()
        {
            if (!homecomingEchoQueued) return;
            homecomingEchoQueued = false;
            Result.Events.Add(new PresentationEvent { Type = PresentationEventType.EchoHit, Now = now,
                X = homecomingEchoAt.X, Z = homecomingEchoAt.Y, Radius = homecomingEchoRadius, Duration = .35f,
                Value = homecomingEchoDamage, SourceId = "item.old-guitar", RootSourceId = "item.old-guitar" });
            int hit = 0;
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive || Vec2.SqrDistance(homecomingEchoAt, enemy.Position) > homecomingEchoRadius * homecomingEchoRadius) continue;
                ApplyEnemyDamage(enemy, homecomingEchoDamage, PresentationEventType.EnemyHit, homecomingEchoAt, "item.old-guitar", allowMechanisms: false);
                HomecomingEchoHits++; if (++hit >= homecomingEchoCap) break;
            }
        }
    }
}
