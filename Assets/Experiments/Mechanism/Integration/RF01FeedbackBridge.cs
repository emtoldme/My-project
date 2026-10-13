using System.Collections.Generic;
using ReverbInTheRye.VFX;
using UnityEngine;
using Waffle;

namespace Reverb.Experiments.Mechanism.Integration
{
    // Views only. Direct events go to SonicVFX; derived events stay in M01DerivedFx.
    [DefaultExecutionOrder(-80)]
    public sealed class RF01FeedbackBridge : MonoBehaviour
    {
        public GameLoop loop;
        public SonicAttackVFX sonic;
        public Transform environment;
        public Material windMaterial;
        Material runWind;
        bool terminal;
        public int Shots { get; private set; }
        public int DirectHits { get; private set; }
        public int Deaths { get; private set; }
        public float WindTime => runWind != null ? runWind.GetFloat("_VisualTime") : 0f;

        void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (windMaterial == null || environment == null) return;
            runWind = new Material(windMaterial) { name = "RF01_RunWind" };
            runWind.SetFloat("_UseExternalTime", 1f);
            runWind.SetFloat("_VisualTime", 0f);
            foreach (var renderer in environment.GetComponentsInChildren<Renderer>(true))
                if (renderer.sharedMaterial == windMaterial) renderer.sharedMaterial = runWind;
        }

        void OnEnable() { if (loop != null) loop.PresentationDispatched += OnEvents; }
        void OnDisable()
        {
            if (loop != null) loop.PresentationDispatched -= OnEvents;
            if (sonic != null) sonic.Clear();
        }
        void OnDestroy() { if (runWind != null) Destroy(runWind); }

        void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            foreach (var item in events)
            {
                if (item.Type == PresentationEventType.Restart || item.Type == PresentationEventType.Victory || item.Type == PresentationEventType.Defeat)
                { terminal = true; sonic.Clear(); }
                else if (!terminal && item.Type == PresentationEventType.Shot)
                {
                    Shots++;
                    if (GetComponent<IPresentationEventOwner>()?.OwnsPresentation(item) == true) continue;
                    Vector3 direction = new Vector3(item.DirectionX, 0f, item.DirectionZ);
                    sonic.PlayFire(new Vector3(item.X, .15f, item.Z), direction);
                }
                else if (!terminal && item.Type == PresentationEventType.EnemyHit)
                { DirectHits++; if (GetComponent<IPresentationEventOwner>()?.OwnsPresentation(item) != true) sonic.PlayHit(new Vector3(item.X, .08f, item.Z)); }
                else if (!terminal && item.Type == PresentationEventType.EnemyKilled)
                { Deaths++; sonic.PlayDeath(new Vector3(item.X, .08f, item.Z)); }
            }
        }

        void LateUpdate()
        {
            if (terminal || loop?.Sim == null) return;
            bool playing = loop.Sim.State.Phase == RunPhase.Playing;
            if (runWind != null) runWind.SetFloat("_VisualTime", loop.Sim.State.Now);
            sonic.Advance(playing ? loop.Sim.LastDt : 0f);
        }
    }
}
