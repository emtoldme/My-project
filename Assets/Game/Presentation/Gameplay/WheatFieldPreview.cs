using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Explicit scene preview using the same solid models, never a second live streaming system.</summary>
    [ExecuteAlways]
    public sealed class WheatFieldPreview : MonoBehaviour
    {
        public Terrain terrain;
        public Texture2D dressing;
        public Material material;
        public Mesh[] meshes;
        public ProceduralFieldStreamer streamer;
        List<Matrix4x4[]>[] batches;
        void OnEnable(){RenderPipelineManager.beginCameraRendering+=Draw;batches=null;}
        void OnDisable(){RenderPipelineManager.beginCameraRendering-=Draw;batches=null;}
        void OnValidate(){batches=null;}
        public void RefreshPreview(){batches=null;}
        void Draw(ScriptableRenderContext context,Camera camera)
        {
            if(Application.isPlaying&&streamer!=null&&streamer.enabled)return;
            if(terrain==null||material==null||meshes==null||meshes.Length==0)return;
            if(batches==null)Prepare();
            for(int i=0;i<meshes.Length;i++)foreach(var group in batches[i])
                Graphics.DrawMeshInstanced(meshes[i],0,material,group,group.Length,null,ShadowCastingMode.On,true,gameObject.layer,camera);
        }
        void Prepare()
        {
            var layout=new ProceduralFieldLayout(14104,terrain.terrainData,dressing);
            var groups=new List<Matrix4x4>[meshes.Length];for(int i=0;i<groups.Length;i++)groups[i]=new List<Matrix4x4>();
            const int grid=26;const float spacing=24f/grid;
            for(int cz=-2;cz<2;cz++)for(int cx=-2;cx<2;cx++)for(int z=0;z<grid;z++)for(int x=0;x<grid;x++)
            {
                int gx=cx*grid+x,gz=cz*grid+z;
                float px=cx*24+(x+.5f)*spacing+(layout.Random(gx,gz,101)-.5f)*.34f;
                float pz=cz*24+(z+.5f)*spacing+(layout.Random(gx,gz,102)-.5f)*.34f;
                var sample=layout.Sample(px,pz);if(sample.Density<.02f||layout.Random(gx,gz,103)>sample.Density)continue;
                int index=Mathf.Min(meshes.Length-1,(int)(layout.Random(gx,gz,104)*meshes.Length));
                float width=.86f+layout.Random(gx,gz,106)*.28f,height=(.78f+layout.Random(gx,gz,107)*.24f)*sample.Height;
                groups[index].Add(Matrix4x4.TRS(new Vector3(px,0,pz),Quaternion.Euler(0,layout.Random(gx,gz,105)*360,0),new Vector3(width,height,width)));
            }
            batches=new List<Matrix4x4[]>[meshes.Length];
            for(int i=0;i<groups.Length;i++)
            {
                batches[i]=new List<Matrix4x4[]>();for(int n=0;n<groups[i].Count;n+=1023)
                    batches[i].Add(groups[i].GetRange(n,Mathf.Min(1023,groups[i].Count-n)).ToArray());
            }
        }
    }
}
