using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Bounded presentation streaming. Ground is flat, matching the rules' XZ play plane.</summary>
    [DefaultExecutionOrder(70)]
    public sealed class ProceduralFieldStreamer : MonoBehaviour
    {
        public const int ChunkSize=24;
        public GameLoop loop;
        public Terrain authoredTerrain;
        public Transform authoredWheat;
        public Texture2D authoredDressing;
        public Material groundMaterial,wheatMaterial;
        public Mesh[] stalkMeshes;
        public Mesh[] mediumStalkMeshes, distantStalkMeshes;
        public int ResidentWheatVertices { get { int n=0; foreach(var c in live.Values)n+=c.wheat.vertexCount;return n; } }
        public int ResidentWheatTriangles { get { int n=0; foreach(var c in live.Values)n+=(int)c.wheat.GetIndexCount(0)/3;return n; } }
        public int[] ActiveWheatLods { get { var n=new int[3];foreach(var c in live.Values)n[c.lod]++;return n; } }
        public float WheatVisualTime=>wind!=null?wind.GetFloat("_VisualTime"):0;
        public GameObject[] landmarkPrefabs;
        [Range(2,4)] public int loadRadius=3;
        public int worldSeed=14104;
        public bool randomizeSeed=true;
        public int Seed {get;private set;}
        public int LoadedChunkCount=>live.Count;
        public int ResidentChunkCount=>all.Count;
        public int ChunkBudget=>(loadRadius*2+1)*(loadRadius*2+1);
        public int GeneratedChunks {get;private set;}
        public ProceduralFieldLayout Layout {get;private set;}
        public Vector2Int Center {get;private set;}
        readonly Dictionary<Vector2Int,Chunk> live=new Dictionary<Vector2Int,Chunk>();
        readonly List<Chunk> all=new List<Chunk>();
        readonly Stack<Chunk> spare=new Stack<Chunk>();
        readonly HashSet<Vector2Int> wanted=new HashSet<Vector2Int>();
        readonly List<Vector2Int> pending=new List<Vector2Int>(),removed=new List<Vector2Int>();
        readonly List<Vector3> vertices=new List<Vector3>(40000),normals=new List<Vector3>(40000);
        readonly List<Vector2> uv=new List<Vector2>(40000);
        readonly List<Vector4> surfaceData=new List<Vector4>(40000);
        readonly List<Vector4> roots=new List<Vector4>(40000);
        readonly List<Color> colors=new List<Color>(40000);
        readonly List<int> indices=new List<int>(150000);
        Source[][] levels;Material wind;Transform generated,fallback;Mesh fallbackMesh;
        bool initialized,terrainActive,wheatActive;
        sealed class Source{public Vector3[] vertices,normals;public Vector2[] uv;public Vector4[] surface;public int[] indices;}
        sealed class Chunk{public Vector2Int key;public Transform root;public Mesh ground,wheat;public int lod;public GameObject landmark;public Light[] lights;}

        void Start()
        {
            if(loop==null)loop=GetComponent<GameLoop>();
            if(loop==null||authoredTerrain==null||authoredWheat==null||groundMaterial==null||wheatMaterial==null||stalkMeshes==null||stalkMeshes.Length==0){enabled=false;Debug.LogError("Exploration field bindings missing");return;}
            loadRadius=Mathf.Clamp(loadRadius,2,4);
            Seed=randomizeSeed?unchecked(Environment.TickCount*397^Guid.NewGuid().GetHashCode()):worldSeed;
            Layout=new ProceduralFieldLayout(Seed,authoredTerrain.terrainData,authoredDressing);
            loop.InstallHomecomingWorld(Seed);
            levels=new[]{ReadSources(stalkMeshes),ReadSources(mediumStalkMeshes),ReadSources(distantStalkMeshes)};
            if(levels[1].Length==0)levels[1]=levels[0];if(levels[2].Length==0)levels[2]=levels[1];
            wind=new Material(wheatMaterial){name="Exploration wheat · run instance"};wind.SetFloat("_ProceduralField",1);wind.SetFloat("_UseExternalTime",1);
            generated=new GameObject("Exploration · resident field chunks").transform;generated.SetParent(transform,false);
            terrainActive=authoredTerrain.gameObject.activeSelf;wheatActive=authoredWheat.gameObject.activeSelf;HideAuthored();
            fallbackMesh=new Mesh{name="Streaming ground underlay",vertices=new[]{new Vector3(-1,0,-1),new Vector3(-1,0,1),new Vector3(1,0,1),new Vector3(1,0,-1)},normals=new[]{Vector3.up,Vector3.up,Vector3.up,Vector3.up},colors=new[]{new Color(.18f,.68f,.14f),new Color(.18f,.68f,.14f),new Color(.18f,.68f,.14f),new Color(.18f,.68f,.14f)},triangles=new[]{0,1,2,0,2,3}};
            fallback=Surface("Streaming ground underlay",generated,fallbackMesh,groundMaterial);
            fallback.localScale=new Vector3(ChunkSize*(loadRadius+2),1,ChunkSize*(loadRadius+2));
            initialized=true;Refresh(true);
            // The nearest nine are ready before the first playable frame. The outer ring fills incrementally.
            for(int i=0;i<9&&pending.Count>0;i++)BuildNext();
            Debug.Log("EXPLORATION_FIELD_STARTED seed="+Seed+" chunkBudget="+ChunkBudget);
        }
        void Update()
        {
            if(!initialized||loop.Sim==null)return;
            wind.SetFloat("_VisualTime",loop.Sim.State.Now);
            float rain=Shader.GetGlobalVector("_ReverbWeather").x;
            foreach(var chunk in live.Values)if(chunk.lights!=null)foreach(var light in chunk.lights)if(light!=null&&light.color.r>.5f)light.intensity=Mathf.Lerp(.5f,3.2f,rain);
            if(loop.Sim.State.Phase!=RunPhase.Playing)return;
            Refresh(false);if(pending.Count>0)BuildNext();else RefreshDetail();
        }
        public static Vector2Int Coordinate(float x,float z)=>new Vector2Int(Mathf.FloorToInt(x/ChunkSize),Mathf.FloorToInt(z/ChunkSize));
        void Refresh(bool force)
        {
            var center=Coordinate(loop.Sim.PlayerX,loop.Sim.PlayerZ);if(!force&&center==Center)return;Center=center;
            fallback.position=new Vector3((center.x+.5f)*ChunkSize,-.035f,(center.y+.5f)*ChunkSize);
            wanted.Clear();pending.Clear();removed.Clear();
            for(int z=-loadRadius;z<=loadRadius;z++)for(int x=-loadRadius;x<=loadRadius;x++)
            {var key=center+new Vector2Int(x,z);wanted.Add(key);if(!live.ContainsKey(key))pending.Add(key);}
            foreach(var pair in live)if(!wanted.Contains(pair.Key))removed.Add(pair.Key);
            foreach(var key in removed){var chunk=live[key];live.Remove(key);chunk.root.gameObject.SetActive(false);if(chunk.landmark!=null){Destroy(chunk.landmark);chunk.landmark=null;chunk.lights=null;}spare.Push(chunk);}
            pending.Sort((a,b)=>(b-center).sqrMagnitude.CompareTo((a-center).sqrMagnitude));
        }
        void BuildNext()
        {
            int last=pending.Count-1;var key=pending[last];pending.RemoveAt(last);
            Chunk chunk;
            if(spare.Count>0)chunk=spare.Pop();
            else
            {
                if(all.Count>=ChunkBudget)return;
                chunk=new Chunk{root=new GameObject("Field chunk").transform,ground=new Mesh{name="Procedural soil"},wheat=new Mesh{name="Procedural rye",indexFormat=IndexFormat.UInt32}};
                chunk.root.SetParent(generated,false);Surface("Soil and tracks",chunk.root,chunk.ground,groundMaterial);Surface("Wind rye and stubble",chunk.root,chunk.wheat,wind);all.Add(chunk);
            }
            chunk.key=key;chunk.root.name="Field "+key.x+","+key.y;chunk.root.position=new Vector3(key.x*ChunkSize,0,key.y*ChunkSize);
            BuildGround(chunk);BuildWheat(chunk);AddLandmark(chunk);chunk.root.gameObject.SetActive(true);live.Add(key,chunk);GeneratedChunks++;
        }
        static Source[] ReadSources(Mesh[] meshes)
        {
            if(meshes==null)return Array.Empty<Source>();
            var result=new Source[meshes.Length];
            for(int i=0;i<result.Length;i++){var m=meshes[i];result[i]=new Source{vertices=m.vertices,normals=m.normals,uv=m.uv,surface=ReadSurface(m),indices=m.triangles};}
            return result;
        }
        static Vector4[] ReadSurface(Mesh mesh)
        {var values=new List<Vector4>();mesh.GetUVs(2,values);return values.ToArray();}
        int Detail(Vector2Int key)
        {
            int distance=Mathf.Max(Mathf.Abs(key.x-Center.x),Mathf.Abs(key.y-Center.y));
            return distance<=1?0:distance<=2?1:2;
        }
        void RefreshDetail()
        {
            // One existing patch per frame. Nearest detail upgrades precede distant simplification.
            Chunk next=null;int priority=int.MaxValue;
            foreach(var chunk in live.Values)
            {
                int lod=Detail(chunk.key);if(lod==chunk.lod)continue;
                int rank=lod*100+(chunk.key-Center).sqrMagnitude;
                if(rank<priority){next=chunk;priority=rank;}
            }
            if(next!=null)BuildWheat(next);
        }
        void ClearBuffers(){vertices.Clear();normals.Clear();uv.Clear();roots.Clear();surfaceData.Clear();colors.Clear();indices.Clear();}
        void BuildGround(Chunk chunk)
        {
            ClearBuffers();const int grid=12;
            for(int z=0;z<=grid;z++)for(int x=0;x<=grid;x++)
            {
                float px=x*2,pz=z*2;var s=Layout.Sample(chunk.key.x*ChunkSize+px,chunk.key.y*ChunkSize+pz);
                vertices.Add(new Vector3(px,0,pz));normals.Add(Vector3.up);uv.Add(new Vector2(px,pz));colors.Add(new Color(s.Soil,Mathf.Max(0,1-s.Soil-s.Straw),s.Straw));
                if(x<grid&&z<grid){int i=z*(grid+1)+x;indices.AddRange(new[]{i,i+grid+1,i+1,i+1,i+grid+1,i+grid+2});}
            }
            Upload(chunk.ground);
        }
        void BuildWheat(Chunk chunk)
        {
            ClearBuffers();chunk.lod=Detail(chunk.key);var sources=levels[chunk.lod];const int grid=26;const float spacing=ChunkSize/(float)grid;
            for(int z=0;z<grid;z++)for(int x=0;x<grid;x++)
            {
                int gx=chunk.key.x*grid+x,gz=chunk.key.y*grid+z;
                float px=(x+.5f)*spacing+(Layout.Random(gx,gz,101)-.5f)*.34f,pz=(z+.5f)*spacing+(Layout.Random(gx,gz,102)-.5f)*.34f;
                var sample=Layout.Sample(chunk.key.x*ChunkSize+px,chunk.key.y*ChunkSize+pz);
                if(sample.Density<.02f||Layout.Random(gx,gz,103)>sample.Density)continue;
                var source=sources[Mathf.Min(sources.Length-1,(int)(Layout.Random(gx,gz,104)*sources.Length))];
                float a=Layout.Random(gx,gz,105)*Mathf.PI*2,c=Mathf.Cos(a),s=Mathf.Sin(a),width=.86f+Layout.Random(gx,gz,106)*.28f,height=.78f+Layout.Random(gx,gz,107)*.24f;
                int start=vertices.Count;
                for(int k=0;k<source.vertices.Length;k++)
                {
                    var p=source.vertices[k];p=new Vector3(p.x*width,p.y*height,p.z*width);vertices.Add(new Vector3(p.x*c+p.z*s+px,p.y,-p.x*s+p.z*c+pz));
                    var n=source.normals[k];n=new Vector3(n.x/width,n.y/height,n.z/width).normalized;
                    normals.Add(new Vector3(n.x*c+n.z*s,n.y,-n.x*s+n.z*c));uv.Add(source.uv[k]);
                    roots.Add(new Vector4(px,pz,WheatGeometry.Height*height,Layout.Random(gx,gz,108)));
                    surfaceData.Add(k<source.surface.Length?source.surface[k]:new Vector4(.5f,p.y/WheatGeometry.Height,.5f,1));
                    colors.Add(new Color(sample.Height,sample.Ripeness,Layout.Random(gx,gz,109),1));
                }
                foreach(int index in source.indices)indices.Add(start+index);
            }
            Upload(chunk.wheat);chunk.wheat.bounds=new Bounds(new Vector3(12,1,12),new Vector3(27,5,27));
        }
        void Upload(Mesh mesh){mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(colors);if(roots.Count==vertices.Count)mesh.SetUVs(1,roots);if(surfaceData.Count==vertices.Count)mesh.SetUVs(2,surfaceData);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();}
        void AddLandmark(Chunk chunk)
        {
            int minX=Mathf.FloorToInt(chunk.root.position.x/72)-1,minZ=Mathf.FloorToInt(chunk.root.position.z/72)-1;
            for(int z=minZ;z<=minZ+2;z++)for(int x=minX;x<=minX+2;x++)
            {
                var site=Layout.Landmark(x,z);if(!site.Exists||Coordinate(site.Position.x,site.Position.y)!=chunk.key||site.Kind>=landmarkPrefabs.Length||landmarkPrefabs[site.Kind]==null)continue;
                chunk.landmark=Instantiate(landmarkPrefabs[site.Kind],chunk.root);chunk.landmark.name=site.Kind==0?"Abandoned harvest machine":site.Kind==1?"Forgotten field swing":"Quiet radio outpost";
                chunk.landmark.transform.SetPositionAndRotation(new Vector3(site.Position.x,0,site.Position.y),Quaternion.Euler(0,site.Yaw,0));
                chunk.lights=chunk.landmark.GetComponentsInChildren<Light>();
            }
        }
        static Transform Surface(string name,Transform parent,Mesh mesh,Material material)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            // Flat floors receive the wheat/scenery shadows, but must not cast onto
            // the almost coplanar streaming underlay (large unstable dark patches).
            if(name!="Wind rye and stubble")renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }
        void HideAuthored(){authoredTerrain.gameObject.SetActive(false);authoredWheat.gameObject.SetActive(false);}
        void OnEnable(){if(initialized){HideAuthored();generated.gameObject.SetActive(true);}}
        void OnDisable(){if(initialized){if(generated!=null)generated.gameObject.SetActive(false);if(authoredTerrain!=null)authoredTerrain.gameObject.SetActive(terrainActive);if(authoredWheat!=null)authoredWheat.gameObject.SetActive(wheatActive);}}
        void OnDestroy(){foreach(var chunk in all){if(chunk.ground!=null)Destroy(chunk.ground);if(chunk.wheat!=null)Destroy(chunk.wheat);}if(fallbackMesh!=null)Destroy(fallbackMesh);if(wind!=null)Destroy(wind);if(generated!=null)Destroy(generated.gameObject);}
    }
}
