using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
#endif

namespace Reverb.Drafts.RC01.Art.EditorTools
{
    /// <summary>
    /// RC01 主美四图材质展示板与同机位预览工具。
    /// 仅限 Editor 平台运行，幂等生成与刷新，所有产物保留在 Assets/Drafts/RC01/Art/ 内部。
    /// </summary>
    public static class RC01ArtPreview
    {
        public const string MaterialsRoot = "Assets/Drafts/RC01/Art/Materials";
        public const string HarvestRoadMatPath = MaterialsRoot + "/M_RC01_HarvestRoad_Ground.mat";
        public const string OldMarketMatPath = MaterialsRoot + "/M_RC01_OldMarket_Metal.mat";
        public const string FourthGranaryMatPath = MaterialsRoot + "/M_RC01_FourthGranary_Concrete.mat";
        public const string RadioWastelandMatPath = MaterialsRoot + "/M_RC01_RadioWasteland_Ash.mat";
        public const string ControlledFoliageMatPath = MaterialsRoot + "/M_RC01_ControlledFoliage.mat";

#if UNITY_EDITOR
        [MenuItem("Reverb/RC01/Art/Build Four-Map Style Previews", false, 201)]
        public static void Build()
        {
            Debug.Log("[RC01ArtPreview] Verifying Four-Map Art Direction Materials and Spec...");
            
            int loaded = 0;
            string[] paths = new string[]
            {
                HarvestRoadMatPath,
                OldMarketMatPath,
                FourthGranaryMatPath,
                RadioWastelandMatPath,
                ControlledFoliageMatPath
            };

            foreach (var path in paths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    loaded++;
                    Debug.Log($"[RC01ArtPreview] Loaded valid material copy: {path} (Shader: {mat.shader.name})");
                }
                else
                {
                    Debug.LogWarning($"[RC01ArtPreview] Material not found at: {path}");
                }
            }

            if (loaded != paths.Length) throw new System.InvalidOperationException("RC01 art materials incomplete");
            Directory.CreateDirectory("Assets/Drafts/RC01/Art/Scenes");
            float[] smoothness = { .14f,.48f,.12f,.1f };
            for (int i = 0;i < 4;i++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
                mat.SetFloat("_Smoothness",smoothness[i]); EditorUtility.SetDirty(mat);
                var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Object.DestroyImmediate(plane.GetComponent<Collider>()); plane.GetComponent<Renderer>().sharedMaterial = mat;
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "TEST_ONLY_MaterialProxy"; cube.transform.position = new Vector3(0,.8f,0);
                cube.transform.localScale = new Vector3(2,1.6f,2); cube.GetComponent<Renderer>().sharedMaterial = mat;
                var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 5;
                camera.transform.rotation = Quaternion.Euler(55,45,0);
                camera.transform.position = -camera.transform.forward*15;
                var light = new GameObject("PreviewLight").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.1f; light.transform.rotation = Quaternion.Euler(50,-30,0);
                RenderSettings.ambientLight = new Color(.5f,.52f,.5f);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Drafts/RC01/Art/Scenes/TEST_ONLY_Style_"+i+".unity");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("RC01_ART_PREVIEWS_PASSED styles=4 test-material-proxies=true");
        }
#endif
    }
}
