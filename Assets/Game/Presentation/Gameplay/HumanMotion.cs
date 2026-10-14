using System.Collections.Generic;
using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Distance-driven original walk pose, entirely frozen by the run clock.</summary>
    [DefaultExecutionOrder(230)]
    public sealed class HumanMotion : MonoBehaviour
    {
        public GameLoop loop;
        sealed class Bone { public Transform transform; public Quaternion rest; }
        readonly Dictionary<string,Bone> bones=new Dictionary<string,Bone>();
        Vector3 previous; float lastTime, phase, blend, dodgeUntil;
        Vector2 travel=Vector2.up;
        public float StridePhase => phase;
        public float MovementBlend => blend;
        void OnEnable()
        {
            if(loop==null)loop=GetComponentInParent<GameLoop>();
            if(loop==null)loop=FindAnyObjectByType<GameLoop>();
            if(bones.Count==0)foreach(var t in GetComponentsInChildren<Transform>(true))
                if(t.name is "Hips" or "Spine" or "Chest" or "Head" or "L_UpperArm" or "R_UpperArm" or "L_Forearm" or "R_Forearm" or "L_Thigh" or "R_Thigh" or "L_Shin" or "R_Shin" or "L_Foot" or "R_Foot")
                    bones[t.name]=new Bone{transform=t,rest=t.localRotation};
            if(loop!=null){loop.PresentationDispatched+=Events;previous=loop.playerView.position;lastTime=loop.Sim?.State.Now??0;}
        }
        void Events(IReadOnlyList<PresentationEvent> events)
        {foreach(var e in events)if(e.Type==PresentationEventType.Dodge)dodgeUntil=e.Now+.28f;}
        void Pose(string name,float x,float z=0)
        {
            if(!bones.TryGetValue(name,out var b))return;
            var p=b.transform.parent;
            var right=p.InverseTransformDirection(transform.right);
            var forward=p.InverseTransformDirection(transform.forward);
            b.transform.localRotation=Quaternion.AngleAxis(z,forward)*Quaternion.AngleAxis(x,right)*b.rest;
        }
        void LateUpdate()
        {
            if(loop?.Sim==null)return;
            float now=loop.Sim.State.Now,dt=now-lastTime;
            if(loop.Sim.State.Phase!=RunPhase.Playing||dt<=0)return;
            var current=loop.playerView.position;float distance=Vector3.Distance(current,previous);
            if(distance>4||dt>.5f)distance=0;
            phase+=distance/2.15f*Mathf.PI*2;
            blend=Mathf.Lerp(blend,Mathf.Clamp01(distance/Mathf.Max(dt,.001f)/3),1-Mathf.Exp(-dt*14));
            if(distance>.0001f)
            {
                var local=transform.InverseTransformDirection((current-previous).normalized);
                travel=Vector2.Lerp(travel,new Vector2(local.x,local.z),1-Mathf.Exp(-dt*12));
            }
            float step=Mathf.Sin(phase)*blend, liftL=Mathf.Max(0,-Mathf.Sin(phase))*blend,liftR=Mathf.Max(0,Mathf.Sin(phase))*blend;
            float dodge=Mathf.Clamp01((dodgeUntil-now)/.28f);
            Pose("Hips",dodge*13,Mathf.Sin(phase)*2*blend);
            Pose("Spine",2+Mathf.Sin(now*1.8f)*.9f+dodge*12);
            Pose("Chest",-1,-step*3);Pose("Head",-dodge*12,Mathf.Sin(now*.7f)*1.5f);
            Pose("L_Thigh",step*29*travel.y,-step*17*travel.x);Pose("R_Thigh",-step*29*travel.y,step*17*travel.x);
            Pose("L_Shin",-liftL*42);Pose("R_Shin",-liftR*42);
            Pose("L_Foot",liftL*18);Pose("R_Foot",liftR*18);
            Pose("L_UpperArm",-step*23-7-dodge*30,4);Pose("R_UpperArm",step*23-7-dodge*30,-4);
            Pose("L_Forearm",-12-liftR*18);Pose("R_Forearm",-12-liftL*18);
            previous=current;lastTime=now;
        }
        void OnDisable(){if(loop!=null)loop.PresentationDispatched-=Events;foreach(var b in bones.Values)if(b.transform!=null)b.transform.localRotation=b.rest;}
    }
}
