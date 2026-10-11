using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Waffle.EditorTools
{
    /// <summary>
    /// 菜单 Waffle/Setup S1 Presentation：在 S1_Graybox 上装配表现层（P-01）。
    /// 依据 Docs/Art/01（视觉规范）、Docs/Art/02（素材交接表）、Docs/Audio（事件表、导入建议）。
    /// 可以重复运行：材质和 Prefab 会被更新，场景里已有的表现节点会先删除再重建。
    /// 只改表现：不改 GameLoop 的玩法参数。
    /// </summary>
    public static class S1PresentationSetup
    {
        const string ArtRoot = "Assets/Waffle/Art";
        const string MatDir = ArtRoot + "/Materials";
        const string PrefabDir = "Assets/Waffle/Prefabs/Visual";
        const string MechDir = "Assets/Art_ThirdParty/Quaternius_Mech/";
        const string SfxDir = "Assets/Audio/SFX/";
        const string MusicDir = "Assets/Audio/Music/";

        [MenuItem("Waffle/Setup S1 Presentation")]
        public static void Setup()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(S1SceneSetup.ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Waffle", "还没有 S1_Graybox.unity，请先运行 Waffle/Setup S1 Scene。", "好");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(S1SceneSetup.ScenePath, OpenSceneMode.Single);

            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(PrefabDir);

            // ---------- 材质 M-01 ~ M-06（Docs/Art/02 第二节） ----------
            var m01 = LitMaterial("M_Precast_Concrete", Hex(0x383D46), 0.08f, 0.25f, null);
            var m02 = LitMaterial("M_Player_HeavyMech", Color.white, 0.28f, 0.48f, Tex(MechDir + "George_Texture.png"));
            var m03 = LitMaterial("M_Enemy_Vanguard", Color.white, 0.22f, 0.38f, Tex(MechDir + "Mike_Texture.png"));
            var m04 = LitMaterial("M_Enemy_Striker", Color.white, 0.25f, 0.40f, Tex(MechDir + "Stan_Texture.png"));
            var m05 = LitMaterial("M_Enemy_Flanker", Color.white, 0.20f, 0.35f, Tex(MechDir + "Leela_Texture.png"));
            // 交接表写的 T_Trim_01 贴图不在素材包里，先用“冷轧深色结构钢”纯色
            var trim = Tex("Assets/Art_ThirdParty/Quaternius_SciFiKit/Textures/T_Trim_01_BaseColor.png");
            var m06 = LitMaterial("M_SciFi_Architecture", trim != null ? Color.white : Hex(0x242830), 0.32f, 0.42f, trim);

            // ---------- 特效材质 ----------
            var fxMuzzle = UnlitMaterial("M_FX_Muzzle_Cyan", new Color(0f, 0.9f * 1.5f, 1.5f, 1f), false);
            var fxAfter = UnlitMaterial("M_FX_Afterimage", new Color(0f, 0.9f, 1f, 0.5f), true);
            var fxFrag = LitMaterial("M_FX_Fragment", Hex(0x1C1E22), 0.22f, 0.38f, null);
            fxFrag.SetColor("_EmissionColor", new Color(1.2f, 0.27f * 1.2f, 0f, 1f));
            EditorUtility.SetDirty(fxFrag);

            // ---------- 视觉 Prefab（只有模型和材质，没有脚本和碰撞） ----------
            var playerPrefab = VisualPrefab("Player_George_Visual", MechDir + "George.fbx", m02);
            VisualPrefab("Enemy_Vanguard_Mike_Visual", MechDir + "Mike.fbx", m03);
            VisualPrefab("Enemy_Striker_Stan_Visual", MechDir + "Stan.fbx", m04);
            VisualPrefab("Enemy_Flanker_Leela_Visual", MechDir + "Leela.fbx", m05);

            // ---------- 场地 ----------
            var arena = GameObject.Find("Arena_60x60");
            if (arena != null) arena.GetComponent<MeshRenderer>().sharedMaterial = m01;

            // ---------- 玩家：胶囊只保留为逻辑位置，外观换成 George ----------
            var player = GameObject.Find("Player");
            if (player != null)
            {
                var capsule = player.GetComponent<MeshRenderer>();
                if (capsule != null) capsule.enabled = false;
                var old = player.transform.Find("Visual");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                if (playerPrefab != null)
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, player.transform);
                    visual.name = "Visual";
                    // 胶囊中心 y = 1，模型轴心在脚底
                    visual.transform.localPosition = new Vector3(0f, -player.transform.position.y, 0f);
                    visual.transform.localRotation = Quaternion.identity;
                }
            }

            // ---------- 相机：正交 Size 9，Pitch 55°，Yaw 45°（Docs/Art/01 §四，用户已确认） ----------
            var camGo = GameObject.Find("Main Camera");
            if (camGo != null)
            {
                var cam = camGo.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Hex(0x242833);
                var follow = camGo.GetComponent<CameraFollow>();
                if (follow == null) follow = camGo.AddComponent<CameraFollow>();
                if (player != null) follow.target = player.transform;
                follow.orthographic = true;
                follow.orthographicSize = 9f;
                follow.pitch = 55f;
                follow.yaw = 45f;
                follow.Apply();
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }

            // ---------- 灯光 ----------
            var key = GameObject.Find("Directional Light");
            if (key == null) key = GameObject.Find("Key Light");
            if (key == null) { key = new GameObject("Key Light"); key.AddComponent<Light>().type = LightType.Directional; }
            key.name = "Key Light";
            var keyLight = key.GetComponent<Light>();
            keyLight.color = new Color(1f, 0.96f, 0.90f);
            keyLight.intensity = 1.45f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.85f;
            key.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            var fill = GameObject.Find("Fill Light");
            if (fill == null) { fill = new GameObject("Fill Light"); fill.AddComponent<Light>().type = LightType.Directional; }
            var fillLight = fill.GetComponent<Light>();
            fillLight.color = new Color(0.45f, 0.55f, 0.70f);
            fillLight.intensity = 0.45f;
            fillLight.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(-30f, 145f, 0f);

            // ---------- 后处理：Bloom 1.15 / 0.25，Vignette 0.2（≤ 0.25），不开色差 ----------
            string profilePath = ArtRoot + "/S1_PostFX.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            // Repair profiles saved by earlier setup versions with transient components.
            profile.components.RemoveAll(component => component == null);
            Bloom bloom;
            if (!profile.TryGet(out bloom)) bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(1.15f);
            bloom.intensity.Override(0.25f);
            Vignette vignette;
            if (!profile.TryGet(out vignette)) vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.Override(0.2f);
            foreach (var component in profile.components)
            {
                // VolumeProfile.Add only creates the object; it must also become a saved subasset.
                if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
                EditorUtility.SetDirty(component);
            }
            EditorUtility.SetDirty(profile);
            var volGo = GameObject.Find("PostFX Volume");
            if (volGo == null) volGo = new GameObject("PostFX Volume");
            var volume = volGo.GetComponent<Volume>();
            if (volume == null) volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            // ---------- 音频与特效入口（挂在 GameLoop 上） ----------
            var loopGo = GameObject.Find("GameLoop");
            if (loopGo != null)
            {
                var loop = loopGo.GetComponent<GameLoop>();
                var audio = loopGo.GetComponent<AudioDirector>();
                if (audio == null) audio = loopGo.AddComponent<AudioDirector>();
                audio.loop = loop;
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
                EditorUtility.SetDirty(audio);

                var vfx = loopGo.GetComponent<VfxDirector>();
                if (vfx == null) vfx = loopGo.AddComponent<VfxDirector>();
                vfx.loop = loop;
                vfx.playerVisual = player != null ? player.transform : null;
                vfx.muzzleMaterial = fxMuzzle;
                vfx.fragmentMaterial = fxFrag;
                vfx.afterimageMaterial = fxAfter;
                EditorUtility.SetDirty(vfx);
            }

            ApplyAudioImportSettings();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Waffle] S1 表现层已装配：材质、视觉 Prefab、相机、灯光、后处理、AudioDirector、VfxDirector");
        }

        // =====================================================================

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        static Texture2D Tex(string path) { return AssetDatabase.LoadAssetAtPath<Texture2D>(path); }
        static AudioClip Clip(string path) { return AssetDatabase.LoadAssetAtPath<AudioClip>(path); }

        static AudioClip[] Clips(params string[] names)
        {
            var list = new AudioClip[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                list[i] = Clip(SfxDir + names[i] + ".wav");
                if (list[i] == null) Debug.LogError("[Waffle] 缺少音效 " + names[i]);
            }
            return list;
        }

        static Material LoadOrCreate(string name, Shader shader)
        {
            string path = MatDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            return mat;
        }

        /// <summary>
        /// URP Lit。打开 _EMISSION 但默认自发光为黑：素材没有自发光遮罩贴图，整块发光会违反规范；
        /// 这样受击闪白（MaterialPropertyBlock 写 _EmissionColor）才能生效。
        /// </summary>
        static Material LitMaterial(string name, Color baseColor, float metallic, float smoothness, Texture2D baseMap)
        {
            var mat = LoadOrCreate(name, Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (baseMap != null) mat.SetTexture("_BaseMap", baseMap);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material UnlitMaterial(string name, Color color, bool transparent)
        {
            var mat = LoadOrCreate(name, Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", color);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject VisualPrefab(string name, string fbxPath, Material mat)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null) { Debug.LogError("[Waffle] 找不到模型 " + fbxPath); return null; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            inst.name = name;
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = mat;
            }
            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, PrefabDir + "/" + name + ".prefab");
            Object.DestroyImmediate(inst);
            return prefab;
        }

        /// <summary>按 Docs/Audio/S1_Audio_Acceptance_Log 的导入建议设置（会修改对应 .meta，需要提交）。</summary>
        static void ApplyAudioImportSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                var s = importer.defaultSampleSettings;
                if (path.StartsWith(MusicDir))
                {
                    s.loadType = AudioClipLoadType.CompressedInMemory;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.7f;
                }
                else
                {
                    s.loadType = AudioClipLoadType.DecompressOnLoad;
                    s.compressionFormat = AudioCompressionFormat.PCM;
                }
                importer.defaultSampleSettings = s;
                importer.SaveAndReimport();
            }
        }
    }
}
