using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Waffle.Experiments.VisualPipeline.EditorTools
{
    /// <summary>
    /// 菜单 Experiments/Setup E01 Visual Pipeline。
    /// 批处理：-executeMethod Waffle.Experiments.VisualPipeline.EditorTools.E01VisualPipelineSetup.Setup
    /// 只重建本实验场景和 Assets/Experiments/VisualPipeline 下的生成资源。
    /// 不改 S1/S2、共享 Views、Settings、Packages、ProjectSettings，也不弹覆盖确认。
    /// </summary>
    public static class E01VisualPipelineSetup
    {
        public const string ScenePath = "Assets/Experiments/VisualPipeline/Scenes/E01_VisualPipeline.unity";
        const string Root = "Assets/Experiments/VisualPipeline";
        const string MatDir = Root + "/Materials";
        const string MeshDir = Root + "/Meshes";
        const string FactsPath = Root + "/E01_IMPORT_FACTS.txt";
        const string GraphPath = Root + "/Shaders/E01_WaveRing.shadergraph";
        const string HlslPath = Root + "/Shaders/E01_WaveRing.shader";
        const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        const string PlayerModel = "Assets/Art_ThirdParty/Quaternius_Mech/George.fbx";
        const string PlayerPrefab = "Assets/Waffle/Prefabs/Visual/Player_George_Visual.prefab";
        const string EnemyPrefab = "Assets/Waffle/Prefabs/Visual/Enemy_Vanguard_Mike_Visual.prefab";

        [MenuItem("Experiments/Setup E01 Visual Pipeline")]
        public static void Setup()
        {
            if (Application.isPlaying)
                throw new System.InvalidOperationException("先退出 Play Mode，再运行 E01 装配。");

            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(MeshDir);
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Shaders");

            bool graphOk = E01ShaderGraphBuilder.TryCreate(GraphPath, out string graphDetail);
            if (graphOk) AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceSynchronousImport);
            Shader graphShader = graphOk ? AssetDatabase.LoadAssetAtPath<Shader>(GraphPath) : null;
            bool graphHasRadius = graphShader != null && graphShader.FindPropertyIndex("_Radius") >= 0;
            if (!graphHasRadius)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GraphPath) != null)
                    AssetDatabase.DeleteAsset(GraphPath);
                else if (File.Exists(GraphPath))
                {
                    File.Delete(GraphPath);
                    if (File.Exists(GraphPath + ".meta")) File.Delete(GraphPath + ".meta");
                }
            }
            Shader hlsl = AssetDatabase.LoadAssetAtPath<Shader>(HlslPath);
            if (hlsl == null) hlsl = Shader.Find("Waffle/Experiments/E01_WaveRing");
            Shader ringShader = graphHasRadius ? graphShader : hlsl;
            if (ringShader == null) throw new System.InvalidOperationException("找不到 E01 声波 Shader。");

            Material ring = SaveUnlit("M_E01_WaveRing", ringShader, new Color(0.35f, 0.9f, 1f, 1f));
            Shader sparkShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sparkShader == null) sparkShader = Shader.Find("Unlit/Color");
            if (sparkShader == null) sparkShader = ringShader;
            Material spark = SaveUnlit("M_E01_Spark", sparkShader, new Color(0.35f, 0.9f, 1f, 1f));
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            Material floorMat = SaveLit("M_E01_Floor_TestOnly", lit, new Color(0.42f, 0.44f, 0.40f), 0f, 0.16f);

            Mesh floor = FloorMesh();
            Mesh wave = E01WavePlayer.BuildQuad(6f);
            SaveMesh(wave, MeshDir + "/E01_WaveQuad.asset");

            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!scene.IsValid() || (!string.IsNullOrEmpty(scene.path) && scene.path != ScenePath))
                throw new System.InvalidOperationException("没有打开 " + ScenePath + "，已停止，避免改到别的场景。本工具不弹覆盖确认。");
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

            var floorGo = new GameObject("TEST_ONLY_FLOOR");
            floorGo.transform.localScale = Vector3.one;
            floorGo.AddComponent<MeshFilter>().sharedMesh = floor;
            floorGo.AddComponent<MeshRenderer>().sharedMaterial = floorMat;

            string facts = PlacePlayer(out GameObject player);
            File.WriteAllText(FactsPath, facts);
            AssetDatabase.ImportAsset(FactsPath);

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
            follow.loop = gameLoop;

            var views = loopGo.AddComponent<CombatViews>();
            views.loop = gameLoop;
            views.enemyVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
            views.projectileMaterial = spark;
            views.gemMaterial = spark;

            var wavePlayer = loopGo.AddComponent<E01WavePlayer>();
            wavePlayer.loop = gameLoop;
            wavePlayer.ringMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshDir + "/E01_WaveQuad.asset");
            wavePlayer.ringMaterial = ring;
            wavePlayer.sparkMaterial = spark;
            wavePlayer.quadMeters = 6f;

            var hud = loopGo.AddComponent<RunHud>();
            hud.loop = gameLoop;
            var console = loopGo.AddComponent<DebugConsole>();
            console.loop = gameLoop;
            console.visible = false;

            if (player.transform.localScale != Vector3.one)
                throw new System.InvalidOperationException("玩家根缩放不是 1");
            if (Object.FindAnyObjectByType<VfxDirector>() != null)
                throw new System.InvalidOperationException("E01 场景不应挂 VfxDirector");
            if (Object.FindAnyObjectByType<AudioDirector>() != null)
                throw new System.InvalidOperationException("E01 场景不应挂 AudioDirector");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            string mode = graphHasRadius ? "shadergraph" : "hlsl";
            Debug.Log("E01_VISUAL_PIPELINE_SETUP_PASSED shader=" + mode + " graph=" + graphDetail + " model=TEST_ONLY");
            if (!graphHasRadius)
                Debug.LogWarning("[E01] ShaderGraph 未接管材质（" + graphDetail + "）。声波使用 HLSL E01_WaveRing.shader。不能把这次写成 ShaderGraph 已通过。");
            else
                Debug.Log("E01_SHADERGRAPH_OK " + GraphPath);
        }

        static string PlacePlayer(out GameObject player)
        {
            var report = new StringBuilder();
            report.AppendLine("E01 import facts");
            report.AppendLine("label: TEST_ONLY");
            report.AppendLine("not rural art, not a user-approved model");
            report.AppendLine("source model: " + PlayerModel);
            report.AppendLine("source prefab: " + PlayerPrefab);
            report.AppendLine("A-R01 brief: Docs/Art/Reverb/A-R01_ASSET_BRIEF.md (integrated; model remains TEST_ONLY)");
            var importer = AssetImporter.GetAtPath(PlayerModel) as ModelImporter;
            if (importer == null)
                report.AppendLine("model importer: MISSING");
            else
            {
                report.AppendLine("globalScale: " + importer.globalScale);
                report.AppendLine("useFileScale: " + importer.useFileScale);
                report.AppendLine("bakeAxisConversion: " + importer.bakeAxisConversion);
                report.AppendLine("fileScale: " + importer.fileScale);
            }

            player = new GameObject("Player");
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
                report.AppendLine("placed: MISSING_MODEL capsule, prefab was not imported");
                return report.ToString();
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.transform.SetParent(visual, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            report.AppendLine("rendererCount: " + renderers.Length);
            if (renderers.Length > 0)
            {
                Bounds before = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) before.Encapsulate(renderers[i].bounds);
                report.AppendLine("boundsBefore: center=" + before.center + " size=" + before.size);
                float height = Mathf.Max(0.01f, before.size.y);
                visual.localScale = Vector3.one * (1.8f / height);
                renderers = model.GetComponentsInChildren<Renderer>(true);
                Bounds after = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) after.Encapsulate(renderers[i].bounds);
                visual.position += new Vector3(-after.center.x, -after.min.y, -after.center.z);
                after = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) after.Encapsulate(renderers[i].bounds);
                report.AppendLine("visualLocalScale: " + visual.localScale);
                report.AppendLine("boundsAfter: center=" + after.center + " size=" + after.size + " minY=" + after.min.y);
            }
            report.AppendLine("playerRootScale: " + player.transform.localScale);
            report.AppendLine("nodes:");
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                report.AppendLine("  " + AnimationUtility.CalculateTransformPath(t, model.transform));
            report.AppendLine("enemyPrefab: " + EnemyPrefab + " (CombatViews may scale the instance to about 1.5m; not changed here)");
            report.AppendLine("axis: FBX import, bakeAxisConversion 0 means Unity converts the axis at import. +Y up in the scene. +Z is the gameplay forward used by GameLoop.");
            return report.ToString();
        }

        static Mesh FloorMesh()
        {
            const string path = MeshDir + "/E01_Floor.asset";
            var mesh = new Mesh { name = "E01_Floor" };
            mesh.vertices = new[]
            {
                new Vector3(-30f, 0f, -30f), new Vector3(30f, 0f, -30f),
                new Vector3(-30f, 0f, 30f), new Vector3(30f, 0f, 30f)
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return SaveMesh(mesh, path);
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Material SaveUnlit(string name, Shader shader, Color color)
        {
            string path = MatDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Radius")) mat.SetFloat("_Radius", 0.4f);
            if (mat.HasProperty("_Width")) mat.SetFloat("_Width", 0.09f);
            if (mat.HasProperty("_QuadMeters")) mat.SetFloat("_QuadMeters", 6f);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.5f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material SaveLit(string name, Shader shader, Color color, float metal, float smoothness)
        {
            string path = MatDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metal);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
