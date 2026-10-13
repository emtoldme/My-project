using System;
using Waffle.Combat;

namespace Waffle.GameplayNext
{
    public static class GameplayGeometry
    {
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool Finite(Vec2 value) => Finite(value.X) && Finite(value.Y);
        public static bool Valid(AttackShape shape)
        {
            return Enum.IsDefined(typeof(AttackShapeKind), shape.Kind) && Finite(shape.Center) && Finite(shape.End)
                && Finite(shape.Radius) && shape.Radius > 0 && shape.Radius <= 100
                && Finite(shape.InnerRadius) && shape.InnerRadius >= 0 && shape.InnerRadius < shape.Radius
                && Finite(shape.ExpansionSpeed) && shape.ExpansionSpeed >= 0 && shape.ExpansionSpeed <= 30
                && (shape.Kind == AttackShapeKind.Annulus || shape.ExpansionSpeed == 0 && shape.InnerRadius == 0);
        }

        /// <summary>Continuous player/expanding-ring sweep. Scratch requires at least 8 floats; no frame allocations.</summary>
        public static bool Hits(AttackShape shape, Vec2 from, Vec2 to, float playerRadius,
            float ageFrom, float ageTo, float[] scratch)
        {
            if (shape.Kind == AttackShapeKind.Circle)
                return PointSegmentSquared(shape.Center, from, to) <= Square(shape.Radius + playerRadius);
            if (shape.Kind == AttackShapeKind.Capsule)
                return SegmentsSquared(from, to, shape.Center, shape.End) <= Square(shape.Radius + playerRadius);
            if (scratch == null || scratch.Length < 8) throw new ArgumentException("Eight scratch floats required.");
            Vec2 p = from - shape.Center, d = to - from;
            float outer = shape.Radius + ageFrom * shape.ExpansionSpeed + playerRadius;
            float inner = shape.InnerRadius + ageFrom * shape.ExpansionSpeed - playerRadius;
            float growth = (ageTo - ageFrom) * shape.ExpansionSpeed;
            int count = 0; scratch[count++] = 0; scratch[count++] = 1;
            AddRoots(Vec2.Dot(d, d) - growth * growth, 2 * (Vec2.Dot(p, d) - outer * growth), p.SqrLength - outer * outer, scratch, ref count);
            AddRoots(Vec2.Dot(d, d) - growth * growth, 2 * (Vec2.Dot(p, d) - inner * growth), p.SqrLength - inner * inner, scratch, ref count);
            if (growth > 0) Add(-inner / growth, scratch, ref count);
            for (int i = 1; i < count; i++) { float v = scratch[i]; int j = i - 1; while (j >= 0 && scratch[j] > v) { scratch[j + 1] = scratch[j]; j--; } scratch[j + 1] = v; }
            for (int i = 0; i < count; i++)
            {
                if (WithinRing(p, d, inner, outer, growth, scratch[i])) return true;
                if (i + 1 < count && WithinRing(p, d, inner, outer, growth, (scratch[i] + scratch[i + 1]) * .5f)) return true;
            }
            return false;
        }
        static bool WithinRing(Vec2 p, Vec2 d, float inner, float outer, float growth, float u)
        { float r = (p + d * u).SqrLength; return r <= Square(outer + growth * u) + 1e-5f && r >= Square(Math.Max(0, inner + growth * u)) - 1e-5f; }
        static void AddRoots(float a, float b, float c, float[] scratch, ref int count)
        {
            if (Math.Abs(a) < 1e-7f) { if (Math.Abs(b) > 1e-7f) Add(-c / b, scratch, ref count); return; }
            float disc = b * b - 4 * a * c; if (disc < 0) return;
            float root = (float)Math.Sqrt(disc); Add((-b - root) / (2 * a), scratch, ref count); Add((-b + root) / (2 * a), scratch, ref count);
        }
        static void Add(float v, float[] scratch, ref int count) { if (v > 0 && v < 1 && count < scratch.Length) scratch[count++] = v; }
        static float Square(float v) => v * v;
        static float Cross(Vec2 a, Vec2 b) => a.X * b.Y - a.Y * b.X;
        public static float PointSegmentSquared(Vec2 point, Vec2 a, Vec2 b)
        { Vec2 ab = b - a; float t = ab.SqrLength > 1e-10f ? Math.Max(0, Math.Min(1, Vec2.Dot(point - a, ab) / ab.SqrLength)) : 0; return Vec2.SqrDistance(point, a + ab * t); }
        static float SegmentsSquared(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
        {
            Vec2 ab = b - a, cd = d - c; float denominator = Cross(ab, cd);
            if (Math.Abs(denominator) > 1e-8f)
            { float u = Cross(c - a, cd) / denominator, v = Cross(c - a, ab) / denominator; if (u >= 0 && u <= 1 && v >= 0 && v <= 1) return 0; }
            return Math.Min(Math.Min(PointSegmentSquared(a, c, d), PointSegmentSquared(b, c, d)), Math.Min(PointSegmentSquared(c, a, b), PointSegmentSquared(d, a, b)));
        }
    }
}
