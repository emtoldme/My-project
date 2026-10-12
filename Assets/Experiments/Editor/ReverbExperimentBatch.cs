using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reverb.Experiments.AudioSync;
using Waffle.Experiments.Equipment;
using Waffle.Experiments.VisualPipeline.EditorTools;

namespace Reverb.Experiments.EditorTools
{
    public static class ReverbExperimentBatch
    {
        public const string AudioScene = "Assets/Experiments/AudioSync/E03_AudioSync.unity";
        [MenuItem("Experiments/Use E03 TEST_ONLY V2 Signals")]
        public static void SetupAudioV2()
        {
            EditorSceneManager.OpenScene(AudioScene, OpenSceneMode.Single);
            var controller = UnityEngine.Object.FindAnyObjectByType<DspSyncExperimentController>();
            var a = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Experiments/AudioSync/TEST_ONLY_track_a_base_16s_v2.wav");
            var b = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Experiments/AudioSync/TEST_ONLY_track_b_shimmer_16s_v2.wav");
            if (controller == null || a == null || b == null) throw new InvalidOperationException("V2 assets not imported.");
            controller.ConfigureClips(a, b);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("E03_V2_SCENE_READY " + a.name + " / " + b.name);
        }
        public static void Setup()
        {
            E01VisualPipelineSetup.Setup();
            string graphPath = "Assets/Experiments/VisualPipeline/Shaders/E01_WaveRing.shadergraph";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(graphPath);
            if (shader == null || shader.FindPropertyIndex("_Radius") < 0 || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("E01 ShaderGraph not ready; see import log.");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("TEST_ONLY Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.04f, 0.05f);
            camera.gameObject.AddComponent<AudioListener>();
            var controller = new GameObject("TEST_ONLY DSP Harness").AddComponent<DspSyncExperimentController>();
            controller.ConfigureClips(
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Experiments/AudioSync/TEST_ONLY_track_a_base_16s_v2.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Experiments/AudioSync/TEST_ONLY_track_b_shimmer_16s_v2.wav"));
            controller.gameObject.AddComponent<DspSyncDebugGui>();
            EditorSceneManager.SaveScene(scene, AudioScene);
            Directory.CreateDirectory("Docs/Verification/Reverb-Experiments-01");
            File.WriteAllText("Docs/Verification/Reverb-Experiments-01/e02-comparison.txt", ComboLabScenario.CompareAll());
            AssetDatabase.SaveAssets();
            Debug.Log("REVERB_EXPERIMENT_SETUP_PASSED ShaderGraph=True audioScene=True equipmentComparison=True");
        }
    }
}
