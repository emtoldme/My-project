using System;
using System.Collections.Generic;
using UnityEngine;
using Waffle.Combat;
using Waffle.Homecoming;
using Waffle.Homecoming.Places;

namespace Reverb.Homecoming.Places
{
    /// <summary>
    /// 只读地点表现适配器：每帧读取宿主提供的PoiView快照，摆放/复用有限个地点Prefab。
    /// 不写HP/XP/遗物，不调用Prepare/Confirm，不新建GameLoop；领取账本在规则模块，Prefab销毁不影响状态。
    /// 尚未接Main：Codex设置 Source（IHomecomingViewSource 或任何返回 PoiView 的委托）。
    /// </summary>
    public sealed class HomecomingPlacesPresenter : MonoBehaviour
    {
        [Serializable] public struct TemplatePrefab { public string templateId; public HomecomingPlaceView prefab; }
        public TemplatePrefab[] prefabs;
        public Transform root;
        [Range(1, 8)] public int maxInstances = PoiModule.MaxNearby;

        public Func<IReadOnlyList<PoiView>> Source;
        public void Bind(IHomecomingViewSource source) => Source = source == null ? null : (Func<IReadOnlyList<PoiView>>)source.ReadNearby;

        readonly Dictionary<string, HomecomingPlaceView> live = new Dictionary<string, HomecomingPlaceView>(StringComparer.Ordinal);
        readonly HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> stale = new List<string>();

        public static Vector3 ToWorld(Vec2 p, float y = 0f) => new Vector3(p.X, y, p.Y);

        void LateUpdate()
        {
            var views = Source?.Invoke();
            if (views == null) return;
            seen.Clear();
            for (int i = 0; i < views.Count && seen.Count < maxInstances; i++)
            {
                var v = views[i];
                if (v.Site.SiteId == null || !seen.Add(v.Site.SiteId)) continue;
                if (!live.TryGetValue(v.Site.SiteId, out var inst) || inst == null)
                {
                    var prefab = Find(v.Site.TemplateId);
                    if (prefab == null) continue;
                    inst = Instantiate(prefab, ToWorld(v.Site.Position), Quaternion.identity, root != null ? root : transform);
                    inst.name = v.Site.SiteId;
                    live[v.Site.SiteId] = inst;
                }
                inst.Apply(v.Status);
            }
            stale.Clear();
            foreach (var kv in live) if (!seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale) { if (live[id] != null) Destroy(live[id].gameObject); live.Remove(id); }
        }

        HomecomingPlaceView Find(string t)
        {
            if (prefabs != null) foreach (var p in prefabs) if (p.templateId == t) return p.prefab;
            return null;
        }
    }
}
