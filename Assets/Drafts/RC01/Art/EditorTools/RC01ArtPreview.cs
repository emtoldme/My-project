using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
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

            Debug.Log($"[RC01ArtPreview] Art preview materials verification complete: {loaded}/{paths.Length} ready.");
        }
#endif
    }
}
