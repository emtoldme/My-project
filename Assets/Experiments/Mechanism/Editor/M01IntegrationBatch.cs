using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reverb.Experiments.Mechanism.Integration;
using Waffle;

namespace Waffle.Experiments.Mechanism.EditorTools
{
    public static class M01IntegrationBatch
    {
        [MenuItem("Experiments/Setup Integrated M01 Playtest")]
        public static void Setup()
        {
            // Only this tool's isolated scene is written. Never overwrite an open E01 scene.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            M01PlaytestSetup.Setup();
            var loop = UnityEngine.Object.FindAnyObjectByType<GameLoop>();
            loop.combatConfig.phaseHp = new[] { 45f, 45f, 55f, 60f };
            loop.combatConfig.phaseSpeed = new[] { 1.2f, 1.3f, 1.4f, 1.5f };
            loop.combatConfig.eliteTimes = new float[0];
            // Freeze weapon stats for the four-mode comparison; ordinary scenes retain XP/upgrades.
            loop.combatConfig.gemValue = 0; loop.combatConfig.eliteGemValue = 0;
            loop.gameObject.AddComponent<M01PlaytestController>();
            loop.gameObject.AddComponent<M01StandaloneSmoke>();
            var audio = loop.gameObject.AddComponent<AudioDirector>();
            audio.loop = loop; audio.enableMechanismAudio = true; audio.sfxTrimDb = -18f;
            audio.shoot.clips = Clips("Assets/Audio/S03/sfx_s03_shot.wav");
            audio.enemyHit.clips = Clips("Assets/Audio/S03/sfx_s03_enemy_hit.wav");
            audio.enemyDeath.clips = Clips("Assets/Audio/SFX/sfx_enemy_death_01.wav", "Assets/Audio/SFX/sfx_enemy_death_02.wav");
            audio.playerHurt.clips = Clips("Assets/Audio/S03/sfx_s03_player_hurt.wav");
            audio.dodge.clips = Clips("Assets/Audio/S03/sfx_s03_dodge.wav");
            audio.echo.clips = Clips("Assets/Audio/SFX/sfx_enemy_hit_01.wav");
            audio.propagation.clips = Clips("Assets/Audio/S03/sfx_s03_shot.wav");
            audio.echoPropagation.clips = Clips("Assets/Audio/S03/sfx_s03_shot.wav");
            audio.echo.pitch = 0.85f; audio.propagation.pitch = 1.15f; audio.echoPropagation.pitch = 0.9f;
            foreach (var rule in new[] { audio.shoot, audio.enemyHit, audio.enemyDeath, audio.echo, audio.propagation, audio.echoPropagation })
            { rule.dropWhenFull = true; rule.spatialBlend = 0f; rule.maxVoices = Mathf.Min(2, rule.maxVoices); }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("M01_INTEGRATED_SETUP_PASSED controls=ready-on-play seed=12345 assets=TEST_ONLY");
        }

        static AudioClip[] Clips(params string[] paths)
        {
            var result = new AudioClip[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            { result[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]); if (result[i] == null) throw new Exception("Missing TEST_ONLY clip: " + paths[i]); }
            return result;
        }

        public static void Build()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/M01-MechanismPlaytest/Reverb.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { M01PlaytestSetup.ScenePath },
                locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("M01 build failed: " + report.summary.result);
            Debug.Log("M01_BUILD_PASSED warnings=" + report.summary.totalWarnings + " errors=" + report.summary.totalErrors + " path=" + output);
        }
    }
}
