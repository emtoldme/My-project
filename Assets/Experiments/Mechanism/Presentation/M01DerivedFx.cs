using System.Collections.Generic;
using UnityEngine;

namespace Waffle.Experiments.Mechanism.Presentation
{
    /// <summary>
    /// TEST_ONLY 只读表现。回声是双线环，传播是两点短线。
    /// 只在 Codex 调用 PlayEcho / PlayPropagation 时出现。不订阅 EnemyHit 或 Shot，也不挂 E01 波纹。
    /// 线段寿命不是规则延迟。暂停时不加 LastDt。
    /// </summary>
    public class M01DerivedFx : MonoBehaviour
    {
        public GameLoop loop;
        public Material lineMaterial;
        public int echoCapacity = 6;
        public int linkCapacity = 6;

        public int ActiveEchoes { get; private set; }
        public int ActiveLinks { get; private set; }

        const float EchoLife = 0.28f;
        const float LinkLife = 0.16f;
        const int Segments = 28;
        static readonly Color EchoColor = new Color(0.88f, 0.14f, 0.76f, 1f);
        static readonly Color LinkColor = new Color(1f, 0.84f, 0.18f, 1f);

        sealed class Echo
        {
            public GameObject go;
            public LineRenderer inner;
            public LineRenderer outer;
            public float age = -1f;
        }

        sealed class Link
        {
            public LineRenderer line;
            public float age = -1f;
            public Vector3 from, to;
        }

        readonly List<Echo> echoes = new List<Echo>();
        readonly List<Link> links = new List<Link>();
        Material echoMaterial;
        Material linkMaterial;

        void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            echoMaterial = Tint(EchoColor);
            linkMaterial = Tint(LinkColor);
            for (int i = 0; i < echoCapacity; i++) echoes.Add(CreateEcho(i));
            for (int i = 0; i < linkCapacity; i++) links.Add(CreateLink(i));
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

        void OnDestroy()
        {
            if (echoMaterial != null) Destroy(echoMaterial);
            if (linkMaterial != null) Destroy(linkMaterial);
        }

        void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Type == PresentationEventType.Restart) Clear();
            }
        }

        void Update()
        {
            bool playing = loop != null && loop.Sim != null && loop.Sim.State.Phase == RunPhase.Playing;
            float dt = playing ? loop.Sim.LastDt : 0f;
            ActiveEchoes = 0;
            ActiveLinks = 0;
            for (int i = 0; i < echoes.Count; i++)
            {
                Echo echo = echoes[i];
                if (echo.age < 0f) continue;
                if (dt > 0f) echo.age += dt;
                float k = EchoLife <= 0.0001f ? 1f : echo.age / EchoLife;
                if (k >= 1f) { HideEcho(echo); continue; }
                WriteCircle(echo.inner, Mathf.Lerp(0.28f, 1.15f, k));
                WriteCircle(echo.outer, Mathf.Lerp(0.46f, 1.75f, k));
                SetAlpha(echo.inner, 1f - k); SetAlpha(echo.outer, 1f - k);
                ActiveEchoes++;
            }
            for (int i = 0; i < links.Count; i++)
            {
                Link link = links[i];
                if (link.age < 0f) continue;
                if (dt > 0f) link.age += dt;
                if (link.age >= LinkLife) { HideLink(link); continue; }
                float progress = Mathf.Clamp01(link.age / 0.05f);
                link.line.SetPosition(1, Vector3.Lerp(link.from, link.to, progress));
                SetAlpha(link.line, 1f - link.age / LinkLife);
                ActiveLinks++;
            }
        }

        public void PlayEcho(float x, float z)
        {
            if (echoes.Count == 0 || !Finite(x) || !Finite(z)) return;
            Echo echo = echoes[Pick(echoes, e => e.age)];
            echo.age = 0f;
            echo.go.transform.position = new Vector3(x, 0.08f, z);
            echo.go.SetActive(true);
            WriteCircle(echo.inner, 0.28f);
            WriteCircle(echo.outer, 0.46f);
            SetAlpha(echo.inner, 1f); SetAlpha(echo.outer, 1f);
        }

        public void PlayPropagation(float fromX, float fromZ, float toX, float toZ)
        {
            if (links.Count == 0 || !Finite(fromX) || !Finite(fromZ) || !Finite(toX) || !Finite(toZ)) return;
            float dx = toX - fromX;
            float dz = toZ - fromZ;
            if (dx * dx + dz * dz < 0.05f * 0.05f) return;
            Link link = links[Pick(links, item => item.age)];
            link.age = 0f;
            link.from = new Vector3(fromX, 0.12f, fromZ); link.to = new Vector3(toX, 0.12f, toZ);
            link.line.SetPosition(0, link.from);
            link.line.SetPosition(1, link.from);
            SetAlpha(link.line, 1f);
            link.line.gameObject.SetActive(true);
        }

        public void Clear()
        {
            for (int i = 0; i < echoes.Count; i++) HideEcho(echoes[i]);
            for (int i = 0; i < links.Count; i++) HideLink(links[i]);
            ActiveEchoes = 0;
            ActiveLinks = 0;
        }

        Echo CreateEcho(int index)
        {
            var go = new GameObject("M01_Echo_" + index);
            go.transform.SetParent(transform, false);
            var echo = new Echo
            {
                go = go,
                inner = AddRing(go, "Inner", 0.04f, echoMaterial),
                outer = AddRing(go, "Outer", 0.055f, echoMaterial)
            };
            go.SetActive(false);
            return echo;
        }

        Link CreateLink(int index)
        {
            var go = new GameObject("M01_Link_" + index);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            Prepare(line, 0.07f, linkMaterial);
            line.useWorldSpace = true;
            line.positionCount = 2;
            go.SetActive(false);
            return new Link { line = line };
        }

        static LineRenderer AddRing(GameObject parent, string name, float width, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var line = go.AddComponent<LineRenderer>();
            Prepare(line, width, material);
            line.loop = false;
            return line;
        }

        static void Prepare(LineRenderer line, float width, Material material)
        {
            line.useWorldSpace = false;
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 0;
        }

        static void WriteCircle(LineRenderer line, float radius)
        {
            line.positionCount = Segments + 1;
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        Material Tint(Color color)
        {
            if (lineMaterial == null) return null;
            var copy = new Material(lineMaterial) { name = "M01_" + color.r.ToString("F2") + "_Runtime" };
            if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", color);
            if (copy.HasProperty("_Color")) copy.SetColor("_Color", color);
            copy.SetFloat("_Surface", 1f);
            copy.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            copy.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            copy.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            copy.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            copy.SetFloat("_ZWrite", 0f);
            copy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            copy.SetOverrideTag("RenderType", "Transparent");
            copy.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return copy;
        }

        static void SetAlpha(LineRenderer line, float alpha)
        { var color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)); line.startColor = color; line.endColor = color; }

        static int Pick<T>(List<T> items, System.Func<T, float> ageOf)
        {
            int free = -1;
            int oldest = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (ageOf(items[i]) < 0f) { free = i; break; }
                if (ageOf(items[i]) > ageOf(items[oldest])) oldest = i;
            }
            return free >= 0 ? free : oldest;
        }

        static void HideEcho(Echo echo)
        {
            echo.age = -1f;
            if (echo.go != null) echo.go.SetActive(false);
        }

        static void HideLink(Link link)
        {
            link.age = -1f;
            if (link.line != null) link.line.gameObject.SetActive(false);
        }

        static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
