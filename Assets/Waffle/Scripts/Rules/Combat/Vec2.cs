using System;

namespace Waffle.Combat
{
    /// <summary>XZ 平面上的二维向量（X = 世界 x，Y = 世界 z）。纯 C#，不依赖 UnityEngine。</summary>
    [Serializable]
    public struct Vec2
    {
        public float X;
        public float Y;

        public Vec2(float x, float y) { X = x; Y = y; }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);

        public float SqrLength { get { return X * X + Y * Y; } }
        public float Length { get { return (float)Math.Sqrt(X * X + Y * Y); } }

        /// <summary>单位向量；长度近似 0 时返回 Zero。</summary>
        public Vec2 Normalized
        {
            get
            {
                float l = Length;
                return l > 1e-6f ? new Vec2(X / l, Y / l) : Zero;
            }
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) { return new Vec2(a.X + b.X, a.Y + b.Y); }
        public static Vec2 operator -(Vec2 a, Vec2 b) { return new Vec2(a.X - b.X, a.Y - b.Y); }
        public static Vec2 operator *(Vec2 a, float k) { return new Vec2(a.X * k, a.Y * k); }
        public static float Dot(Vec2 a, Vec2 b) { return a.X * b.X + a.Y * b.Y; }
        public static float SqrDistance(Vec2 a, Vec2 b) { return (a - b).SqrLength; }
        public static float Distance(Vec2 a, Vec2 b) { return (a - b).Length; }
        public static Vec2 FromAngle(float rad) { return new Vec2((float)Math.Cos(rad), (float)Math.Sin(rad)); }

        public override string ToString() { return "(" + X.ToString("0.###") + ", " + Y.ToString("0.###") + ")"; }
    }

    /// <summary>确定性随机流（xorshift32）。每个 CombatModule 自己持有，不用 static，也不依赖 System.Random 的实现差异。</summary>
    public sealed class CombatRng
    {
        private uint state;

        public CombatRng(int seed)
        {
            state = (uint)seed ^ 0x9E3779B9u;
            if (state == 0) state = 0x6C8E9CF5u;
            for (int i = 0; i < 4; i++) NextUInt();
        }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        /// <summary>[0, 1)。</summary>
        public float Next01() { return (NextUInt() >> 8) * (1f / 16777216f); }

        public float Range(float min, float max) { return min + (max - min) * Next01(); }
    }
}
