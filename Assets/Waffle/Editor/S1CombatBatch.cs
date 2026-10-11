using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Waffle.EditorTools
{
    public static class S1CombatBatch
    {
        [MenuItem("Waffle/Setup S1 Combat")]
        public static void Setup()
        {
            S1PresentationSetup.Setup();
            var loop = UnityEngine.Object.FindAnyObjectByType<GameLoop>();
            if (loop == null) throw new Exception("Missing S1 GameLoop");
            var views = loop.GetComponent<CombatViews>();
            if (views == null) views = loop.gameObject.AddComponent<CombatViews>();
            views.loop = loop;
            views.enemyVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Waffle/Prefabs/Visual/Enemy_Vanguard_Mike_Visual.prefab");
            views.projectileMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Waffle/Art/Materials/M_FX_Muzzle_Cyan.mat");
            views.gemMaterial = views.projectileMaterial;
            if (views.enemyVisualPrefab == null || views.projectileMaterial == null) throw new Exception("Missing combat visual asset");
            loop.combatConfig = new Waffle.Combat.CombatConfig();
            // Keep the baseline's one-enemy/one-weapon design values; no art direction change.
            var console = loop.GetComponent<DebugConsole>();
            if (console != null) console.visible = false;
            EditorUtility.SetDirty(loop); EditorUtility.SetDirty(views);
            EditorSceneManager.MarkSceneDirty(loop.gameObject.scene);
            EditorSceneManager.SaveScene(loop.gameObject.scene);
            AssetDatabase.SaveAssets();
            S1IntegrationBatch.ValidateScene();
            Debug.Log("S1_COMBAT_SETUP_PASSED");
        }

        public static void Build()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/S1-C01/Waffle.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { S1SceneSetup.ScenePath }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("S1 C01 build failed: " + report.summary.result);
            Debug.Log("S1_C01_BUILD_PASSED: " + output + " warnings=" + report.summary.totalWarnings);
        }
    }
}
