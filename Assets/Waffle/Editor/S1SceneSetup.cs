using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Waffle.EditorTools
{
    /// <summary>
    /// 菜单 Waffle/Setup S1 Scene：自动搭出 S1 灰盒场景并保存到 Assets/Waffle/Scenes/S1_Graybox.unity。
    /// 不修改 SampleScene、项目设置和 URP 资源。
    /// </summary>
    public static class S1SceneSetup
    {
        public const string ScenePath = "Assets/Waffle/Scenes/S1_Graybox.unity";
        public const string ActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Waffle/Setup S1 Scene")]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Waffle", "S1_Graybox.unity 已存在，要覆盖重建吗？", "覆盖", "取消"))
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 灯光
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 场地：Plane 是 10×10 m，缩放 6 → 60×60 m（§1.1）
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena_60x60";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            Object.DestroyImmediate(ground.GetComponent<Collider>()); // 玩法不用物理（§8.3）

            // 玩家表现（只同步位置，逻辑在 RunSimulation 里）
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
            Object.DestroyImmediate(player.GetComponent<Collider>());

            // 固定斜俯视相机（§8.2）
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow>();
            follow.target = player.transform;
            camGo.transform.position = player.transform.position + follow.offset;
            camGo.transform.LookAt(player.transform.position);

            // GameLoop + HUD + 调试控制台
            var loopGo = new GameObject("GameLoop");
            var loop = loopGo.AddComponent<GameLoop>();
            loop.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            loop.playerView = player.transform;
            loop.gameplayCamera = cam;
            var hud = loopGo.AddComponent<RunHud>();
            hud.loop = loop;
            var console = loopGo.AddComponent<DebugConsole>();
            console.loop = loop;

            if (loop.actions == null)
                Debug.LogError("[Waffle] 找不到 " + ActionsPath + "，请手动给 GameLoop.actions 赋值");

            System.IO.Directory.CreateDirectory("Assets/Waffle/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[Waffle] 已生成 " + ScenePath);
        }

        /// <summary>
        /// 可选：把 S1 场景加进 Build Settings（会修改 ProjectSettings/EditorBuildSettings.asset）。
        /// 编辑器里试玩不需要；做 Development Build（F-10）时需要。
        /// </summary>
        [MenuItem("Waffle/Add S1 Scene To Build Settings")]
        public static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == ScenePath) { Debug.Log("[Waffle] 已在 Build Settings 中"); return; }
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Waffle] 已把 S1_Graybox 加到 Build Settings 第 0 位");
        }
    }
}
