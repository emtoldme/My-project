using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>One art clock for the broken resonators and their projected light. No audio or rules.</summary>
    [DefaultExecutionOrder(180)]
    public sealed class ResonanceScenography : MonoBehaviour
    {
        public GameLoop loop;
        public GardenWeather weather;
        public Transform[] suspendedArcs;
        public Renderer[] inscriptions;
        public Light[] patternedLights;
        public Color inscriptionColor = new Color(.65f, 1.25f, 1.15f, .7f);
        public float VisualTime { get; private set; }
        public int ProjectorCount => patternedLights == null ? 0 : patternedLights.Length;
        Quaternion[] rest;
        float[] lightRest;
        MaterialPropertyBlock block;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        void Start()
        {
            if (loop == null) loop = FindAnyObjectByType<GameLoop>();
            rest = new Quaternion[suspendedArcs.Length];
            for (int i = 0; i < rest.Length; i++) rest[i] = suspendedArcs[i].localRotation;
            lightRest = new float[patternedLights.Length];
            for (int i = 0; i < lightRest.Length; i++) lightRest[i] = patternedLights[i].intensity;
            block = new MaterialPropertyBlock();
        }
        void LateUpdate()
        {
            if (rest == null || loop?.Sim == null || loop.Sim.State.Phase != RunPhase.Playing) return;
            VisualTime = loop.Sim.State.Now;
            for (int i = 0; i < rest.Length; i++)
                if (suspendedArcs[i] != null) suspendedArcs[i].localRotation = rest[i] * Quaternion.Euler(0, 0, Mathf.Sin(VisualTime * .19f + i * 1.7f) * 2.2f);
            for (int i = 0; i < inscriptions.Length; i++)
            {
                // Staggered, slow excitation. It is a visual rhythm, not beat synchronization.
                float wave = .58f + .42f * Mathf.Pow(.5f + .5f * Mathf.Sin(VisualTime * 1.1f - i * .8f), 3);
                block.Clear(); block.SetColor(BaseColor, inscriptionColor * wave);
                inscriptions[i].SetPropertyBlock(block);
            }
            for (int i = 0; i < patternedLights.Length; i++)
            {
                var light = patternedLights[i]; if (light == null) continue;
                float carrier = .8f + .2f * Mathf.Sin(VisualTime * .7f + i * 2.1f);
                // Weather evaluates first and owns its spot intensities; multiply its current result once.
                float basis = weather != null ? light.intensity : lightRest[i];
                light.intensity = basis * carrier;
            }
        }
        void OnDisable()
        {
            if (rest != null) for (int i = 0; i < rest.Length; i++) if (suspendedArcs[i] != null) suspendedArcs[i].localRotation = rest[i];
            if (lightRest != null) for (int i = 0; i < lightRest.Length; i++) if (patternedLights[i] != null) patternedLights[i].intensity = lightRest[i];
            if (inscriptions != null) foreach (var renderer in inscriptions) if (renderer != null) renderer.SetPropertyBlock(null);
        }
    }
}
