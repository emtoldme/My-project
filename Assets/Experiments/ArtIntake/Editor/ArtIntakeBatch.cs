using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Reverb.ArtIntake.EditorTools
{
    // Inspection only. Does not change gameplay scenes, original models or Build Settings.
    public static class ArtIntakeBatch
    {
        const string Root = "Assets/Experiments/ArtIntake";
        const string Weapon = "Assets/ReverbInTheRye/Weapons/OldSonicModulator";
        public const string ScenePath = Root + "/Scenes/A01_AssetInspection.unity";
        static readonly string[] SurfaceNames = { "WPN_Painted_Olive", "WPN_Aged_Brass", "WPN_Copper_Coil", "WPN_Bakelite", "WPN_Worn_Wood", "WPN_Dusty_Canvas" };
        static readonly string[] Prefixes = { "01_Olive_ChippedPaint", "02_Aged_Brass", "03_Oxidized_Copper", "04_Brown_Bakelite", "05_Worn_Wood", "06_Dusty_Canvas" };

        [Serializable] class ModelInfo { public string name; public int triangles; public Vector3 size; }
        [Serializable] class Report
        {
            public string unity, graphics, scene, capture;
            public bool passed;
            public ModelInfo[] models;
            public string[] shaderErrors, unresolvedWeaponMaterials;
            public string limitations = "Editor import and static inspection only; no gameplay, animation, wind/pause, player build, performance or artistic acceptance.";
        }

        public static void RunBatch()
        {
            try { Inspect(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(2); }
        }

        [MenuItem("Experiments/Art Intake/Create Inspection Scene")]
        public static void Inspect()
        {
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Weapon + "/Materials");
            Directory.CreateDirectory(Weapon + "/Prefabs");
            AssetDatabase.Refresh();
            ReverbGroundSetup.CreateGroundMaterials();
            ReverbFoliageSetup.CreatePrefabs();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var materials = CreateWeaponMaterials();
            var unresolved = new HashSet<string>();
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(Weapon + "/Models/WPN_OldSonicModulator.fbx");
            if (imported == null) throw new Exception("Weapon FBX failed to import.");
            var weapon = (GameObject)PrefabUtility.InstantiatePrefab(imported);
            foreach (var renderer in weapon.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    string name = slots[i] != null ? slots[i].name : "<missing>";
                    if (materials.TryGetValue(name, out var mat)) slots[i] = mat;
                    else unresolved.Add(name);
                }
                renderer.sharedMaterials = slots;
            }
            PrefabUtility.SaveAsPrefabAsset(weapon, Weapon + "/Prefabs/P_OldSonicModulator.prefab");
            weapon.transform.position = new Vector3(0f, .6f, -.5f);
            var modelReport = new List<ModelInfo>();
            foreach (string path in AssetDatabase.FindAssets("t:Model", new[] { "Assets/ReverbInTheRye/Foliage/Models", Weapon + "/Models" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var meshes = model.GetComponentsInChildren<MeshFilter>(true);
                var bounds = new Bounds(); bool first = true;
                foreach (var filter in meshes)
                {
                    var b = filter.sharedMesh.bounds;
                    foreach (var sign in new[] { new Vector3(-1,-1,-1), new Vector3(-1,-1,1), new Vector3(-1,1,-1), new Vector3(-1,1,1), new Vector3(1,-1,-1), new Vector3(1,-1,1), new Vector3(1,1,-1), Vector3.one })
                    {
                        var p = model.transform.InverseTransformPoint(filter.transform.TransformPoint(b.center + Vector3.Scale(b.extents, sign)));
                        if (first) { bounds = new Bounds(p, Vector3.zero); first = false; } else bounds.Encapsulate(p);
                    }
                }
                modelReport.Add(new ModelInfo { name = model.name, triangles = meshes.Sum(x => x.sharedMesh.triangles.Length / 3), size = bounds.size });
            }
            for (int i = 0; i < 3; i++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = new[] { "DrySoil", "DenseStraw", "TrampledPath" }[i];
                tile.transform.position = new Vector3((i - 1) * 6f, -.1f, 0f);
                tile.transform.localScale = new Vector3(6f, .2f, 9f);
                var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/ReverbInTheRye/Ground/Materials/Ground_" + tile.name + ".mat");
                var previewMat = new Material(mat) { name = "M_Inspect_" + tile.name };
                previewMat.SetTextureScale("_BaseMap", new Vector2(2f, 3f));
                string path = Root + "/M_Inspect_" + tile.name + ".mat";
                var previous = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (previous != null) { EditorUtility.CopySerialized(previewMat, previous); UnityEngine.Object.DestroyImmediate(previewMat); previewMat = previous; }
                else AssetDatabase.CreateAsset(previewMat, path);
                tile.GetComponent<Renderer>().sharedMaterial = previewMat;
            }
            for (int i = 0; i < 4; i++)
            {
                string name = new[] { "P_Wheat_A", "P_Wheat_B", "P_CutStubble", "P_WildGrass" }[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ReverbInTheRye/Foliage/Prefabs/" + name + ".prefab");
                if (prefab == null) throw new Exception("Missing foliage prefab " + name);
                var plant = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                plant.transform.position = new Vector3(-7f + 2f * i, 0f, 1.5f);
                // Inspect actual LOD0. Orthographic LOD selection is reviewed separately.
                var lod = plant.GetComponent<LODGroup>(); if (lod != null) lod.ForceLOD(0);
            }
            ReverbInTheRye.VFX.Editor.SonicVFXSetupEditor.Setup();
            var sun = new GameObject("Inspection Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.2f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.38f, .4f, .42f);
            var camera = new GameObject("Inspection Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 7.5f;
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.transform.position = -camera.transform.forward * 18f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.07f, .08f, .09f);
            var shaderNames = new[] { "ReverbInTheRye/FoliageWindURP", "ReverbInTheRye/VFX/ResonancePulseURP", "ReverbInTheRye/VFX/StrawDustURP" };
            for (int i = 1; i < shaderNames.Length; i++)
            {
                string path = "Assets/ReverbInTheRye/VFX/Materials/" + (i == 1 ? "M_SonicPulse.mat" : "M_SonicStrawDust.mat");
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                var card = GameObject.CreatePrimitive(PrimitiveType.Quad); card.name = "Static shader swatch " + i;
                card.transform.position = new Vector3(4f + i, .08f, 1f);
                card.transform.rotation = Quaternion.Euler(90f, 0f, 0f); card.GetComponent<Renderer>().sharedMaterial = mat;
            }
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
            string evidence = GetOutput(); Directory.CreateDirectory(evidence);
            string capture = Path.Combine(evidence, "asset-inspection.png");
            Capture(camera, capture);
            var cameraPosition = camera.transform.position;
            camera.orthographicSize = 2.5f;
            camera.transform.position = new Vector3(-5f, .7f, 1.5f) - camera.transform.forward * 12f;
            Capture(camera, Path.Combine(evidence, "foliage-closeup.png"));
            camera.orthographicSize = 1.6f;
            camera.transform.position = new Vector3(0f, .8f, -.5f) - camera.transform.forward * 12f;
            Capture(camera, Path.Combine(evidence, "weapon-closeup.png"));
            camera.transform.position = cameraPosition; camera.orthographicSize = 7.5f;
            var errors = new List<string>();
            foreach (string name in shaderNames)
            {
                var shader = Shader.Find(name);
                if (shader == null || !shader.isSupported) errors.Add(name + ": missing/unsupported");
                if (shader != null) foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    if (message.severity.ToString() == "Error") errors.Add(name + ": " + message.message);
            }
            bool passed = errors.Count == 0 && unresolved.Count == 0 && modelReport.Count == 7;
            File.WriteAllText(Path.Combine(evidence, "import-report.json"), JsonUtility.ToJson(new Report { unity = Application.unityVersion, graphics = SystemInfo.graphicsDeviceName, scene = ScenePath, capture = "asset-inspection.png", passed = passed, models = modelReport.ToArray(), shaderErrors = errors.ToArray(), unresolvedWeaponMaterials = unresolved.ToArray() }, true));
            if (!passed) throw new Exception("Art intake failed; see import-report.json");
            Debug.Log("ART_INTAKE_PASSED models=7 foliagePrefabs=4 groundMaterials=3 weaponPrefab=1 capture=" + capture);
        }

        static Dictionary<string, Material> CreateWeaponMaterials()
        {
            var result = new Dictionary<string, Material>();
            foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[] { Weapon + "/Textures" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                bool normal = path.Contains("_Normal_");
                bool color = path.Contains("_BaseColor") || path.Contains("_RGBA") || path.Contains("_Emission");
                if (importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) || importer.sRGBTexture != color)
                { importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default; importer.sRGBTexture = color; importer.SaveAndReimport(); }
            }
            for (int i = 0; i < SurfaceNames.Length; i++)
            {
                var mat = Lit(SurfaceNames[i]);
                mat.SetTexture("_BaseMap", Tex(Prefixes[i] + "_BaseColor.png"));
                mat.SetTexture("_BumpMap", Tex(Prefixes[i] + "_Normal_OpenGL.png")); mat.SetFloat("_BumpScale", i == 0 ? .18f : .45f); mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_MetallicGlossMap", Tex(Prefixes[i] + "_Unity_MetallicSmoothness.png")); mat.SetFloat("_Smoothness", 1f); mat.SetFloat("_SmoothnessTextureChannel", 0f); mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                result.Add(SurfaceNames[i], mat); EditorUtility.SetDirty(mat);
            }
            foreach (string name in new[] { "WPN_Glass", "MAT_Vacuum_Glass", "WPN_Tube_AmberGlow", "MAT_Amber_Filament", "MAT_Tube_Metal", "WPN_Analog_Meter_Decal", "WPN_Tuning_Dial_Decal" })
            {
                var mat = Lit(name); result.Add(name, mat);
                if (name.Contains("Glass")) { Transparent(mat); mat.SetColor("_BaseColor", new Color(.85f,.88f,.92f,.18f)); mat.SetFloat("_Smoothness",.92f); }
                else if (name.Contains("Amber")) { mat.SetColor("_BaseColor", new Color(1f,.5f,.125f)); mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1f,.5f,.125f) * 3f); }
                else if (name.Contains("Decal")) { mat.SetTexture("_BaseMap", Tex(name.Contains("Meter") ? "08_Analog_Meter_Face_RGBA.png" : "09_Tuning_Dial_RGBA.png")); Transparent(mat); mat.SetFloat("_Smoothness",.15f); }
                else { mat.SetColor("_BaseColor", new Color(.35f,.35f,.35f)); mat.SetFloat("_Metallic",.8f); mat.SetFloat("_Smoothness",.3f); }
                EditorUtility.SetDirty(mat);
            }
            return result;
        }
        static Texture2D Tex(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(Weapon + "/Textures/" + file) ?? throw new Exception("Missing weapon texture " + file);
        static Material Lit(string name)
        {
            string path = Weapon + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; AssetDatabase.CreateAsset(mat, path); }
            return mat;
        }
        static void Transparent(Material mat)
        {
            mat.SetFloat("_Surface",1f); mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha); mat.SetFloat("_ZWrite",0f);
            mat.SetOverrideTag("RenderType","Transparent"); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = (int)RenderQueue.Transparent;
        }
        static string GetOutput()
        {
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args,"--art-intake-output");
            return index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1]) : Path.GetFullPath("Docs/Verification/Art-Intake-01");
        }
        static void Capture(Camera camera, string path)
        {
            var target = RenderTexture.GetTemporary(1280,720,24); var previous = RenderTexture.active; Texture2D pixels = null;
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera,request)) throw new Exception("URP capture unavailable");
                // Warm up the render path before recording newly imported textures and lights.
                for (int i = 0; i < 3; i++) RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active = target;
                pixels = new Texture2D(1280,720,TextureFormat.RGB24,false); pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels); }
        }
    }
}
