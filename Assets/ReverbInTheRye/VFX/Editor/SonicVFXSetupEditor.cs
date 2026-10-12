#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ReverbInTheRye.VFX;

namespace ReverbInTheRye.VFX.Editor
{
    /// <summary>
    /// One-click technical preview setup. Creates only shader materials and a runtime emitter;
    /// never modifies enemy/weapon prefabs, art, audio, combat rules or project settings.
    /// </summary>
    public static class SonicVFXSetupEditor
    {
        private const string Root = "Assets/ReverbInTheRye";
        private const string VfxFolder = Root + "/VFX";
        private const string MaterialFolder = VfxFolder + "/Materials";

        [MenuItem("Tools/Reverb in the Rye/Setup Sonic VFX Preview")]
        public static void Setup()
        {
            EnsureFolder("Assets", "ReverbInTheRye");
            EnsureFolder(Root, "VFX");
            EnsureFolder(VfxFolder, "Materials");

            Material pulse = GetOrCreateMaterial(
                MaterialFolder + "/M_SonicPulse.mat", "ReverbInTheRye/VFX/ResonancePulseURP");
            Material dust = GetOrCreateMaterial(
                MaterialFolder + "/M_SonicStrawDust.mat", "ReverbInTheRye/VFX/StrawDustURP");
            if (pulse == null || dust == null) return;

            SonicAttackVFX emitter = Object.FindAnyObjectByType<SonicAttackVFX>();
            if (emitter == null)
            {
                var go = new GameObject("SonicAttackVFX_Prototype");
                Undo.RegisterCreatedObjectUndo(go, "Setup Sonic Attack VFX");
                emitter = go.AddComponent<SonicAttackVFX>();
            }

            Undo.RecordObject(emitter, "Assign Sonic VFX Materials");
            var serialized = new SerializedObject(emitter);
            serialized.FindProperty("resonancePulseMaterial").objectReferenceValue = pulse;
            serialized.FindProperty("strawDustMaterial").objectReferenceValue = dust;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(emitter);
            Selection.activeObject = emitter.gameObject;
            Debug.Log("Sonic VFX prototype is ready. Save your scene, enter Play Mode, and use the component's Preview context menus.");
        }

        private static void EnsureFolder(string parent, string leaf)
        {
            string path = parent + "/" + leaf;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, leaf);
        }

        private static Material GetOrCreateMaterial(string path, string shaderName)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("Missing shader: " + shaderName + ". Import the .shader files first.");
                return null;
            }
            var created = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            return created;
        }
    }
}
#endif
