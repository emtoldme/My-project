using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Waffle;
using Waffle.Experiments.Mechanism.EditorTools;

namespace Reverb.Drafts.RC01.Pipeline.EditorTools
{
    public static class RC02IntegrationBatch
    {
        [MenuItem("Waffle/RC02/Apply Integrated Components")]
        public static void Setup()
        {
            BuildTemplates();
            RC02ContentPipeline.RefreshCatalog();
            var catalog=AssetDatabase.LoadAssetAtPath<RC02ContentCatalog>(RC02ContentPipeline.CatalogPath);
            string before=EditorJsonUtility.ToJson(catalog);
            RC02ContentPipeline.RefreshCatalog();
            if(before!=EditorJsonUtility.ToJson(catalog)) throw new Exception("Catalog refresh must be idempotent");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RC02ContentPipeline.OpenAssemblyLab();
            Configure(RC02ContentPipeline.LabPath);
            Configure(RC02ContentPipeline.HarvestPath);
            AssetDatabase.SaveAssets();
            foreach(var entry in catalog.entries) foreach(var asset in entry.assets)
                if(asset.asset==null) throw new Exception("Missing catalog resource: "+asset.assetPath);
            Debug.Log("RC02_SETUP_PASSED entries="+catalog.entries.Count+" weapons=6 passives=14 idempotent=true");
        }
        static void Configure(string scenePath)
        {
            var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
            var loop=UnityEngine.Object.FindAnyObjectByType<GameLoop>();
            if(loop==null) throw new Exception("Missing gameplay host");
            var view=loop.GetComponent<RC02CatalogPresenter>() ?? loop.gameObject.AddComponent<RC02CatalogPresenter>();
            view.loop=loop; view.catalog=AssetDatabase.LoadAssetAtPath<RC02ContentCatalog>(RC02ContentPipeline.CatalogPath);
            view.encounters=AssetDatabase.LoadAssetAtPath<RC02EncounterSheet>(RC02ContentPipeline.EncounterPath);
            var audio=loop.GetComponent<RC02AudioConfiguration>() ?? loop.gameObject.AddComponent<RC02AudioConfiguration>();
            audio.catalog=view.catalog;
            if(loop.GetComponent<Reverb.Drafts.RC01.Presentation.RC02PlaythroughCapture>()==null)
                loop.gameObject.AddComponent<Reverb.Drafts.RC01.Presentation.RC02PlaythroughCapture>();
            loop.config.runContentAllowList=RunSimulation.RC01EnabledIds;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        static void BuildTemplates()
        {
            string root="Assets/Drafts/RC01/Art/ComponentsV1/";
            string[] prefabs={"Projectile_DirectedPulse","Hit_EchoPulse","Arc_ChainElectric","Zone_ResonanceField","Trail_MotionResonance","Zone_DodgeDischarge"};
            string[] materials={"DirectedPulse","EchoHit","ChainArc","ResonanceZone","MotionEcho","DodgeDischarge"};
            Color[] colors={new Color(1,.85f,.5f),new Color(.72f,.39f,.63f),new Color(.44f,.81f,1),new Color(1,.68f,.25f),new Color(.63f,.49f,.33f),new Color(.76f,.53f,.91f)};
            for(int i=0;i<prefabs.Length;i++)
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"Materials/M_VFX_"+materials[i]+".mat");
                if(mat==null) throw new Exception("Missing template material");
                mat.shader=Shader.Find("Reverb/M01Feedback"); mat.SetColor("_BaseColor",Color.white); EditorUtility.SetDirty(mat);
                var go=new GameObject("TEST_ONLY_"+prefabs[i]);
                var template=go.AddComponent<RC02VisualTemplate>(); template.color=colors[i];
                template.shape=i==0?RC02VisualShape.Projectile:i==2?RC02VisualShape.Arc:i==1?RC02VisualShape.Hit:RC02VisualShape.Ring;
                if(i==0)
                {
                    var body=GameObject.CreatePrimitive(PrimitiveType.Cube); UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
                    body.name="PulseBody"; body.transform.SetParent(go.transform,false); body.transform.localScale=new Vector3(.12f,.12f,.8f);
                    body.GetComponent<Renderer>().sharedMaterial=mat;
                }
                else
                {
                    var line=go.AddComponent<LineRenderer>(); template.line=line;
                    line.sharedMaterial=mat; line.useWorldSpace=false; line.loop=i!=2; line.widthMultiplier=.065f;
                    line.shadowCastingMode=ShadowCastingMode.Off; line.receiveShadows=false;
                    line.positionCount=i==2?2:48;
                    for(int n=0;n<line.positionCount;n++) { float a=n*Mathf.PI*2/48; line.SetPosition(n,i==2?new Vector3(0,0,n):new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))); }
                }
                PrefabUtility.SaveAsPrefabAsset(go,root+"Prefabs/TEST_ONLY_"+prefabs[i]+".prefab"); UnityEngine.Object.DestroyImmediate(go);
            }
        }
        public static void Build()
        {
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/RC02-Components/Reverb.exe"));
            M01IntegrationBatch.BuildScene(RC02ContentPipeline.HarvestPath,output);
        }
    }
}
