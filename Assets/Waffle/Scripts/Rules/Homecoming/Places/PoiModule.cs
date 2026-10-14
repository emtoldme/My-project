using System;
using System.Collections.Generic;
using System.Globalization;
using Waffle.Combat;

namespace Waffle.Homecoming.Places
{
    /// <summary>
    /// HC-A IPoiModule 实现。只记账、只提出奖励意图；HP/XP/遗物均不在此写入。
    /// 账本只随 Reset 清空，地块卸载/Prefab销毁不影响状态。
    /// 有限预算：注册≤MaxRegisteredSites，发现记录≤MaxDiscoveries，每次快照≤MaxNearby。
    /// 超预算：新的通用站点注册返回false（三处唯一站点总有保留名额）；发现记录满后通用站点不再记为已发现，
    /// 唯一站点不计入该预算；快照按距离+SiteId截断。
    /// </summary>
    public sealed class PoiModule : IPoiModule
    {
        public const int MaxRegisteredSites = 256;
        public const int MaxDiscoveries = 64;
        public const int MaxNearby = 8;
        const float PositionTolerance = 0.05f;

        sealed class Record
        {
            public PoiSite Site;
            public bool Discovered, Claimed, Pending;
            public PoiReward PendingReward;
        }

        readonly Dictionary<string, Record> sites = new Dictionary<string, Record>(StringComparer.Ordinal);
        readonly List<string> discoveryOrder = new List<string>();
        readonly List<string> claimOrder = new List<string>();
        readonly List<PoiView> nearby = new List<PoiView>();
        IReadOnlyList<PoiView> nearbySnapshot = Array.Empty<PoiView>();
        int genericDiscoveries, genericRegistered;

        public Guid RunId { get; private set; }
        public int WorldSeed { get; private set; }
        public float DiscoveryRadius { get; } = HomecomingPlacesLayout.DiscoveryRadius;

        public void Reset(Guid runId, int worldSeed)
        {
            RunId = runId; WorldSeed = worldSeed;
            sites.Clear(); discoveryOrder.Clear(); claimOrder.Clear(); nearby.Clear();
            nearbySnapshot = Array.Empty<PoiView>();
            genericDiscoveries = 0; genericRegistered = 0;
        }

        public bool Register(PoiSite site)
        {
            if (RunId == Guid.Empty || !Valid(site)) return false;
            if (sites.TryGetValue(site.SiteId, out var existing)) return Same(existing.Site, site);
            bool unique = HomecomingPlaceCatalog.IsUnique(site.TemplateId);
            if (!unique && genericRegistered >= MaxRegisteredSites - 3) return false;
            sites.Add(site.SiteId, new Record { Site = site });
            if (!unique) genericRegistered++;
            return true;
        }

        bool Valid(PoiSite s)
        {
            if (string.IsNullOrEmpty(s.SiteId) || !HomecomingPlaceCatalog.IsKnownTemplate(s.TemplateId)) return false;
            if (s.NarrativeId != HomecomingPlaceCatalog.NarrativeIdFor(s.TemplateId)) return false;
            if (!Finite(s.Position.X) || !Finite(s.Position.Y) || Math.Abs(s.Position.X) > 1e6f || Math.Abs(s.Position.Y) > 1e6f) return false;
            if (!Finite(s.InteractionRadius) || s.InteractionRadius <= 0 || s.InteractionRadius > 8f) return false;
            string expected = HomecomingContract.SiteId(WorldSeed, HomecomingPlacesLayout.ChunkOf(s.Position.X), HomecomingPlacesLayout.ChunkOf(s.Position.Y), s.TemplateId);
            if (s.SiteId != expected) return false;
            if (HomecomingPlaceCatalog.IsUnique(s.TemplateId))
            {
                // 唯一故事地点只认本种子的保留落点，防止被复制成几十个“主角家”。
                foreach (var u in HomecomingPlacesLayout.UniqueSites(WorldSeed))
                    if (u.TemplateId == s.TemplateId) return u.SiteId == s.SiteId && Near(u.Position, s.Position, PositionTolerance);
                return false;
            }
            return true;
        }

        static bool Same(PoiSite a, PoiSite b) => a.TemplateId == b.TemplateId && a.NarrativeId == b.NarrativeId
            && Near(a.Position, b.Position, PositionTolerance) && Math.Abs(a.InteractionRadius - b.InteractionRadius) < 1e-4f;
        static bool Near(Vec2 a, Vec2 b, float t) => Math.Abs(a.X - b.X) <= t && Math.Abs(a.Y - b.Y) <= t;
        static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        static float Dist2(Vec2 a, Vec2 b) { float dx = a.X - b.X, dy = a.Y - b.Y; return dx * dx + dy * dy; }

        bool Accepts(in ExplorationContext c) => c.CanInteract && c.RunId == RunId && c.WorldSeed == WorldSeed
            && Finite(c.Player.X) && Finite(c.Player.Y);

        public void Step(ExplorationContext context)
        {
            if (!Accepts(context)) return; // 暂停/升级/终局/死亡/旧局：冻结，快照保持上一帧
            nearby.Clear();
            float d2 = DiscoveryRadius * DiscoveryRadius;
            var hits = new List<(float d, Record r)>();
            foreach (var r in sites.Values)
            {
                float d = Dist2(r.Site.Position, context.Player);
                if (d > d2) continue;
                if (!r.Discovered) MarkDiscovered(r);
                hits.Add((d, r));
            }
            hits.Sort((a, b) => a.d != b.d ? a.d.CompareTo(b.d) : string.CompareOrdinal(a.r.Site.SiteId, b.r.Site.SiteId));
            for (int i = 0; i < hits.Count && i < MaxNearby; i++)
            {
                var r = hits[i].r;
                float rr = r.Site.InteractionRadius * r.Site.InteractionRadius;
                nearby.Add(new PoiView(r.Site, StatusOf(r, hits[i].d <= rr), HomecomingPlaceCatalog.RewardDescription(r.Site.TemplateId)));
            }
            nearbySnapshot = new List<PoiView>(nearby).AsReadOnly();
        }

        void MarkDiscovered(Record r)
        {
            if (!HomecomingPlaceCatalog.IsUnique(r.Site.TemplateId))
            {
                if (genericDiscoveries >= MaxDiscoveries) return;
                genericDiscoveries++;
            }
            r.Discovered = true;
            discoveryOrder.Add(r.Site.SiteId);
        }

        PoiStatus StatusOf(Record r, bool inRange)
        {
            if (r.Claimed) return PoiStatus.Claimed;
            if (inRange && HomecomingPlaceCatalog.HasReward(r.Site.TemplateId)) return PoiStatus.Available;
            return r.Discovered ? PoiStatus.Discovered : PoiStatus.Unseen;
        }

        public IReadOnlyList<PoiView> ReadNearby() => nearbySnapshot;

        /// <summary>自动拾取：从最近一次快照里挑一个Available（快照已按距离+SiteId排序）。宿主每tick至多处理一个。</summary>
        public bool TrySelectAutoClaim(ExplorationContext context, out string siteId)
        {
            siteId = null;
            if (!Accepts(context)) return false;
            foreach (var v in nearbySnapshot)
                if (v.Status == PoiStatus.Available) { siteId = v.Site.SiteId; return true; }
            return false;
        }

        public bool TryPrepareClaim(ExplorationContext context, string siteId, out PoiReward reward)
        {
            reward = default;
            if (!Accepts(context) || siteId == null || !sites.TryGetValue(siteId, out var r)) return false;
            if (r.Claimed || !HomecomingPlaceCatalog.HasReward(r.Site.TemplateId)) return false;
            float rr = r.Site.InteractionRadius;
            if (Dist2(r.Site.Position, context.Player) > rr * rr) return false; // 用当前位置，拒绝远程伪造
            if (!r.Pending)
            {
                if (!r.Discovered) MarkDiscovered(r);
                var g = HomecomingPlaceCatalog.RewardFor(r.Site.TemplateId);
                r.PendingReward = new PoiReward(RunId, r.Site.SiteId, g.ItemId, g.AdditionalSlots);
                r.Pending = true;
            }
            reward = r.PendingReward;
            return true;
        }

        public void ConfirmClaim(Guid runId, string siteId, bool accepted)
        {
            if (runId == Guid.Empty || runId != RunId || siteId == null || !sites.TryGetValue(siteId, out var r) || !r.Pending) return;
            if (accepted) { r.Claimed = true; r.Pending = false; claimOrder.Add(siteId); }
            else r.Pending = false; // 保留可领取，下次准备得到同一ClaimId/内容
        }

        // ---- 账本查询（宿主构造结算/日志用；均为副本） ----
        public PoiStatus StatusOf(string siteId) => siteId != null && sites.TryGetValue(siteId, out var r)
            ? (r.Claimed ? PoiStatus.Claimed : r.Discovered ? PoiStatus.Discovered : PoiStatus.Unseen) : PoiStatus.Unseen;
        public bool HasPendingClaim(string siteId) => siteId != null && sites.TryGetValue(siteId, out var r) && r.Pending;
        public IReadOnlyList<string> DiscoveredNarrativeIds() => Map(discoveryOrder, r => r.Site.NarrativeId);
        public IReadOnlyList<string> ClaimedTemplateIds() => Map(claimOrder, r => r.Site.TemplateId);
        public IReadOnlyList<string> ClaimedSiteIds() => new List<string>(claimOrder).AsReadOnly();
        public int RegisteredCount => sites.Count;

        IReadOnlyList<string> Map(List<string> ids, Func<Record, string> f)
        {
            var o = new List<string>();
            foreach (var id in ids) { string v = f(sites[id]); if (!o.Contains(v)) o.Add(v); }
            return o.AsReadOnly();
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "PoiModule(run={0}, seed={1}, sites={2})", RunId, WorldSeed, sites.Count);
    }
}
