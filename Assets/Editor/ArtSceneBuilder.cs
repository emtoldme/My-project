using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ArtVerification
{
    public static class ArtSceneBuilder
    {
        [MenuItem("Art/Build Industrial Mech Test Scene")]
        public static void BuildScene()
        {
            var scenePath = "Assets/Art_TestScene_IndustrialMech.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Clean existing objects
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots)
            {
                UnityEngine.Object.DestroyImmediate(r);
            }

            // 1. Shaders & Material Setup
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            // 2. Lighting Setup (Key Sunlight + Cool Fill + Directional Soft Shadows)
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.90f); // Warm sun (5500K)
            light.intensity = 1.45f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            var lightData = lightGo.AddComponent<UniversalAdditionalLightData>();
            lightData.lightCookieSize = Vector2.one;

            // Fill Light (ambient bounce from industrial concrete ground)
            var fillLightGo = new GameObject("Fill Light (Ambient)");
            var fillLight = fillLightGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.45f, 0.55f, 0.70f); // Cool blue-grey ambient bounce
            fillLight.intensity = 0.45f;
            fillLight.shadows = LightShadows.None;
            fillLightGo.transform.rotation = Quaternion.Euler(-30f, 145f, 0f);

            // 3. Camera Setup (55 degree oblique orthographic, Yaw 45 deg, looking at Origin)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9.0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.14f, 0.16f, 0.20f); // Dark industrial slate fog
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 150f;
            camGo.transform.position = new Vector3(-8.91f, 18.0f, -8.91f);
            camGo.transform.rotation = Quaternion.Euler(55f, 45f, 0f);
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            // 4. Ground Foundation (Precast Concrete Slab with Tactical Industrial Trim)
            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Industrial_Concrete_Floor";
            groundGo.transform.position = Vector3.zero;
            groundGo.transform.localScale = new Vector3(4.5f, 1f, 4.5f); // 45m x 45m

            var floorMat = new Material(urpShader);
            floorMat.name = "M_Precast_Concrete";
            // Precast concrete tone (#383D46 -> r:0.22, g:0.24, b:0.27)
            floorMat.SetColor("_BaseColor", new Color(0.25f, 0.28f, 0.33f));
            floorMat.SetFloat("_Smoothness", 0.25f);
            floorMat.SetFloat("_Metallic", 0.08f);
            groundGo.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // 5. Shared Trim Material for Sci-Fi Architecture & Props
            var trimTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art_ThirdParty/Quaternius_SciFiKit/Textures/T_Trim_01_BaseColor.png");
            var scifiArchMat = new Material(urpShader);
            scifiArchMat.name = "M_SciFi_Architecture";
            if (trimTex != null) scifiArchMat.SetTexture("_BaseMap", trimTex);
            scifiArchMat.SetFloat("_Smoothness", 0.40f);
            scifiArchMat.SetFloat("_Metallic", 0.30f);

            // 6. Perimeter Brutalist Structure (Door Arch, Heavy Columns & Perimeter Walls)
            var columnFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art_ThirdParty/Quaternius_SciFiKit/FBX/Column_Astra.fbx");
            var wallFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art_ThirdParty/Quaternius_SciFiKit/FBX/WallAstra_Straight.fbx");
            var doorArchFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art_ThirdParty/Quaternius_SciFiKit/FBX/Door_Frame_SquareTall.fbx");
            var crate3Fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art_ThirdParty/Quaternius_SciFiKit/FBX/Prop_Crate3.fbx");
            var crate4Fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art_ThirdParty/Quaternius_SciFiKit/FBX/Prop_Crate4.fbx");
            var barrelFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art_ThirdParty/Quaternius_SciFiKit/FBX/Prop_Barrel_Large.fbx");

            if (doorArchFbx != null)
            {
                var arch = (GameObject)PrefabUtility.InstantiatePrefab(doorArchFbx);
                arch.name = "Industrial_Blast_Gate";
                arch.transform.position = new Vector3(0f, 0f, 9.5f);
                arch.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                arch.transform.localScale = Vector3.one * 1.8f;
                foreach (var r in arch.GetComponentsInChildren<Renderer>()) r.sharedMaterial = scifiArchMat;
            }

            if (wallFbx != null)
            {
                // Flanking rear walls
                float[] wallPositions = new float[] { -7.6f, -3.8f, 3.8f, 7.6f };
                for (int i = 0; i < wallPositions.Length; i++)
                {
                    var w = (GameObject)PrefabUtility.InstantiatePrefab(wallFbx);
                    w.name = $"RearWall_Segment_{i}";
                    w.transform.position = new Vector3(wallPositions[i], 0f, 9.5f);
                    w.transform.localScale = Vector3.one * 1.8f;
                    foreach (var r in w.GetComponentsInChildren<Renderer>()) r.sharedMaterial = scifiArchMat;
                }
            }

            if (columnFbx != null)
            {
                // Left Column Tower
                var colL = (GameObject)PrefabUtility.InstantiatePrefab(columnFbx);
                colL.name = "Heavy_Pillar_West";
                colL.transform.position = new Vector3(-8.5f, 0f, 3.0f);
                colL.transform.localScale = Vector3.one * 2.2f;
                foreach (var r in colL.GetComponentsInChildren<Renderer>()) r.sharedMaterial = scifiArchMat;

                // Right Column Tower
                var colR = (GameObject)PrefabUtility.InstantiatePrefab(columnFbx);
                colR.name = "Heavy_Pillar_East";
                colR.transform.position = new Vector3(8.5f, 0f, 3.0f);
                colR.transform.localScale = Vector3.one * 2.2f;
                foreach (var r in colR.GetComponentsInChildren<Renderer>()) r.sharedMaterial = scifiArchMat;
            }

            // Tactical Prop Clusters (Cover / Staging Zone)
            if (crate3Fbx != null)
            {
                var c1 = (GameObject)PrefabUtility.InstantiatePrefab(crate3Fbx);
                c1.name = "Supply_Crate_Cluster_1";
                c1.transform.position = new Vector3(-4.8f, 0f, 1.2f);
                c1.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
                c1.transform.localScale = Vector3.one * 1.4f;
                foreach (var r in c1.GetComponentsInChildren<Renderer>()) r.sharedMaterial = scifiArchMat;
            }

            if (crate4Fbx != null)
            {
                var c2 = (GameObject)PrefabUtility.InstantiatePrefab(crate4Fbx);
                c2.name = "Heavy_Munitions_Crate_2";
                c2.transform.position = new Vector3(-4.2f, 0f, 2.5f);
                c2.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
                c2.transform.localScale = Vector3.one * 1.5f;
                foreach (var r in c2.GetComponentsInChildren<Renderer>()) r.sharedMaterial = scifiArchMat;
            }

            if (barrelFbx != null)
            {
                var b1 = (GameObject)PrefabUtility.InstantiatePrefab(barrelFbx);
                b1.name = "Hazard_Fuel_Barrel_1";
                b1.transform.position = new Vector3(5.5f, 0f, -1.0f);
                b1.transform.localScale = Vector3.one * 1.3f;
                foreach (var r in b1.GetComponentsInChildren<Renderer>()) r.sharedMaterial = scifiArchMat;

                var b2 = (GameObject)PrefabUtility.InstantiatePrefab(barrelFbx);
                b2.name = "Hazard_Fuel_Barrel_2";
                b2.transform.position = new Vector3(6.2f, 0f, -0.2f);
                b2.transform.localScale = Vector3.one * 1.3f;
                foreach (var r in b2.GetComponentsInChildren<Renderer>()) r.sharedMaterial = scifiArchMat;
            }

            // 7. Units Setup: Player Heavy Mech & 3 Distinct Enemy Drones
            var georgeFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art_ThirdParty/Quaternius_Mech/George.fbx");
            var mikeFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art_ThirdParty/Quaternius_Mech/Mike.fbx");
            var stanFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art_ThirdParty/Quaternius_Mech/Stan.fbx");
            var leelaFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art_ThirdParty/Quaternius_Mech/Leela.fbx");

            // Player Heavy Bipedal Walker (George) - Tactical Gray/White Armor with Cyan Optics
            if (georgeFbx != null)
            {
                var player = (GameObject)PrefabUtility.InstantiatePrefab(georgeFbx);
                player.name = "Player_HeavyMech_George";
                player.transform.position = new Vector3(-0.8f, 0f, -0.5f);
                player.transform.rotation = Quaternion.Euler(0f, 40f, 0f); // Facing enemy encounter zone
                player.transform.localScale = Vector3.one * 1.25f;

                var georgeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art_ThirdParty/Quaternius_Mech/George_Texture.png");
                if (georgeTex != null)
                {
                    var playerMat = new Material(urpShader);
                    playerMat.name = "M_Player_HeavyMech";
                    playerMat.SetTexture("_BaseMap", georgeTex);
                    playerMat.SetFloat("_Smoothness", 0.48f);
                    playerMat.SetFloat("_Metallic", 0.30f);
                    foreach (var r in player.GetComponentsInChildren<Renderer>()) r.sharedMaterial = playerMat;
                }
            }

            // Enemy Unit 1: Mike (Heavy Assault Vanguard Drone)
            if (mikeFbx != null)
            {
                var enemy1 = (GameObject)PrefabUtility.InstantiatePrefab(mikeFbx);
                enemy1.name = "Enemy_Vanguard_Mike";
                enemy1.transform.position = new Vector3(2.8f, 0f, 2.2f);
                enemy1.transform.rotation = Quaternion.Euler(0f, -135f, 0f); // Facing player
                enemy1.transform.localScale = Vector3.one * 1.05f;

                var mikeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art_ThirdParty/Quaternius_Mech/Mike_Texture.png");
                if (mikeTex != null)
                {
                    var enemyMat1 = new Material(urpShader);
                    enemyMat1.name = "M_Enemy_Vanguard";
                    enemyMat1.SetTexture("_BaseMap", mikeTex);
                    enemyMat1.SetFloat("_Smoothness", 0.38f);
                    enemyMat1.SetFloat("_Metallic", 0.22f);
                    foreach (var r in enemy1.GetComponentsInChildren<Renderer>()) r.sharedMaterial = enemyMat1;
                }
            }

            // Enemy Unit 2: Stan (Recon Striker Drone)
            if (stanFbx != null)
            {
                var enemy2 = (GameObject)PrefabUtility.InstantiatePrefab(stanFbx);
                enemy2.name = "Enemy_Striker_Stan";
                enemy2.transform.position = new Vector3(4.8f, 0f, 4.5f);
                enemy2.transform.rotation = Quaternion.Euler(0f, -150f, 0f);
                enemy2.transform.localScale = Vector3.one * 0.98f;

                var stanTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art_ThirdParty/Quaternius_Mech/Stan_Texture.png");
                if (stanTex != null)
                {
                    var enemyMat2 = new Material(urpShader);
                    enemyMat2.name = "M_Enemy_Striker";
                    enemyMat2.SetTexture("_BaseMap", stanTex);
                    enemyMat2.SetFloat("_Smoothness", 0.40f);
                    enemyMat2.SetFloat("_Metallic", 0.25f);
                    foreach (var r in enemy2.GetComponentsInChildren<Renderer>()) r.sharedMaterial = enemyMat2;
                }
            }

            // Enemy Unit 3: Leela (Flanker Swarm Drone)
            if (leelaFbx != null)
            {
                var enemy3 = (GameObject)PrefabUtility.InstantiatePrefab(leelaFbx);
                enemy3.name = "Enemy_Flanker_Leela";
                enemy3.transform.position = new Vector3(-2.8f, 0f, 4.0f);
                enemy3.transform.rotation = Quaternion.Euler(0f, 130f, 0f);
                enemy3.transform.localScale = Vector3.one * 0.92f;

                var leelaTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art_ThirdParty/Quaternius_Mech/Leela_Texture.png");
                if (leelaTex != null)
                {
                    var enemyMat3 = new Material(urpShader);
                    enemyMat3.name = "M_Enemy_Flanker";
                    enemyMat3.SetTexture("_BaseMap", leelaTex);
                    enemyMat3.SetFloat("_Smoothness", 0.35f);
                    enemyMat3.SetFloat("_Metallic", 0.20f);
                    foreach (var r in enemy3.GetComponentsInChildren<Renderer>()) r.sharedMaterial = enemyMat3;
                }
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log("Successfully constructed Industrial Cyber-Brutalism Art Scene!");
        }
    }
}
