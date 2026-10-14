using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    public sealed class RedGhostView : MonoBehaviour, IEnemyVisualPose
    {
        public Transform drapery;
        Renderer[] renderers;MaterialPropertyBlock block;float born, lastTime=-1;
        public int Serial {get;private set;}
        public float VisualTime {get;private set;}
        public float Dissolve {get;private set;}
        void Awake(){renderers=GetComponentsInChildren<Renderer>(true);block=new MaterialPropertyBlock();}
        public void Present(in EnemyVisualPose pose)
        {
            if(pose.Recycled||Serial!=pose.Serial){born=pose.Now;Serial=pose.Serial;lastTime=-1;}
            float dt=lastTime<0?0:Mathf.Max(0,pose.Now-lastTime);
            float phase=pose.Serial*2.39996f;
            if(drapery!=null)
            {
                drapery.localPosition=Vector3.up*(.13f+Mathf.Sin(pose.Now*2.1f+phase)*.065f);
                var local=transform.InverseTransformDirection(pose.Velocity);
                var lean=Quaternion.Euler(Mathf.Clamp(local.z*2,-12,12),Mathf.Sin(pose.Now*1.4f+phase)*4,Mathf.Clamp(-local.x*2,-8,8));
                drapery.localRotation=pose.Recycled?lean:Quaternion.Slerp(drapery.localRotation,lean,1-Mathf.Exp(-dt*5));
            }
            Apply(pose.Now,phase,Mathf.Clamp01((pose.Now-born)/.36f),0,pose.Hit,pose.Velocity.magnitude);
            lastTime=pose.Now;
        }
        public void Afterimage(float now,float phase,float dissolve,Color color)
        {
            if(drapery!=null){drapery.localPosition=Vector3.up*(.15f+dissolve*.38f);drapery.localRotation=Quaternion.Euler(0,dissolve*12,0);}
            Apply(now,phase,1,dissolve,0,0,color);
        }
        void Apply(float now,float phase,float emerge,float dissolve,float hit,float flow,Color? color=null)
        {
            if(block==null)Awake();VisualTime=now;Dissolve=dissolve;
            foreach(var r in renderers)
            {
                r.GetPropertyBlock(block);
                if(color.HasValue)block.SetColor("_BaseColor",color.Value);
                block.SetFloat("_VisualTime",now);block.SetFloat("_SerialPhase",phase);block.SetFloat("_Emerge",emerge);
                block.SetFloat("_Dissolve",dissolve);block.SetFloat("_Hit",hit);block.SetFloat("_Flow",Mathf.Min(6,flow));r.SetPropertyBlock(block);
            }
        }
    }
}
