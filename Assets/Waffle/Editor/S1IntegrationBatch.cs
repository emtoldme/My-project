using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Waffle.EditorTools
{
    /// <summary>Small repeatable S1 checks and scene preview; no full regression or build required.</summary>
    public static class S1IntegrationBatch
    {
        public static void ValidateScene()
        {
            EditorSceneManager.OpenScene(S1SceneSetup.ScenePath);
            var loop = UnityEngine.Object.FindAnyObjectByType<GameLoop>();
            if (loop == null || loop.actions == null || loop.playerView == null || loop.gameplayCamera == null)
                throw new Exception("S1 scene is missing a core reference.");
            var audio = loop.GetComponent<AudioDirector>();
            var vfx = loop.GetComponent<VfxDirector>();
            if (audio == null || audio.combatLoop == null || audio.levelUpLoop == null)
                throw new Exception("S1 scene is missing music references.");
            var rules = new[] { audio.shoot, audio.enemyHit, audio.enemyDeath, audio.playerHurt, audio.dodge,
                audio.gemPickup, audio.levelUpAppear, audio.levelUpConfirm, audio.victory, audio.defeat };
            if (rules.Any(rule => rule.clips == null || rule.clips.Length == 0 || rule.clips.Any(clip => clip == null)))
                throw new Exception("S1 scene is missing a sound-effect reference.");
            if (vfx == null || vfx.afterimageMaterial == null || vfx.fragmentMaterial == null || vfx.muzzleMaterial == null)
                throw new Exception("S1 scene is missing a VFX material.");
            if (loop.playerView.Find("Visual") == null)
                throw new Exception("S1 scene has no player visual.");
            if (!loop.gameplayCamera.orthographic)
                throw new Exception("S1 camera must be orthographic.");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Waffle/Art/S1_PostFX.asset");
            if (profile == null || profile.components.Count != 2 ||
                profile.components.Any(component => component == null || !AssetDatabase.Contains(component)))
                throw new Exception("S1 post-processing components were not persisted as subassets.");
            Debug.Log("S1_SCENE_CHECK_PASSED: core references, player visual, two music clips, all sound effects and VFX materials.");
        }

        public static void SetupAndCapture()
        {
            S1PresentationSetup.Setup();
            CapturePreview();
        }

        public static void CapturePreview()
        {
            ValidateScene();
            var camera = UnityEngine.Object.FindAnyObjectByType<GameLoop>().gameplayCamera;
            var target = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                target.Create();
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new Exception("Current render pipeline does not support the preview request.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Docs/Verification/P-01");
                File.WriteAllBytes("Docs/Verification/P-01/scene-preview.png", pixels.EncodeToPNG());
                Debug.Log("S1_PREVIEW_SAVED: static engine render without runtime HUD.");
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
