using UnityEngine;
using UnityEngine.Rendering;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>One 8m culling cell, shared original solid meshes; no copied stalk vertices.</summary>
    public sealed class InstancedWheatPatch
    {
        public const int Capacity=96;
        public Bounds Bounds {get;private set;}
        public int Lod {get;private set;}=1;
        public int Count {get;private set;}
        readonly Matrix4x4[][] matrices;
        readonly Vector4[][] paint;
        readonly int[] counts;
        readonly MaterialPropertyBlock[] blocks;
        bool uploaded;
        public InstancedWheatPatch(Vector3 center,int variants)
        {
            Bounds=new Bounds(center+Vector3.up, new Vector3(10,5,10));
            matrices=new Matrix4x4[variants][];paint=new Vector4[variants][];counts=new int[variants];blocks=new MaterialPropertyBlock[variants];
            for(int i=0;i<variants;i++){matrices[i]=new Matrix4x4[Capacity];paint[i]=new Vector4[Capacity];blocks[i]=new MaterialPropertyBlock();}
        }
        public void Add(int variant,Matrix4x4 matrix,Vector4 color)
        {int i=counts[variant];if(i>=Capacity)return;matrices[variant][i]=matrix;paint[variant][i]=color;counts[variant]++;Count++;}
        public void Upload()
        {
            for(int variant=0;variant<counts.Length;variant++)if(counts[variant]>0)
            {
                // Hash order makes far thinning spatially distributed, not the
                // first two thirds of grid rows. Arrays are reused after streaming.
                for(int i=1;i<counts[variant];i++)
                {
                    var color=paint[variant][i];var matrix=matrices[variant][i];int j=i-1;
                    while(j>=0&&paint[variant][j].z>color.z){paint[variant][j+1]=paint[variant][j];matrices[variant][j+1]=matrices[variant][j];j--;}
                    paint[variant][j+1]=color;matrices[variant][j+1]=matrix;
                }
                float ripeness=0;for(int i=0;i<counts[variant];i++)ripeness+=paint[variant][i].y;
                blocks[variant].SetVector("_PatchPaint",new Vector4(1,ripeness/counts[variant],.5f,1));
            }
            uploaded=true;
        }
        public void Reset(Vector3 center)
        {Bounds=new Bounds(center+Vector3.up,new Vector3(10,5,10));System.Array.Clear(counts,0,counts.Length);Count=0;uploaded=false;Lod=1;}
        public int Draw(Camera camera,Vector3 player,Mesh[][] meshes,Material[] materials,int layer,out int triangles)
        {
            triangles=0;if(!uploaded)return 0;
            float distance=Vector2.Distance(new Vector2(Bounds.center.x,Bounds.center.z),new Vector2(player.x,player.z));
            // Hysteresis prevents repeated swaps when standing at a detail boundary.
            if(Lod==0&&distance>11.5f)Lod=1;
            else if(Lod==1&&distance<8.5f)Lod=0;
            else if(Lod==1&&distance>27.5f)Lod=2;
            else if(Lod==2&&distance<24.5f)Lod=1;
            int calls=0;var shadow=distance<25?ShadowCastingMode.On:ShadowCastingMode.Off;
            for(int i=0;i<counts.Length;i++)if(counts[i]>0)
            {
                var mesh=meshes[Lod][i%meshes[Lod].Length];int count=counts[i];
                // Far density is deterministically reduced without allocations or rule changes.
                if(Lod==2)count=Mathf.Max(1,(count*2)/3);
                Graphics.DrawMeshInstanced(mesh,0,materials[Lod],matrices[i],count,blocks[i],shadow,true,layer,camera,LightProbeUsage.Off,null);
                triangles+=(int)mesh.GetIndexCount(0)/3*count;calls++;
            }
            return calls;
        }
    }
}
