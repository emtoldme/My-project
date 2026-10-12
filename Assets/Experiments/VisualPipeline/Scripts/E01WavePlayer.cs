using System.Collections.Generic;
using UnityEngine;

namespace Waffle.Experiments.VisualPipeline
{
    /// <summary>
    /// TEST_ONLY。只读 Shot / EnemyHit，在地面放一个 Alpha Clip 网格环和少量粒子。
    /// 不改伤害、不缩放玩家根。只在 Playing 用 LastDt。暂停、升级、终局冻结；重开清空。
    /// 本场景不要再挂 VfxDirector，否则射击/命中会有两套表现。
    /// </summary>
    public class E01WavePlayer : MonoBehaviour
    {
        public GameLoop loop;
        public Mesh ringMesh;
        public Material ringMaterial;
        public Material sparkMaterial;
        public bool waveEnabled = true;
        public float quadMeters = 6f;
        public float ringLife = 0.32f;
        public float startRadius = 0.2f;
        public float endRadius = 2.2f;
        public float ringWidth = 0.09f;
        public float sparkLife = 0.18f;

        public int ActiveRings { get; private set; }
        public int ActiveSparks { get; private set; }
        public int SpawnCount { get; private set; }

        const int RingPool = 6;
        const int SparkPool = 18;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int WidthId = Shader.PropertyToID("_Width");
        static readonly int QuadId = Shader.PropertyToID("_QuadMeters");
        static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        sealed class Ring
        {
            public GameObject go;
            public Renderer renderer;
            public MaterialPropertyBlock block;
            public float age = -1f;
            public Color color;
        }

        sealed class Spark
        {
            public Transform transform;
            public Vector3 velocity;
            public float age = -1f;
        }

        readonly List<Ring> rings = new List<Ring>();
        readonly List<Spark> sparks = new List<Spark>();
        readonly MaterialPropertyBlock sparkBlock = new MaterialPropertyBlock();

        void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (ringMesh == null) ringMesh = BuildQuad(quadMeters);
            for (int i = 0; i < RingPool; i++)
            {
                var go = new GameObject("E01_Wave_" + i);
                go.transform.SetParent(transform, false);
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = ringMesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = ringMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                go.SetActive(false);
                rings.Add(new Ring { go = go, renderer = renderer, block = new MaterialPropertyBlock() });
            }
            for (int i = 0; i < SparkPool; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "E01_Spark_" + i;
                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(0.06f, 0.06f, 0.12f);
                var renderer = go.GetComponent<Renderer>();
                if (sparkMaterial != null) renderer.sharedMaterial = sparkMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(false);
                sparks.Add(new Spark { transform = go.transform });
            }
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
            if (!waveEnabled) return;
            for (int i = 0; i < events.Count; i++)
            {
                PresentationEvent e = events[i];
                if (e.Type == PresentationEventType.Shot)
                    Spawn(new Vector3(e.X, 0.05f, e.Z), new Color(0.35f, 0.9f, 1f, 1f));
                else if (e.Type == PresentationEventType.EnemyHit)
                    Spawn(new Vector3(e.X, 0.05f, e.Z), new Color(1f, 0.45f, 0.72f, 1f));
                else if (e.Type == PresentationEventType.Restart)
                    Clear();
            }
        }

        void Update()
        {
            bool playing = loop != null && loop.Sim != null && loop.Sim.State.Phase == RunPhase.Playing;
            float dt = playing ? loop.Sim.LastDt : 0f;
            ActiveRings = 0;
            ActiveSparks = 0;
            for (int i = 0; i < rings.Count; i++)
            {
                Ring ring = rings[i];
                if (ring.age < 0f) continue;
                if (!waveEnabled) { HideRing(ring); continue; }
                if (dt > 0f) ring.age += dt;
                float k = ringLife <= 0.0001f ? 1f : ring.age / ringLife;
                if (k >= 1f) { HideRing(ring); continue; }
                float radius = Mathf.Lerp(startRadius, endRadius, k);
                ring.block.Clear();
                ring.block.SetColor(BaseColorId, ring.color);
                ring.block.SetColor(ColorId, ring.color);
                ring.block.SetFloat(RadiusId, radius);
                ring.block.SetFloat(WidthId, ringWidth);
                ring.block.SetFloat(QuadId, quadMeters);
                ring.block.SetFloat(CutoffId, 0.5f);
                ring.renderer.SetPropertyBlock(ring.block);
                ActiveRings++;
            }
            for (int i = 0; i < sparks.Count; i++)
            {
                Spark spark = sparks[i];
                if (spark.age < 0f) continue;
                if (!waveEnabled) { HideSpark(spark); continue; }
                if (dt > 0f)
                {
                    spark.age += dt;
                    spark.transform.position += spark.velocity * dt;
                }
                if (spark.age >= sparkLife) HideSpark(spark);
                else ActiveSparks++;
            }
        }

        void OnGUI()
        {
            ImguiScale.Apply();
            Rect box = ImguiScale.Fit(new Rect(16f, ImguiScale.Height - 78f, 360f, 62f));
            GUILayout.BeginArea(box, GUI.skin.box);
            GUILayout.Label("TEST_ONLY 声波  环 " + ActiveRings + "  粒 " + ActiveSparks + "  累计 " + SpawnCount);
            if (GUILayout.Button(waveEnabled ? "声波 关（同机位对照）" : "声波 开")) waveEnabled = !waveEnabled;
            GUILayout.EndArea();
        }

        void Spawn(Vector3 position, Color color)
        {
            for (int i = 0; i < rings.Count; i++)
            {
                if (rings[i].age >= 0f) continue;
                rings[i].age = 0f;
                rings[i].color = color;
                rings[i].go.transform.position = position;
                rings[i].go.transform.rotation = Quaternion.identity;
                rings[i].go.transform.localScale = Vector3.one;
                rings[i].go.SetActive(true);
                SpawnCount++;
                break;
            }
            int spawned = 0;
            for (int i = 0; i < sparks.Count && spawned < 3; i++)
            {
                if (sparks[i].age >= 0f) continue;
                float angle = spawned * 2.2f;
                sparks[i].age = 0f;
                sparks[i].transform.gameObject.SetActive(true);
                sparks[i].transform.position = position + Vector3.up * 0.2f;
                sparks[i].velocity = new Vector3(Mathf.Cos(angle), 1.2f, Mathf.Sin(angle)) * 2.2f;
                var renderer = sparks[i].transform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    sparkBlock.Clear();
                    sparkBlock.SetColor(BaseColorId, color);
                    sparkBlock.SetColor(ColorId, color);
                    renderer.SetPropertyBlock(sparkBlock);
                }
                spawned++;
            }
        }

        void HideRing(Ring ring)
        {
            ring.age = -1f;
            if (ring.go != null) ring.go.SetActive(false);
        }

        void HideSpark(Spark spark)
        {
            spark.age = -1f;
            if (spark.transform != null) spark.transform.gameObject.SetActive(false);
        }

        void Clear()
        {
            for (int i = 0; i < rings.Count; i++) HideRing(rings[i]);
            for (int i = 0; i < sparks.Count; i++) HideSpark(sparks[i]);
            ActiveRings = 0;
            ActiveSparks = 0;
        }

        public static Mesh BuildQuad(float meters)
        {
            float h = meters * 0.5f;
            var mesh = new Mesh { name = "E01_WaveQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-h, 0f, -h), new Vector3(h, 0f, -h),
                new Vector3(-h, 0f, h), new Vector3(h, 0f, h)
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
