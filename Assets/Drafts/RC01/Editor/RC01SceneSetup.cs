using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Reverb.Drafts.RC01.Presentation;
using Waffle;
using Waffle.Combat;

namespace Reverb.Drafts.RC01.EditorTools
{
    /// <summary>
    /// 批处理：-executeMethod Reverb.Drafts.RC01.EditorTools.RC01SceneSetup.CreatePreviews
    /// 只写 Assets/Drafts/RC01 下的场景、环境和预览。不调用会重写 RF01/M01 的 Setup。
    /// </summary>
    public static class RC01SceneSetup
    {
        public const string EnvRoot = "Assets/Drafts/RC01/Environments";
        public const string SceneRoot = "Assets/Drafts/RC01/Scenes";
        const int Seed = 20261012;
        const string LayerRoot = "Assets/ReverbInTheRye/Ground/TerrainLayers/";
        const string PlayerPrefab = "Assets/Waffle/Prefabs/Visual/Player_George_Visual.prefab";
        const string EnemyPrefab = "Assets/Waffle/Prefabs/Visual/Enemy_Vanguard_Mike_Visual.prefab";
        const string WeaponPrefab = "Assets/ReverbInTheRye/Weapons/OldSonicModulator/Prefabs/P_OldSonicModulator.prefab";

        struct Kit
        {
            public TerrainLayer Soil, Path, Straw;
            public GameObject WheatA, WheatB, Grass, Stubble, Weapon, Player, Enemy;
            public Material Proxy;
            public Shader TerrainShader;
        }

        [MenuItem("Experiments/Create RC01 Scene Previews")]
        public static void CreatePreviews()
        {
            if (Application.isPlaying)
                throw new System.InvalidOperationException("先退出 Play Mode。");
            string active = SceneManager.GetActiveScene().path;
            if (active.StartsWith("Assets/Experiments/RyeField") || active.StartsWith("Assets/Experiments/Mechanism")
                || active.StartsWith("Assets/ReverbInTheRye") || active.StartsWith("Assets/Waffle/Scenes"))
                throw new System.InvalidOperationException("先换开空场景。本工具不保存 RF01、M01 或用户素材场景。");

            Directory.CreateDirectory(EnvRoot);
            Directory.CreateDirectory(SceneRoot);
            Directory.CreateDirectory(EnvRoot + "/Terrain");
            Directory.CreateDirectory(EnvRoot + "/Materials");
            Kit kit = LoadKit();

            BuildMap(kit, "HarvestRoad", 40f, 200, HarvestWeight, HarvestKind);
            BuildMap(kit, "OldMarket", 36f, 100, MarketWeight, MarketKind);
            BuildMap(kit, "FourthGranary", 36f, 80, GranaryWeight, GranaryKind);
            BuildMap(kit, "RadioWasteland", 40f, 70, WastelandWeight, WastelandKind);
            BuildHarvestScene(kit);
            BuildLayoutScene(kit, "OldMarket");
            BuildLayoutScene(kit, "FourthGranary");
            BuildLayoutScene(kit, "RadioWasteland");
            BuildGrowthPreview();
            AssetDatabase.SaveAssets();
            Debug.Log("RC01_SCENE_SETUP_PASSED maps=4 harvest=GameLoop growth=waiting preview=TEST_ONLY");
        }

        static void BuildMap(Kit kit, string id, float size, int cap, System.Func<float, float, Vector3> weight, System.Func<float, float, int, int, int> kindOf)
        {
            var root = new GameObject("RC01_" + id);
            TerrainData data = Paint(EnvRoot + "/Terrain/RC01_" + id + ".asset", size, weight);
            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Ground";
            terrainGo.transform.SetParent(root.transform, false);
            terrainGo.transform.position = new Vector3(-size * 0.5f, 0f, -size * 0.5f);
            var collider = terrainGo.GetComponent<TerrainCollider>();
            if (collider != null) Object.DestroyImmediate(collider);
            var terrain = terrainGo.GetComponent<Terrain>();
            terrain.basemapDistance = 1000f;
            terrain.materialTemplate = TerrainMaterial(kit.TerrainShader);
            var plants = new GameObject("Foliage").transform;
            plants.SetParent(root.transform, false);
            PlacePlants(kit, plants, size, cap, kindOf);
            if (id == "OldMarket") PlaceBoxes(root.transform, kit.Proxy, MarketStalls(), "TEST_ONLY_Stall", new Vector3(1.4f, 1.2f, 1.4f));
            if (id == "FourthGranary") PlaceBoxes(root.transform, kit.Proxy, GranaryColumns(), "TEST_ONLY_Column", new Vector3(0.8f, 3f, 0.8f));
            if (root.GetComponentInChildren<Camera>(true) != null || root.GetComponentInChildren<Light>(true) != null)
                throw new System.InvalidOperationException(id + " 环境里不能有相机或灯。");
            PrefabUtility.SaveAsPrefabAsset(root, EnvRoot + "/P_RC01_" + id + ".prefab");
            Object.DestroyImmediate(root);
        }

        static void BuildHarvestScene(Kit kit)
        {
            InScene(SceneRoot + "/RC01_HarvestRoad.unity", () =>
            {
                GameObject env = PlaceEnv("HarvestRoad");
                GameObject player = PlacePlayer(kit.Player);
                Camera cam = PlaceCamera(player.transform);
                PlaceLight();
                var loopGo = new GameObject("GameLoop");
                var loop = loopGo.AddComponent<GameLoop>();
                loop.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                loop.playerView = player.transform;
                loop.gameplayCamera = cam;
                loop.config.seed = Seed;
                loop.combatConfig.aliveCap = 16;
                loop.combatConfig.eventOverflow = 0;
                loop.combatConfig.enemyPoolSize = 24;
                loop.combatConfig.waveCounts = new[] { 2, 2, 3, 3 };
                loop.combatConfig.pushCounts = new[] { 0, 0 };
                loop.combatConfig.ringCounts = new[] { 0, 0, 0 };
                var follow = cam.GetComponent<CameraFollow>();
                follow.loop = loop;
                var views = loopGo.AddComponent<CombatViews>();
                views.loop = loop;
                views.enemyVisualPrefab = kit.Enemy;
                var hud = loopGo.AddComponent<RC01GrowthHud>();
                hud.loop = loop;
                PlaceWeapon(kit.Weapon, new Vector3(22f, 0f, -22f));
                if (Object.FindAnyObjectByType<RC01GrowthPreviewSource>(FindObjectsInactive.Include) != null)
                    throw new System.InvalidOperationException("秋收之路不能挂排版假数据。");
                if (Object.FindObjectsByType<GameLoop>(FindObjectsSortMode.None).Length != 1)
                    throw new System.InvalidOperationException("秋收之路只能有一个 GameLoop。");
                if (env.GetComponentInChildren<RC01GrowthHud>(true) != null)
                    throw new System.InvalidOperationException("成长界面不能烘进环境 Prefab。");
            });
        }

        static void BuildLayoutScene(Kit kit, string id)
        {
            InScene(SceneRoot + "/RC01_" + id + ".unity", () =>
            {
                PlaceEnv(id);
                PlaceCamera(PlaceMarker(kit.Player).transform);
                PlaceLight();
                PlaceWeapon(kit.Weapon, new Vector3(20f, 0f, -20f));
                if (Object.FindAnyObjectByType<GameLoop>() != null)
                    throw new System.InvalidOperationException(id + " 只是布局预览，不能再放一套战斗循环。");
            });
        }

        static void BuildGrowthPreview()
        {
            InScene(SceneRoot + "/RC01_GrowthPreview.unity", () =>
            {
                var go = new GameObject("TEST_ONLY_GrowthPreview");
                var source = go.AddComponent<RC01GrowthPreviewSource>();
                source.previewEnabled = true;
                go.AddComponent<RC01GrowthHud>();
                PlaceCamera(null);
                if (Object.FindAnyObjectByType<GameLoop>() != null)
                    throw new System.InvalidOperationException("排版预览不能带 GameLoop。");
            });
        }

        static Vector3 HarvestWeight(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float road = Band(r, 10.5f, 15.5f, 1.5f);
            float straw = r >= 17.5f ? 0.85f : (r <= 8f ? 0.25f : 0.05f);
            return Norm(1f - road, road, straw * (1f - road));
        }

        static Vector3 MarketWeight(float x, float z)
        {
            bool cross = Mathf.Abs(x) < 2.6f || Mathf.Abs(z) < 2.6f;
            float road = cross ? 1f : 0f;
            if (!cross)
            {
                float edge = Mathf.Min(Mathf.Abs(Mathf.Abs(x) - 2.6f), Mathf.Abs(Mathf.Abs(z) - 2.6f));
                road = 1f - Mathf.SmoothStep(0f, 1.4f, edge);
            }
            float straw = (Mathf.Abs(x) > 12f && Mathf.Abs(z) > 12f) ? 0.7f : 0.05f;
            return Norm(1f - road, road, straw * (1f - road));
        }

        static Vector3 GranaryWeight(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float road = Band(r, 14f, 17f, 1.2f);
            bool lane = z < -8f && Mathf.Abs(x) < 3f;
            if (lane) road = Mathf.Max(road, 0.85f);
            return Norm(lane ? 0.15f : 0.7f, road, 0.08f * (1f - road));
        }

        static Vector3 WastelandWeight(float x, float z)
        {
            float islands = Island(x, z, -8f, 6f) + Island(x, z, 9f, -2f) + Island(x, z, -2f, -10f);
            float r = Mathf.Sqrt(x * x + z * z);
            float straw = r > 16f ? 0.45f : 0f;
            return Norm(1f - islands, islands, straw * (1f - islands));
        }

        static int HarvestKind(float x, float z, int ix, int iz)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            if (r > 9f && r < 17.2f) return -1;
            if (r <= 7.2f) return Hash01(ix, iz, 3) > 0.78f ? (Hash01(ix, iz, 4) < 0.7f ? 2 : 3) : -1;
            if (Hash01(ix, iz, 5) < 0.28f) return -1;
            return Hash01(ix, iz, 6) < 0.5f ? 0 : 1;
        }

        static int MarketKind(float x, float z, int ix, int iz)
        {
            if (Mathf.Abs(x) < 4.2f || Mathf.Abs(z) < 4.2f) return -1;
            if (Mathf.Abs(x) < 11f || Mathf.Abs(z) < 11f) return Hash01(ix, iz, 7) > 0.82f ? 3 : -1;
            if (Hash01(ix, iz, 8) < 0.4f) return -1;
            return Hash01(ix, iz, 9) < 0.5f ? 0 : 1;
        }

        static int GranaryKind(float x, float z, int ix, int iz)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            if (z < -6f && Mathf.Abs(x) < 4f) return -1;
            if (r < 16f) return Hash01(ix, iz, 10) > 0.88f ? 2 : -1;
            if (Hash01(ix, iz, 11) < 0.35f) return -1;
            return 0;
        }

        static int WastelandKind(float x, float z, int ix, int iz)
        {
            if (Mathf.Sqrt(x * x + z * z) < 16f) return Hash01(ix, iz, 12) > 0.9f ? 3 : -1;
            if (Hash01(ix, iz, 13) < 0.45f) return -1;
            return 1;
        }

        static void PlacePlants(Kit kit, Transform parent, float size, int cap, System.Func<float, float, int, int, int> kindOf)
        {
            int placed = 0;
            int cells = 16;
            float half = size * 0.5f;
            float step = size / cells;
            for (int iz = 0; iz < cells && placed < cap; iz++)
            {
                for (int ix = 0; ix < cells && placed < cap; ix++)
                {
                    float x = -half + (ix + 0.5f) * step;
                    float z = -half + (iz + 0.5f) * step;
                    int kind = kindOf(x, z, ix, iz);
                    if (kind < 0) continue;
                    GameObject source = kind == 0 ? kit.WheatA : kind == 1 ? kit.WheatB : kind == 2 ? kit.Stubble : kit.Grass;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
                    instance.transform.localPosition = new Vector3(x, 0f, z);
                    instance.transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-40f, 40f, Hash01(ix, iz, 21)) + 15f, 0f);
                    instance.transform.localScale = Vector3.one;
                    foreach (var body in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(body);
                    ForceLod0(instance);
                    placed++;
                }
            }
        }

        static void PlaceBoxes(Transform parent, Material material, Vector3[] points, string label, Vector3 scale)
        {
            for (int i = 0; i < points.Length; i++)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = label + "_" + i;
                Object.DestroyImmediate(box.GetComponent<Collider>());
                box.transform.SetParent(parent, false);
                box.transform.localPosition = points[i];
                box.transform.localScale = scale;
                box.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        static Vector3[] MarketStalls()
        {
            return new[]
            {
                new Vector3(-6f, 0.6f, 5f), new Vector3(-6f, 0.6f, -5f),
                new Vector3(6f, 0.6f, 5f), new Vector3(6f, 0.6f, -5f),
                new Vector3(5f, 0.6f, 8f), new Vector3(-5f, 0.6f, -8f)
            };
        }

        static Vector3[] GranaryColumns()
        {
            var list = new List<Vector3>();
            for (int z = -2; z <= 2; z++)
            for (int x = -2; x <= 2; x++)
            {
                if (x == 0 && z <= 0) continue;
                list.Add(new Vector3(x * 3.2f, 1.5f, z * 3.2f));
            }
            return list.ToArray();
        }

        static TerrainData Paint(string path, float size, System.Func<float, float, Vector3> weight)
        {
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, path);
            }
            data.heightmapResolution = 33;
            data.size = new Vector3(size, 1f, size);
            data.alphamapResolution = 128;
            data.baseMapResolution = 256;
            data.terrainLayers = new[]
            {
                AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerRoot + "Ground_DrySoil.terrainlayer"),
                AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerRoot + "Ground_TrampledPath.terrainlayer"),
                AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerRoot + "Ground_DenseStraw.terrainlayer")
            };
            int res = data.alphamapResolution;
            var maps = new float[res, res, 3];
            float half = size * 0.5f;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float wx = -half + (x + 0.5f) / res * size;
                float wz = -half + (y + 0.5f) / res * size;
                Vector3 w = weight(wx, wz);
                maps[y, x, 0] = w.x;
                maps[y, x, 1] = w.y;
                maps[y, x, 2] = w.z;
            }
            data.SetAlphamaps(0, 0, maps);
            EditorUtility.SetDirty(data);
            return data;
        }

        static Material TerrainMaterial(Shader shader)
        {
            const string path = EnvRoot + "/Materials/M_RC01_Terrain.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = "M_RC01_Terrain" };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            return mat;
        }

        static Kit LoadKit()
        {
            var kit = new Kit
            {
                Soil = LoadLayer("Ground_DrySoil"),
                Path = LoadLayer("Ground_TrampledPath"),
                Straw = LoadLayer("Ground_DenseStraw"),
                WheatA = LoadPrefab("Assets/ReverbInTheRye/Foliage/Prefabs/P_Wheat_A.prefab"),
                WheatB = LoadPrefab("Assets/ReverbInTheRye/Foliage/Prefabs/P_Wheat_B.prefab"),
                Grass = LoadPrefab("Assets/ReverbInTheRye/Foliage/Prefabs/P_WildGrass.prefab"),
                Stubble = LoadPrefab("Assets/ReverbInTheRye/Foliage/Prefabs/P_CutStubble.prefab"),
                Weapon = LoadPrefab(WeaponPrefab),
                Player = LoadPrefab(PlayerPrefab),
                Enemy = LoadPrefab(EnemyPrefab),
                TerrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit")
            };
            if (kit.TerrainShader == null)
                throw new System.InvalidOperationException("找不到 URP Terrain/Lit。");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = kit.TerrainShader;
            const string proxyPath = EnvRoot + "/Materials/M_RC01_Proxy.mat";
            kit.Proxy = AssetDatabase.LoadAssetAtPath<Material>(proxyPath);
            if (kit.Proxy == null)
            {
                kit.Proxy = new Material(lit) { name = "M_RC01_Proxy" };
                kit.Proxy.color = new Color(0.45f, 0.4f, 0.34f);
                AssetDatabase.CreateAsset(kit.Proxy, proxyPath);
            }
            return kit;
        }

        static GameObject PlaceEnv(string id)
        {
            var prefab = LoadPrefab(EnvRoot + "/P_RC01_" + id + ".prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;
            return instance;
        }

        static GameObject PlacePlayer(GameObject prefab)
        {
            var player = new GameObject("Player");
            player.transform.localScale = Vector3.one;
            var visual = new GameObject("Visual").transform;
            visual.SetParent(player.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, visual);
            model.transform.localScale = Vector3.one;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                visual.localScale = Vector3.one * (1.8f / Mathf.Max(0.01f, bounds.size.y));
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                visual.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            }
            return player;
        }

        static GameObject PlaceMarker(GameObject prefab)
        {
            GameObject marker = PlacePlayer(prefab);
            marker.name = "TEST_ONLY_ScaleMarker";
            return marker;
        }

        static Camera PlaceCamera(Transform target)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.66f, 0.7f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 150f;
            go.AddComponent<AudioListener>();
            var follow = go.AddComponent<CameraFollow>();
            follow.target = target;
            follow.orthographic = true;
            follow.orthographicSize = 9f;
            follow.pitch = 55f;
            follow.yaw = 45f;
            follow.Apply();
            return cam;
        }

        static void PlaceLight()
        {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.58f);
            var light = new GameObject("Key Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static void PlaceWeapon(GameObject prefab, Vector3 position)
        {
            var weapon = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            weapon.name = "RC01_WeaponDisplay";
            weapon.transform.position = position;
            weapon.transform.rotation = Quaternion.identity;
            weapon.transform.localScale = Vector3.one;
        }

        static void InScene(string path, System.Action build)
        {
            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!scene.IsValid() || (!string.IsNullOrEmpty(scene.path) && scene.path != path))
                throw new System.InvalidOperationException("没有打开 " + path + "，已停止。");
            foreach (GameObject old in scene.GetRootGameObjects()) Object.DestroyImmediate(old);
            build();
            EditorSceneManager.SaveScene(scene, path);
        }

        static void ForceLod0(GameObject instance)
        {
            var group = instance.GetComponent<LODGroup>();
            if (group != null)
            {
                LOD[] lods = group.GetLODs();
                if (lods.Length > 0)
                {
                    lods[0].screenRelativeTransitionHeight = 0.02f;
                    group.SetLODs(new[] { lods[0] });
                }
            }
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                if (child.name == "LOD1") child.gameObject.SetActive(false);
        }

        static float Band(float value, float inner, float outer, float feather)
        {
            float enter = Mathf.SmoothStep(inner - feather, inner, value);
            float exit = 1f - Mathf.SmoothStep(outer, outer + feather, value);
            return enter * exit;
        }

        static float Island(float x, float z, float cx, float cz)
        {
            float d = Vector2.Distance(new Vector2(x, z), new Vector2(cx, cz));
            return 1f - Mathf.SmoothStep(2.2f, 3.4f, d);
        }

        static Vector3 Norm(float soil, float path, float straw)
        {
            soil = Mathf.Max(0f, soil);
            path = Mathf.Max(0f, path);
            straw = Mathf.Max(0f, straw);
            float sum = soil + path + straw;
            if (sum < 0.0001f) return new Vector3(1f, 0f, 0f);
            return new Vector3(soil / sum, path / sum, straw / sum);
        }

        static float Hash01(int x, int z, int salt)
        {
            unchecked
            {
                uint n = (uint)(x * 374761393 + z * 668265263 + salt * 1442695041 + Seed);
                n = (n ^ (n >> 13)) * 1274126177u;
                n ^= n >> 16;
                return (n & 0xFFFFFF) / 16777215f;
            }
        }

        static TerrainLayer LoadLayer(string name)
        {
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerRoot + name + ".terrainlayer");
            if (layer == null) throw new System.InvalidOperationException("缺少图层 " + name);
            return layer;
        }

        static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new System.InvalidOperationException("缺少 Prefab " + path);
            return prefab;
        }
    }
}
