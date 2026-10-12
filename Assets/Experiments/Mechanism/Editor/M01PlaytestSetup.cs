using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Waffle.Combat;
using Waffle.Experiments.Mechanism.Presentation;

namespace Waffle.Experiments.Mechanism.EditorTools
{
    /// <summary>
    /// 菜单 Experiments/Setup M01 Mechanism Playtest。
    /// 批处理：-executeMethod Waffle.Experiments.Mechanism.EditorTools.M01PlaytestSetup.Setup
    /// 只重建 M01 场景。不调用 E01 装配，不改 E01 资产、Build Settings、正式场景。
    /// </summary>
    public static class M01PlaytestSetup
    {
        public const string ScenePath = "Assets/Experiments/Mechanism/Scenes/M01_MechanismPlaytest.unity";
        const string E01ScenePath = "Assets/Experiments/VisualPipeline/Scenes/E01_VisualPipeline.unity";
        const string FloorMeshPath = "Assets/Experiments/VisualPipeline/Meshes/E01_Floor.asset";
        const string FloorMatPath = "Assets/Experiments/VisualPipeline/Materials/M_E01_Floor_TestOnly.mat";
        const string SparkMatPath = "Assets/Experiments/VisualPipeline/Materials/M_E01_Spark.mat";
        const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        const string PlayerPrefab = "Assets/Waffle/Prefabs/Visual/Player_George_Visual.prefab";
        const string PlayerModel = "Assets/Art_ThirdParty/Quaternius_Mech/George.fbx";
        const string EnemyPrefab = "Assets/Waffle/Prefabs/Visual/Enemy_Vanguard_Mike_Visual.prefab";
        public const int PlaytestSeed = 12345;

        [MenuItem("Experiments/Setup M01 Mechanism Playtest")]
        public static void Setup()
        {
            if (Application.isPlaying)
                throw new System.InvalidOperationException("先退出 Play Mode，再运行 M01 装配。");
            Scene active = SceneManager.GetActiveScene();
            if (active.path == E01ScenePath)
                throw new System.InvalidOperationException("当前开着 E01 场景。先换开别的场景再运行，本工具不会调用 E01 装配，也不覆盖 E01。");

            Directory.CreateDirectory("Assets/Experiments/Mechanism/Scenes");
            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!scene.IsValid() || (!string.IsNullOrEmpty(scene.path) && scene.path != ScenePath))
                throw new System.InvalidOperationException("没有打开 " + ScenePath + "，已停止，避免改到别的场景。");

            foreach (GameObject go in scene.GetRootGameObjects())
                Object.DestroyImmediate(go);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.56f, 0.52f);

            var key = new GameObject("Key Light").AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.1f;
            key.color = new Color(1f, 0.96f, 0.88f);
            key.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Mesh floorMesh = AssetDatabase.LoadAssetAtPath<Mesh>(FloorMeshPath);
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(FloorMatPath);
            Material spark = AssetDatabase.LoadAssetAtPath<Material>(SparkMatPath);
            if (floorMesh == null || floorMat == null || spark == null)
                throw new System.InvalidOperationException("缺少已提交的 E01 地面或火花材质。不在这里重新生成，避免改写 E01。");

            var floorGo = new GameObject("TEST_ONLY_FLOOR");
            floorGo.transform.localScale = Vector3.one;
            floorGo.AddComponent<MeshFilter>().sharedMesh = floorMesh;
            floorGo.AddComponent<MeshRenderer>().sharedMaterial = floorMat;

            GameObject player = PlacePlayer();
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.58f, 0.52f);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.orthographic = true;
            follow.orthographicSize = 9f;
            follow.pitch = 55f;
            follow.yaw = 45f;
            follow.followSmoothTime = 0.08f;
            follow.Apply();

            var loopGo = new GameObject("GameLoop");
            var gameLoop = loopGo.AddComponent<GameLoop>();
            gameLoop.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            gameLoop.playerView = player.transform;
            gameLoop.gameplayCamera = cam;
            gameLoop.config.seed = PlaytestSeed;
            ApplyLowCount(gameLoop.combatConfig);
            follow.loop = gameLoop;

            var views = loopGo.AddComponent<CombatViews>();
            views.loop = gameLoop;
            views.enemyVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
            views.projectileMaterial = spark;
            views.gemMaterial = spark;

            var fx = loopGo.AddComponent<M01DerivedFx>();
            fx.loop = gameLoop;
            fx.lineMaterial = spark;

            var panel = loopGo.AddComponent<M01PlaytestPanel>();
            panel.loop = gameLoop;
            var hud = loopGo.AddComponent<RunHud>();
            hud.loop = gameLoop;

            if (player.transform.localScale != Vector3.one)
                throw new System.InvalidOperationException("玩家根缩放不是 1");
            if (Object.FindObjectsByType<GameLoop>(FindObjectsSortMode.None).Length != 1)
                throw new System.InvalidOperationException("场景里必须只有一个 GameLoop");
            if (Object.FindAnyObjectByType<VfxDirector>() != null || Object.FindAnyObjectByType<AudioDirector>() != null)
                throw new System.InvalidOperationException("M01 不挂第二套音效或特效管理器");
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour != null && behaviour.GetType().Name == "E01WavePlayer")
                    throw new System.InvalidOperationException("M01 不挂 E01 波纹");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("M01_PLAYTEST_SETUP_PASSED scene=" + ScenePath + " seed=" + PlaytestSeed + " controls=waiting model=TEST_ONLY");
        }

        static void ApplyLowCount(CombatConfig combat)
        {
            combat.aliveCap = 8;
            combat.eventOverflow = 0;
            combat.enemyPoolSize = 16;
            combat.waveCounts = new[] { 2, 2, 2, 2 };
            combat.waveIntervals = new[] { 2f, 2f, 2f, 2f };
            combat.pushCounts = new[] { 0, 0 };
            combat.ringCounts = new[] { 0, 0, 0 };
            combat.projectilePoolSize = 8;
        }

        static GameObject PlacePlayer()
        {
            var player = new GameObject("Player");
            player.transform.localScale = Vector3.one;
            player.transform.position = Vector3.zero;
            var visual = new GameObject("Visual").transform;
            visual.SetParent(player.transform, false);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModel);
            if (prefab == null)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                marker.name = "MISSING_MODEL";
                Object.DestroyImmediate(marker.GetComponent<Collider>());
                marker.transform.SetParent(visual, false);
                marker.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                Debug.LogWarning("[M01] 玩家模型没导入，放了占位胶囊。");
                return player;
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.transform.SetParent(visual, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds before = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) before.Encapsulate(renderers[i].bounds);
                visual.localScale = Vector3.one * (1.8f / Mathf.Max(0.01f, before.size.y));
                renderers = model.GetComponentsInChildren<Renderer>(true);
                Bounds after = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) after.Encapsulate(renderers[i].bounds);
                visual.position += new Vector3(-after.center.x, -after.min.y, -after.center.z);
            }
            return player;
        }
    }
}
