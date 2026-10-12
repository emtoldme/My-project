using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Waffle.S2.EditorTools
{
    public static class S2IntegrationBatch
    {
        public static void Setup()
        {
            Art.S2.Editor.SculptureKitBuilder.BuildAll();
            S2StyleLabSetup.Setup();
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == S2StyleLabSetup.ScenePath))
                scenes.Add(new EditorBuildSettingsScene(S2StyleLabSetup.ScenePath, true));
            else foreach (var scene in scenes) if (scene.path == S2StyleLabSetup.ScenePath) scene.enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
            Validate();
            Debug.Log("S2_INTEGRATION_SETUP_PASSED");
        }

        public static void SetupAndCapture()
        {
            Setup();
            var loop = UnityEngine.Object.FindAnyObjectByType<GameLoop>();
            var cam = loop.gameplayCamera;
            var target = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                target.Create();
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(cam, request)) throw new Exception("Render request unavailable");
                RenderPipeline.SubmitRenderRequest(cam, request);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Docs/Verification/S2-StyleLab");
                File.WriteAllBytes("Docs/Verification/S2-StyleLab/scene-preview.png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
            }
            Debug.Log("S2_PREVIEW_CAPTURE_PASSED");
        }

        public static void Validate()
        {
            foreach (string name in new[] { "RS_Player", "RS_Enemy" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/S2/Prefabs/" + name + ".prefab");
                if (prefab == null || prefab.transform.localScale != Vector3.one) throw new Exception("Invalid " + name);
                var clone = UnityEngine.Object.Instantiate(prefab);
                var renderers = clone.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null) throw new Exception("Missing material");
                }
                if (Mathf.Abs(bounds.min.y) > 0.001f || Mathf.Abs(bounds.size.y - (name == "RS_Player" ? 1.8f : 1.4f)) > 0.001f)
                    throw new Exception("Invalid foot pivot or height: " + name + " " + bounds);
                foreach (var mf in clone.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = mf.sharedMesh;
                    if (mesh == null || !AssetDatabase.Contains(mesh) || mesh.vertexCount == 0 || mesh.uv.Length != mesh.vertexCount)
                        throw new Exception("Non-persistent or invalid mesh: " + mf.name);
                    var v = mesh.vertices; var t = mesh.triangles;
                    for (int i = 0; i < t.Length; i += 3)
                    {
                        Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                        if (Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f - mesh.bounds.center) <= 0f)
                            throw new Exception("Inward or degenerate face: " + mesh.name);
                    }
                }
                UnityEngine.Object.DestroyImmediate(clone);
            }
            Debug.Log("S2_ASSET_REFERENCE_AND_GEOMETRY_PASSED");
        }

        public static void Build()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/S2-StyleLab/Waffle.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { S2StyleLabSetup.ScenePath }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("S2 build failed: " + report.summary.result);
            Debug.Log("S2_BUILD_PASSED warnings=" + report.summary.totalWarnings + " errors=" + report.summary.totalErrors + " path=" + output);
        }
    }
}
