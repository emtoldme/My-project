using System.Collections.Generic;
using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Pipeline
{
    /// <summary>
    /// 只消费已分发的表现事件。不播声音，不扣血，不改 timeScale。
    /// 同一物体上若已有旧表现，重叠的事件不播，避免双播。
    /// </summary>
    public class RC02CatalogPresenter : MonoBehaviour
    {
        public RC02ContentCatalog catalog;
        public RC02EncounterSheet encounters;
        public GameLoop loop;
        public int suppressed;
        readonly List<Live> live = new List<Live>(16);
        readonly Dictionary<string, float> nextTime = new Dictionary<string, float>();
        GameObject preview;
        bool paused;

        class Live
        {
            public GameObject go;
            public ParticleSystem[] particles;
            public string id;
            public RC02PausePolicy policy;
            public float life;
            public float duration;
            public bool frozen;
        }

        void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
        }

        void OnEnable()
        {
            if (loop != null) loop.PresentationDispatched += OnEvents;
        }

        void OnDisable()
        {
            if (loop != null) loop.PresentationDispatched -= OnEvents;
        }

        void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            if (catalog == null || events == null) return;
            for (int i = 0; i < events.Count; i++)
            {
                PresentationEvent item = events[i];
                if (item.Type == PresentationEventType.Restart)
                {
                    Clear();
                    continue;
                }
                if (!catalog.TrySelect("art", item.Type.ToString(), item.SourceId, out RC02CatalogEntry entry, out _))
                    continue;
                if (OwnedByOldView(item.Type))
                {
                    suppressed++;
                    continue;
                }
                if (IsPreviewOnly(entry)) continue;
                Spawn(entry, item);
            }
        }

        void LateUpdate()
        {
            if (loop == null || loop.Sim == null) return;
            bool nowPaused = loop.Sim.State.Phase == RunPhase.Paused;
            float dt = loop.Sim.LastDt;
            if (nowPaused && !paused) OnPause();
            if (!nowPaused && paused) OnResume();
            paused = nowPaused;
            if (paused) return;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                Live item = live[i];
                item.life += dt;
                if (item.life >= item.duration) Release(i);
            }
        }

        public GameObject Preview(RC02CatalogEntry entry)
        {
            if (entry == null) return null;
            ClearPreview();
            var go = MakeVisual(entry, Vector3.zero, Vector3.forward);
            go.name = "TEST_ONLY_CatalogPreview";
            go.hideFlags = HideFlags.DontSave;
            preview = go;
            return go;
        }

        public void ClearPreview()
        {
            if (preview == null) return;
            if (Application.isPlaying) Destroy(preview);
            else DestroyImmediate(preview);
            preview = null;
        }

        void Spawn(RC02CatalogEntry entry, PresentationEvent item)
        {
            int cap = Mathf.Max(1, entry.maxInstances);
            int active = 0;
            for (int i = 0; i < live.Count; i++) if (live[i].id == entry.id) active++;
            if (active >= cap) return;
            if (nextTime.TryGetValue(entry.id, out float ready) && item.Now < ready) return;
            nextTime[entry.id] = item.Now + Mathf.Max(0f, entry.minInterval);
            var direction = new Vector3(item.DirectionX, 0f, item.DirectionZ);
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            float life = Parameter(entry, "tailSeconds", item.Duration > 0f ? item.Duration : 0.6f);
            var go = MakeVisual(entry, new Vector3(item.X, 0.05f, item.Z), direction.normalized);
            float width = Parameter(entry, "width", item.Radius > 0f ? item.Radius : 0.4f);
            go.transform.localScale = Vector3.one * Mathf.Max(0.05f, width);
            live.Add(new Live
            {
                go = go, id = entry.id, policy = entry.pausePolicy,
                duration = Mathf.Max(0.05f, life),
                particles = go.GetComponentsInChildren<ParticleSystem>(true)
            });
        }

        GameObject MakeVisual(RC02CatalogEntry entry, Vector3 position, Vector3 forward)
        {
            UnityEngine.Object asset = First(entry, "prefab");
            GameObject go;
            if (asset is GameObject prefab)
                go = Instantiate(prefab, position, Quaternion.LookRotation(forward, Vector3.up));
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                RemoveCollider(go);
                var material = First(entry, "material") as Material;
                if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
                go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.up, forward));
            }
            go.name = "RC02_" + entry.id;
            return go;
        }

        static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider == null) return;
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }

        static UnityEngine.Object First(RC02CatalogEntry entry, string role)
        {
            if (entry.assets == null) return null;
            for (int i = 0; i < entry.assets.Length; i++)
                if (entry.assets[i] != null && entry.assets[i].role == role) return entry.assets[i].asset;
            return null;
        }

        static float Parameter(RC02CatalogEntry entry, string key, float fallback)
        {
            if (entry.parameters == null) return fallback;
            for (int i = 0; i < entry.parameters.Length; i++)
                if (entry.parameters[i].key == key) return entry.parameters[i].value;
            return fallback;
        }

        bool OwnedByOldView(PresentationEventType type)
        {
            if (type == PresentationEventType.Shot || type == PresentationEventType.EnemyHit || type == PresentationEventType.EnemyKilled)
                return Has("RF01FeedbackBridge");
            if (type == PresentationEventType.EchoHit || type == PresentationEventType.PropagationHit || type == PresentationEventType.EchoPropagationHit)
                return Has("RC01BattleView") || Has("M01PlaytestController");
            if (type == PresentationEventType.WeaponZone || type == PresentationEventType.WeaponOrbit)
                return Has("RC01BattleView");
            return false;
        }

        bool Has(string typeName)
        {
            var behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
                if (behaviours[i] != null && behaviours[i].GetType().Name == typeName) return true;
            return false;
        }

        static bool IsPreviewOnly(RC02CatalogEntry entry)
        {
            return entry.contentId == "W05" || entry.contentId == "W06";
        }

        void OnPause()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                if (live[i].policy == RC02PausePolicy.Stop) Release(i);
                else Freeze(live[i], true);
            }
        }

        void OnResume()
        {
            for (int i = 0; i < live.Count; i++) Freeze(live[i], false);
        }

        static void Freeze(Live item, bool freeze)
        {
            item.frozen = freeze;
            if (item.particles == null) return;
            for (int i = 0; i < item.particles.Length; i++)
            {
                if (item.particles[i] == null) continue;
                if (freeze) item.particles[i].Pause(true);
                else item.particles[i].Play(true);
            }
        }

        void Clear()
        {
            for (int i = live.Count - 1; i >= 0; i--) Release(i);
            nextTime.Clear();
        }

        void Release(int index)
        {
            if (live[index].go != null) Destroy(live[index].go);
            live.RemoveAt(index);
        }
    }
}
