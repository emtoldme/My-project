#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ReverbFoliageSetup
{
    private const string Root = "Assets/ReverbInTheRye/Foliage";
    private const string MaterialPath = Root + "/Materials/M_RyeFoliageWind.mat";
    private const string AtlasPath = Root + "/Textures/Rye_Foliage_Atlas.png";
    private const string PrefabRoot = Root + "/Prefabs";

    [MenuItem("Tools/Reverb in the Rye/Create Foliage Prefabs")]
    public static void CreatePrefabs()
    {
        Shader shader = Shader.Find("ReverbInTheRye/FoliageWindURP");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogError("未找到受支持的 RyeFoliageWindURP Shader。请检查 URP 是否启用、Shader 是否编译成功。");
            return;
        }
        EnsureFolder(Root + "/Materials");
        EnsureFolder(PrefabRoot);
        var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
        if (importer != null && (!importer.mipmapEnabled || !importer.sRGBTexture))
        {
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        if (atlas == null) { Debug.LogError("贴图丢失: " + AtlasPath); return; }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        mat.shader = shader;
        mat.SetTexture("_BaseMap", atlas);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetVector("_WindDirection", new Vector4(.85f, 0f, .5f, 0f));
        mat.SetFloat("_WindStrength", .13f);
        mat.SetFloat("_WindSpeed", 1.15f);
        mat.SetFloat("_WindSpatialScale", .45f);
        mat.SetFloat("_PlantHeight", 2.0f);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);

        CreateLodPrefab("Wheat_Cluster_A", "Wheat_Cluster_A_LOD1", "P_Wheat_A", mat);
        CreateLodPrefab("Wheat_Cluster_B", "Wheat_Cluster_B_LOD1", "P_Wheat_B", mat);
        CreateSinglePrefab("Cut_Stubble_A", "P_CutStubble", mat);
        CreateSinglePrefab("Wild_Grass_A", "P_WildGrass", mat);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rye foliage: 4 prefabs ready in " + PrefabRoot + " (2 wheat LOD prefabs + stubble + grass).");
    }

    private static Mesh LoadMesh(string file)
    {
        string path = Root + "/Models/" + file + ".obj";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null) { Debug.LogError("OBJ 模型缺失: " + path); return null; }
        var filter = model.GetComponentInChildren<MeshFilter>(true);
        if (filter == null) { Debug.LogError("OBJ 没有 MeshFilter: " + path); return null; }
        return filter.sharedMesh;
    }

    private static MeshRenderer CreateMeshChild(GameObject root, string name, Mesh mesh, Material mat)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = child.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.On;
        r.receiveShadows = true;
        return r;
    }

    private static void CreateSinglePrefab(string modelName, string prefabName, Material mat)
    {
        Mesh mesh = LoadMesh(modelName);
        if (mesh == null) return;
        GameObject root = new GameObject(prefabName);
        try
        {
            CreateMeshChild(root, modelName, mesh, mat);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/" + prefabName + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void CreateLodPrefab(string modelName, string lodName, string prefabName, Material mat)
    {
        Mesh high = LoadMesh(modelName);
        Mesh low = LoadMesh(lodName);
        if (high == null || low == null) return;
        GameObject root = new GameObject(prefabName);
        try
        {
            var r0 = CreateMeshChild(root, "LOD0", high, mat);
            var r1 = CreateMeshChild(root, "LOD1", low, mat);
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(new[]
            {
                new LOD(.45f, new Renderer[] { r0 }),
                new LOD(.08f, new Renderer[] { r1 }),
            });
            group.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/" + prefabName + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [MenuItem("Tools/Reverb in the Rye/Scatter Foliage Demo (Current Scene)")]
    public static void ScatterDemo()
    {
        GameObject a = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/P_Wheat_A.prefab");
        GameObject b = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/P_Wheat_B.prefab");
        GameObject c = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/P_CutStubble.prefab");
        GameObject d = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/P_WildGrass.prefab");
        if (a == null || b == null || c == null || d == null)
        {
            Debug.LogError("请先执行 Create Foliage Prefabs。"); return;
        }
        // Creates 205 independent objects for a small editor-side validation patch.
        // Do not use this GameObject-per-plant method to populate a huge production map.
        GameObject root = new GameObject("Rye_Foliage_TestPatch_DeleteWhenDone");
        Undo.RegisterCreatedObjectUndo(root, "Create Rye Foliage Test Patch");
        System.Random random = new System.Random(20261012);
        for (int i = 0; i < 205; ++i)
        {
            float x = (float)(random.NextDouble() * 14.0 - 7.0);
            float z = (float)(random.NextDouble() * 14.0 - 7.0);
            // Walkway down the middle, deliberately kept unobstructed.
            if (Mathf.Abs(x) < 1.15f && i < 160) continue;
            GameObject source = i < 90 ? a : i < 160 ? b : i < 184 ? c : d;
            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null) continue;
            Undo.RegisterCreatedObjectUndo(instance, "Scatter Rye Plant");
            instance.transform.SetParent(root.transform);
            instance.transform.localPosition = new Vector3(x, 0, z);
            instance.transform.localRotation = Quaternion.Euler(0f, (float)(random.NextDouble() * 360f), 0f);
            float s = .82f + (float)random.NextDouble() * .35f;
            instance.transform.localScale = new Vector3(s, s, s);
        }
        Selection.activeGameObject = root;
        Debug.Log("Rye demo patch inserted at world origin on y=0. Delete parent object to clear. Save scene yourself if desired.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
