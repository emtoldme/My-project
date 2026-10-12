using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Waffle.S2.EditorTools
{
    /// <summary>
    /// 菜单 Waffle/Setup S2 Style Lab。可重复运行：只重建 S2_StyleLab 和 Assets/Waffle/S2 下的占位资源。
    /// 不改 S1_Graybox、不改共享脚本、不弹覆盖确认。
    /// 批处理：-executeMethod Waffle.S2.EditorTools.S2StyleLabSetup.Setup
    /// </summary>
    public static class S2StyleLabSetup
    {
        public const string ScenePath = "Assets/Waffle/Scenes/S2_StyleLab.unity";
        const string Root = "Assets/Waffle/S2";
        const string MatDir = Root + "/Materials";
        const string MeshDir = Root + "/Meshes";
        const string PrefabDir = Root + "/Prefabs";
        const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        const string SfxDir = "Assets/Audio/SFX/";
        const string MusicDir = "Assets/Audio/Music/";

        [MenuItem("Waffle/Setup S2 Style Lab")]
        public static void Setup()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(MeshDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory("Assets/Waffle/Scenes");

            Material ceramic = Lit("M_S2_Ceramic", new Color(0.90f, 0.90f, 0.86f), 0f, 0.16f, Color.black);
            Material graphite = Lit("M_S2_Graphite", new Color(0.22f, 0.23f, 0.25f), 0.08f, 0.2f, Color.black);
            Material metal = Lit("M_S2_Metal", new Color(0.55f, 0.58f, 0.62f), 0.55f, 0.38f, Color.black);
            Material glow = Lit("M_S2_Accent", new Color(0.16f, 0.18f, 0.2f), 0f, 0.12f, new Color(1f, 0.27f, 0f) * 1.2f);
            Material ground = Lit("M_S2_Ground", new Color(0.45f, 0.48f, 0.5f), 0f, 0.14f, Color.black);
            Material line = Unlit("M_S2_Line", new Color(1f, 0.27f, 0f, 1f));
            Material shard = Unlit("M_S2_Shard", new Color(0.93f, 0.74f, 0.32f, 1f));
            Material burstMat = ParticleUnlit("M_S2_Burst", new Color(1f, 0.27f, 0f, 1f));

            Mesh floor = FloorMesh();
            GameObject playerPrefab = Sculpture("RS_Player", ceramic, metal, glow, false);
            GameObject enemyPrefab = Sculpture("RS_Enemy", graphite, metal, glow, true);
            GameObject groundPrefab = GroundModule(ground, metal);
            GameObject shardPrefab = ShardPrefab(shard);
            ParticleSystem burst = BurstPrefab(burstMat);
            S2FxProfile profile = Profile();

            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!scene.IsValid() || (!string.IsNullOrEmpty(scene.path) && scene.path != ScenePath))
                throw new System.InvalidOperationException("没有打开 " + ScenePath + "，已停止，避免改到别的场景。请先保存或放弃当前场景后再运行。本工具不弹覆盖确认。");
            foreach (GameObject go in scene.GetRootGameObjects())
                Object.DestroyImmediate(go);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.55f, 0.58f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.86f, 0.9f, 0.95f);
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(52f, -36f, 0f);
            var fill = new GameObject("Fill Light").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.25f;
            fill.color = new Color(0.75f, 0.8f, 0.88f);
            fill.transform.rotation = Quaternion.Euler(20f, 140f, 0f);

            var floorGo = new GameObject("Arena_Floor_Placeholder");
            floorGo.transform.localScale = Vector3.one;
            var floorMesh = floorGo.AddComponent<MeshFilter>();
            floorMesh.sharedMesh = floor;
            var floorRenderer = floorGo.AddComponent<MeshRenderer>();
            floorRenderer.sharedMaterial = ground;

            GameObject groundArt = LoadArt("RS_Ground", "Ground", "RS_GroundModule");
            GameObject groundInstance = (GameObject)PrefabUtility.InstantiatePrefab(groundArt != null ? groundArt : groundPrefab);
            groundInstance.name = groundArt != null ? "RS_Ground_Art" : "RS_GroundModule_Placeholder";
            groundInstance.transform.position = new Vector3(0f, 0.02f, 0f);
            groundInstance.transform.localScale = Vector3.one;

            GameObject playerArt = LoadArt("RS_Player", "Player");
            bool playerArtOk = playerArt != null && HasMotion(playerArt);
            if (playerArt != null && !playerArtOk)
                Debug.LogWarning("[Waffle] A-03 玩家预制体没有 MotionRoot，样板继续用占位几何。");
            var player = new GameObject("Player");
            player.transform.localScale = Vector3.one;
            player.transform.position = Vector3.zero;
            var visual = new GameObject("Visual").transform;
            visual.SetParent(player.transform, false);
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(playerArtOk ? playerArt : playerPrefab);
            model.transform.SetParent(visual, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            StripColliders(player);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.63f, 0.67f, 0.71f);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.orthographic = true;
            follow.orthographicSize = 9f;
            follow.pitch = 55f;
            follow.yaw = 45f;
            follow.followSmoothTime = 0.08f;
            follow.Apply();

            var loopGo = new GameObject("GameLoop");
            var gameLoop = loopGo.AddComponent<GameLoop>();
            gameLoop.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            gameLoop.playerView = player.transform;
            gameLoop.gameplayCamera = cam;
            follow.loop = gameLoop;

            GameObject enemyArt = LoadArt("RS_Enemy", "Enemy");
            bool enemyArtOk = enemyArt != null && HasMotion(enemyArt);
            if (enemyArt != null && !enemyArtOk)
                Debug.LogWarning("[Waffle] A-03 敌人预制体没有 MotionRoot，样板继续用占位几何。");
            var views = loopGo.AddComponent<CombatViews>();
            views.loop = gameLoop;
            views.enemyVisualPrefab = enemyArtOk ? enemyArt : enemyPrefab;
            views.projectileMaterial = LoadOr<Material>("Assets/Waffle/Art/Materials/M_FX_Muzzle_Cyan.mat", line);
            views.gemMaterial = shard;

            var audio = loopGo.AddComponent<AudioDirector>();
            audio.loop = gameLoop;
            audio.combatLoop = Clip(MusicDir + "bgm_combat_master_64s_seamless.wav");
            audio.levelUpLoop = Clip(MusicDir + "bgm_levelup_suspended_loop.wav");
            audio.shoot.clips = Clips("sfx_player_shoot_01", "sfx_player_shoot_02", "sfx_player_shoot_03");
            audio.enemyHit.clips = Clips("sfx_enemy_hit_01", "sfx_enemy_hit_02", "sfx_enemy_hit_03");
            audio.enemyDeath.clips = Clips("sfx_enemy_death_01", "sfx_enemy_death_02", "sfx_enemy_death_03");
            audio.playerHurt.clips = Clips("sfx_player_hurt_01", "sfx_player_hurt_02");
            audio.dodge.clips = Clips("sfx_player_dodge_01", "sfx_player_dodge_02");
            audio.gemPickup.clips = Clips("sfx_gem_pickup_01", "sfx_gem_pickup_02", "sfx_gem_pickup_03", "sfx_gem_pickup_04", "sfx_gem_pickup_05");
            audio.levelUpAppear.clips = Clips("sfx_ui_levelup_appear");
            audio.levelUpConfirm.clips = Clips("sfx_ui_levelup_confirm");
            audio.victory.clips = Clips("sfx_game_victory");
            audio.defeat.clips = Clips("sfx_game_defeat");

            var vfx = loopGo.AddComponent<VfxDirector>();
            vfx.loop = gameLoop;
            vfx.playerVisual = player.transform;
            vfx.muzzleMaterial = LoadOr<Material>("Assets/Waffle/Art/Materials/M_FX_Muzzle_Cyan.mat", line);
            vfx.fragmentMaterial = LoadOr<Material>("Assets/Waffle/Art/Materials/M_FX_Fragment.mat", shard);
            vfx.afterimageMaterial = LoadOr<Material>("Assets/Waffle/Art/Materials/M_FX_Afterimage.mat", line);

            var motion = loopGo.AddComponent<S2MotionDriver>();
            motion.loop = gameLoop;
            motion.profile = profile;
            var fx = loopGo.AddComponent<S2FxPlayer>();
            fx.loop = gameLoop;
            fx.motion = motion;
            fx.profile = profile;
            fx.shardMaster = shardPrefab;
            fx.burstMaster = burst;
            fx.lineMaterial = line;
            var hud = loopGo.AddComponent<S2ChoiceHud>();
            hud.loop = gameLoop;
            var console = loopGo.AddComponent<DebugConsole>();
            console.loop = gameLoop;
            console.visible = false;

            if (Object.FindAnyObjectByType<RunHud>() != null)
                throw new System.InvalidOperationException("S2 scene still has RunHud");
            if (player.transform.localScale != Vector3.one)
                throw new System.InvalidOperationException("Player root scale is not 1");
            if (!HasMotion(player))
                throw new System.InvalidOperationException("Player has no MotionRoot");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("S2_STYLELAB_SETUP_PASSED placeholder=" + (!playerArtOk) + " enemyPlaceholder=" + (!enemyArtOk) + " groundArt=" + (groundArt != null));
        }

        static GameObject Sculpture(string prefabName, Material body, Material metal, Material glow, bool enemy)
        {
            var root = new GameObject(prefabName);
            root.transform.localScale = Vector3.one;
            var motion = new GameObject("MotionRoot").transform;
            motion.SetParent(root.transform, false);
            if (!enemy)
            {
                Part("Core", motion, new Vector3(0.06f, 0.72f, 0f), new Vector3(0.46f, 1.32f, 0.34f), body);
                Part("Segment_A", motion, new Vector3(0.1f, 1.66f, 0.08f), new Vector3(0.58f, 0.28f, 0.18f), metal);
                Part("Segment_B", motion, new Vector3(-0.16f, 0.42f, -0.06f), new Vector3(0.18f, 0.62f, 0.18f), body);
                Transform core = motion.Find("Core");
                Part("Slit", core, new Vector3(0.08f, 0.15f, 0.52f), new Vector3(0.12f, 0.55f, 0.08f), glow);
            }
            else
            {
                Part("Core", motion, new Vector3(0.05f, 0.52f, 0f), new Vector3(0.4f, 0.92f, 0.3f), body);
                Part("Segment_A", motion, new Vector3(0f, 1.28f, 0.06f), new Vector3(0.5f, 0.24f, 0.16f), metal);
                Part("Segment_B", motion, new Vector3(-0.12f, 0.3f, -0.04f), new Vector3(0.16f, 0.4f, 0.16f), body);
                Transform core = motion.Find("Core");
                Part("Slit", core, new Vector3(0.06f, 0.1f, 0.55f), new Vector3(0.18f, 0.42f, 0.08f), glow);
            }
            StripColliders(root);
            string path = PrefabDir + "/" + prefabName + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject GroundModule(Material ground, Material metal)
        {
            var root = new GameObject("RS_GroundModule");
            root.transform.localScale = Vector3.one;
            Part("Slab", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(4f, 0.08f, 4f), ground);
            Part("Seam", root.transform, new Vector3(0.35f, 0.1f, 0f), new Vector3(0.08f, 0.04f, 3.2f), metal);
            StripColliders(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/RS_GroundModule.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject ShardPrefab(Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "FX_Shard";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.08f, 0.08f, 0.16f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/FX_Shard.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static ParticleSystem BurstPrefab(Material material)
        {
            var go = new GameObject("FX_EliteBurst");
            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.4f;
            main.startLifetime = 0.32f;
            main.startSpeed = 2.2f;
            main.startSize = 0.07f;
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(1f, 0.27f, 0f, 1f);
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/FX_EliteBurst.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ParticleSystem>();
        }

        static GameObject Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return go;
        }

        static Mesh FloorMesh()
        {
            const string path = MeshDir + "/ArenaFloor.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool fresh = mesh == null;
            if (fresh) mesh = new Mesh { name = "ArenaFloor" };
            mesh.vertices = new[]
            {
                new Vector3(-30f, 0f, -30f), new Vector3(30f, 0f, -30f),
                new Vector3(-30f, 0f, 30f), new Vector3(30f, 0f, 30f)
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (fresh) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static S2FxProfile Profile()
        {
            const string path = Root + "/S2FxProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<S2FxProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<S2FxProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            return profile;
        }

        static Material Lit(string name, Color color, float metal, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return SaveMat(name, shader, color, metal, smoothness, emission);
        }

        static Material ParticleUnlit(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            return SaveMat(name, shader, color, 0f, 0f, Color.black);
        }

        static Material Unlit(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            return SaveMat(name, shader, color, 0f, 0f, Color.black);
        }

        static Material SaveMat(string name, Shader shader, Color color, float metal, float smoothness, Color emission)
        {
            string path = MatDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null) mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metal);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0.001f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject LoadArt(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                string[] paths =
                {
                    "Assets/Art/S2/" + names[i] + ".prefab",
                    "Assets/Art/S2/Prefabs/" + names[i] + ".prefab"
                };
                for (int p = 0; p < paths.Length; p++)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[p]);
                    if (prefab != null) return prefab;
                }
            }
            return null;
        }

        static bool HasMotion(GameObject go)
        {
            Transform[] all = go.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == "MotionRoot") return true;
            return false;
        }

        static void StripColliders(GameObject go)
        {
            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) Object.DestroyImmediate(colliders[i]);
        }

        static T LoadOr<T>(string path, T fallback) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : fallback;
        }

        static AudioClip Clip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogWarning("[Waffle] 缺少音频（S-03 未入 main 时用现有文件）" + path);
            return clip;
        }

        static AudioClip[] Clips(params string[] names)
        {
            var list = new AudioClip[names.Length];
            for (int i = 0; i < names.Length; i++) list[i] = Clip(SfxDir + names[i] + ".wav");
            return list;
        }
    }
}
