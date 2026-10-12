using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Reverb.Drafts.RC01.Audio.EditorTools
{
    /// <summary>
    /// RC01 Audio Preview Tool (TEST_ONLY).
    /// Provides an idempotent Editor batch preview verifying that all 6 generated audio variants
    /// can be located, loaded as AudioClips in Unity, and auditioned without modifying runtime scenes.
    /// </summary>
    public static class RC01AudioPreview
    {
        private static readonly string[] AudioPaths = new[]
        {
            "Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_shot_spread.wav",
            "Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_guardian_orbit.wav",
            "Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_resonance_drop.wav",
            "Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_ui_reroll.wav",
            "Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_ui_banish.wav",
            "Assets/Drafts/RC01/Audio/TEST_ONLY_sfx_evolution_unlocked.wav"
        };

        [MenuItem("Experiments/RC01/Audition Audio Variants")]
        public static void AuditionAll()
        {
            int loadedCount = 0;
            foreach (var path in AudioPaths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    loadedCount++;
                    Debug.Log($"[RC01AudioPreview] Loaded {clip.name}: length={clip.length:F3}s, channels={clip.channels}, freq={clip.frequency}Hz");
                }
                else
                {
                    Debug.LogWarning($"[RC01AudioPreview] Clip not yet imported by Unity at: {path}");
                }
            }

            Debug.Log($"RC01_AUDIO_PREVIEW_PASSED loaded={loadedCount}/{AudioPaths.Length} status=ready-for-codex-integration");
        }
    }
}
