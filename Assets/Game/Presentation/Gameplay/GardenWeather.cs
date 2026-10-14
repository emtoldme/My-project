using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    public enum GardenWeatherKind { Clear, Rain, Storm }

    /// <summary>Presentation only: all motion and particles advance on the simulation clock.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class GardenWeather : MonoBehaviour
    {
        [Header("Weather · F6 cycles presets during play")]
        public GardenWeatherKind weather = GardenWeatherKind.Clear;
        [Range(0, 1)] public float rainAmount = .72f;
        [Range(0, 1)] public float wetness = .82f;
        [Range(.1f, 2)] public float windStrength = .65f;
        [Range(.25f, 1)] public float particleBudget = 1f;
        public bool lightning = true;
        public bool animateSearchlights = true;
        public GameLoop loop;
        public Light sun;
        public Texture2D cloudShadow;
        public Material cloudBlendMaterial;
        [Min(10)] public float cloudShadowScale = 38;
        [Range(0, 2)] public float cloudSpeed = .35f;
        public Terrain ground;
        public ParticleSystem rain, splashes, mist;
        public Light[] searchlights;
        public Light[] pathLights;
        public ReflectionProbe reflection;
        public Material[] wettableMaterials;
        [Header("Atmosphere lighting")]
        public float wetSearchlightIntensity = 26f;
        public float wetPathIntensity = 2.4f;
        public float overcastFogStart = 18f, overcastFogEnd = 62f;

        public float VisualTime { get; private set; }
        public float CurrentRain { get; private set; }
        public float CurrentWetness { get; private set; }
        public float LightningFlash { get; private set; }
        readonly List<RendererBinding> bindings = new List<RendererBinding>();
        readonly List<WetMaterial> materials = new List<WetMaterial>();
        readonly List<LightBinding> localLights = new List<LightBinding>();
        readonly System.Random random = new System.Random(73921); // Never consume gameplay RNG.
        Color skyColor, equatorColor, groundColor, fogColor, sunColor;
        float sunIntensity, fogStart, fogEnd, reflectionIntensity;
        bool fogEnabled;
        Quaternion sunRotation;
        Material originalSky, runtimeSky;
        Color originalSkyTint;
        float originalSkyExposure;
        Texture originalCookie;
        UniversalAdditionalLightData sunData;
        Vector2 originalCookieSize, originalCookieOffset;
        float nextLightning = 9, flashStart = -100, splashCarry;
        bool initialized;
        RenderTexture continuousCookie;
        Material cookieBlend;
        public float CloudCover {get;private set;}
        public Texture ActiveCloudCookie => continuousCookie;

        sealed class RendererBinding { public Renderer renderer; public Material[] original; }
        sealed class WetMaterial { public Material runtime; public Color color; public float smoothness; }
        sealed class LightBinding { public Light light; public float intensity; public Quaternion rotation; }

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (loop == null) loop = FindAnyObjectByType<GameLoop>();
            skyColor = RenderSettings.ambientSkyColor;
            equatorColor = RenderSettings.ambientEquatorColor;
            groundColor = RenderSettings.ambientGroundColor;
            fogColor = RenderSettings.fogColor;
            fogEnabled = RenderSettings.fog;
            fogStart = RenderSettings.fogStartDistance;
            fogEnd = RenderSettings.fogEndDistance;
            if (sun != null)
            {
                sunColor = sun.color; sunIntensity = sun.intensity; sunRotation = sun.transform.rotation;
                originalCookie = sun.cookie; sunData = sun.GetComponent<UniversalAdditionalLightData>();
                if (sunData != null) { originalCookieSize = sunData.lightCookieSize; originalCookieOffset = sunData.lightCookieOffset; }
                if(cloudShadow!=null&&cloudBlendMaterial!=null)
                {
                    cookieBlend=new Material(cloudBlendMaterial);
                    cookieBlend.SetTexture("_ClearTex",originalCookie!=null?originalCookie:Texture2D.whiteTexture);
                    continuousCookie=new RenderTexture(256,256,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
                    {name="Continuous weather cloud cookie",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear,useMipMap=true,autoGenerateMips=true};
                    continuousCookie.Create();sun.cookie=continuousCookie;
                }
            }
            if (reflection != null) reflectionIntensity = reflection.intensity;
            originalSky = RenderSettings.skybox;
            if (originalSky != null)
            {
                runtimeSky = new Material(originalSky); RenderSettings.skybox = runtimeSky;
                originalSkyTint = runtimeSky.GetColor("_SkyTint"); originalSkyExposure = runtimeSky.GetFloat("_Exposure");
            }
            CloneWetMaterials();
            foreach (var group in new[] { searchlights, pathLights }) if (group != null) foreach (var light in group)
                if (light != null) localLights.Add(new LightBinding { light = light, intensity = light.intensity, rotation = light.transform.rotation });
            initialized = true;
            CurrentRain = TargetRain();
            CurrentWetness = weather == GardenWeatherKind.Clear ? 0 : wetness;
            foreach (var ps in new[] { rain, splashes, mist })
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true);
                    ps.Pause(true); // Start emission, then advance explicitly on the run clock.
                }
            Evaluate(loop?.Sim != null ? loop.Sim.State.Now : 0, 0, false);
        }

        void CloneWetMaterials()
        {
            var replacements = new Dictionary<Material, Material>();
            if (wettableMaterials != null) foreach (var source in wettableMaterials)
            {
                if (source == null || replacements.ContainsKey(source) || !source.HasProperty("_BaseColor")) continue;
                var clone = new Material(source) { name = source.name + " · weather instance" };
                replacements.Add(source, clone);
                materials.Add(new WetMaterial { runtime = clone, color = source.GetColor("_BaseColor"), smoothness = source.GetFloat("_Smoothness") });
            }
            foreach (var renderer in FindObjectsByType<Renderer>())
            {
                var original = renderer.sharedMaterials;
                var changed = (Material[])original.Clone();
                bool replace = false;
                for (int i = 0; i < changed.Length; i++)
                    if (changed[i] != null && replacements.TryGetValue(changed[i], out var material)) { changed[i] = material; replace = true; }
                if (!replace) continue;
                bindings.Add(new RendererBinding { renderer = renderer, original = original });
                renderer.sharedMaterials = changed;
            }
        }

        void LateUpdate()
        {
            if (!initialized || loop?.Sim == null) return;
            bool playing = loop.Sim.State.Phase == RunPhase.Playing;
            if (playing && Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame)
                weather = (GardenWeatherKind)(((int)weather + 1) % 3);
            float now = loop.Sim.State.Now;
            float dt = playing ? Mathf.Clamp(now - VisualTime, 0, .1f) : 0;
            Evaluate(now, dt, playing);
        }

        float TargetRain() => weather == GardenWeatherKind.Clear ? 0 : weather == GardenWeatherKind.Storm ? 1 : rainAmount;

        public void Evaluate(float now, float dt, bool playing)
        {
            if (!initialized) return;
            VisualTime = now;
            CurrentRain = Mathf.Lerp(CurrentRain, TargetRain(), 1 - Mathf.Exp(-dt * .65f));
            float targetWetness = weather == GardenWeatherKind.Clear ? 0 : weather == GardenWeatherKind.Storm ? 1 : wetness;
            CurrentWetness = Mathf.MoveTowards(CurrentWetness, targetWetness, dt * (targetWetness > CurrentWetness ? .12f : .035f));
            if (playing && lightning && weather == GardenWeatherKind.Storm && now >= nextLightning)
            { flashStart = now; nextLightning = now + 10 + (float)random.NextDouble() * 13; }
            float age = now - flashStart;
            LightningFlash = weather == GardenWeatherKind.Storm && lightning
                ? Mathf.Exp(-age * 17) + .55f * Mathf.Exp(-Mathf.Pow((age - .16f) * 35, 2)) : 0;
            if (age > 1.2f) LightningFlash = 0;
            float gust = .7f + .3f * Mathf.Sin(now * .47f) * Mathf.Sin(now * .19f + 1.5f);
            Shader.SetGlobalVector("_ReverbWeather", new Vector4(CurrentRain, CurrentWetness, now, LightningFlash));
            Shader.SetGlobalVector("_ReverbWind", new Vector4(.85f, .5f, windStrength * gust * CurrentRain, 1));
            UpdateLighting(now);
            if (rain != null)
            {
                Vector3 center = loop != null && loop.playerView != null ? loop.playerView.position : Vector3.zero;
                rain.transform.position = center + new Vector3(2, 13, 1);
                var emission = rain.emission; emission.rateOverTime = 1900 * particleBudget * CurrentRain;
                var velocity = rain.velocityOverLifetime; velocity.x = -2.5f * windStrength * gust; velocity.z = -1.5f * windStrength * gust;
                if (mist != null) { mist.transform.position = center + Vector3.up * .35f; var e = mist.emission; e.rateOverTime = 15 * particleBudget * CurrentRain; }
                if (playing && dt > 0)
                {
                    rain.Simulate(dt, true, false, false);
                    if (mist != null) mist.Simulate(dt, true, false, false);
                    EmitSplashes(center, dt);
                }
            }
            foreach (var entry in materials)
            {
                entry.runtime.SetColor("_BaseColor", entry.color * Mathf.Lerp(1, .88f, CurrentWetness));
                entry.runtime.SetFloat("_Smoothness", Mathf.Lerp(entry.smoothness, Mathf.Min(.58f, entry.smoothness + .14f), CurrentWetness));
            }
        }

        void UpdateLighting(float now)
        {
            float overcast = Mathf.Clamp01(CurrentRain * .85f);
            RenderSettings.ambientSkyColor = Color.Lerp(skyColor, new Color(.18f, .235f, .28f), overcast) + Color.white * LightningFlash * .3f;
            RenderSettings.ambientEquatorColor = Color.Lerp(equatorColor, new Color(.13f, .17f, .21f), overcast);
            RenderSettings.ambientGroundColor = Color.Lerp(groundColor, new Color(.075f, .1f, .105f), overcast);
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(fogColor, new Color(.27f, .34f, .4f), overcast);
            RenderSettings.fogStartDistance = Mathf.Lerp(fogStart, overcastFogStart, overcast);
            RenderSettings.fogEndDistance = Mathf.Lerp(fogEnd, overcastFogEnd, overcast);
            if (sun != null)
            {
                sun.color = Color.Lerp(sunColor, new Color(.7f, .82f, 1), overcast);
                sun.intensity = Mathf.Lerp(sunIntensity, .72f, overcast) + LightningFlash * 3.4f;
                // Stable celestial direction prevents cascades crawling across a static road.
                // Cookie identity stays fixed even during lightning; only its pixels blend.
                sun.transform.rotation = sunRotation;
                CloudCover=CurrentRain*.7f;
                if(continuousCookie!=null)
                {
                    cookieBlend.SetFloat("_Cover",CloudCover);
                    var active=RenderTexture.active;
                    Graphics.Blit(cloudShadow,continuousCookie,cookieBlend);
                    RenderTexture.active=active;
                }
                if (sunData != null)
                {
                    sunData.lightCookieSize = Vector2.one * cloudShadowScale;
                    sunData.lightCookieOffset = new Vector2(now * cloudSpeed, now * cloudSpeed * .57f);
                }
            }
            if (runtimeSky != null)
            {
                runtimeSky.SetColor("_SkyTint", Color.Lerp(originalSkyTint, new Color(.28f, .36f, .49f), overcast));
                runtimeSky.SetFloat("_Exposure", Mathf.Lerp(originalSkyExposure, originalSkyExposure * .65f, overcast) + LightningFlash * .7f);
            }
            if (reflection != null) reflection.intensity = Mathf.Lerp(reflectionIntensity, reflectionIntensity * .65f, overcast);
            if (searchlights != null) for (int i = 0; i < searchlights.Length; i++)
            {
                var light = searchlights[i]; if (light == null) continue;
                light.intensity = Mathf.Lerp(.35f, wetSearchlightIntensity, CurrentRain);
                if (animateSearchlights && i < localLights.Count)
                    light.transform.rotation = localLights[i].rotation * Quaternion.Euler(Mathf.Sin(now*.11f+i)*4, Mathf.Sin(now*.075f+i*2)*17, 0);
            }
            if (pathLights != null) foreach (var light in pathLights) if (light != null) light.intensity = Mathf.Lerp(.5f, wetPathIntensity, CurrentRain);
        }

        void EmitSplashes(Vector3 center, float dt)
        {
            if (splashes == null) return;
            splashCarry += dt * 110 * particleBudget * CurrentRain;
            int count = Mathf.Min(24, (int)splashCarry); splashCarry -= count;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = center + new Vector3(((float)random.NextDouble() - .5f) * 29, 0, ((float)random.NextDouble() - .5f) * 29);
                p.y = ground != null && ground.isActiveAndEnabled ? ground.SampleHeight(p) + ground.transform.position.y + .035f : .035f;
                var emit = new ParticleSystem.EmitParams { position = p, startSize = .18f + (float)random.NextDouble() * .25f, startLifetime = .35f + (float)random.NextDouble() * .2f };
                splashes.Emit(emit, 1);
            }
            splashes.Simulate(dt, true, false, false);
        }

        void OnDisable()
        {
            if (!initialized) return;
            foreach (var ps in new[] { rain, splashes, mist }) if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var binding in bindings) if (binding.renderer != null) binding.renderer.sharedMaterials = binding.original;
            foreach (var entry in materials) Destroy(entry.runtime);
            bindings.Clear(); materials.Clear();
            RenderSettings.ambientSkyColor = skyColor; RenderSettings.ambientEquatorColor = equatorColor;
            RenderSettings.ambientGroundColor = groundColor; RenderSettings.fogColor = fogColor;
            RenderSettings.fog = fogEnabled; RenderSettings.fogStartDistance = fogStart; RenderSettings.fogEndDistance = fogEnd;
            if (sun != null) { sun.color = sunColor; sun.intensity = sunIntensity; sun.transform.rotation = sunRotation; sun.cookie = originalCookie; }
            if (sunData != null) { sunData.lightCookieSize = originalCookieSize; sunData.lightCookieOffset = originalCookieOffset; }
            if(continuousCookie!=null){continuousCookie.Release();Destroy(continuousCookie);continuousCookie=null;}
            if(cookieBlend!=null){Destroy(cookieBlend);cookieBlend=null;}
            if (reflection != null) reflection.intensity = reflectionIntensity;
            foreach (var binding in localLights) if (binding.light != null)
            { binding.light.intensity = binding.intensity; binding.light.transform.rotation = binding.rotation; }
            localLights.Clear();
            if (RenderSettings.skybox == runtimeSky) RenderSettings.skybox = originalSky;
            if (runtimeSky != null) Destroy(runtimeSky);
            Shader.SetGlobalVector("_ReverbWeather", Vector4.zero);
            Shader.SetGlobalVector("_ReverbWind", Vector4.zero);
            initialized = false;
        }
    }
}
