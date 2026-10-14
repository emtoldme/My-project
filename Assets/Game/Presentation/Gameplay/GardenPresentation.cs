using UnityEngine;
using Waffle;
using Waffle.Combat;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Ambient art only. Uses the run clock so scenery stops with the game.</summary>
    public sealed class GardenPresentation : MonoBehaviour
    {
        public GameLoop loop;
        public Transform[] crowns;
        public ParticleSystem pollen;
        public Transform memorySwing;
        public int PickupRevealCount { get; private set; }
        readonly Vector4[] reveals=new Vector4[8];
        readonly float[] distances=new float[8];
        Quaternion swingRest;
        bool swingCaptured;
        bool held;
        Texture2D contactTexture;
        Color[] contactPixels;
        public WheatContactField ContactField {get;private set;}
        void UpdateWheatContact(bool playing,float now)
        {
            if(contactTexture==null)
            {
                ContactField=new WheatContactField();contactPixels=new Color[WheatContactField.Size*WheatContactField.Size];
                contactTexture=new Texture2D(WheatContactField.Size,WheatContactField.Size,TextureFormat.RGBAHalf,false,true)
                {name="Wheat · local contact memory",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            }
            if(ContactField.Step(new Vector2(loop.Sim.PlayerX,loop.Sim.PlayerZ),now,playing,reveals,PickupRevealCount))
            {ContactField.WritePixels(contactPixels);contactTexture.SetPixels(contactPixels);contactTexture.Apply(false,false);}
            Vector2 origin=ContactField.Origin;
            Shader.SetGlobalTexture("_ReverbWheatContact",contactTexture);
            Shader.SetGlobalVector("_ReverbWheatContactRect",new Vector4(origin.x,origin.y,WheatContactField.Extent,1));
        }
        void OnEnable()
        {if(loop?.Sim!=null)UpdateWheatContact(loop.Sim.State.Phase==RunPhase.Playing,loop.Sim.State.Now);}
        void Start(){if(memorySwing!=null){swingRest=memorySwing.localRotation;swingCaptured=true;}}
        void LateUpdate()
        {
            if(loop?.Sim==null)return;
            var state=loop.Sim.State;
            Shader.SetGlobalVector("_ReverbPlayer",new Vector4(loop.Sim.PlayerX,0,loop.Sim.PlayerZ,1));
            if(state.Phase==RunPhase.Playing)
            {
                PickupRevealCount=0;for(int i=0;i<8;i++){reveals[i]=Vector4.zero;distances[i]=float.MaxValue;}
                if(loop.Sim.Combat!=null)
                {
                    foreach(var pickup in loop.Sim.Combat.Pickups)Reveal(pickup.Position.X,pickup.Position.Y);
                    foreach(var gem in loop.Sim.Combat.Gems)if(gem.Life==GemLife.Free||gem.Life==GemLife.Magnet)Reveal(gem.Position.X,gem.Position.Y);
                }
                Shader.SetGlobalVectorArray("_ReverbPickupReveal",reveals);Shader.SetGlobalInt("_ReverbPickupCount",PickupRevealCount);
                if(memorySwing!=null)memorySwing.localRotation=swingRest*Quaternion.Euler(Mathf.Sin(state.Now*.7f)*1.5f,0,0);
            }
            UpdateWheatContact(state.Phase==RunPhase.Playing,state.Now);
            if(crowns!=null)foreach(var crown in crowns)if(crown!=null)crown.localRotation=Quaternion.Euler(0,state.Now*.45f,0);
            if(pollen==null)return;
            bool hold=state.Phase!=RunPhase.Playing;
            if(hold==held)return;held=hold;
            if(state.Phase.IsTerminal())pollen.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            else if(hold)pollen.Pause(true);else pollen.Play(true);
        }
        void Reveal(float x,float z)
        {
            float d=(x-loop.Sim.PlayerX)*(x-loop.Sim.PlayerX)+(z-loop.Sim.PlayerZ)*(z-loop.Sim.PlayerZ);
            if(d>100||d>=distances[7])return;
            int slot=Mathf.Min(PickupRevealCount,7);
            while(slot>0&&d<distances[slot-1]){distances[slot]=distances[slot-1];reveals[slot]=reveals[slot-1];slot--;}
            distances[slot]=d;reveals[slot]=new Vector4(x,0,z,1);PickupRevealCount=Mathf.Min(8,PickupRevealCount+1);
        }
        void OnDisable(){Shader.SetGlobalVector("_ReverbWheatContactRect",Vector4.zero);Shader.SetGlobalTexture("_ReverbWheatContact",null);
            if(contactTexture!=null)Destroy(contactTexture);contactTexture=null;ContactField=null;contactPixels=null;
            Shader.SetGlobalVector("_ReverbPlayer",Vector4.zero);Shader.SetGlobalInt("_ReverbPickupCount",0);if(memorySwing!=null&&swingCaptured)memorySwing.localRotation=swingRest;}
    }
}
