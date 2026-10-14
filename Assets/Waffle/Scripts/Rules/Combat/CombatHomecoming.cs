using System;

namespace Waffle.Combat
{
    public sealed partial class CombatModule
    {
        bool homecomingEchoQueued;
        Vec2 homecomingEchoAt;
        float homecomingEchoRadius, homecomingEchoDamage;
        int homecomingEchoCap;
        public int HomecomingEchoHits { get; private set; }
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
