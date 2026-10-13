using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Ambient art only. Uses the run clock so scenery stops with the game.</summary>
    public sealed class GardenPresentation : MonoBehaviour
    {
        public GameLoop loop;
        public Transform[] crowns;
        public ParticleSystem pollen;
        bool held;
        void LateUpdate()
        {
            if(loop?.Sim==null)return;
            var state=loop.Sim.State;
            Shader.SetGlobalVector("_ReverbPlayer",new Vector4(loop.Sim.PlayerX,0,loop.Sim.PlayerZ,1));
            if(crowns!=null)foreach(var crown in crowns)if(crown!=null)crown.localRotation=Quaternion.Euler(0,state.Now*.45f,0);
            if(pollen==null)return;
            bool hold=state.Phase!=RunPhase.Playing;
            if(hold==held)return;held=hold;
            if(state.Phase.IsTerminal())pollen.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            else if(hold)pollen.Pause(true);else pollen.Play(true);
        }
        void OnDisable(){Shader.SetGlobalVector("_ReverbPlayer",Vector4.zero);}
    }
}
