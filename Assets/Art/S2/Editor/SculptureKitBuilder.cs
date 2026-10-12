using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Art.S2.Editor
{
    /// <summary>
    /// 一键构建并持久化 S2 雕塑样板素材包（模型 Mesh、材质 Material、预制体 Prefab）
    /// 遵循契约：1米=1单位，+Z朝前，脚底中心轴心(0,0,0)，根缩放(1,1,1)
    /// 包含三族材质母版：哑光瓷白、石墨冷灰、半哑光金属，以及局部发光槽位
    /// </summary>
    public static class SculptureKitBuilder
    {
        private const string ModelsDir = "Assets/Art/S2/Models";
        private const string MaterialsDir = "Assets/Art/S2/Materials";
        private const string PrefabsDir = "Assets/Art/S2/Prefabs";

        [MenuItem("Art/S2/Build Sculpture StyleKit (Models, Materials, Prefabs)")]
        public static void BuildAll()
        {
            EnsureDirectories();

            // 1. 构建与保存材质母版
            var matCeramicWhite = CreateOrUpdateMaterial(
                $"{MaterialsDir}/M_S2_CeramicWhite.mat",
                new Color(0.88f, 0.90f, 0.92f, 1.0f), // 瓷白主体
                metallic: 0.05f,
                smoothness: 0.28f, // 细腻哑光
                emissionColor: Color.black
            );

            var matGraphiteDark = CreateOrUpdateMaterial(
                $"{MaterialsDir}/M_S2_GraphiteDark.mat",
                new Color(0.12f, 0.13f, 0.15f, 1.0f), // 石墨冷灰主体
                metallic: 0.15f,
                smoothness: 0.32f, // 哑光暗调
                emissionColor: Color.black
            );

            var matSatinMetal = CreateOrUpdateMaterial(
                $"{MaterialsDir}/M_S2_SatinMetal.mat",
                new Color(0.38f, 0.40f, 0.44f, 1.0f), // 结构中灰金属
                metallic: 0.65f, // 半哑光金属
                smoothness: 0.45f,
                emissionColor: Color.black
            );

            var matFloorConcrete = CreateOrUpdateMaterial(
                $"{MaterialsDir}/M_S2_FloorSlab.mat",
                new Color(0.18f, 0.20f, 0.24f, 1.0f), // 深冷灰基底
                metallic: 0.08f,
                smoothness: 0.22f, // 绝对哑光，防反光油腻
                emissionColor: Color.black
            );

            var matPlayerCoreCyan = CreateOrUpdateMaterial(
                $"{MaterialsDir}/M_S2_Core_Cyan.mat",
                new Color(0.0f, 0.85f, 1.0f, 1.0f), // 玩家信息青蓝
                metallic: 0.0f,
                smoothness: 0.50f,
                emissionColor: new Color(0.0f, 1.8f, 2.2f, 1.0f) // HDR 局部发光
            );

            var matEnemyAlertOrange = CreateOrUpdateMaterial(
                $"{MaterialsDir}/M_S2_Alert_Orange.mat",
                new Color(1.0f, 0.28f, 0.0f, 1.0f), // 敌方危险朱橙
                metallic: 0.0f,
                smoothness: 0.50f,
                emissionColor: new Color(2.5f, 0.65f, 0.0f, 1.0f) // HDR 危险发光
            );

            // 2. 生成并持久化几何体 Mesh
            // 玩家部件 Mesh
            var meshPlayerCore = CreateBeveledBoxMesh("Mesh_Player_Core", 0.55f, 0.85f, 0.65f, 0.08f, 0.05f); // 倒角偏心主体
            var meshPlayerUpper = CreateBeveledBoxMesh("Mesh_Player_UpperCowl", 0.70f, 0.45f, 0.80f, 0.08f, -0.06f); // 上部悬浮罩体
            var meshPlayerWingL = CreateWedgeMesh("Mesh_Player_Fin_L", 0.22f, 0.65f, 0.45f, isLeft: true); // 左翼偏心导流片
            var meshPlayerWingR = CreateWedgeMesh("Mesh_Player_Fin_R", 0.18f, 0.55f, 0.40f, isLeft: false); // 右翼非对称分节
            var meshPlayerVisor = CreateBeveledBoxMesh("Mesh_Player_SensorVisor", 0.25f, 0.08f, 0.18f, 0.02f, 0.0f); // 核心光学视窗

            meshPlayerCore = SaveMeshAsset(meshPlayerCore, $"{ModelsDir}/Mesh_Player_Core.asset");
            meshPlayerUpper = SaveMeshAsset(meshPlayerUpper, $"{ModelsDir}/Mesh_Player_UpperCowl.asset");
            meshPlayerWingL = SaveMeshAsset(meshPlayerWingL, $"{ModelsDir}/Mesh_Player_Fin_L.asset");
            meshPlayerWingR = SaveMeshAsset(meshPlayerWingR, $"{ModelsDir}/Mesh_Player_Fin_R.asset");
            meshPlayerVisor = SaveMeshAsset(meshPlayerVisor, $"{ModelsDir}/Mesh_Player_SensorVisor.asset");

            // 敌人部件 Mesh
            var meshEnemyTorso = CreateHexagonalPrismMesh("Mesh_Enemy_Torso", 0.48f, 0.80f, 0.06f); // 钝角六棱体躯干
            var meshEnemyBladeL = CreateWedgeMesh("Mesh_Enemy_Blade_L", 0.18f, 0.50f, 0.35f, isLeft: true); // 左突进切刃
            var meshEnemyBladeR = CreateWedgeMesh("Mesh_Enemy_Blade_R", 0.18f, 0.50f, 0.35f, isLeft: false); // 右突进切刃
            var meshEnemyCore = CreateBeveledBoxMesh("Mesh_Enemy_AlertCore", 0.20f, 0.15f, 0.22f, 0.02f, 0.0f); // 警戒核心

            meshEnemyTorso = SaveMeshAsset(meshEnemyTorso, $"{ModelsDir}/Mesh_Enemy_Torso.asset");
            meshEnemyBladeL = SaveMeshAsset(meshEnemyBladeL, $"{ModelsDir}/Mesh_Enemy_Blade_L.asset");
            meshEnemyBladeR = SaveMeshAsset(meshEnemyBladeR, $"{ModelsDir}/Mesh_Enemy_Blade_R.asset");
            meshEnemyCore = SaveMeshAsset(meshEnemyCore, $"{ModelsDir}/Mesh_Enemy_AlertCore.asset");

            // 地面模块 Mesh (6m x 6m 倒角预制板与带缝组合)
            var meshFloorTile = CreateFloorTileMesh("Mesh_Floor_Tile_6x6", 6.0f, 0.25f, 0.025f);
            meshFloorTile = SaveMeshAsset(meshFloorTile, $"{ModelsDir}/Mesh_Floor_Tile_6x6.asset");

            // 3. 构建并保存标准化预制体 (Prefabs)
            BuildPlayerPrefab(meshPlayerCore, meshPlayerUpper, meshPlayerWingL, meshPlayerWingR, meshPlayerVisor,
                              matCeramicWhite, matGraphiteDark, matSatinMetal, matPlayerCoreCyan);

            BuildEnemyPrefab(meshEnemyTorso, meshEnemyBladeL, meshEnemyBladeR, meshEnemyCore,
                             matGraphiteDark, matSatinMetal, matEnemyAlertOrange);

            BuildFloorPrefab(meshFloorTile, matFloorConcrete);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SculptureKitBuilder] 成功构建并持久化 S2 雕塑样板素材包 (Models, Materials, Prefabs)！");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(ModelsDir)) Directory.CreateDirectory(ModelsDir);
            if (!Directory.Exists(MaterialsDir)) Directory.CreateDirectory(MaterialsDir);
            if (!Directory.Exists(PrefabsDir)) Directory.CreateDirectory(PrefabsDir);
        }

        private static Material CreateOrUpdateMaterial(string path, Color baseColor, float metallic, float smoothness, Color emissionColor)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            if (emissionColor.maxColorComponent > 0.01f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh SaveMeshAsset(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>
        /// 构建符合契约的 RS_Player Prefab (高度约 1.8m，脚底中心(0,0,0)，带 MotionRoot 与分节节点)
        /// </summary>
        private static void BuildPlayerPrefab(Mesh meshCore, Mesh meshUpper, Mesh meshFinL, Mesh meshFinR, Mesh meshVisor,
                                              Material matWhite, Material matDark, Material matMetal, Material matCyan)
        {
            var root = new GameObject("RS_Player");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var motionRoot = new GameObject("MotionRoot");
            motionRoot.transform.SetParent(root.transform, false);

            // 1. Core 主体 (高 0.85m，中心在 Y=0.75m)
            var goCore = new GameObject("Core");
            goCore.transform.SetParent(motionRoot.transform, false);
            goCore.transform.localPosition = new Vector3(0.0f, 0.75f, 0.02f);
            var mfCore = goCore.AddComponent<MeshFilter>();
            mfCore.sharedMesh = meshCore;
            var mrCore = goCore.AddComponent<MeshRenderer>();
            mrCore.sharedMaterial = matWhite;

            // 2. Segment_Upper 悬浮上罩 (Y=1.35m，总高达到约 1.8m)
            var goUpper = new GameObject("Segment_Upper");
            goUpper.transform.SetParent(motionRoot.transform, false);
            goUpper.transform.localPosition = new Vector3(0.0f, 1.35f, -0.04f);
            var mfUpper = goUpper.AddComponent<MeshFilter>();
            mfUpper.sharedMesh = meshUpper;
            var mrUpper = goUpper.AddComponent<MeshRenderer>();
            mrUpper.sharedMaterial = matWhite;

            // 3. Segment_FinL 左悬浮导流片 (非对称偏心，X=-0.38m, Y=0.85m)
            var goFinL = new GameObject("Segment_FinL");
            goFinL.transform.SetParent(motionRoot.transform, false);
            goFinL.transform.localPosition = new Vector3(-0.38f, 0.85f, -0.05f);
            goFinL.transform.localRotation = Quaternion.Euler(0f, 8f, -4f);
            var mfFinL = goFinL.AddComponent<MeshFilter>();
            mfFinL.sharedMesh = meshFinL;
            var mrFinL = goFinL.AddComponent<MeshRenderer>();
            mrFinL.sharedMaterial = matMetal;

            // 4. Segment_FinR 右非对称悬浮分节 (X=0.34m, Y=0.78m, 体量略小)
            var goFinR = new GameObject("Segment_FinR");
            goFinR.transform.SetParent(motionRoot.transform, false);
            goFinR.transform.localPosition = new Vector3(0.34f, 0.78f, -0.02f);
            goFinR.transform.localRotation = Quaternion.Euler(0f, -6f, 3f);
            var mfFinR = goFinR.AddComponent<MeshFilter>();
            mfFinR.sharedMesh = meshFinR;
            var mrFinR = goFinR.AddComponent<MeshRenderer>();
            mrFinR.sharedMaterial = matMetal;

            // 5. SensorVisor 光学指示视窗 (Y=1.28m, Z=0.32m 前方，青蓝高频发光)
            var goVisor = new GameObject("SensorVisor");
            goVisor.transform.SetParent(motionRoot.transform, false);
            goVisor.transform.localPosition = new Vector3(0.0f, 1.28f, 0.34f);
            var mfVisor = goVisor.AddComponent<MeshFilter>();
            mfVisor.sharedMesh = meshVisor;
            var mrVisor = goVisor.AddComponent<MeshRenderer>();
            mrVisor.sharedMaterial = matCyan;

            // 6. Muzzle_Socket 武器发射参考挂点 (前方 +Z 0.45m, Y=0.90m)
            var muzzle = new GameObject("Muzzle_Socket");
            muzzle.transform.SetParent(motionRoot.transform, false);
            muzzle.transform.localPosition = new Vector3(0.15f, 0.90f, 0.45f);

            NormalizeVisual(root, 1.8f);
            var path = $"{PrefabsDir}/RS_Player.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        /// <summary>
        /// 构建符合契约的 RS_Enemy Prefab (高度约 1.35m，脚底中心(0,0,0)，石墨暗灰主体+朱橙警戒核心)
        /// </summary>
        private static void BuildEnemyPrefab(Mesh meshTorso, Mesh meshBladeL, Mesh meshBladeR, Mesh meshCore,
                                             Material matDark, Material matMetal, Material matOrange)
        {
            var root = new GameObject("RS_Enemy");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var motionRoot = new GameObject("MotionRoot");
            motionRoot.transform.SetParent(root.transform, false);

            // 1. Torso 躯干 (高 0.80m, Y=0.60m)
            var goTorso = new GameObject("Torso");
            goTorso.transform.SetParent(motionRoot.transform, false);
            goTorso.transform.localPosition = new Vector3(0.0f, 0.60f, 0.0f);
            var mfTorso = goTorso.AddComponent<MeshFilter>();
            mfTorso.sharedMesh = meshTorso;
            var mrTorso = goTorso.AddComponent<MeshRenderer>();
            mrTorso.sharedMaterial = matDark;

            // 2. Segment_BladeL 左前刺刃 (X=-0.28m, Y=0.55m, Z=0.15m)
            var goBladeL = new GameObject("Segment_BladeL");
            goBladeL.transform.SetParent(motionRoot.transform, false);
            goBladeL.transform.localPosition = new Vector3(-0.28f, 0.55f, 0.15f);
            goBladeL.transform.localRotation = Quaternion.Euler(15f, 0f, -10f);
            var mfBladeL = goBladeL.AddComponent<MeshFilter>();
            mfBladeL.sharedMesh = meshBladeL;
            var mrBladeL = goBladeL.AddComponent<MeshRenderer>();
            mrBladeL.sharedMaterial = matMetal;

            // 3. Segment_BladeR 右前刺刃 (X=0.28m, Y=0.55m, Z=0.15m)
            var goBladeR = new GameObject("Segment_BladeR");
            goBladeR.transform.SetParent(motionRoot.transform, false);
            goBladeR.transform.localPosition = new Vector3(0.28f, 0.55f, 0.15f);
            goBladeR.transform.localRotation = Quaternion.Euler(15f, 0f, 10f);
            var mfBladeR = goBladeR.AddComponent<MeshFilter>();
            mfBladeR.sharedMesh = meshBladeR;
            var mrBladeR = goBladeR.AddComponent<MeshRenderer>();
            mrBladeR.sharedMaterial = matMetal;

            // 4. AlertCore 危险警戒核心 (Y=0.85m, Z=0.22m, 朱橙 HDR 发光)
            var goCore = new GameObject("AlertCore");
            goCore.transform.SetParent(motionRoot.transform, false);
            goCore.transform.localPosition = new Vector3(0.0f, 0.85f, 0.22f);
            var mfCore = goCore.AddComponent<MeshFilter>();
            mfCore.sharedMesh = meshCore;
            var mrCore = goCore.AddComponent<MeshRenderer>();
            mrCore.sharedMaterial = matOrange;

            NormalizeVisual(root, 1.4f);
            var path = $"{PrefabsDir}/RS_Enemy.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        /// <summary>
        /// 构建 6m x 6m 几何预制地面模块 Prefab
        /// </summary>
        private static void BuildFloorPrefab(Mesh meshFloorTile, Material matFloor)
        {
            var root = new GameObject("RS_Floor_Tile_6x6");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var mf = root.AddComponent<MeshFilter>();
            mf.sharedMesh = meshFloorTile;
            var mr = root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = matFloor;

            var path = $"{PrefabsDir}/RS_Floor_Tile_6x6.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        // ================= 几何生成算法：带倒角与偏心特征 =================

        /// <summary>
        /// 生成具有结构倒角与偏心切角特征的盒状体网格
        /// </summary>
        private static Mesh CreateBeveledBoxMesh(string name, float width, float height, float length, float bevel, float asymmetryZ)
        {
            // Six inset faces, twelve chamfer strips and eight corner triangles.
            // The delivered six disconnected quads left holes where bevels belong.
            var half = new Vector3(width, height, length + asymmetryZ) * 0.5f;
            var center = new Vector3(0f, 0f, asymmetryZ * 0.5f);
            float b = Mathf.Min(bevel, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.8f);
            var faces = new System.Collections.Generic.List<Vector3[]>();
            for (int axis = 0; axis < 3; axis++)
            {
                int u = (axis + 1) % 3, v = (axis + 2) % 3;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    var f = new Vector3[4];
                    int[] su = { -1, 1, 1, -1 }, sv = { -1, -1, 1, 1 };
                    for (int i = 0; i < 4; i++)
                    {
                        f[i][axis] = sign * half[axis];
                        f[i][u] = su[i] * (half[u] - b);
                        f[i][v] = sv[i] * (half[v] - b);
                        f[i] += center;
                    }
                    faces.Add(f);
                }
            }
            for (int a = 0; a < 3; a++) for (int d = a + 1; d < 3; d++)
            {
                int c = 3 - a - d;
                for (int sa = -1; sa <= 1; sa += 2) for (int sd = -1; sd <= 1; sd += 2)
                {
                    var f = new Vector3[4];
                    for (int i = 0; i < 4; i++)
                    {
                        bool first = i == 0 || i == 3;
                        f[i][a] = sa * (half[a] - (first ? 0f : b));
                        f[i][d] = sd * (half[d] - (first ? b : 0f));
                        f[i][c] = (i < 2 ? -1 : 1) * (half[c] - b);
                        f[i] += center;
                    }
                    faces.Add(f);
                }
            }
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                var f = new Vector3[3];
                var signs = new Vector3(x, y, z);
                for (int i = 0; i < 3; i++)
                {
                    f[i] = Vector3.Scale(signs, half - Vector3.one * b);
                    f[i][i] = signs[i] * half[i];
                    f[i] += center;
                }
                faces.Add(f);
            }
            return FlatSolid(name, faces, center);
        }

        private static Mesh FlatSolid(string name, System.Collections.Generic.List<Vector3[]> faces, Vector3 center)
        {
            var vertices = new System.Collections.Generic.List<Vector3>();
            var indices = new System.Collections.Generic.List<int>();
            var uv = new System.Collections.Generic.List<Vector2>();
            foreach (var face in faces)
            {
                Vector3 middle = Vector3.zero;
                foreach (var point in face) middle += point;
                middle /= face.Length;
                if (Vector3.Dot(Vector3.Cross(face[1] - face[0], face[2] - face[0]), middle - center) < 0f)
                    Array.Reverse(face);
                int start = vertices.Count;
                for (int i = 0; i < face.Length; i++)
                {
                    vertices.Add(face[i]);
                    uv.Add(i == 0 ? Vector2.zero : i == 1 ? Vector2.right : i == 2 ? Vector2.one : Vector2.up);
                }
                for (int i = 1; i < face.Length - 1; i++)
                { indices.Add(start); indices.Add(start + i); indices.Add(start + i + 1); }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.SetUVs(0, uv);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        private static void NormalizeVisual(GameObject root, float height)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = height / bounds.size.y;
            Vector3 offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            var motion = root.transform.Find("MotionRoot");
            foreach (Transform part in motion)
            {
                part.localPosition = (part.localPosition + offset) * scale;
                part.localScale *= scale;
            }
        }

        /// <summary>
        /// 生成尖角/楔形导流分节几何网格 (Wedge / Fin)
        /// </summary>
        private static Mesh CreateWedgeMesh(string name, float width, float height, float length, bool isLeft)
        {
            var mesh = new Mesh { name = name };
            float hx = width * 0.5f;
            float hy = height * 0.5f;
            float hz = length * 0.5f;
            float tipOffset = isLeft ? -0.05f : 0.05f;

            Vector3[] v = new Vector3[]
            {
                // 底面四边形
                new Vector3(-hx, -hy, -hz),
                new Vector3( hx, -hy, -hz),
                new Vector3( hx + tipOffset, -hy,  hz),
                new Vector3(-hx + tipOffset, -hy,  hz),
                // 顶脊线两点 (形成倾斜雕塑切面)
                new Vector3(-hx * 0.3f, hy, -hz * 0.6f),
                new Vector3( hx * 0.3f, hy, -hz * 0.6f)
            };

            int[] t = new int[]
            {
                // 底面
                0, 2, 1, 0, 3, 2,
                // 后立面
                0, 1, 5, 0, 5, 4,
                // 左斜切面
                0, 4, 3,
                // 右斜切面
                1, 2, 5,
                // 前倾斜主切面
                3, 4, 5, 3, 5, 2
            };

            var faces = new System.Collections.Generic.List<Vector3[]> {
                new[] { v[0], v[1], v[2], v[3] }, new[] { v[0], v[4], v[5], v[1] },
                new[] { v[0], v[3], v[4] }, new[] { v[1], v[5], v[2] },
                new[] { v[3], v[2], v[5], v[4] }
            };
            UnityEngine.Object.DestroyImmediate(mesh);
            return FlatSolid(name, faces, new Vector3(0f, -height / 6f, -length * 0.1f));
        }

        /// <summary>
        /// 生成六棱柱钝角躯干网格 (用于敌人，形成紧凑厚重的冲击体块)
        /// </summary>
        private static Mesh CreateHexagonalPrismMesh(string name, float radius, float height, float taper)
        {
            var mesh = new Mesh { name = name };
            int segments = 6;
            float hy = height * 0.5f;

            var vList = new System.Collections.Generic.List<Vector3>();
            var tList = new System.Collections.Generic.List<int>();

            // 顶圆周与底圆周点
            Vector3 centerTop = new Vector3(0, hy, 0);
            Vector3 centerBottom = new Vector3(0, -hy, 0);

            Vector3[] topRing = new Vector3[segments];
            Vector3[] bottomRing = new Vector3[segments];

            for (int i = 0; i < segments; i++)
            {
                float angle = i * (Mathf.PI * 2f / segments);
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                topRing[i] = new Vector3(cos * (radius - taper), hy, sin * (radius - taper));
                bottomRing[i] = new Vector3(cos * radius, -hy, sin * radius);
            }

            // 侧面四边形
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int baseIndex = vList.Count;

                vList.Add(bottomRing[i]);
                vList.Add(bottomRing[next]);
                vList.Add(topRing[next]);
                vList.Add(topRing[i]);

                tList.Add(baseIndex + 0);
                tList.Add(baseIndex + 2);
                tList.Add(baseIndex + 1);

                tList.Add(baseIndex + 0);
                tList.Add(baseIndex + 3);
                tList.Add(baseIndex + 2);
            }

            // 顶盖
            int topCenterIndex = vList.Count;
            vList.Add(centerTop);
            for (int i = 0; i < segments; i++) vList.Add(topRing[i]);
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tList.Add(topCenterIndex);
                tList.Add(topCenterIndex + 1 + i);
                tList.Add(topCenterIndex + 1 + next);
            }

            // 底盖
            int bottomCenterIndex = vList.Count;
            vList.Add(centerBottom);
            for (int i = 0; i < segments; i++) vList.Add(bottomRing[i]);
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tList.Add(bottomCenterIndex);
                tList.Add(bottomCenterIndex + 1 + next);
                tList.Add(bottomCenterIndex + 1 + i);
            }

            var faces = new System.Collections.Generic.List<Vector3[]>();
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                faces.Add(new[] { bottomRing[i], bottomRing[next], topRing[next], topRing[i] });
            }
            faces.Add(topRing); faces.Add(bottomRing);
            UnityEngine.Object.DestroyImmediate(mesh);
            return FlatSolid(name, faces, Vector3.zero);
        }

        /// <summary>
        /// 生成 6m x 6m 倒角地板块，边缘微下凹，拼接时产生自然冷阴影接缝
        /// </summary>
        private static Mesh CreateFloorTileMesh(string name, float size, float thickness, float bevel)
        {
            var mesh = CreateBeveledBoxMesh(name, size, thickness, size, bevel, 0f);
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i].y -= thickness * 0.5f;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
