using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.Homecoming;
using Waffle.Homecoming.Places;
using Waffle.RunContent;

namespace Waffle.Tests
{
    public sealed class HomecomingPlacesTests
    {
        const int Seed = 14104;
        static readonly Guid Run = new Guid("11111111-2222-3333-4444-555555555555");

        static ExplorationContext At(Vec2 p, RunPhase phase = RunPhase.Playing, bool alive = true, Guid? run = null, int seed = Seed)
            => new ExplorationContext(run ?? Run, seed, 1, .05f, p, phase, alive);

        static PoiModule NewModule(int seed = Seed)
        {
            var m = new PoiModule(); m.Reset(Run, seed);
            foreach (var s in HomecomingPlacesLayout.UniqueSites(seed)) Assert.That(m.Register(s), Is.True);
            return m;
        }
        static PoiSite Unique(string t, int seed = Seed) => HomecomingPlacesLayout.UniqueSites(seed).First(s => s.TemplateId == t);

        [Test] public void LayoutIsDeterministicForSeedAndUniqueStoriesAreGuaranteed()
        {
            foreach (int seed in new[] { 0, 1, -7, Seed, int.MinValue, int.MaxValue })
            {
                var a = HomecomingPlacesLayout.UniqueSites(seed); var b = HomecomingPlacesLayout.UniqueSites(seed);
                Assert.That(a.Select(s => s.SiteId), Is.EqualTo(b.Select(s => s.SiteId)));
                var home = a[0]; var silo = a[1]; var guitar = a[2];
                float homeDist = home.Position.Length;
                Assert.That(homeDist / 6.2f, Is.InRange(30f, 60f), "旧家满移速≥30秒");
                Assert.That(homeDist / 5f, Is.InRange(30f, 60f), "旧家基础移速≤60秒");
                Assert.That(home.Position.X, Is.GreaterThan(0)); // 固定+X方向
                Assert.That(Ring(silo), Is.EqualTo(1));
                Assert.That(Ring(guitar), Is.GreaterThanOrEqualTo(2));
                Assert.That(a.Select(s => s.TemplateId), Is.EqualTo(new[] { "poi.old-home", "poi.old-silo", "poi.old-guitar" }));
            }
        }
        static int Ring(PoiSite s) => Math.Max(Math.Abs(HomecomingPlacesLayout.MacroOf(s.Position.X)), Math.Abs(HomecomingPlacesLayout.MacroOf(s.Position.Y)));

        [Test] public void GenericSitesAreOrderIndependentNegativeSafeAndNeverStoryTemplates()
        {
            var forward = new List<string>(); var backward = new List<string>();
            for (int x = -12; x <= 12; x++) for (int z = -12; z <= 12; z++) if (HomecomingPlacesLayout.TryGenericSite(Seed, x, z, out var s)) forward.Add(s.SiteId);
            for (int x = 12; x >= -12; x--) for (int z = 12; z >= -12; z--) if (HomecomingPlacesLayout.TryGenericSite(Seed, x, z, out var s)) backward.Add(s.SiteId);
            forward.Sort(StringComparer.Ordinal); backward.Sort(StringComparer.Ordinal);
            Assert.That(forward, Is.EqualTo(backward)); Assert.That(forward.Count, Is.GreaterThan(10));
            Assert.That(forward.Any(id => id.Contains(":-")), Is.True, "包含负坐标站点");
            Assert.That(forward.All(id => !HomecomingPlaceCatalog.UniqueTemplates.Any(id.EndsWith)), Is.True);
            var near = HomecomingPlacesLayout.SitesNear(Seed, new Vec2(-700, -700), 240);
            Assert.That(near.Count, Is.LessThanOrEqualTo(64));
            Assert.That(near.All(s => !HomecomingPlaceCatalog.HasReward(s.TemplateId)), Is.True);
        }

        [Test] public void RegisterIsIdempotentAndRejectsConflictsForgeriesAndCopies()
        {
            var m = NewModule(); var home = Unique("poi.old-home");
            Assert.That(m.Register(home), Is.True);
            Assert.That(m.Register(new PoiSite(home.SiteId, home.TemplateId, home.NarrativeId, new Vec2(home.Position.X + 3, home.Position.Y), 2.4f)), Is.False);
            var copy = HomecomingPlacesLayout.Make(Seed, "poi.old-home", new Vec2(-500, -500));
            Assert.That(m.Register(copy), Is.False, "不能复制出第二个主角家");
            Assert.That(m.Register(HomecomingPlacesLayout.Make(Seed + 1, "poi.field-swing", new Vec2(-500, -500))), Is.False, "错误种子");
            Assert.That(m.Register(new PoiSite("hc1:x", "poi.field-swing", "poi.field-swing", new Vec2(0, 0), 2.4f)), Is.False);
            Assert.That(m.Register(new PoiSite(home.SiteId, home.TemplateId, home.NarrativeId, new Vec2(float.NaN, 0), 2.4f)), Is.False);
            Assert.That(m.Register(new PoiSite(home.SiteId, home.TemplateId, home.NarrativeId, home.Position, 0)), Is.False);
            Assert.That(m.Register(HomecomingPlacesLayout.Make(Seed, "poi.unknown", new Vec2(-500, -500))), Is.False);
            Assert.That(m.RegisteredCount, Is.EqualTo(3));
            var fresh = new PoiModule(); Assert.That(fresh.Register(home), Is.False, "Reset前拒绝");
        }

        [Test] public void DiscoveryAndAvailabilityUseRadii()
        {
            var m = NewModule(); var home = Unique("poi.old-home");
            m.Step(At(new Vec2(home.Position.X + 18.5f, home.Position.Y))); Assert.That(m.ReadNearby(), Is.Empty);
            m.Step(At(new Vec2(home.Position.X + 17.5f, home.Position.Y)));
            Assert.That(m.ReadNearby().Single().Status, Is.EqualTo(PoiStatus.Discovered));
            Assert.That(m.TryPrepareClaim(At(new Vec2(home.Position.X + 2.5f, home.Position.Y)), home.SiteId, out _), Is.False);
            m.Step(At(new Vec2(home.Position.X + 2.3f, home.Position.Y)));
            Assert.That(m.ReadNearby().Single().Status, Is.EqualTo(PoiStatus.Available));
            Assert.That(m.TrySelectAutoClaim(At(new Vec2(home.Position.X + 2.3f, home.Position.Y)), out var id), Is.True);
            Assert.That(id, Is.EqualTo(home.SiteId));
            Assert.That(m.DiscoveredNarrativeIds(), Is.EqualTo(new[] { "poi.old-home" }));
        }

        [Test] public void ClaimIntentIsStableFalseKeepsClaimableTrueSeals()
        {
            var m = NewModule(); var home = Unique("poi.old-home"); var ctx = At(home.Position);
            m.Step(ctx);
            Assert.That(m.TryPrepareClaim(ctx, home.SiteId, out var r1), Is.True);
            Assert.That(m.TryPrepareClaim(ctx, home.SiteId, out var r2), Is.True);
            Assert.That(r2, Is.EqualTo(r1));
            Assert.That(r1.ItemId, Is.EqualTo("item.sewing-tin")); Assert.That(r1.AdditionalSlots, Is.EqualTo(1));
            Assert.That(r1.ClaimId, Is.EqualTo(home.SiteId)); Assert.That(r1.RunId, Is.EqualTo(Run));
            Assert.That(m.StatusOf(home.SiteId), Is.Not.EqualTo(PoiStatus.Claimed), "B未接受前不封账");
            m.ConfirmClaim(Guid.NewGuid(), home.SiteId, true); Assert.That(m.HasPendingClaim(home.SiteId), Is.True, "旧RunId确认无效");
            m.ConfirmClaim(Run, home.SiteId, false);
            Assert.That(m.TryPrepareClaim(ctx, home.SiteId, out var r3), Is.True); Assert.That(r3, Is.EqualTo(r1));
            m.ConfirmClaim(Run, home.SiteId, true);
            Assert.That(m.StatusOf(home.SiteId), Is.EqualTo(PoiStatus.Claimed));
            Assert.That(m.TryPrepareClaim(ctx, home.SiteId, out _), Is.False);
            m.ConfirmClaim(Run, home.SiteId, true);
            Assert.That(m.ClaimedTemplateIds(), Is.EqualTo(new[] { "poi.old-home" }));
            m.Step(ctx); Assert.That(m.ReadNearby().Single().Status, Is.EqualTo(PoiStatus.Claimed));
            Assert.That(m.TrySelectAutoClaim(ctx, out _), Is.False);
        }

        [TestCase(RunPhase.Paused, true)][TestCase(RunPhase.LevelUp, true)][TestCase(RunPhase.Defeat, true)][TestCase(RunPhase.Victory, true)][TestCase(RunPhase.Playing, false)]
        public void FrozenOrDeadFramesNeitherAdvanceNorClaim(RunPhase phase, bool alive)
        {
            var m = NewModule(); var silo = Unique("poi.old-silo");
            m.Step(At(silo.Position, phase, alive));
            Assert.That(m.ReadNearby(), Is.Empty);
            Assert.That(m.TryPrepareClaim(At(silo.Position, phase, alive), silo.SiteId, out _), Is.False);
            Assert.That(m.DiscoveredNarrativeIds(), Is.Empty);
        }

        [Test] public void OldRunWrongSeedAndRemoteClaimsAreRejected()
        {
            var m = NewModule(); var g = Unique("poi.old-guitar");
            Assert.That(m.TryPrepareClaim(At(g.Position, run: Guid.NewGuid()), g.SiteId, out _), Is.False);
            Assert.That(m.TryPrepareClaim(At(g.Position, seed: Seed + 1), g.SiteId, out _), Is.False);
            Assert.That(m.TryPrepareClaim(At(new Vec2(0, 0)), g.SiteId, out _), Is.False);
            Assert.That(m.TryPrepareClaim(At(g.Position), "hc1:nope", out _), Is.False);
            Assert.That(m.TryPrepareClaim(At(g.Position), g.SiteId, out var r), Is.True);
            Assert.That(r.ItemId, Is.EqualTo("item.old-guitar")); Assert.That(r.AdditionalSlots, Is.EqualTo(0));
        }

        [Test] public void ReRegisteringAfterChunkUnloadKeepsClaimAndResetClears()
        {
            var m = NewModule(); var home = Unique("poi.old-home"); var ctx = At(home.Position);
            m.TryPrepareClaim(ctx, home.SiteId, out _); m.ConfirmClaim(Run, home.SiteId, true);
            m.Step(At(new Vec2(-1000, -1000)));            // 离开，地块回收
            Assert.That(m.Register(home), Is.True);          // 返回后宿主再次注册
            Assert.That(m.StatusOf(home.SiteId), Is.EqualTo(PoiStatus.Claimed));
            Assert.That(m.TryPrepareClaim(ctx, home.SiteId, out _), Is.False);
            var next = Guid.NewGuid(); m.Reset(next, Seed);
            Assert.That(m.ClaimedTemplateIds(), Is.Empty); Assert.That(m.RegisteredCount, Is.EqualTo(0));
            m.Register(home);
            Assert.That(m.TryPrepareClaim(At(home.Position, run: next), home.SiteId, out var r), Is.True);
            Assert.That(r.RunId, Is.EqualTo(next));
        }

        [Test] public void GenericSitesGiveNarrativeOnlyAndBudgetsAreFinite()
        {
            var m = NewModule(); int accepted = 0; PoiSite first = default;
            for (int x = -40; x <= 40 && accepted < 400; x++) for (int z = -40; z <= 40; z++)
                if (HomecomingPlacesLayout.TryGenericSite(Seed, x, z, out var s) && m.Register(s)) { if (accepted == 0) first = s; accepted++; }
            Assert.That(m.RegisteredCount, Is.LessThanOrEqualTo(PoiModule.MaxRegisteredSites));
            Assert.That(m.Register(Unique("poi.old-home")), Is.True, "唯一站点保留名额");
            m.Step(At(first.Position));
            Assert.That(m.ReadNearby().Count, Is.InRange(1, PoiModule.MaxNearby));
            Assert.That(m.ReadNearby()[0].Status, Is.EqualTo(PoiStatus.Discovered), "通用地点不给奖励");
            Assert.That(m.TryPrepareClaim(At(first.Position), first.SiteId, out _), Is.False);
        }

        [Test] public void NearbySnapshotIsSortedBoundedAndImmutable()
        {
            var m = NewModule(); var home = Unique("poi.old-home");
            m.Step(At(home.Position));
            var snap = m.ReadNearby();
            Assert.Throws<NotSupportedException>(() => ((IList<PoiView>)snap).Add(default));
            m.Step(At(new Vec2(-1000, -1000)));
            Assert.That(snap.Count, Is.EqualTo(1)); Assert.That(m.ReadNearby(), Is.Empty);
        }

        [Test] public void NarrativeCatalogCoversEveryRealContentItemAndHomecomingId()
        {
            var cat = HomecomingNarrativeCatalog.Default;
            var ids = new List<string>();
            ids.AddRange(HomecomingPlaceCatalog.UniqueTemplates); ids.AddRange(HomecomingPlaceCatalog.GenericTemplates);
            ids.AddRange(new[] { "item.sewing-tin", "item.work-gloves", "item.old-guitar", "meta.mended-coat", "meta.travel-light", "meta.sorted-satchel" });
            foreach (var g in new[] { "goal.homeward", "goal.harvest-memory", "goal.unfinished-song" }) { ids.Add(g); ids.Add(HomecomingNarrativeCatalog.CompletionId(g)); }
            foreach (var d in RunContentCatalog.Default.All) ids.Add(d.Id);
            Assert.That(RunContentCatalog.Default.All.Count, Is.EqualTo(36));
            foreach (var id in ids) Assert.That(cat.Resolve(id), Is.Not.Null, "缺叙事：" + id);
            foreach (var d in RunContentCatalog.Default.All) Assert.That(cat.Resolve(d.Id).Title, Is.EqualTo(d.DisplayName), "标题沿用原名");
            Assert.That(cat.All.Select(e => e.Body).Distinct().Count(), Is.EqualTo(cat.All.Count), "正文各不相同");
            Assert.That(cat.All.Select(e => e.Id).Distinct().Count(), Is.EqualTo(cat.All.Count));
            Assert.That(cat.Resolve("nope"), Is.Null); Assert.That(cat.Resolve(null), Is.Null);
            Assert.That(cat.OfKind(NarrativeKind.Upgrade).Count(), Is.EqualTo(3));
            foreach (var t in HomecomingPlaceCatalog.UniqueTemplates)
                Assert.That(cat.Resolve(HomecomingPlaceCatalog.RewardFor(t).ItemId), Is.Not.Null);
        }
    }
}
