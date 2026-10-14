using System;
using System.Collections.Generic;
using Waffle.Combat;

namespace Waffle.Homecoming.Places
{
    /// <summary>
    /// HC-A 确定性地点布局。纯整数哈希，只读世界种子（ProceduralFieldStreamer.Seed），不消费战斗RNG。
    /// 道路几何沿用 R4：每72米一条南北/东西田间路（中线摆动≤6米），流式地块24米。
    /// 所有站点放在路段中点、离路中线12米的田边空地，远离路口农机/秋千地标（地标只在路口±19米内）。
    /// </summary>
    public static class HomecomingPlacesLayout
    {
        public const float RoadSpacing = 72f;
        public const float ChunkSize = 24f;      // 与 ProceduralFieldStreamer.ChunkSize 一致
        public const float RoadsideOffset = 12f;
        public const float InteractionRadius = 2.4f;
        public const float DiscoveryRadius = 18f;
        /// <summary>通用远处地点只在切比雪夫宏格环≥此值出现，内圈只留三处故事地点。</summary>
        public const int GenericMinRing = 3;
        public const float GenericChance = 0.3f;
        public const uint Salt = 0x48434150u; // "HCAP"，与战斗RNG/地形哈希盐都不同

        /// <summary>旧家：固定+X方向，沿东西主路(z≈0)第4段路中点北侧。步行约252米：5m/s约50秒，满移速6.2m/s约41秒。</summary>
        public static readonly Vec2 OldHomePosition = new Vec2(252f, RoadsideOffset);

        // 筒仓：第一圈（宏格环1）的三个路段中点之一，由种子选择；不与旧家同方向。
        static readonly Vec2[] SiloCandidates = { new Vec2(36f, 72f + RoadsideOffset), new Vec2(-72f - RoadsideOffset, 36f), new Vec2(-36f, -72f - RoadsideOffset) };
        // 吉他：保底在第一圈之外（宏格环2）的四个路段中点之一。
        static readonly Vec2[] GuitarCandidates = { new Vec2(108f, 144f + RoadsideOffset), new Vec2(-144f - RoadsideOffset, 108f), new Vec2(-108f, -144f - RoadsideOffset), new Vec2(144f + RoadsideOffset, -108f) };

        public static int ChunkOf(float v) => (int)Math.Floor(v / ChunkSize);
        public static int MacroOf(float v) => (int)Math.Floor((v + RoadSpacing * 0.5f) / RoadSpacing);

        public static PoiSite Make(int seed, string templateId, Vec2 p)
            => new PoiSite(HomecomingContract.SiteId(seed, ChunkOf(p.X), ChunkOf(p.Y), templateId), templateId,
                HomecomingPlaceCatalog.NarrativeIdFor(templateId), p, InteractionRadius);

        public static Vec2 SiloPosition(int seed) => SiloCandidates[(int)(Hash(seed, 0, 0, 1) % (uint)SiloCandidates.Length)];
        public static Vec2 GuitarPosition(int seed) => GuitarCandidates[(int)(Hash(seed, 0, 0, 2) % (uint)GuitarCandidates.Length)];

        /// <summary>本种子三处唯一故事地点（顺序固定：旧家、筒仓、吉他）。</summary>
        public static IReadOnlyList<PoiSite> UniqueSites(int seed) => new[]
        {
            Make(seed, HomecomingPlaceCatalog.OldHome, OldHomePosition),
            Make(seed, HomecomingPlaceCatalog.OldSilo, SiloPosition(seed)),
            Make(seed, HomecomingPlaceCatalog.OldGuitar, GuitarPosition(seed)),
        };

        /// <summary>某宏格（72米）的通用远处地点；至多1个，只看(seed,mx,mz)，与查询顺序无关。</summary>
        public static bool TryGenericSite(int seed, int macroX, int macroZ, out PoiSite site)
        {
            site = default;
            if (Math.Max(Math.Abs(macroX), Math.Abs(macroZ)) < GenericMinRing) return false;
            if (IsReservedMacro(seed, macroX, macroZ)) return false;
            if (Unit(Hash(seed, macroX, macroZ, 3)) >= GenericChance) return false;
            var t = HomecomingPlaceCatalog.GenericTemplates;
            string template = t[(int)(Hash(seed, macroX, macroZ, 4) % (uint)t.Count)];
            bool eastWest = (Hash(seed, macroX, macroZ, 5) & 1) == 0;
            float side = (Hash(seed, macroX, macroZ, 6) & 1) == 0 ? RoadsideOffset : -RoadsideOffset;
            Vec2 p = eastWest ? new Vec2(macroX * RoadSpacing + RoadSpacing * 0.5f, macroZ * RoadSpacing + side)
                              : new Vec2(macroX * RoadSpacing + side, macroZ * RoadSpacing + RoadSpacing * 0.5f);
            site = Make(seed, template, p);
            return true;
        }

        /// <summary>宿主用：返回中心附近（半径≤240米）的全部地点，按SiteId排序且数量受限（最多64）。</summary>
        public static IReadOnlyList<PoiSite> SitesNear(int seed, Vec2 center, float radius, int maxCount = 64)
        {
            if (float.IsNaN(radius) || radius <= 0) return Array.Empty<PoiSite>();
            radius = Math.Min(radius, 240f);
            var list = new List<PoiSite>();
            float r2 = radius * radius;
            foreach (var s in UniqueSites(seed)) if (Dist2(s.Position, center) <= r2) list.Add(s);
            int x0 = MacroOf(center.X - radius) - 1, x1 = MacroOf(center.X + radius) + 1;
            int z0 = MacroOf(center.Y - radius) - 1, z1 = MacroOf(center.Y + radius) + 1;
            for (int x = x0; x <= x1; x++) for (int z = z0; z <= z1; z++)
                if (TryGenericSite(seed, x, z, out var g) && Dist2(g.Position, center) <= r2) list.Add(g);
            list.Sort((a, b) => string.CompareOrdinal(a.SiteId, b.SiteId));
            if (list.Count > maxCount) list.RemoveRange(maxCount, list.Count - maxCount);
            return list.AsReadOnly();
        }

        /// <summary>保留宏格：出生路口(0,0)、旧家所在两格、筒仓/吉他所在格，不放通用地点。</summary>
        public static bool IsReservedMacro(int seed, int mx, int mz)
        {
            if (Math.Max(Math.Abs(mx), Math.Abs(mz)) <= 2) return true;
            foreach (var s in UniqueSites(seed))
            {
                int sx = MacroOf(s.Position.X), sz = MacroOf(s.Position.Y);
                if (Math.Abs(mx - sx) <= 1 && Math.Abs(mz - sz) <= 1) return true;
            }
            return false;
        }

        static float Dist2(Vec2 a, Vec2 b) { float dx = a.X - b.X, dy = a.Y - b.Y; return dx * dx + dy * dy; }
        static float Unit(uint h) => (h & 0xffffff) / 16777216f;
        static uint Hash(int seed, int x, int z, uint k)
        {
            unchecked
            {
                uint h = (uint)seed * 0x27D4EB2Fu ^ (uint)x * 0x9E3779B9u ^ (uint)z * 0x85EBCA6Bu ^ (Salt + k) * 0xC2B2AE35u;
                h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
                return h;
            }
        }
    }
}
