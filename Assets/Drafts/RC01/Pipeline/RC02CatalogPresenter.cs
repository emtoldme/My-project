using System.Collections.Generic;
using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Pipeline
{
    public class RC02CatalogPresenter : MonoBehaviour, IPresentationEventOwner
    {
        public RC02ContentCatalog catalog;
        public RC02EncounterSheet encounters;
        public GameLoop loop;
        public int suppressed;
        public int SpawnedCount { get; private set; }
        public int ActiveCount => live.Count + trails.Count;
        readonly Dictionary<string, Stack<Live>> pools = new Dictionary<string, Stack<Live>>();
        readonly Dictionary<string, float> readyAt = new Dictionary<string, float>();
        readonly List<Live> live = new List<Live>();
        readonly Dictionary<int, Live> trails = new Dictionary<int, Live>();
        readonly List<int> stale = new List<int>();
        readonly List<LineRenderer> links = new List<LineRenderer>();
        bool frozen, terminal;
        GameObject preview;
        class Live
        {
            public GameObject go;
            public RC02VisualTemplate template;
            public ParticleSystem[] particles;
            public TrailRenderer[] ribbons;
            public RC02CatalogEntry entry;
            public float age, duration;
        }
        void Awake() { if (loop == null) loop = GetComponent<GameLoop>(); }
        void OnEnable() { if (loop != null) loop.PresentationDispatched += OnEvents; }
        void Start()
        {
            if (catalog == null) return;
            foreach (var entry in catalog.entries)
            {
                if (entry.kind != "art" || entry.Asset<GameObject>("prefab") == null) continue;
                var pool = new Stack<Live>(); pools.Add(entry.id, pool);
                for (int i = 0; i < entry.maxInstances; i++) pool.Push(Create(entry));
            }
        }
        void OnDisable() { if (loop != null) loop.PresentationDispatched -= OnEvents; Clear(); ClearPreview(); }
        Live Create(RC02CatalogEntry entry)
        {
            var go = Instantiate(entry.Asset<GameObject>("prefab"), transform); go.name = "RC02_" + entry.id; go.SetActive(false);
            return new Live { go=go, template=go.GetComponent<RC02VisualTemplate>(), entry=entry,
                particles=go.GetComponentsInChildren<ParticleSystem>(true), ribbons=go.GetComponentsInChildren<TrailRenderer>(true) };
        }
        public Texture Icon(string id)
        {
            if (catalog != null) foreach (var entry in catalog.entries) if (entry.contentId==id && entry.Asset<Texture>("icon")!=null) return entry.Asset<Texture>("icon");
            return null;
        }
        public bool OwnsPresentation(PresentationEvent item)
        {
            return isActiveAndEnabled && catalog != null && catalog.TrySelect("art",item.Type.ToString(),item.SourceId,out var entry,out _)
                && entry.Asset<GameObject>("prefab") != null;
        }
        void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            foreach (var item in events)
            {
                if (item.Type==PresentationEventType.Restart || item.Type==PresentationEventType.Victory || item.Type==PresentationEventType.Defeat)
                { terminal=true; Clear(); continue; }
                if (terminal || !OwnsPresentation(item)) continue;
                if (item.Type==PresentationEventType.WeaponZone && item.SourceId=="W06" && item.Value==0) continue;
                catalog.TrySelect("art",item.Type.ToString(),item.SourceId,out var entry,out _);
                if (readyAt.TryGetValue(entry.id,out float next) && item.Now<next) continue;
                var slot=Rent(entry,item); if(slot==null) continue;
                readyAt[entry.id]=item.Now+entry.minInterval;
                slot.duration=item.Duration>0 ? item.Duration : entry.Parameter("tailSeconds",.25f); live.Add(slot);
            }
        }
        Live Rent(RC02CatalogEntry entry, PresentationEvent item)
        {
            if(!pools.TryGetValue(entry.id,out var pool)||pool.Count==0) { suppressed++; return null; }
            var slot=pool.Pop(); slot.age=0; slot.duration=.25f; slot.go.SetActive(true);
            if(slot.template!=null) slot.template.Prepare(entry,item);
            else { slot.go.transform.SetPositionAndRotation(new Vector3(item.X,.1f,item.Z),Quaternion.identity);
                slot.go.transform.localScale=Vector3.one*(item.Radius>0?item.Radius:entry.Parameter("scale",1)); }
            foreach(var r in slot.ribbons) r.Clear();
            foreach(var p in slot.particles) { p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); p.Play(true); }
            SpawnedCount++; return slot;
        }
        void Return(Live slot)
        {
            foreach(var p in slot.particles) p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach(var r in slot.ribbons) r.Clear(); slot.go.SetActive(false);
            if(pools.TryGetValue(slot.entry.id,out var pool)) pool.Push(slot);
        }
        void LateUpdate()
        {
            if(loop?.Sim==null || terminal) return;
            bool hold=loop.Sim.State.Phase!=RunPhase.Playing;
            if(hold!=frozen)
            {
                for(int i=live.Count-1;i>=0;i--)
                    if(hold&&live[i].entry.pausePolicy==RC02PausePolicy.Stop) { Return(live[i]); live.RemoveAt(i); } else Freeze(live[i],hold);
                foreach(var slot in trails.Values) Freeze(slot,hold); frozen=hold;
            }
            if(hold) return; SyncTrails();
            for(int i=live.Count-1;i>=0;i--)
            {
                var slot=live[i]; slot.age+=loop.Sim.LastDt;
                if(slot.age>=slot.duration) { Return(slot); live.RemoveAt(i); } else slot.template?.Tint(1-slot.age/Mathf.Max(.05f,slot.duration));
            }
        }
        void SyncTrails()
        {
            var combat=loop.Sim.Combat; var points=combat?.RunTrails;
            if(points==null||catalog==null||!catalog.TrySelect("art","WeaponZone","W06",out var entry,out _)) return;
            stale.Clear(); foreach(var key in trails.Keys) stale.Add(key);
            for(int i=0;i<points.Count;i++)
            {
                var point=points[i]; stale.Remove(point.Serial);
                if(!trails.TryGetValue(point.Serial,out var slot))
                {
                    slot=Rent(entry,new PresentationEvent { SourceId="W06",X=point.Position.X,Z=point.Position.Y,Radius=combat.RunTrailRadius });
                    if(slot!=null) trails.Add(point.Serial,slot);
                }
                if(slot!=null) slot.template?.Tint(Mathf.Clamp01((point.EndsAt-loop.Sim.State.Now)/.5f));
            }
            foreach(var key in stale) { Return(trails[key]); trails.Remove(key); }
            int count=combat.RunTrailLinks?Mathf.Max(0,points.Count-1):0;
            while(links.Count<count)
            {
                var line=new GameObject("RC02_TrailLink").AddComponent<LineRenderer>(); line.transform.SetParent(transform);
                line.useWorldSpace=true; line.positionCount=2; line.sharedMaterial=entry.Asset<Material>("material"); line.widthMultiplier=.4f; links.Add(line);
            }
            for(int i=0;i<links.Count;i++)
            {
                links[i].gameObject.SetActive(i<count); if(i>=count) continue;
                links[i].SetPosition(0,new Vector3(points[i].Position.X,.1f,points[i].Position.Y));
                links[i].SetPosition(1,new Vector3(points[i+1].Position.X,.1f,points[i+1].Position.Y));
            }
        }
        static void Freeze(Live slot,bool hold) { foreach(var p in slot.particles) if(hold) p.Pause(true); else p.Play(true); }
        public void Clear()
        {
            foreach(var slot in live) Return(slot); live.Clear(); foreach(var slot in trails.Values) Return(slot); trails.Clear();
            foreach(var line in links) if(line!=null) line.gameObject.SetActive(false); readyAt.Clear();
        }
        public GameObject Preview(RC02CatalogEntry entry)
        {
            ClearPreview(); var prefab=entry?.Asset<GameObject>("prefab"); if(prefab==null) return null;
            preview=Instantiate(prefab); preview.name="TEST_ONLY_CatalogPreview"; preview.hideFlags=HideFlags.DontSave;
            preview.GetComponent<RC02VisualTemplate>()?.Prepare(entry,new PresentationEvent { X=1,FromX=-1,DirectionZ=1,Radius=1 }); return preview;
        }
        public void ClearPreview() { if(preview==null) return; if(Application.isPlaying) Destroy(preview); else DestroyImmediate(preview); preview=null; }
    }
}
