using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ReverbInTheRye.VFX
{
    /// <summary>
    /// Prototype-only sonic feedback for a Unity 6 URP 3D, top-down game.
    /// Owns NO gameplay state: damage, knockback, hit detection and death are external.
    /// Assign two URP materials in the Inspector before entering Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SonicAttackVFX : MonoBehaviour
    {
        [Header("Assign URP materials with the two supplied shaders")]
        [SerializeField] private Material resonancePulseMaterial;
        [SerializeField] private Material strawDustMaterial;

        [Header("Scene scale (one Unity unit = approximately one metre)")]
        [SerializeField, Min(0.1f)] private float visualScale = 1f;

        [Header("Pooling caps (recycle oldest effect when full)")]
        [SerializeField, Range(1, 64)] private int maxFireInstances = 20;
        [SerializeField, Range(1, 64)] private int maxHitInstances = 24;
        [SerializeField, Range(1, 64)] private int maxDeathInstances = 12;
        [SerializeField] private bool externallyDriven;
        [SerializeField] private bool compactDirectHits;
        public int PoolSize => instances.Count;
        public int FireCount { get; private set; }
        public int HitCount { get; private set; }
        public int DeathCount { get; private set; }
        public float VisualTime { get; private set; }
        public int ActiveEffects { get { int count = 0; foreach (var item in instances) if (item.active) count++; return count; } }

        private enum Kind { Fire, Hit, Death }

        private sealed class Ring
        {
            public Transform transform;
            public MeshRenderer renderer;
            public MaterialPropertyBlock properties;
            public float delay;
            public float duration;
            public float initialSize;
            public float finalSize;
            public float peakOpacity;
        }

        private sealed class Effect
        {
            public Kind kind;
            public GameObject root;
            public Ring[] rings;
            public ParticleSystem particles;
            public float elapsed;
            public float duration;
            public bool active;
        }

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int WidthId = Shader.PropertyToID("_RingWidth");
        private static readonly int CoreMixId = Shader.PropertyToID("_CoreMix");
        private static readonly int DistortionId = Shader.PropertyToID("_Distortion");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");
        private static readonly int VisualTimeId = Shader.PropertyToID("_VisualTime");

        private readonly List<Effect> instances = new List<Effect>(64);
        private Mesh quadMesh;
        private bool missingMaterialsLogged;

        private static readonly Color SignalCyan = Html("#77D9D4");
        private static readonly Color SignalPurple = Html("#9780C2");
        private static readonly Color ImpactMagenta = Html("#B863A0");
        private static readonly Color WarmFlash = Html("#F1E6C7");
        private static readonly Color StrawGold = Html("#B89565");

        private static Color Html(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color value);
            return value;
        }

        private void Awake()
        {
            quadMesh = BuildQuad();
            if (!EnsureMaterials()) return;
            Prewarm(Kind.Fire, maxFireInstances);
            Prewarm(Kind.Hit, maxHitInstances);
            Prewarm(Kind.Death, maxDeathInstances);
        }

        private void Prewarm(Kind kind, int count)
        {
            for (int i = 0; i < Mathf.Clamp(count, 1, 64); i++) instances.Add(CreateEffect(kind));
        }

        private bool EnsureMaterials()
        {
            if (resonancePulseMaterial != null && strawDustMaterial != null) return true;
            if (!missingMaterialsLogged)
            {
                Debug.LogError("SonicAttackVFX: assign ResonancePulseURP and StrawDustURP materials to the component.", this);
                missingMaterialsLogged = true;
            }
            return false;
        }

        /// <summary>Call once when a sound-wave shot really fires. World direction must be nonzero.</summary>
        public void PlayFire(Vector3 muzzleWorldPosition, Vector3 aimWorldDirection)
        {
            Spawn(Kind.Fire, muzzleWorldPosition, aimWorldDirection);
        }

        /// <summary>Call when a damageable target registers a confirmed hit, not on every trigger overlap.</summary>
        public void PlayHit(Vector3 hitWorldPosition)
        {
            Spawn(Kind.Hit, hitWorldPosition, Vector3.up);
        }

        /// <summary>Call exactly once on confirmed death, before destroying/deactivating the enemy.</summary>
        public void PlayDeath(Vector3 deathWorldPosition)
        {
            Spawn(Kind.Death, deathWorldPosition, Vector3.up);
        }

        // Play Mode inspector previews; does not assume Old or New Input System.
        [ContextMenu("Preview / Fire At This Object")]
        private void PreviewFire() => PlayFire(transform.position + Vector3.up * 0.25f, transform.forward);

        [ContextMenu("Preview / Hit Two Metres Ahead")]
        private void PreviewHit() => PlayHit(transform.position + transform.forward * 2f + Vector3.up * 0.25f);

        [ContextMenu("Preview / Death Two Metres Ahead")]
        private void PreviewDeath() => PlayDeath(transform.position + transform.forward * 2f + Vector3.up * 0.25f);

        private void Spawn(Kind kind, Vector3 position, Vector3 direction)
        {
            if (!EnsureMaterials()) return;

            Effect effect = Acquire(kind);
            if (effect == null) return;
            if (kind == Kind.Fire) FireCount++; else if (kind == Kind.Hit) HitCount++; else DeathCount++;
            effect.root.transform.position = position;
            effect.root.transform.rotation = Quaternion.identity;
            effect.root.transform.localScale = Vector3.one * visualScale;
            effect.root.SetActive(true);
            effect.active = true;
            effect.elapsed = 0f;

            Vector3 particleForward = kind == Kind.Fire ? direction : Vector3.up;
            if (particleForward.sqrMagnitude < 0.0001f) particleForward = Vector3.forward;
            effect.particles.transform.rotation = Quaternion.LookRotation(
                particleForward.normalized,
                Mathf.Abs(Vector3.Dot(particleForward.normalized, Vector3.up)) > 0.99f
                    ? Vector3.forward : Vector3.up);
            effect.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Manual simulation keeps particles on the same clock as the rings, including pauses.
            effect.particles.Emit(kind == Kind.Fire ? 7 : kind == Kind.Hit ? 11 : 23);
            effect.particles.Pause(true);
            TickRings(effect);
        }

        private Effect Acquire(Kind kind)
        {
            Effect oldest = null;
            foreach (Effect item in instances)
            {
                if (item.kind != kind) continue;
                if (!item.active) return item;
                if (oldest == null || item.elapsed > oldest.elapsed) oldest = item;
            }

            if (oldest != null)
            {
                oldest.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return oldest;
            }

            return null; // Never allocate during combat, even if configuration is edited at runtime.
        }

        private Effect CreateEffect(Kind kind)
        {
            var result = new Effect
            {
                kind = kind,
                root = new GameObject("VFX_Sonic_" + kind),
                duration = kind == Kind.Fire ? 0.4f : kind == Kind.Hit ? 0.7f : 1.05f
            };
            result.root.transform.SetParent(transform, false);

            var rings = new List<Ring>(4);
            if (kind == Kind.Fire)
            {
                rings.Add(MakeRing(result.root.transform, "MuzzleWhite", 0.00f, 0.10f, 0.24f, 0.7f, 0.95f, WarmFlash, SignalCyan, true, 0.0f, 0.07f, 0.02f));
                rings.Add(MakeRing(result.root.transform, "PrimaryCyan", 0.00f, 0.18f, 0.42f, 2.1f, 0.88f, SignalCyan, SignalPurple, false, 0.025f, 0.041f, 0.04f));
                rings.Add(MakeRing(result.root.transform, "SecondaryPurple", 0.047f, 0.18f, 0.35f, 1.65f, 0.42f, SignalPurple, SignalCyan, false, 0.040f, 0.032f, 0.055f));
            }
            else if (kind == Kind.Hit && compactDirectHits)
            {
                // Warm, small first impact. Magenta is reserved for M01's delayed echo feedback.
                rings.Add(MakeRing(result.root.transform, "DirectWarmFlash", 0f, .10f, .12f, .55f, .9f, WarmFlash, StrawGold, true, 0f, .04f, .02f));
                rings.Add(MakeRing(result.root.transform, "DirectDustEdge", .01f, .13f, .18f, .68f, .35f, StrawGold, WarmFlash, false, .01f, .025f, .03f));
            }
            else if (kind == Kind.Hit)
            {
                rings.Add(MakeRing(result.root.transform, "ImpactFlash", 0f, 0.11f, 0.22f, 0.85f, 1.1f, WarmFlash, ImpactMagenta, true, 0.0f, 0.06f, 0.03f));
                rings.Add(MakeRing(result.root.transform, "ImpactResonance", 0.012f, 0.23f, 0.4f, 2.6f, 0.8f, ImpactMagenta, SignalPurple, false, 0.035f, 0.047f, 0.038f));
                rings.Add(MakeRing(result.root.transform, "ImpactEcho", 0.07f, 0.25f, 0.48f, 1.8f, 0.34f, SignalPurple, SignalCyan, false, 0.045f, 0.028f, 0.049f));
            }
            else
            {
                rings.Add(MakeRing(result.root.transform, "SignalCore", 0.045f, 0.24f, 0.30f, 1.1f, 0.85f, WarmFlash, ImpactMagenta, true, 0.0f, 0.055f, 0.03f));
                rings.Add(MakeRing(result.root.transform, "SignalFractureA", 0.015f, 0.34f, 0.63f, 2.8f, 0.73f, ImpactMagenta, SignalPurple, false, 0.055f, 0.040f, 0.06f));
                rings.Add(MakeRing(result.root.transform, "SignalFractureB", 0.135f, 0.43f, 0.53f, 3.4f, 0.51f, SignalCyan, SignalPurple, false, 0.070f, 0.028f, 0.08f));
                rings.Add(MakeRing(result.root.transform, "FinalGhost", 0.235f, 0.48f, 0.57f, 2.3f, 0.28f, SignalPurple, ImpactMagenta, false, 0.075f, 0.024f, 0.10f));
            }

            result.rings = rings.ToArray();
            result.particles = MakeParticles(result.root.transform, kind);
            result.root.SetActive(false);
            return result;
        }

        private Ring MakeRing(Transform parent, string name, float delay, float duration,
            float initialSize, float finalSize, float peak, Color color, Color edge,
            bool core, float distortion, float width, float phase)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.015f + phase * 0.15f, 0f);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = quadMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = resonancePulseMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = core ? 2 : 1;

            var properties = new MaterialPropertyBlock();
            properties.SetColor(ColorId, color);
            properties.SetColor(EdgeColorId, edge);
            properties.SetFloat(WidthId, width);
            properties.SetFloat(CoreMixId, core ? 1f : 0f);
            properties.SetFloat(DistortionId, distortion);
            properties.SetFloat(PhaseId, phase * 20f);
            properties.SetFloat(OpacityId, 0f);
            renderer.SetPropertyBlock(properties);
            renderer.enabled = false;

            return new Ring
            {
                transform = go.transform,
                renderer = renderer,
                properties = properties,
                delay = delay,
                duration = duration,
                initialSize = initialSize,
                finalSize = finalSize,
                peakOpacity = peak
            };
        }

        private ParticleSystem MakeParticles(Transform parent, Kind kind)
        {
            var go = new GameObject(kind == Kind.Fire ? "BlueRadioMotes" : "StrawAndDust");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1.0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            main.startLifetime = kind == Kind.Fire
                ? new ParticleSystem.MinMaxCurve(0.10f, 0.24f)
                : kind == Kind.Hit ? new ParticleSystem.MinMaxCurve(0.25f, 0.48f)
                : new ParticleSystem.MinMaxCurve(0.50f, 0.90f);
            main.startSpeed = kind == Kind.Fire
                ? new ParticleSystem.MinMaxCurve(1.8f, 3.7f)
                : kind == Kind.Hit ? new ParticleSystem.MinMaxCurve(0.9f, 2.5f)
                : new ParticleSystem.MinMaxCurve(0.5f, 2.2f);
            main.gravityModifier = kind == Kind.Fire ? 0.0f : 0.24f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startSize3D = true;
            main.startSizeX = kind == Kind.Fire
                ? new ParticleSystem.MinMaxCurve(0.014f, 0.035f)
                : new ParticleSystem.MinMaxCurve(0.018f, 0.065f);
            main.startSizeY = kind == Kind.Death
                ? new ParticleSystem.MinMaxCurve(0.12f, 0.30f)
                : new ParticleSystem.MinMaxCurve(0.06f, 0.17f);
            main.startSizeZ = 0.025f;
            main.startColor = kind == Kind.Fire
                ? new ParticleSystem.MinMaxGradient(SignalCyan, WarmFlash)
                : kind == Kind.Hit
                    ? new ParticleSystem.MinMaxGradient(StrawGold, WarmFlash)
                    : new ParticleSystem.MinMaxGradient(StrawGold, ImpactMagenta);

            var emission = ps.emission;
            emission.enabled = false; // Only manual Emit; no uncontrolled spawning.

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = kind == Kind.Fire ? 14f : kind == Kind.Hit ? 60f : 78f;
            shape.radius = kind == Kind.Death ? 0.3f : 0.05f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0.0f, 0f),
                    new GradientAlphaKey(1.0f, 0.12f),
                    new GradientAlphaKey(0.65f, 0.55f),
                    new GradientAlphaKey(0.0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = strawDustMaterial;
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.shadowCastingMode = ShadowCastingMode.Off;
            pr.receiveShadows = false;
            return ps;
        }

        private void Update()
        {
            if (!externallyDriven) Advance(Time.deltaTime);
        }

        /// <summary>Advance presentation only. The caller supplies the authoritative playing step.</summary>
        public void Advance(float delta)
        {
            if (delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            VisualTime += delta;
            foreach (Effect effect in instances)
            {
                if (!effect.active) continue;
                effect.elapsed += delta;
                if (effect.elapsed >= effect.duration)
                {
                    effect.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    effect.root.SetActive(false);
                    effect.active = false;
                    continue;
                }
                effect.particles.Simulate(delta, false, false, false);
                TickRings(effect);
            }
        }

        private static void TickRings(Effect effect)
        {
            foreach (Ring ring in effect.rings)
            {
                float t = (effect.elapsed - ring.delay) / ring.duration;
                if (t < 0f || t > 1f)
                {
                    ring.renderer.enabled = false;
                    continue;
                }
                ring.renderer.enabled = true;
                float eased = 1f - Mathf.Pow(1f - t, 2.0f);
                float size = Mathf.Lerp(ring.initialSize, ring.finalSize, eased);
                ring.transform.localScale = new Vector3(size, 1f, size);
                float opacity = ring.peakOpacity * Mathf.Sin(t * Mathf.PI);
                ring.properties.SetFloat(OpacityId, Mathf.Max(0f, opacity));
                ring.properties.SetFloat(VisualTimeId, effect.elapsed);
                ring.renderer.SetPropertyBlock(ring.properties);
            }
        }

        private static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "SonicVFX_XZ_Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f,  0.5f),
                new Vector3( 0.5f, 0f,  0.5f),
                new Vector3( 0.5f, 0f, -0.5f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (quadMesh != null) Destroy(quadMesh);
        }

        private void OnDisable() => Clear();

        public void Clear()
        {
            foreach (var effect in instances)
            {
                effect.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.root.SetActive(false);
                effect.active = false;
                effect.elapsed = 0f;
            }
            VisualTime = 0f;
        }
    }
}
