using System;
using UnityEngine;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Bounded world-aligned spring field. Presentation only; no plant colliders or gameplay writes.</summary>
    public sealed class WheatContactField
    {
        public const int Size=64;
        public const float Cell=.375f, Extent=Size*Cell;
        Vector2[] bend=new Vector2[Size*Size],velocity=new Vector2[Size*Size];
        Vector2[] nextBend=new Vector2[Size*Size],nextVelocity=new Vector2[Size*Size];
        Vector2Int origin;
        Vector2 previousPlayer;
        float previousTime;
        bool initialized;
        public Vector2 Origin=>new Vector2(origin.x*Cell,origin.y*Cell);
        public float Peak {get;private set;}
        public int Resets {get;private set;}
        public float Time=>previousTime;
        Vector2Int Anchor(Vector2 p)=>new Vector2Int(Mathf.FloorToInt(p.x/Cell)-Size/2,Mathf.FloorToInt(p.y/Cell)-Size/2);
        void Reset(Vector2 player,float now)
        {
            Array.Clear(bend,0,bend.Length);Array.Clear(velocity,0,velocity.Length);
            origin=Anchor(player);previousPlayer=player;previousTime=now;initialized=true;Peak=0;Resets++;
        }
        void Shift(Vector2Int anchor)
        {
            var delta=anchor-origin;if(Mathf.Abs(delta.x)<4&&Mathf.Abs(delta.y)<4)return;
            Array.Clear(nextBend,0,nextBend.Length);Array.Clear(nextVelocity,0,nextVelocity.Length);
            for(int z=0;z<Size;z++)for(int x=0;x<Size;x++)
            {
                int sx=x+delta.x,sz=z+delta.y;if(sx<0||sz<0||sx>=Size||sz>=Size)continue;
                nextBend[z*Size+x]=bend[sz*Size+sx];nextVelocity[z*Size+x]=velocity[sz*Size+sx];
            }
            var swap=bend;bend=nextBend;nextBend=swap;swap=velocity;velocity=nextVelocity;nextVelocity=swap;origin=anchor;
        }
        public bool Step(Vector2 player,float now,bool playing,Vector4[] pickups=null,int count=0)
        {
            if(!initialized){Reset(player,now);return true;}
            if(!playing||now<=previousTime)return false;
            float dt=now-previousTime;Vector2 motion=player-previousPlayer;
            if(dt>.5f||motion.sqrMagnitude>36){Reset(player,now);return true;}
            Shift(Anchor(player));
            int steps=Mathf.Max(1,Mathf.CeilToInt(dt/.0125f));float step=dt/steps;
            float lengthSquared=motion.sqrMagnitude;Vector2 forward=lengthSquared>.00001f?motion.normalized:Vector2.up;
            Vector2 worldOrigin=Origin;Peak=0;
            for(int z=0;z<Size;z++)for(int x=0;x<Size;x++)
            {
                int i=z*Size+x;Vector2 p=worldOrigin+new Vector2((x+.5f)*Cell,(z+.5f)*Cell);
                float along=lengthSquared>.00001f?Mathf.Clamp01(Vector2.Dot(p-previousPlayer,motion)/lengthSquared):1;
                Vector2 away=p-(previousPlayer+motion*along);float distance=away.magnitude;
                float touch=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,1.25f,distance));
                Vector2 direction=(away/(distance+.02f)+forward*.24f).normalized;
                Vector2 target=direction*(touch*.78f);
                int n=pickups==null?0:Mathf.Min(count,8);
                for(int j=0;j<n;j++)
                {
                    Vector2 delta=p-new Vector2(pickups[j].x,pickups[j].z);float d=delta.magnitude;
                    float force=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.9f,d)))*.32f;
                    if(force>target.magnitude)target=delta/(d+.02f)*force;
                }
                // Fast, soft contact; slower damped recovery leaves a short-lived trail.
                float frequency=target.sqrMagnitude>.0001f?10:4.6f,damping=target.sqrMagnitude>.0001f?18:8;
                var b=bend[i];var v=velocity[i];
                for(int s=0;s<steps;s++){v+=((target-b)*(frequency*frequency)-v*damping)*step;b+=v*step;}
                b=Vector2.ClampMagnitude(b,.9f);if(b.sqrMagnitude<.00000001f&&v.sqrMagnitude<.00000001f){b=Vector2.zero;v=Vector2.zero;}
                bend[i]=b;velocity[i]=v;Peak=Mathf.Max(Peak,b.magnitude);
            }
            previousPlayer=player;previousTime=now;return true;
        }
        public Vector2 Sample(Vector2 world)
        {
            Vector2 p=(world-Origin)/Cell-new Vector2(.5f,.5f);
            int x=Mathf.FloorToInt(p.x),z=Mathf.FloorToInt(p.y);if(x<0||z<0||x>=Size-1||z>=Size-1)return Vector2.zero;
            return Vector2.Lerp(Vector2.Lerp(bend[z*Size+x],bend[z*Size+x+1],p.x-x),Vector2.Lerp(bend[(z+1)*Size+x],bend[(z+1)*Size+x+1],p.x-x),p.y-z);
        }
        public void WritePixels(Color[] pixels)
        {for(int i=0;i<bend.Length;i++)pixels[i]=new Color(bend[i].x,bend[i].y,0,1);}
    }
}
