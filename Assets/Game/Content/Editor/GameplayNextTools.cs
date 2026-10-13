using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waffle;
using Waffle.GameplayNext;
using Reverb.Drafts.RC01.Presentation;
using Reverb.Drafts.RC01.Presentation.GameplayNext;

namespace Reverb.Drafts.RC01.Pipeline.EditorTools
{
    public static class GameplayNextTools
    {
        [MenuItem("Reverb/玩法/打开玩法配置")]
        public static void SelectInstaller()
        {
            var installer = Object.FindAnyObjectByType<GameplayNextInstaller>();
            if (installer != null) Selection.activeObject = installer;
            else Debug.LogWarning("请打开已接线的Main场景。");
        }

        public static void ConfigureMain()
        {
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Main.unity");
            var loop = Object.FindAnyObjectByType<GameLoop>();
            if (loop == null) throw new System.InvalidOperationException("Main has no GameLoop.");
            var installer = Ensure<GameplayNextInstaller>(loop.gameObject);
            installer.loop = loop;
            installer.firstTuningAnchor = Anchor(loop.transform, "TuningAnchor01", new Vector3(-9f, 0f, 5f));
            installer.secondTuningAnchor = Anchor(loop.transform, "TuningAnchor02", new Vector3(9f, 0f, -5f));
            loop.config.gameplayNext.enabled = true;
            loop.config.gameplayNext.endingMode = RunEndingMode.BossClear;
            loop.config.gameplayNext.bossOrdinaryCap = 16;
            loop.config.runDuration = 600f;
            loop.combatConfig.aliveCap = 48;
            loop.combatConfig.eventOverflow = 8;
            loop.gameplayModuleProvider = installer;
            const string materialPath = "Assets/Game/Presentation/GameplayNext/GameplayIndicator.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Reverb/GameplayIndicator");
                if (shader == null) throw new System.InvalidOperationException("Indicator shader is missing.");
                material = new Material(shader) { name = "GameplayIndicator" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var pool = loop.GetComponentInChildren<DangerIndicatorPool>();
            if (pool == null)
            {
                var child = new GameObject("GameplayDangerIndicators");
                child.transform.SetParent(loop.transform, false);
                pool = child.AddComponent<DangerIndicatorPool>();
            }
            pool.lineMaterial = pool.fillMaterial = material;
            var feedback = Ensure<GameplayFeedbackView>(loop.gameObject);
            feedback.loop = loop; feedback.dangerPool = pool; feedback.isPreviewMode = false;
            var marker = Ensure<TuningMarkerView>(loop.gameObject);
            marker.loop = loop; marker.ringMaterial = material;
            marker.resonatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Art/VisualIdentity/Prefabs/P_OrbitResonator.prefab");
            var hud = loop.GetComponent<RC01GrowthHud>();
            if (hud != null) { hud.gameplayNextSettlement = true; EditorUtility.SetDirty(hud); }
            EditorUtility.SetDirty(loop); EditorUtility.SetDirty(installer);
            EditorUtility.SetDirty(feedback); EditorUtility.SetDirty(pool); EditorUtility.SetDirty(marker);
            EditorSceneManager.MarkSceneDirty(loop.gameObject.scene);
            EditorSceneManager.SaveScene(loop.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("GP_INTEGRATION_CONFIGURED: Main; A/B/C/D; BossClear; two anchors; weather preserved.");
        }

        public static void BuildWindows()
        {
            string directory = System.Environment.GetEnvironmentVariable("REVERB_GAMEPLAY_BUILD");
            if (string.IsNullOrEmpty(directory)) directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Builds/GameplayNext"));
            System.IO.Directory.CreateDirectory(directory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Game/Scenes/Main.unity" },
                locationPathName = System.IO.Path.Combine(directory, "ReverbGameplay.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            var summary = report.summary;
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "build-summary.txt"),
                "result=" + summary.result + "\nerrors=" + summary.totalErrors + "\nwarnings=" + summary.totalWarnings
                + "\nbytes=" + summary.totalSize + "\n");
            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("Gameplay build failed.");
            Debug.Log("GP_INTEGRATION_BUILD_OK errors=" + summary.totalErrors);
        }

        static T Ensure<T>(GameObject target) where T : Component => target.GetComponent<T>() ?? target.AddComponent<T>();
        static Transform Anchor(Transform parent, string name, Vector3 position)
        {
            var child = parent.Find(name);
            if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
            var terrain = Terrain.activeTerrain;
            position.y = terrain != null ? terrain.SampleHeight(position) + terrain.transform.position.y : 0f;
            child.position = position;
            return child;
        }
    }
}
