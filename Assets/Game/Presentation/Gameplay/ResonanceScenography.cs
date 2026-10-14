using UnityEngine;
using Waffle;
using Waffle.GameplayNext;

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
        public float Activation { get; private set; }
        public int ProjectorCount => patternedLights == null ? 0 : patternedLights.Length;
        Quaternion[] rest;
        float[] lightRest;
        MaterialPropertyBlock block;
        int activeSource;
        float completedAt=-100;
        ObjectiveStatus previousStatus;
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
            if (rest == null || loop?.Sim == null) return;
            bool playing=loop.Sim.State.Phase==RunPhase.Playing;
            VisualTime = loop.Sim.State.Now;
            var view=loop.ReadGameplayView();
            float target=0;bool finale=view!=null&&view.Finale.Spawned&&!view.Finale.Defeated;
            if(view!=null)
            {
                var objective=view.Objective;
                if(objective.Status==ObjectiveStatus.Available)target=.12f;
                if(objective.Status==ObjectiveStatus.Channeling)target=.35f+.65f*objective.Progress;
                if(objective.Status==ObjectiveStatus.Completed&&previousStatus!=ObjectiveStatus.Completed)completedAt=VisualTime;
                if(VisualTime-completedAt<.8f)target=Mathf.Max(target,1-(VisualTime-completedAt)/.8f);
                previousStatus=objective.Status;
                activeSource=0;float closest=float.MaxValue;
                for(int i=0;i<patternedLights.Length;i++)if(patternedLights[i]!=null)
                {var p=patternedLights[i].transform.position;float d=(p.x-objective.Position.X)*(p.x-objective.Position.X)+(p.z-objective.Position.Y)*(p.z-objective.Position.Y);if(d<closest){closest=d;activeSource=i;}}
            }
            if(finale)target=.8f;
            Activation=Mathf.MoveTowards(Activation,target,playing?Mathf.Min(.1f,loop.Sim.LastDt)*2:0);
            if(loop.Sim.State.Phase.IsTerminal())Activation=0;
            for (int i = 0; i < rest.Length; i++)
                if (suspendedArcs[i] != null) suspendedArcs[i].localRotation = rest[i] * Quaternion.Euler(0, 0, Mathf.Sin(VisualTime * .19f + i * 1.7f) * Mathf.Lerp(.35f,2.2f,Activation));
            for (int i = 0; i < inscriptions.Length; i++)
            {
                // Staggered, slow excitation. It is a visual rhythm, not beat synchronization.
                if(inscriptions[i]==null)continue;
                float excitation=finale||NearestSource(inscriptions[i].transform.position)==activeSource?Activation:0;
                float wave=.7f+.3f*Mathf.Sin(VisualTime*1.1f-i*.8f);
                Color tint=inscriptionColor*Mathf.Lerp(.16f,1,excitation)*wave;
                tint.a=inscriptions[i].name.StartsWith("Inlaid standing")?.012f:Mathf.Lerp(.035f,.85f,excitation)*wave;
                block.Clear(); block.SetColor(BaseColor,tint);
                inscriptions[i].SetPropertyBlock(block);
            }
            for (int i = 0; i < patternedLights.Length; i++)
            {
                var light = patternedLights[i]; if (light == null) continue;
                float excitation=finale||i==activeSource?Activation:0;
                light.intensity=Mathf.Lerp(.06f,20f,excitation)*(.85f+.15f*Mathf.Sin(VisualTime*.7f+i*2.1f));
            }
        }
        int NearestSource(Vector3 point)
        {
            int index=0;float closest=float.MaxValue;
            for(int i=0;i<patternedLights.Length;i++)if(patternedLights[i]!=null)
            {float d=(point-patternedLights[i].transform.position).sqrMagnitude;if(d<closest){index=i;closest=d;}}
            return index;
        }
        void OnDisable()
        {
            if (rest != null) for (int i = 0; i < rest.Length; i++) if (suspendedArcs[i] != null) suspendedArcs[i].localRotation = rest[i];
            if (lightRest != null) for (int i = 0; i < lightRest.Length; i++) if (patternedLights[i] != null) patternedLights[i].intensity = lightRest[i];
            if (inscriptions != null) foreach (var renderer in inscriptions) if (renderer != null) renderer.SetPropertyBlock(null);
        }
    }
}
