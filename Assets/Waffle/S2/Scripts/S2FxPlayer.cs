using System.Collections.Generic;
using UnityEngine;

namespace Waffle.S2
{
    /// <summary>
    /// 样板补两样 VfxDirector 没有的表现：精英击杀环、拾取碎片。
    /// 不播第二套残影、炮口或命中碎片。只在 Playing 用 RunSimulation.LastDt 推进。
    /// </summary>
    public class S2FxPlayer : MonoBehaviour
    {
        public GameLoop loop;
        public S2MotionDriver motion;
        public S2FxProfile profile;
        public GameObject shardMaster;
        public ParticleSystem burstMaster;
        public Material lineMaterial;

        const int Pool = 6;

        sealed class Ring
        {
            public LineRenderer line;
            public float age = -1f;
        }

        sealed class Shard
        {
            public Transform transform;
            public Vector3 velocity;
            public float age = -1f;
        }

        readonly List<Ring> rings = new List<Ring>();
        readonly List<Shard> shards = new List<Shard>();
        readonly List<ParticleSystem> bursts = new List<ParticleSystem>();
        readonly List<float> burstAge = new List<float>();

        void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (motion == null) motion = GetComponent<S2MotionDriver>();
            for (int i = 0; i < Pool; i++)
            {
                var go = new GameObject("S2_Ring_" + i);
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.loop = true;
                line.positionCount = 28;
                line.useWorldSpace = false;
                line.widthMultiplier = 0.045f;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                if (lineMaterial != null) line.sharedMaterial = lineMaterial;
                line.enabled = false;
                rings.Add(new Ring { line = line });

                Transform shard = MakeShard(i);
                shards.Add(new Shard { transform = shard });
            }
            if (burstMaster != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    ParticleSystem ps = Instantiate(burstMaster, transform);
                    ps.gameObject.SetActive(false);
                    bursts.Add(ps);
                    burstAge.Add(-1f);
                }
            }
        }

        Transform MakeShard(int index)
        {
            GameObject go = shardMaster != null
                ? Instantiate(shardMaster, transform)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "S2_Shard_" + index;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(0.08f, 0.08f, 0.16f);
            go.SetActive(false);
            return go.transform;
        }

        void OnEnable()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (loop != null) loop.PresentationDispatched += OnEvents;
        }

        void OnDisable()
        {
            if (loop != null) loop.PresentationDispatched -= OnEvents;
            Clear();
        }

        void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                PresentationEvent e = events[i];
                if (e.Type == PresentationEventType.EnemyKilled && motion != null && motion.WasElite(e.Data))
                    PlayElite(new Vector3(e.X, 0.05f, e.Z));
                else if (e.Type == PresentationEventType.GemPickup)
                    PlayPickup(new Vector3(e.X, 0.35f, e.Z));
                else if (e.Type == PresentationEventType.Restart)
                    Clear();
            }
        }

        void Update()
        {
            bool playing = loop != null && loop.Sim != null && loop.Sim.State.Phase == RunPhase.Playing;
            float dt = playing ? loop.Sim.LastDt : 0f;
            float ringLife = profile != null ? profile.eliteRingSeconds : 0.42f;
            float ringRadius = profile != null ? profile.eliteRingRadius : 1.7f;
            float pickupLife = profile != null ? profile.pickupSeconds : 0.16f;
            Color elite = profile != null ? profile.eliteColor : new Color(1f, 0.27f, 0f, 1f);
            for (int i = 0; i < rings.Count; i++)
            {
                Ring ring = rings[i];
                if (ring.age < 0f) continue;
                if (dt > 0f) ring.age += dt;
                float k = ringLife <= 0.0001f ? 1f : ring.age / ringLife;
                if (k >= 1f) { ring.age = -1f; ring.line.enabled = false; continue; }
                float radius = Mathf.Lerp(0.35f, ringRadius, k);
                WriteCircle(ring.line, radius);
                Color c = elite;
                c.a = 1f - k;
                ring.line.startColor = ring.line.endColor = c;
                ring.line.widthMultiplier = Mathf.Lerp(0.06f, 0.01f, k);
            }
            for (int i = 0; i < shards.Count; i++)
            {
                Shard shard = shards[i];
                if (shard.age < 0f) continue;
                if (dt > 0f)
                {
                    shard.age += dt;
                    shard.transform.position += shard.velocity * dt;
                }
                if (shard.age >= pickupLife)
                {
                    shard.age = -1f;
                    shard.transform.gameObject.SetActive(false);
                }
            }
            for (int i = 0; i < bursts.Count; i++)
            {
                if (burstAge[i] < 0f) continue;
                if (dt > 0f)
                {
                    burstAge[i] += dt;
                    bursts[i].Simulate(dt, true, false);
                }
                if (burstAge[i] >= ringLife)
                {
                    burstAge[i] = -1f;
                    bursts[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    bursts[i].gameObject.SetActive(false);
                }
            }
        }

        void PlayElite(Vector3 position)
        {
            for (int i = 0; i < rings.Count; i++)
            {
                if (rings[i].age >= 0f) continue;
                rings[i].age = 0f;
                rings[i].line.transform.position = position;
                rings[i].line.enabled = true;
                WriteCircle(rings[i].line, 0.35f);
                break;
            }
            for (int i = 0; i < bursts.Count; i++)
            {
                if (burstAge[i] >= 0f) continue;
                ParticleSystem ps = bursts[i];
                ps.transform.position = position + Vector3.up * 0.4f;
                ps.gameObject.SetActive(true);
                ps.Simulate(0.0001f, true, true);
                burstAge[i] = 0f;
                break;
            }
        }

        void PlayPickup(Vector3 position)
        {
            int spawned = 0;
            for (int i = 0; i < shards.Count && spawned < 3; i++)
            {
                if (shards[i].age >= 0f) continue;
                float angle = spawned * 2.1f;
                shards[i].age = 0f;
                shards[i].transform.gameObject.SetActive(true);
                shards[i].transform.position = position;
                shards[i].velocity = new Vector3(Mathf.Cos(angle), 1.4f, Mathf.Sin(angle)) * 1.6f;
                spawned++;
            }
        }

        static void WriteCircle(LineRenderer line, float radius)
        {
            int n = line.positionCount;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        void Clear()
        {
            for (int i = 0; i < rings.Count; i++)
            {
                rings[i].age = -1f;
                if (rings[i].line != null) rings[i].line.enabled = false;
            }
            for (int i = 0; i < shards.Count; i++)
            {
                shards[i].age = -1f;
                if (shards[i].transform != null) shards[i].transform.gameObject.SetActive(false);
            }
            for (int i = 0; i < bursts.Count; i++)
            {
                burstAge[i] = -1f;
                bursts[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                bursts[i].gameObject.SetActive(false);
            }
        }
    }
}
