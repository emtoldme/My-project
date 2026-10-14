using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>One small authored aura per nearby relic. Visibility follows the real relic prop, with no particle systems or lights.</summary>
    public sealed class RelicEchoView : MonoBehaviour
    {
        public Transform relic;public Renderer aura;
        GameLoop loop;MaterialPropertyBlock block;float lastVisibility=-1;
        void Start(){loop=GetComponentInParent<GameLoop>();if(loop==null)loop=FindAnyObjectByType<GameLoop>();block=new MaterialPropertyBlock();}
        void LateUpdate()
        {
            if(loop?.Sim==null||aura==null||relic==null)return;
            float d=Vector2.Distance(new Vector2(loop.Sim.PlayerX,loop.Sim.PlayerZ),new Vector2(relic.position.x,relic.position.z));
            float visibility=relic.gameObject.activeInHierarchy?Mathf.SmoothStep(1,0,Mathf.InverseLerp(8,14,d)):0;
            bool active=visibility>.01f;aura.enabled=active;if(!active)return;
            aura.transform.position=relic.position+Vector3.up*.25f;
            if(Mathf.Abs(visibility-lastVisibility)>.01f){block.SetFloat("_Visibility",visibility);aura.SetPropertyBlock(block);lastVisibility=visibility;}
        }
    }
}
