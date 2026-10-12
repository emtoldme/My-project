using System;
using System.IO;
using Reverb.Experiments.Mechanism.Integration;
using Reverb.Experiments.RyeField.EditorTools;
using ReverbInTheRye.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Waffle;

namespace Waffle.Experiments.Mechanism.EditorTools
{
    public static class RF01IntegrationBatch
    {
        public const string ScenePath = "Assets/Experiments/RyeField/Scenes/RF01_MechanismPlaytest.unity";
        const string Materials = "Assets/Experiments/Mechanism/Materials";

        public static void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RyeFieldSetup.CreatePreview();
            M01IntegrationBatch.Setup();
            // Copy the integrated mechanism scene first. Following edits belong only to RF01.
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            var loop = UnityEngine.Object.FindAnyObjectByType<GameLoop>();
            var environment = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RyeFieldSetup.PrefabPath));
            environment.transform.position = new Vector3(0f, .015f, 0f);
            var floor = GameObject.Find("TEST_ONLY_FLOOR");
            if (floor != null)
            {
                floor.name = "GroundContinuation";
                floor.transform.position = new Vector3(0f, -.02f, 0f);
                var soil = CopyMaterial("Assets/ReverbInTheRye/Ground/Materials/Ground_DrySoil.mat", Materials + "/M_RF01_GroundContinuation.mat");
                soil.SetTextureScale("_BaseMap", new Vector2(15f,15f));
                floor.GetComponent<Renderer>().sharedMaterial = soil;
            }
            var wind = CopyMaterial("Assets/ReverbInTheRye/Foliage/Materials/M_RyeFoliageWind.mat", Materials + "/M_RF01_ControlledWind.mat");
            wind.SetFloat("_UseExternalTime", 1f); wind.SetFloat("_VisualTime", 0f);
            foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>(true)) renderer.sharedMaterial = wind;
            var sonic = loop.gameObject.AddComponent<SonicAttackVFX>();
            var serialized = new SerializedObject(sonic);
            serialized.FindProperty("resonancePulseMaterial").objectReferenceValue = Load<Material>("Assets/ReverbInTheRye/VFX/Materials/M_SonicPulse.mat");
            serialized.FindProperty("strawDustMaterial").objectReferenceValue = Load<Material>("Assets/ReverbInTheRye/VFX/Materials/M_SonicStrawDust.mat");
            serialized.FindProperty("externallyDriven").boolValue = true;
            serialized.FindProperty("compactDirectHits").boolValue = true;
            serialized.FindProperty("maxFireInstances").intValue = 6;
            serialized.FindProperty("maxHitInstances").intValue = 8;
            serialized.FindProperty("maxDeathInstances").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var feedback = loop.gameObject.AddComponent<RF01FeedbackBridge>();
            feedback.loop = loop; feedback.sonic = sonic; feedback.environment = environment.transform; feedback.windMaterial = wind;
            var weapon = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/ReverbInTheRye/Weapons/OldSonicModulator/Prefabs/P_OldSonicModulator.prefab"));
            weapon.name = "RF01_WeaponDisplay_OriginalScale"; weapon.transform.position = new Vector3(-4.5f, .02f, 3f);
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.55f,.56f,.52f);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("RF01_INTEGRATED_SETUP_PASSED scene=" + ScenePath + " actors=TEST_ONLY weapon=display-only sonic=18-prewarmed");
        }

        public static void Build()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/RF01-RyeField/Reverb.exe"));
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args,"--rf01-build-output");
            if (flag >= 0 && flag + 1 < args.Length) output = Path.GetFullPath(args[flag + 1]);
            M01IntegrationBatch.BuildScene(ScenePath, output);
        }

        static T Load<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new Exception("Missing asset " + path);
        static Material CopyMaterial(string source, string target)
        {
            var original = Load<Material>(source); var copy = AssetDatabase.LoadAssetAtPath<Material>(target);
            if (copy == null) { copy = new Material(original) { name = Path.GetFileNameWithoutExtension(target) }; AssetDatabase.CreateAsset(copy,target); }
            else EditorUtility.CopySerialized(original,copy);
            EditorUtility.SetDirty(copy); return copy;
        }
    }
}
