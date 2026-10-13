using System;
using System.Collections.Generic;
using Waffle.Combat;

namespace Waffle.RunContent.Components
{
    /// <summary>
    /// 载体：有限残留区（W06）。点数上限、寿命有限、同目标接触节流、每 tick 命中预算。
    /// 可选相邻连线（线宽固定）。不存敌人 HP；只输出 TargetDamage / AreaDamage / ZoneSpawned。
    /// </summary>
    public sealed class ResidualZoneField
    {
        public const int HardPointCap = 12, HitsPerTickCap = 24;
        public const float LinkWidth = 0.4f;

        public struct Point { public int Serial; public Vec2 Position; public float BornAt, EndsAt; }
        int nextSerial = 1;
        readonly List<long> expiredKeys = new List<long>();
        readonly List<Point> points = new List<Point>(HardPointCap);
        readonly Dictionary<long, float> readyAt = new Dictionary<long, float>();
        public IReadOnlyList<Point> Points => points;

        public void Place(Vec2 at, float now, float life, int max, string id, int slot, float radius, List<RuleCommand> output)
        {
            max = Math.Max(1, Math.Min(HardPointCap, max));
            while (points.Count >= max) points.RemoveAt(0); // 最旧的先消失
            points.Add(new Point { Serial = nextSerial++, Position = at, BornAt = now, EndsAt = now + Math.Max(0.1f, life) });
            output.Add(new RuleCommand { Kind = RuleCommandKind.ZoneSpawned, ContentId = id, Slot = slot, RootId = id,
                Position = at, Radius = radius, Duration = life });
        }

        public void Expire(float now)
        { for (int i = points.Count - 1; i >= 0; i--) if (now >= points[i].EndsAt) points.RemoveAt(i); }

        /// <summary>接触判定：点圆（radius）或相邻连线（link）。同目标 hitCd 秒内只吃一次（跨所有点）。</summary>
        public void Contact(float now, IReadOnlyList<EnemySnapshot> enemies, float radius, bool link, float hitCd,
            float damage, string id, int slot, List<RuleCommand> output)
        {
            if (points.Count == 0) return;
            int hits = 0;
            for (int i = 0; i < enemies.Count && hits < HitsPerTickCap; i++)
            {
                var e = enemies[i];
                if (readyAt.TryGetValue(e.Key, out float r) && now < r) continue;
                if (!Touches(e, radius, link, out Vec2 at)) continue;
                readyAt[e.Key] = now + Math.Max(0.05f, hitCd);
                hits++;
                output.Add(new RuleCommand { Kind = RuleCommandKind.TargetDamage, ContentId = id, Slot = slot, RootId = id,
                    Serial = e.Serial, Generation = e.Generation, Position = e.Position, From = at, Damage = damage, Depth = 1 });
            }
            expiredKeys.Clear();
            foreach (var pair in readyAt) if (pair.Value <= now) expiredKeys.Add(pair.Key);
            foreach (var key in expiredKeys) readyAt.Remove(key);
        }

        bool Touches(in EnemySnapshot e, float radius, bool link, out Vec2 at)
        {
            float rr = (radius + e.Radius) * (radius + e.Radius);
            for (int p = 0; p < points.Count; p++)
                if (Vec2.SqrDistance(points[p].Position, e.Position) <= rr) { at = points[p].Position; return true; }
            if (link)
            {
                float w = LinkWidth * 0.5f + e.Radius;
                for (int p = 1; p < points.Count; p++)
                    if (SqrDistToSegment(e.Position, points[p - 1].Position, points[p].Position) <= w * w)
                    { at = points[p].Position; return true; }
            }
            at = default; return false;
        }

        /// <summary>引爆离 center 最近的 count 个点（移除），各输出一次 AreaDamage。</summary>
        public int Detonate(Vec2 center, int count, float damage, float radius, string id, int slot, List<RuleCommand> output)
        {
            int n = 0;
            while (n < count && points.Count > 0)
            {
                int best = 0; float bd = float.MaxValue;
                for (int i = 0; i < points.Count; i++)
                { float d = Vec2.SqrDistance(center, points[i].Position); if (d < bd) { bd = d; best = i; } }
                output.Add(new RuleCommand { Kind = RuleCommandKind.AreaDamage, ContentId = id, Slot = slot, RootId = id,
                    Position = points[best].Position, From = center, Damage = damage, Radius = radius, Depth = 1, BudgetLeft = 12 });
                points.RemoveAt(best); n++;
            }
            return n;
        }

        public void Clear() { points.Clear(); readyAt.Clear(); }

        static float SqrDistToSegment(Vec2 p, Vec2 a, Vec2 b)
        {
            Vec2 ab = b - a; float l = ab.SqrLength;
            float t = l <= 1e-6f ? 0f : Math.Max(0f, Math.Min(1f, Vec2.Dot(p - a, ab) / l));
            return Vec2.SqrDistance(p, a + ab * t);
        }
    }
}
