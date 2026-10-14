using UnityEngine;

namespace Reverb.Drafts.RC01.Presentation
{
    public readonly struct FieldSample
    {
        public readonly float Soil,Straw,Height,Density,Ripeness;
        public FieldSample(float soil,float straw,float height,float density,float ripeness)
        {Soil=soil;Straw=straw;Height=height;Density=density;Ripeness=ripeness;}
    }
    public readonly struct FieldLandmark
    {
        public readonly Vector2 Position;
        public readonly int Kind;
        public readonly float Radius,Yaw;
        public bool Exists=>Kind>=0;
        public FieldLandmark(Vector2 position,int kind,float radius,float yaw){Position=position;Kind=kind;Radius=radius;Yaw=yaw;}
    }

    /// <summary>Coordinate-hashed, order-independent layout; never consumes gameplay RNG.</summary>
    public sealed class ProceduralFieldLayout
    {
        public readonly int Seed;
        readonly float[,,] homePaint;
        readonly Color[] homeDress;
        readonly int paintSize,dressSize;
        readonly Vector2 homeOrigin,homeSize;
        readonly float phase;
        public ProceduralFieldLayout(int seed,TerrainData authoredHome=null,Texture2D dressing=null)
        {
            Seed=seed;phase=Random(0,0,19)*6.28f;
            homeOrigin=new Vector2(-48,-48);homeSize=new Vector2(96,96);
            if(authoredHome!=null){paintSize=authoredHome.alphamapResolution;homePaint=authoredHome.GetAlphamaps(0,0,paintSize,paintSize);homeSize=new Vector2(authoredHome.size.x,authoredHome.size.z);homeOrigin=-homeSize*.5f;}
            if(dressing!=null&&dressing.isReadable){dressSize=dressing.width;homeDress=dressing.GetPixels();}
        }
        public float Random(int x,int z,uint salt=0)
        {
            unchecked
            {
                uint h=(uint)Seed^(uint)x*0x9E3779B9u^(uint)z*0x85EBCA6Bu^salt*0xC2B2AE35u;
                h^=h>>16;h*=0x7FEB352Du;h^=h>>15;h*=0x846CA68Bu;h^=h>>16;
                return (h&0xffffff)/16777216f;
            }
        }
        float Noise(float x,float z,float scale,uint salt)
        {
            x*=scale;z*=scale;int a=Mathf.FloorToInt(x),b=Mathf.FloorToInt(z);
            float u=x-a,v=z-b;u=u*u*(3-2*u);v=v*v*(3-2*v);
            return Mathf.Lerp(Mathf.Lerp(Random(a,b,salt),Random(a+1,b,salt),u),Mathf.Lerp(Random(a,b+1,salt),Random(a+1,b+1,salt),u),v);
        }
        public float RoadDistance(float x,float z)
        {
            float column=Mathf.Round(x/72f)*72f,row=Mathf.Round(z/72f)*72f;
            float ns=column+Mathf.Sin(z*.037f+phase)*4+Mathf.Sin(z*.011f)*2;
            float ew=row+Mathf.Sin(x*.041f+phase)*4;
            return Mathf.Min(Mathf.Abs(x-ns),Mathf.Abs(z-ew));
        }
        public FieldLandmark Landmark(int cellX,int cellZ)
        {
            float x=cellX*72f,z=cellZ*72f;
            for(int i=0;i<3;i++){x=cellX*72f+Mathf.Sin(z*.037f+phase)*4+Mathf.Sin(z*.011f)*2;z=cellZ*72f+Mathf.Sin(x*.041f+phase)*4;}
            x+=Random(cellX,cellZ,51)<.5f?-5.8f:5.8f;z+=Random(cellX,cellZ,52)<.5f?-5.8f:5.8f;
            bool exists=(Mathf.Abs(x)>58||Mathf.Abs(z)>58)&&Random(cellX,cellZ,53)<.65f;
            return new FieldLandmark(new Vector2(x,z),exists?Mathf.Min(2,(int)(Random(cellX,cellZ,54)*3)):-1,6+Random(cellX,cellZ,55)*3,Random(cellX,cellZ,56)*360);
        }
        public FieldSample Sample(float x,float z)
        {
            float soil=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.35f,2.9f,RoadDistance(x,z)));
            float harvest=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.56f,.72f,Noise(x,z,.035f,31)));
            float height=Mathf.Lerp(.78f+Noise(x,z,.09f,32)*.22f,.18f,harvest);
            float straw=harvest*(1-soil)*.8f;
            float density=(1-soil)*Mathf.Lerp(.88f,.65f,harvest);
            float ripe=.22f+Noise(x,z,.027f,33)*.78f;
            int cx=Mathf.RoundToInt(x/72f),cz=Mathf.RoundToInt(z/72f);
            var landmark=Landmark(cx,cz);
            if(landmark.Exists)
            {
                float d=Vector2.Distance(new Vector2(x,z),landmark.Position)/landmark.Radius;
                float clearing=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1.1f,d));
                soil=Mathf.Max(soil,clearing*.86f);density*=1-clearing;straw*=1-clearing;
                if(landmark.Kind==2){float lodge=Mathf.Exp(-d*d*2);height=Mathf.Lerp(height,.28f,lodge);ripe=Mathf.Lerp(ripe,.08f,lodge);}
            }
            float edge=Mathf.Max(Mathf.Abs(x),Mathf.Abs(z));
            if(homePaint!=null&&edge<48)
            {
                float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(39,48,edge));
                float u=Mathf.Clamp01((x-homeOrigin.x)/homeSize.x),v=Mathf.Clamp01((z-homeOrigin.y)/homeSize.y);
                soil=Mathf.Lerp(soil,Paint(u,v,0),blend);straw=Mathf.Lerp(straw,Paint(u,v,2),blend);
                if(homeDress!=null)
                {Color dress=Dress(u,v);height=Mathf.Lerp(height,dress.r,blend);density=Mathf.Lerp(density,dress.g*(1-soil),blend);ripe=Mathf.Lerp(ripe,dress.b,blend);}
                else density*=1-soil;
            }
            return new FieldSample(soil,straw,height,Mathf.Clamp01(density),ripe);
        }
        float Paint(float u,float v,int layer)
        {
            float x=u*(paintSize-1),z=v*(paintSize-1);int a=Mathf.FloorToInt(x),b=Mathf.FloorToInt(z),c=Mathf.Min(a+1,paintSize-1),d=Mathf.Min(b+1,paintSize-1);
            return Mathf.Lerp(Mathf.Lerp(homePaint[b,a,layer],homePaint[b,c,layer],x-a),Mathf.Lerp(homePaint[d,a,layer],homePaint[d,c,layer],x-a),z-b);
        }
        Color Dress(float u,float v)
        {
            float x=u*(dressSize-1),z=v*(dressSize-1);int a=Mathf.FloorToInt(x),b=Mathf.FloorToInt(z),c=Mathf.Min(a+1,dressSize-1),d=Mathf.Min(b+1,dressSize-1);
            return Color.Lerp(Color.Lerp(homeDress[b*dressSize+a],homeDress[b*dressSize+c],x-a),Color.Lerp(homeDress[d*dressSize+a],homeDress[d*dressSize+c],x-a),z-b);
        }
    }
}
