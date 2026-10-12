using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Waffle;
using Waffle.Experiments.Mechanism.EditorTools;
using Waffle.Experiments.Mechanism.Presentation;
using Reverb.Experiments.Mechanism.Integration;
using Reverb.Drafts.RC01.Presentation;
using ReverbInTheRye.VFX;

namespace Reverb.Drafts.RC01.EditorTools
{
    public static class RC01IntegrationBatch
    {
        public const string ScenePath = "Assets/Drafts/RC01/Scenes/RC01_HarvestRoad.unity";
        [Serializable] class Cue { public string id, assetPath; public string[] variants; public float pitch = 1, pitchJitter, volumeDb, minInterval, spatialBlend; public int maxVoices = 1, priority = 8; public bool pauseClear; }
        [Serializable] class Categories { public Cue[] weapons, mechanisms, pickups, growthUi; }
        [Serializable] class Map { public Categories categories; }

        public static void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Reverb.Drafts.RC01.Art.EditorTools.RC01ArtPreview.Build();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RC01SceneSetup.CreatePreviews();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var loop = UnityEngine.Object.FindAnyObjectByType<GameLoop>();
            if (loop == null) throw new Exception("RC01 GameLoop missing");
            loop.config.extraXpStepAfterFour = .4f;
            loop.config.useRunContent = true; loop.config.runDuration = 600;
            loop.config.arenaHalfExtent = 18.5f; loop.config.gemLifetime = 60;
            var c = loop.combatConfig;
            c.aliveCap = 48; c.eventOverflow = 8; c.enemyPoolSize = 56;
            c.projectilePoolSize = 128; c.gemPoolSize = 220; c.maxGems = 200;
            c.phaseStarts = new[] { 0f,120,240,360,480 };
            c.phaseHp = new[] { 12f,20,28,40,48 };
            c.phaseSpeed = new[] { 1.55f,1.9f,2.25f,2.6f,2.9f };
            c.waveCounts = new[] { 2,3,3,4,4 };
            c.waveIntervals = new[] { 2.6f,1.9f,1.45f,1.1f,.9f };
            c.pushTimes = new[] { 115f,355 }; c.pushCounts = new[] { 8,12 };
            c.ringTimes = new[] { 180f,420,540 }; c.ringCounts = new[] { 10,14,16 };
            c.eliteTimes = new[] { 120f,240,360,480,570 };
            c.eliteHpMultiplier = 8; c.eliteSpeedMultiplier = .65f;
            c.gemValue = 2; c.eliteGemValue = 25;
            c.targetRange = 10; c.contactDamage = 6; c.eliteContactDamage = 12;
            var views = loop.GetComponent<CombatViews>(); views.tintEnemyMaterials = false;

            var ground = Load<Material>("Assets/Drafts/RC01/Art/Materials/M_RC01_HarvestRoad_Ground.mat");
            ground.SetFloat("_Smoothness", .14f); EditorUtility.SetDirty(ground);
            var apron = GameObject.CreatePrimitive(PrimitiveType.Plane);
            apron.name = "RC01_GroundContinuation"; apron.transform.position = new Vector3(0,-.025f,0);
            apron.transform.localScale = new Vector3(10,1,10);
            UnityEngine.Object.DestroyImmediate(apron.GetComponent<Collider>());
            const string apronPath = "Assets/Drafts/RC01/Environments/Materials/M_RC01_GroundContinuation.mat";
            var apronMat = AssetDatabase.LoadAssetAtPath<Material>(apronPath);
            if (apronMat == null) { apronMat = new Material(ground); AssetDatabase.CreateAsset(apronMat,apronPath); }
            else EditorUtility.CopySerialized(ground,apronMat);
            apronMat.SetTextureScale("_BaseMap",new Vector2(20,20)); EditorUtility.SetDirty(apronMat);
            apron.GetComponent<Renderer>().sharedMaterial = apronMat;
            var terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            var layers = terrain.terrainData.terrainLayers;
            for (int i = 0;i < layers.Length;i++)
            {
                string path = RC01SceneSetup.EnvRoot + "/Terrain/TL_RC01_Harvest_" + i + ".terrainlayer";
                var copy = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (copy == null) { copy = new TerrainLayer(); AssetDatabase.CreateAsset(copy,path); }
                EditorUtility.CopySerialized(layers[i],copy); copy.smoothness = .14f;
                copy.tileSize = new Vector2(i == 2 ? 6 : 5, i == 2 ? 6 : 5);
                EditorUtility.SetDirty(copy); layers[i] = copy;
            }
            terrain.terrainData.terrainLayers = layers; EditorUtility.SetDirty(terrain.terrainData);
            var env = terrain.transform.parent != null ? terrain.transform.parent.gameObject : null;
            if (env == null) throw new Exception("RC01 environment missing");
            var wind = Load<Material>("Assets/Drafts/RC01/Art/Materials/M_RC01_ControlledFoliage.mat");
            wind.SetFloat("_UseExternalTime",1); wind.SetFloat("_VisualTime",0); EditorUtility.SetDirty(wind);
            foreach (var r in env.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = wind;
            loop.gameObject.AddComponent<M01DisplayControls>();
            loop.gameObject.AddComponent<RC01StandaloneSmoke>();
            var fx = loop.gameObject.AddComponent<M01DerivedFx>(); fx.loop = loop;
            var line = Load<Material>("Assets/Experiments/Mechanism/Materials/M_M01_Transparent.mat"); fx.lineMaterial = line;
            var view = loop.gameObject.AddComponent<RC01BattleView>(); view.lineMaterial = line;
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
            feedback.loop = loop; feedback.sonic = sonic; feedback.environment = env.transform; feedback.windMaterial = wind;
            ConfigureAudio(loop.gameObject.AddComponent<AudioDirector>(),loop);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            Debug.Log("RC01_INTEGRATED_SETUP_PASSED weapons=4 passives=10 evolutions=3 pickups=4 xp=enabled previewMaps=3");
        }
        public static void ApplyFeedback()
        {
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var loop=UnityEngine.Object.FindAnyObjectByType<GameLoop>();
            var audio=UnityEngine.Object.FindAnyObjectByType<AudioDirector>();
            if(loop==null || audio==null) throw new Exception("RC01 host missing");
            loop.config.extraXpStepAfterFour=.4f;
            ConfigureAudio(audio,loop);
            if(audio.enemyDeath.clips.Length!=3 || audio.gemPickup.clips.Length!=5) throw new Exception("Feedback variants missing");
            EditorSceneManager.MarkSceneDirty(loop.gameObject.scene);
            EditorSceneManager.SaveScene(loop.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RC01_SOUND_R1_APPLIED 22 delivered clips; feedback XP preserved");
        }

        static void ConfigureAudio(AudioDirector audio, GameLoop loop)
        {
            audio.loop = loop; audio.enableMechanismAudio = true;
            audio.sfxTrimDb = -10; audio.musicDb = -18;
            audio.combatLoop = Load<AudioClip>("Assets/Audio/S03/bgm_s03_mathrock_polymeter_24s_loop.wav");
            audio.levelUpLoop = Load<AudioClip>("Assets/Audio/Music/bgm_levelup_suspended_loop.wav");
            var map = JsonUtility.FromJson<Map>(File.ReadAllText("Docs/Design/RC01/Audio/cue-map.json"));
            var bindings = new List<AudioContentBinding>();
            foreach (var cue in map.categories.weapons)
            {
                if (cue.id == "W05" || cue.id == "W06") continue;
                var type = cue.id == "W03" ? PresentationEventType.EnemyHit : cue.id == "W04" ? PresentationEventType.WeaponZone : PresentationEventType.Shot;
                bindings.Add(new AudioContentBinding { contentId = cue.id, eventType = type, rule = Rule(cue) });
            }
            foreach (var cue in map.categories.pickups)
                if (cue.id == "D02" || cue.id == "D03" || cue.id == "D04")
                    bindings.Add(new AudioContentBinding { contentId = cue.id, eventType = PresentationEventType.PickupCollected, rule = Rule(cue) });
            foreach (var cue in map.categories.growthUi)
            {
                if (cue.id == "UI_LevelUp_Appear") audio.levelUpAppear = Rule(cue);
                else if (cue.id == "UI_Card_Chosen") audio.levelUpConfirm = Rule(cue);
                else if (cue.id == "UI_Reroll" || cue.id == "UI_Banish" || cue.id == "UI_Evolution_Chest")
                    bindings.Add(new AudioContentBinding { contentId = "*", eventType = cue.id == "UI_Reroll" ? PresentationEventType.GrowthReroll
                        : cue.id == "UI_Banish" ? PresentationEventType.GrowthBanish : PresentationEventType.Evolved, rule = Rule(cue) });
            }
            foreach (var cue in map.categories.mechanisms)
            {
                if (cue.id == "P12") audio.echo = Rule(cue);
                else if (cue.id == "P13") audio.propagation = Rule(cue);
                else if (cue.id == "P12_P13") audio.echoPropagation = Rule(cue);
                else if (cue.id == "DirectHit") audio.enemyHit = Rule(cue);
                else if (cue.id == "EnemyDeath") audio.enemyDeath = Rule(cue);
            }
            audio.playerHurt.clips = new[] { Load<AudioClip>("Assets/Audio/S03/sfx_s03_player_hurt.wav") };
            audio.dodge.clips = new[] { Load<AudioClip>("Assets/Audio/S03/sfx_s03_dodge.wav") };
            audio.gemPickup.clips = new[] { Load<AudioClip>("Assets/Audio/S03/sfx_s03_gem_pickup.wav") };
            audio.victory.clips = new[] { Load<AudioClip>("Assets/Audio/SFX/sfx_game_victory.wav") };
            audio.defeat.clips = new[] { Load<AudioClip>("Assets/Audio/SFX/sfx_game_defeat.wav") };
            // Reuse delivered variations instead of one clip on every hit/shot/pickup.
            foreach (var b in bindings)
                if (b.contentId == "W01") { b.rule.clips = Variants("sfx_player_shoot",3); b.rule.volumeDb -= 2; b.rule.sequential = true; }
            audio.enemyHit.clips = Variants("sfx_enemy_hit",3); audio.enemyHit.sequential = true;
            audio.enemyHit.volumeDb -= 2; audio.enemyHit.minInterval = .075f;
            audio.enemyDeath.minInterval = .10f;
            audio.dodge.clips = Variants("sfx_player_dodge",2); audio.dodge.sequential = true;
            audio.playerHurt.clips = Variants("sfx_player_hurt",2); audio.playerHurt.sequential = true;
            audio.gemPickup.clips = Variants("sfx_gem_pickup",5); audio.gemPickup.volumeDb -= 3;
            audio.echo.minInterval = .10f; audio.propagation.minInterval = .09f; audio.echoPropagation.minInterval = .12f;
            // Overlay only R1's delivered groups; pickup/hurt/dodge/music keep the feedback baseline.
            var polish=JsonUtility.FromJson<Map>(File.ReadAllText("Docs/Design/RC01/Audio/PolishR1/cue-map.json"));
            foreach(var cue in polish.categories.weapons)
            {
                var binding=bindings.Find(b=>b.contentId==cue.id);
                if(binding==null) throw new Exception("Unknown polished weapon "+cue.id);
                binding.rule=PolishedRule(cue);
            }
            foreach(var cue in polish.categories.mechanisms)
            {
                if(cue.id=="EchoHit") audio.echo=PolishedRule(cue);
                else if(cue.id=="PropagationHit")
                {
                    audio.propagation=PolishedRule(cue);
                    audio.echoPropagation=PolishedRule(cue);
                    audio.echoPropagation.name="R1_EchoPropagation";
                    audio.echoPropagation.pitch=.85f;audio.echoPropagation.volumeDb-=1.5f;
                    audio.echoPropagation.maxVoices=1;audio.echoPropagation.minInterval=.12f;
                }
                else if(cue.id=="EnemyDeath") audio.enemyDeath=PolishedRule(cue);
                else throw new Exception("Unknown polished mechanism "+cue.id);
            }
            foreach(var cue in polish.categories.growthUi)
            {
                if(cue.id=="UI_Card_Chosen") audio.levelUpConfirm=PolishedRule(cue);
                else if(cue.id=="UI_Evolution_Chest")
                {
                    var evolved=bindings.Find(b=>b.eventType==PresentationEventType.Evolved);
                    if(evolved==null) throw new Exception("Evolution binding missing");
                    evolved.rule=PolishedRule(cue);
                }
                else throw new Exception("Unknown polished growth cue "+cue.id);
            }
            // Suppress the busiest short voices slightly to keep the mechanism identity audible.
            audio.echo.minInterval=Math.Max(.10f,audio.echo.minInterval);
            audio.propagation.minInterval=Math.Max(.09f,audio.propagation.minInterval);
            audio.enemyDeath.minInterval=Math.Max(.10f,audio.enemyDeath.minInterval);
            audio.contentBindings = bindings.ToArray();
        }
        static AudioEventRule Rule(Cue c) => new AudioEventRule(c.id,c.volumeDb,c.pitchJitter,Math.Max(1,c.maxVoices),c.minInterval,c.spatialBlend,25)
            { clips = CueClips(c), pitch = c.pitch, dropWhenFull = true };
        static AudioEventRule PolishedRule(Cue cue)
        {
            var rule=Rule(cue);rule.name="R1_"+cue.id;rule.sequential=true;
            rule.clearOnPause=cue.pauseClear;
            // Listener is on the camera, 30m from the player; default 1m rolloff attenuated centre combat.
            rule.minDistance=30;rule.maxDistance=65;rule.priority=Mathf.Clamp(160-cue.priority*12,0,256);
            return rule;
        }
        static AudioClip[] CueClips(Cue cue)
        {
            if (cue.variants == null || cue.variants.Length == 0) return new[] { Load<AudioClip>(cue.assetPath) };
            var clips = new AudioClip[cue.variants.Length];
            for (int i=0;i<clips.Length;i++) clips[i]=Load<AudioClip>(cue.variants[i]);
            return clips;
        }
        static AudioClip[] Variants(string name,int count)
        {
            var clips=new AudioClip[count];
            for(int i=0;i<count;i++) clips[i]=Load<AudioClip>("Assets/Audio/SFX/"+name+"_"+(i+1).ToString("00")+".wav");
            return clips;
        }
        static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new Exception("Missing asset " + path);
        public static void Build()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/RC01-SoundR1/Reverb.exe"));
            M01IntegrationBatch.BuildScene(ScenePath,output);
        }
    }
}
