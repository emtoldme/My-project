using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reverb.Experiments.RyeField.EditorTools
{
    /// <summary>
    /// 菜单 Experiments/Create RF01 Field Preview。
    /// 批处理：-executeMethod Reverb.Experiments.RyeField.EditorTools.RyeFieldSetup.CreatePreview
    /// 只写 Assets/Experiments/RyeField。不改用户素材、M01、Build Settings。
    /// </summary>
    public static class RyeFieldSetup
    {
        public const string Root = "Assets/Experiments/RyeField";
        public const string PrefabPath = Root + "/Prefabs/P_RF01_Environment.prefab";
        public const string ScenePath = Root + "/Scenes/RF01_EnvironmentPreview.unity";
        public const string TerrainPath = Root + "/Terrain/RF01_Terrain.asset";
        public const string ReportPath = Root + "/RF01_LAYOUT.txt";
        public const int Seed = 20261012;
        public const int MaxInstances = 300;
        public const float FieldSize = 24f;
        public const float ClearingRadius = 6f;

        const float Half = FieldSize * 0.5f;
        const float TileMeters = 4f;
        static readonly Vector2[] PathPoints =
        {
            new Vector2(-12f, -3f),
            new Vector2(-4f, -1f),
            new Vector2(2f, 2f),
            new Vector2(12f, 0.5f)
        };

        struct Spot
        {
            public float X, Z, Yaw, Density;
            public int Ix, Iz, Kind;
        }

        [MenuItem("Experiments/Create RF01 Field Preview")]
        public static void CreatePreview()
        {
            if (Application.isPlaying)
                throw new System.InvalidOperationException("先退出 Play Mode，再生成 RF01。");
            Scene active = SceneManager.GetActiveScene();
            if (active.path.Contains("M01_MechanismPlaytest") || active.path.StartsWith("Assets/ReverbInTheRye"))
                throw new System.InvalidOperationException("为避免碰到 M01 或用户素材场景，请换开空场景后再跑。");

            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Terrain");
            Directory.CreateDirectory(Root + "/Materials");

            TerrainLayer soil = LoadLayer("Ground_DrySoil");
            TerrainLayer path = LoadLayer("Ground_TrampledPath");
            TerrainLayer straw = LoadLayer("Ground_DenseStraw");
            GameObject wheatA = LoadPrefab("Assets/ReverbInTheRye/Foliage/Prefabs/P_Wheat_A.prefab");
            GameObject wheatB = LoadPrefab("Assets/ReverbInTheRye/Foliage/Prefabs/P_Wheat_B.prefab");
            GameObject grass = LoadPrefab("Assets/ReverbInTheRye/Foliage/Prefabs/P_WildGrass.prefab");
            GameObject stubble = LoadPrefab("Assets/ReverbInTheRye/Foliage/Prefabs/P_CutStubble.prefab");
            GameObject weapon = LoadPrefab("Assets/ReverbInTheRye/Weapons/OldSonicModulator/Prefabs/P_OldSonicModulator.prefab");

            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!scene.IsValid() || (!string.IsNullOrEmpty(scene.path) && scene.path != ScenePath))
                throw new System.InvalidOperationException("没有打开 " + ScenePath + "，已停止。");
            foreach (GameObject old in scene.GetRootGameObjects())
                Object.DestroyImmediate(old);

            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.58f);

            TerrainData terrainData = BuildTerrain(soil, path, straw);
            var environment = new GameObject("RF01_Environment");
            var terrainGo = Terrain.CreateTerrainGameObject(terrainData);
            terrainGo.name = "Ground";
            terrainGo.transform.SetParent(environment.transform, false);
            terrainGo.transform.position = new Vector3(-Half, 0f, -Half);
            Terrain terrain = terrainGo.GetComponent<Terrain>();
            terrain.basemapDistance = 1000f;
            terrain.heightmapPixelError = 1f;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var terrainCollider = terrainGo.GetComponent<TerrainCollider>();
            if (terrainCollider != null) Object.DestroyImmediate(terrainCollider);
            Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader == null)
                throw new System.InvalidOperationException("找不到 URP Terrain/Lit。不改渲染管线，样板停止。");
            Material terrainMaterial = SaveTerrainMaterial(terrainShader);
            terrain.materialTemplate = terrainMaterial;

            var plants = new GameObject("Foliage").transform;
            plants.SetParent(environment.transform, false);
            int wheat = 0, shortPlants = 0, lod1 = 0;
            List<Spot> accepted = PlacePlants();
            foreach (Spot spot in accepted)
            {
                GameObject source = spot.Kind == 0 ? wheatA : spot.Kind == 1 ? wheatB : spot.Kind == 2 ? stubble : grass;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, plants);
                instance.transform.localPosition = new Vector3(spot.X, 0f, spot.Z);
                instance.transform.localRotation = Quaternion.Euler(0f, spot.Yaw, 0f);
                instance.transform.localScale = Vector3.one;
                instance.name = (spot.Kind == 0 ? "WheatA_" : spot.Kind == 1 ? "WheatB_" : spot.Kind == 2 ? "Stubble_" : "Grass_")
                    + spot.Ix + "_" + spot.Iz;
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(collider);
                lod1 += ForceLod0(instance);
                if (spot.Kind <= 1) wheat++;
                else shortPlants++;
            }

            int wheatInside = CountWheatInside(plants);
            int colliders = environment.GetComponentsInChildren<Collider>(true).Length;
            int lod1Left = CountActiveLod1(environment.transform);
            int total = wheat + shortPlants;
            if (wheatInside != 0 || colliders != 0 || lod1Left != 0 || total > MaxInstances || total == 0)
                throw new System.InvalidOperationException("样板约束没过：wheatInside=" + wheatInside
                    + " colliders=" + colliders + " lod1Left=" + lod1Left + " total=" + total);
            if (environment.GetComponentInChildren<Camera>(true) != null || environment.GetComponentInChildren<Light>(true) != null)
                throw new System.InvalidOperationException("环境根里不能有相机或灯。");

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(environment, PrefabPath);
            Object.DestroyImmediate(environment);
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            placed.transform.position = Vector3.zero;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.68f, 0.74f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.rotation = Quaternion.Euler(55f, 45f, 0f);
            camGo.transform.position = -camGo.transform.forward * 30f;

            var light = new GameObject("Key Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var weaponInstance = (GameObject)PrefabUtility.InstantiatePrefab(weapon);
            weaponInstance.name = "RF01_WeaponDisplay";
            weaponInstance.transform.position = new Vector3(16f, 0f, -16f);
            weaponInstance.transform.rotation = Quaternion.identity;
            weaponInstance.transform.localScale = Vector3.one;

            var report = new StringBuilder();
            report.AppendLine("RF01 layout");
            report.AppendLine("label: first sample, not a finished map");
            report.AppendLine("seed: " + Seed);
            report.AppendLine("size: " + FieldSize + "m");
            report.AppendLine("clearingRadius: " + ClearingRadius + "m");
            report.AppendLine("instances: " + total);
            report.AppendLine("wheat: " + wheat);
            report.AppendLine("short: " + shortPlants);
            report.AppendLine("lod1Deactivated: " + lod1);
            report.AppendLine("wheatInsideClearing: " + wheatInside);
            report.AppendLine("colliders: " + colliders);
            report.AppendLine("groundTileMeters: " + TileMeters + " (source TerrainLayer, not edited)");
            report.AppendLine("camera: ortho 9, pitch 55, yaw 45, target origin");
            report.AppendLine("weapon: original scale at (16,0,-16), not parented to a character");
            File.WriteAllText(ReportPath, report.ToString());
            AssetDatabase.ImportAsset(ReportPath);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("RF01_ENVIRONMENT_SETUP_PASSED size=24 seed=" + Seed
                + " instances=" + total + " wheat=" + wheat + " short=" + shortPlants
                + " wheatInClearing=0 lod1Off=" + lod1 + " colliders=0 tile=4");
        }

        static TerrainData BuildTerrain(TerrainLayer soil, TerrainLayer path, TerrainLayer straw)
        {
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, TerrainPath);
            }
            data.heightmapResolution = 33;
            data.size = new Vector3(FieldSize, 1f, FieldSize);
            data.alphamapResolution = 128;
            data.baseMapResolution = 256;
            data.SetTerrainLayers(new[] { soil, path, straw });
            int res = data.alphamapResolution;
            var maps = new float[res, res, 3];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float wx = -Half + (x + 0.5f) / res * FieldSize;
                    float wz = -Half + (y + 0.5f) / res * FieldSize;
                    float dist = Mathf.Sqrt(wx * wx + wz * wz);
                    float pathWeight = PathWeight(wx, wz);
                    float strawWeight = Mathf.SmoothStep(5.4f, 8.2f, dist);
                    strawWeight *= Mathf.Lerp(0.82f, 1f, Noise(wx, wz));
                    strawWeight *= 1f - pathWeight;
                    float soilWeight = (1f - Mathf.SmoothStep(5.4f, 8.2f, dist)) * (1f - pathWeight);
                    float sum = soilWeight + pathWeight + strawWeight;
                    if (sum < 0.0001f) soilWeight = 1f;
                    else
                    {
                        soilWeight /= sum;
                        pathWeight /= sum;
                        strawWeight /= sum;
                    }
                    maps[y, x, 0] = soilWeight;
                    maps[y, x, 1] = pathWeight;
                    maps[y, x, 2] = strawWeight;
                }
            }
            data.SetAlphamaps(0, 0, maps);
            EditorUtility.SetDirty(data);
            return data;
        }

        static Material SaveTerrainMaterial(Shader shader)
        {
            const string path = Root + "/Materials/M_RF01_Terrain.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = "M_RF01_Terrain" };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static List<Spot> PlacePlants()
        {
            var shorts = new List<Spot>();
            var wheat = new List<Spot>();
            const int shortCells = 8;
            for (int iz = 0; iz < shortCells; iz++)
            {
                for (int ix = 0; ix < shortCells; ix++)
                {
                    float x = -5.2f + (ix + 0.5f) * (10.4f / shortCells);
                    float z = -5.2f + (iz + 0.5f) * (10.4f / shortCells);
                    x += (Hash01(ix, iz, 3) - 0.5f) * 0.45f;
                    z += (Hash01(ix, iz, 4) - 0.5f) * 0.45f;
                    if (x * x + z * z > 5.2f * 5.2f) continue;
                    float density = Hash01(ix, iz, 5);
                    if (density < 0.55f) continue;
                    shorts.Add(new Spot
                    {
                        X = x, Z = z, Density = density, Ix = ix, Iz = iz,
                        Kind = Hash01(ix, iz, 6) < 0.72f ? 2 : 3,
                        Yaw = Hash01(ix, iz, 7) * 360f
                    });
                }
            }
            shorts.Sort(CompareSpot);
            var keptShort = new List<Spot>();
            foreach (Spot spot in shorts)
            {
                if (keptShort.Count >= 24) break;
                if (TooClose(keptShort, spot.X, spot.Z, 1.15f)) continue;
                keptShort.Add(spot);
            }

            const int cells = 17;
            float step = FieldSize / cells;
            for (int iz = 0; iz < cells; iz++)
            {
                for (int ix = 0; ix < cells; ix++)
                {
                    float x = -Half + (ix + 0.5f) * step;
                    float z = -Half + (iz + 0.5f) * step;
                    x += (Hash01(ix, iz, 11) - 0.5f) * 0.5f;
                    z += (Hash01(ix, iz, 12) - 0.5f) * 0.5f;
                    if (Mathf.Abs(x) > Half || Mathf.Abs(z) > Half) continue;
                    if (x * x + z * z < ClearingRadius * ClearingRadius) continue;
                    float density = Noise(x, z);
                    if (density < 0.22f) continue;
                    float yaw = Mathf.Lerp(-35f, 35f, Hash01(ix, iz, 13)) + 18f;
                    if (Hash01(ix, iz, 14) > 0.82f) yaw += 180f;
                    wheat.Add(new Spot
                    {
                        X = x, Z = z, Density = density, Ix = ix, Iz = iz, Yaw = yaw,
                        Kind = Hash01(ix, iz, 15) < 0.5f ? 0 : 1
                    });
                }
            }
            wheat.Sort(CompareSpot);
            var kept = new List<Spot>(keptShort);
            int wheatCap = MaxInstances - keptShort.Count;
            int wheatCount = 0;
            foreach (Spot spot in wheat)
            {
                if (wheatCount >= wheatCap) break;
                if (TooClose(kept, spot.X, spot.Z, 0.85f)) continue;
                kept.Add(spot);
                wheatCount++;
            }
            return kept;
        }

        static int CompareSpot(Spot a, Spot b)
        {
            int density = b.Density.CompareTo(a.Density);
            if (density != 0) return density;
            int x = a.Ix.CompareTo(b.Ix);
            return x != 0 ? x : a.Iz.CompareTo(b.Iz);
        }

        static bool TooClose(List<Spot> spots, float x, float z, float min)
        {
            float minSqr = min * min;
            for (int i = 0; i < spots.Count; i++)
            {
                float dx = spots[i].X - x;
                float dz = spots[i].Z - z;
                if (dx * dx + dz * dz < minSqr) return true;
            }
            return false;
        }

        static int ForceLod0(GameObject instance)
        {
            int off = 0;
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
            {
                if (child.name != "LOD1") continue;
                child.gameObject.SetActive(false);
                off++;
            }
            return off;
        }

        static int CountActiveLod1(Transform root)
        {
            int count = 0;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "LOD1" && child.gameObject.activeSelf) count++;
            }
            return count;
        }

        static int CountWheatInside(Transform plants)
        {
            int count = 0;
            for (int i = 0; i < plants.childCount; i++)
            {
                Transform child = plants.GetChild(i);
                if (!child.name.StartsWith("Wheat")) continue;
                Vector3 p = child.localPosition;
                if (p.x * p.x + p.z * p.z < ClearingRadius * ClearingRadius) count++;
            }
            return count;
        }

        static float PathWeight(float x, float z)
        {
            float dist = PolylineDistance(x, z);
            return 1f - Mathf.SmoothStep(0.85f, 2.4f, dist);
        }

        static float PolylineDistance(float x, float z)
        {
            float best = float.MaxValue;
            var point = new Vector2(x, z);
            for (int i = 0; i < PathPoints.Length - 1; i++)
                best = Mathf.Min(best, SegmentDistance(point, PathPoints[i], PathPoints[i + 1]));
            return best;
        }

        static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = ab.sqrMagnitude;
            float t = denom < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, ab) / denom);
            return Vector2.Distance(point, a + ab * t);
        }

        static float Noise(float x, float z)
        {
            float fx = x * 0.37f;
            float fz = z * 0.37f;
            int x0 = Mathf.FloorToInt(fx);
            int z0 = Mathf.FloorToInt(fz);
            float tx = fx - x0;
            float tz = fz - z0;
            float a = Hash01(x0, z0, 21);
            float b = Hash01(x0 + 1, z0, 21);
            float c = Hash01(x0, z0 + 1, 21);
            float d = Hash01(x0 + 1, z0 + 1, 21);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
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
            string path = "Assets/ReverbInTheRye/Ground/TerrainLayers/" + name + ".terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null) throw new System.InvalidOperationException("缺少地表图层 " + path);
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
