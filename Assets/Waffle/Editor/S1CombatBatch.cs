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
            ApplyMovementReferences(loop);
            EditorUtility.SetDirty(loop); EditorUtility.SetDirty(views);
            EditorSceneManager.MarkSceneDirty(loop.gameObject.scene);
            EditorSceneManager.SaveScene(loop.gameObject.scene);
            AssetDatabase.SaveAssets();
            S1IntegrationBatch.ValidateScene();
            Debug.Log("S1_COMBAT_SETUP_PASSED");
        }

        public static void Build()
        {
            BuildAt("S1-C01");
        }

        public static void BuildDodgeFix() { BuildAt("S1-DodgeFix"); }

        private static void BuildAt(string folder)
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/" + folder + "/Waffle.exe"));
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

        [MenuItem("Waffle/Repair S1 Dodge Readability")]
        public static void RepairDodgeReadability()
        {
            var scene = EditorSceneManager.OpenScene(S1SceneSetup.ScenePath);
            ApplyMovementReferences(UnityEngine.Object.FindAnyObjectByType<GameLoop>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("S1_DODGE_SCENE_PASSED");
        }

        private static void ApplyMovementReferences(GameLoop loop)
        {
            if (loop == null) throw new Exception("Missing S1 GameLoop");
            var camera = loop.gameplayCamera.GetComponent<CameraFollow>();
            camera.loop = loop;
            camera.followSmoothTime = 0.08f;
            EditorUtility.SetDirty(camera);
            if (GameObject.Find("ArenaMovementReferences") != null) return;
            const string path = "Assets/Waffle/Art/Materials/M_ArenaReference.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "M_ArenaReference" };
                material.SetColor("_BaseColor", new Color(0.22f, 0.26f, 0.29f));
                AssetDatabase.CreateAsset(material, path);
            }
            var root = new GameObject("ArenaMovementReferences");
            // Quiet 5m ground marks give the graybox a world-space movement reference.
            for (int axis = 0; axis < 2; axis++)
                for (int p = -25; p <= 25; p += 5)
                {
                    var go = new GameObject("GroundLine_" + axis + "_" + p);
                    go.transform.SetParent(root.transform, false);
                    var line = go.AddComponent<LineRenderer>();
                    line.sharedMaterial = material;
                    line.widthMultiplier = 0.025f;
                    line.positionCount = 2;
                    line.SetPosition(0, axis == 0 ? new Vector3(p, 0.015f, -30f) : new Vector3(-30f, 0.015f, p));
                    line.SetPosition(1, axis == 0 ? new Vector3(p, 0.015f, 30f) : new Vector3(30f, 0.015f, p));
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    line.receiveShadows = false;
                }
        }
    }
}
