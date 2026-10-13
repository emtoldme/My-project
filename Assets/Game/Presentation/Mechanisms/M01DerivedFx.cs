using System.Collections.Generic;
using UnityEngine;

namespace Waffle.Experiments.Mechanism.Presentation
{
    /// <summary>
    /// TEST_ONLY。回声预备是身上的紫红标记，命中是收束脉冲；传播是金色连线和落点。
    /// 标记只跟随 SyncEchoCues。伤害标签只跟随带伤害参数的 PlayEcho / PlayPropagation。
    /// 不读 EnemyHit，不扣血，也不自己累计 0.5 秒。暂停时标签和脉冲不加 LastDt。
    /// </summary>
    public class M01DerivedFx : MonoBehaviour
    {
        public GameLoop loop;
        public Material lineMaterial;
        public int echoCapacity = 6;
        public int linkCapacity = 6;
        public int cueCapacity = 8;
        public int labelCapacity = 6;
        public float cueHeight = 0.95f;
        public float cueOpenRadius = 0.58f;
        public float cueClosedRadius = 0.18f;
        public float pulseHeight = 0.95f;
        public float pulseOpenRadius = 0.5f;
        public float pulseClosedRadius = 0.1f;
        public float pulseLife = 0.24f;
        public float labelLife = 0.5f;
        public float labelHeight = 1.55f;
        public float labelCharacterSize = 0.07f;

        public int ActiveEchoes { get; private set; }
        public int ActiveLinks { get; private set; }
        public int ActiveCues { get; private set; }
        public bool CueSyncSeen { get; private set; }

        const float LinkLife = 0.16f;
        static readonly Color EchoColor = new Color(0.88f, 0.14f, 0.76f, 1f);
        static readonly Color LinkColor = new Color(1f, 0.84f, 0.18f, 1f);

        sealed class Pulse
        {
            public GameObject go;
            public LineRenderer line;
            public float age = -1f;
        }

        sealed class Link
        {
            public LineRenderer line;
            public float age = -1f;
            public Vector3 from, to;
        }

        sealed class Cue
        {
            public GameObject go;
            public LineRenderer line;
            public int serial;
            public int generation;
            public bool active;
            public int seen;
            public float progress;
        }

        sealed class Tag
        {
            public Transform transform;
            public TextMesh text;
            public float age = -1f;
        }

        readonly List<Pulse> echoes = new List<Pulse>();
        readonly List<Pulse> impacts = new List<Pulse>();
        readonly List<Link> links = new List<Link>();
        readonly List<Cue> cues = new List<Cue>();
        readonly List<Tag> tags = new List<Tag>();
        Material echoMaterial;
        Material linkMaterial;
        Font font;
        bool ownsFont;
        int syncSerial;

        void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            echoMaterial = Tint(EchoColor);
            linkMaterial = Tint(LinkColor);
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial Unicode MS" }, 32);
            ownsFont = font != null;
            if (font != null) font.RequestCharactersInTexture("回声传播 -0123456789", 32, FontStyle.Normal);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            for (int i = 0; i < echoCapacity; i++) echoes.Add(CreatePulse("M01_Echo_", i, echoMaterial, 0.045f));
            for (int i = 0; i < linkCapacity; i++)
            {
                impacts.Add(CreatePulse("M01_Impact_", i, linkMaterial, 0.04f));
                links.Add(CreateLink(i));
            }
            for (int i = 0; i < cueCapacity; i++) cues.Add(CreateCue(i));
            for (int i = 0; i < labelCapacity; i++) tags.Add(CreateTag(i));
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
            if (ownsFont && font != null) Destroy(font);
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
            Quaternion facing = Facing();
            ActiveEchoes = 0;
            ActiveLinks = 0;
            ActiveEchoes = TickPulse(echoes, dt, pulseOpenRadius, pulseClosedRadius);
            TickPulse(impacts, dt, 0.28f, 0.08f);
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
            for (int i = 0; i < tags.Count; i++)
            {
                Tag tag = tags[i];
                if (tag.age < 0f) continue;
                if (dt > 0f) tag.age += dt;
                if (tag.age >= labelLife) { HideTag(tag); continue; }
                tag.transform.rotation = facing;
                Color color = tag.text.color;
                color.a = 1f - tag.age / labelLife;
                tag.text.color = color;
            }
        }

        int TickPulse(List<Pulse> pulses, float dt, float openRadius, float closedRadius)
        {
            int active = 0;
            for (int i = 0; i < pulses.Count; i++)
            {
                Pulse pulse = pulses[i];
                if (pulse.age < 0f) continue;
                if (dt > 0f) pulse.age += dt;
                float k = pulseLife <= 0.0001f ? 1f : pulse.age / pulseLife;
                if (k >= 1f) { HidePulse(pulse); continue; }
                WriteDiamond(pulse.line, Mathf.Lerp(openRadius, closedRadius, k));
                SetAlpha(pulse.line, 1f - k);
                active++;
            }
            return active;
        }

        public void SyncEchoCues(IReadOnlyList<M01EchoCue> cuesIn)
        {
            CueSyncSeen = true;
            if (syncSerial == int.MaxValue)
            {
                syncSerial = 0;
                for (int i = 0; i < cues.Count; i++) cues[i].seen = 0;
            }
            syncSerial++;
            int count = cuesIn == null ? 0 : cuesIn.Count;
            for (int i = 0; i < count; i++)
            {
                M01EchoCue cue = cuesIn[i];
                if (!Finite(cue.X) || !Finite(cue.Z) || !Finite(cue.Progress)) continue;
                int slot = FindCue(cue.Serial, cue.Generation);
                if (slot >= 0 && cues[slot].seen == syncSerial) continue;
                if (slot < 0) slot = FreeCue();
                if (slot < 0) continue;
                Cue marker = cues[slot];
                marker.serial = cue.Serial;
                marker.generation = cue.Generation;
                marker.active = true;
                marker.seen = syncSerial;
                marker.progress = Mathf.Clamp01(cue.Progress);
                marker.go.transform.position = new Vector3(cue.X, cueHeight, cue.Z);
                marker.go.SetActive(true);
                WriteDiamond(marker.line, Mathf.Lerp(cueOpenRadius, cueClosedRadius, marker.progress));
                SetAlpha(marker.line, 0.45f + 0.55f * marker.progress);
            }
            ActiveCues = 0;
            for (int i = 0; i < cues.Count; i++)
            {
                if (!cues[i].active) continue;
                if (cues[i].seen != syncSerial) HideCue(cues[i]);
                else ActiveCues++;
            }
        }

        public void PlayEcho(float x, float z)
        {
            PlayPulse(echoes, x, z, pulseHeight, pulseOpenRadius);
        }

        public void PlayEcho(float x, float z, float damage)
        {
            PlayEcho(x, z);
            if (!Finite(x) || !Finite(z) || !Finite(damage)) return;
            ShowLabel(x, z, "回声 " + FormatAmount(damage), EchoColor);
        }

        public void PlayPropagation(float fromX, float fromZ, float toX, float toZ)
        {
            if (links.Count == 0 || !Finite(fromX) || !Finite(fromZ) || !Finite(toX) || !Finite(toZ)) return;
            float dx = toX - fromX;
            float dz = toZ - fromZ;
            if (dx * dx + dz * dz < 0.05f * 0.05f) return;
            Link link = links[Pick(links, item => item.age)];
            link.age = 0f;
            link.from = new Vector3(fromX, pulseHeight, fromZ);
            link.to = new Vector3(toX, pulseHeight, toZ);
            link.line.SetPosition(0, link.from);
            link.line.SetPosition(1, link.from);
            SetAlpha(link.line, 1f);
            link.line.gameObject.SetActive(true);
            PlayPulse(impacts, toX, toZ, pulseHeight, 0.28f);
        }

        public void PlayPropagation(float fromX, float fromZ, float toX, float toZ, float damage)
        {
            PlayPropagation(fromX, fromZ, toX, toZ);
            if (!Finite(fromX) || !Finite(fromZ) || !Finite(toX) || !Finite(toZ) || !Finite(damage)) return;
            float dx = toX - fromX;
            float dz = toZ - fromZ;
            if (dx * dx + dz * dz < 0.05f * 0.05f) return;
            ShowLabel(toX, toZ, "传播 " + FormatAmount(damage), LinkColor);
        }

        public void Clear()
        {
            for (int i = 0; i < echoes.Count; i++) HidePulse(echoes[i]);
            for (int i = 0; i < impacts.Count; i++) HidePulse(impacts[i]);
            for (int i = 0; i < links.Count; i++) HideLink(links[i]);
            for (int i = 0; i < cues.Count; i++) HideCue(cues[i]);
            for (int i = 0; i < tags.Count; i++) HideTag(tags[i]);
            ActiveEchoes = 0;
            ActiveLinks = 0;
            ActiveCues = 0;
            CueSyncSeen = false;
        }

        void PlayPulse(List<Pulse> pulses, float x, float z, float height, float radius)
        {
            if (pulses.Count == 0 || !Finite(x) || !Finite(z)) return;
            Pulse pulse = pulses[Pick(pulses, item => item.age)];
            pulse.age = 0f;
            pulse.go.transform.position = new Vector3(x, height, z);
            pulse.go.SetActive(true);
            WriteDiamond(pulse.line, radius);
            SetAlpha(pulse.line, 1f);
        }

        void ShowLabel(float x, float z, string text, Color color)
        {
            if (font == null || tags.Count == 0) return;
            Tag tag = tags[Pick(tags, item => item.age)];
            tag.age = 0f;
            tag.transform.position = new Vector3(x, labelHeight, z);
            tag.transform.rotation = Facing();
            tag.text.text = text;
            tag.text.color = color;
            tag.transform.gameObject.SetActive(true);
        }

        Quaternion Facing()
        {
            Camera cam = loop != null ? loop.gameplayCamera : null;
            return cam != null ? cam.transform.rotation : Quaternion.Euler(55f, 45f, 0f);
        }

        Pulse CreatePulse(string prefix, int index, Material material, float width)
        {
            var go = new GameObject(prefix + index);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            Prepare(line, width, material);
            go.SetActive(false);
            return new Pulse { go = go, line = line };
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

        Cue CreateCue(int index)
        {
            var go = new GameObject("M01_Cue_" + index);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            Prepare(line, 0.035f, echoMaterial);
            go.SetActive(false);
            return new Cue { go = go, line = line };
        }

        Tag CreateTag(int index)
        {
            var go = new GameObject("M01_Tag_" + index);
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMesh>();
            text.font = font;
            text.fontSize = 32;
            text.characterSize = labelCharacterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            var renderer = text.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.SetActive(false);
            return new Tag { transform = go.transform, text = text };
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

        static void WriteDiamond(LineRenderer line, float radius)
        {
            line.positionCount = 5;
            line.SetPosition(0, new Vector3(radius, 0f, 0f));
            line.SetPosition(1, new Vector3(0f, 0f, radius));
            line.SetPosition(2, new Vector3(-radius, 0f, 0f));
            line.SetPosition(3, new Vector3(0f, 0f, -radius));
            line.SetPosition(4, new Vector3(radius, 0f, 0f));
        }

        Material Tint(Color color)
        {
            return CreateTransparentCopy(lineMaterial, color);
        }

        public static Material CreateTransparentCopy(Material source, Color color)
        {
            if (source == null) return null;
            var copy = new Material(source) { name = "M01_" + color.r.ToString("F2") + "_Runtime" };
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
        {
            var color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            line.startColor = color;
            line.endColor = color;
        }

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

        int FindCue(int serial, int generation)
        {
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].active && cues[i].serial == serial && cues[i].generation == generation) return i;
            }
            return -1;
        }

        int FreeCue()
        {
            for (int i = 0; i < cues.Count; i++)
                if (!cues[i].active) return i;
            return -1;
        }

        static void HidePulse(Pulse pulse)
        {
            pulse.age = -1f;
            if (pulse.go != null) pulse.go.SetActive(false);
        }

        static void HideLink(Link link)
        {
            link.age = -1f;
            if (link.line != null) link.line.gameObject.SetActive(false);
        }

        static void HideCue(Cue cue)
        {
            cue.active = false;
            cue.seen = 0;
            if (cue.go != null) cue.go.SetActive(false);
        }

        static void HideTag(Tag tag)
        {
            tag.age = -1f;
            if (tag.transform != null) tag.transform.gameObject.SetActive(false);
        }

        static string FormatAmount(float damage)
        {
            float shown = Mathf.Abs(damage);
            bool whole = Mathf.Abs(shown - Mathf.Round(shown)) < 0.05f;
            return "-" + (whole ? Mathf.RoundToInt(shown).ToString() : shown.ToString("0.0"));
        }

        static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
