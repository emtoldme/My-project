using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Waffle;

namespace Reverb.Drafts.RC01.Pipeline.EditorTools
{
    public static class RC02ContentPipeline
    {
        public const string CatalogPath = "Assets/Game/Content/RC02ContentCatalog.asset";
        public const string ReportPath = "Logs/Reverb-ContentImport.txt";
        public const string HarvestPath = "Assets/Game/Scenes/Main.unity";

        [Serializable] public class Package { public int schemaVersion; public string packageId; public Entry[] entries; }
        [Serializable] public class Entry
        {
            public string id, kind, contentId, eventType, profileId, pausePolicy, status, source;
            public Asset[] assets;
            public Param[] parameters;
            public int maxInstances;
            public float minInterval;
        }
        [Serializable] public class Asset { public string role, path; }
        [Serializable] public class Param { public string key; public float value; }

        [MenuItem("Reverb/刷新视听目录")]
        public static void RefreshCatalog()
        {
            var errors = new List<string>();
            var catalog = AssetDatabase.LoadAssetAtPath<RC02ContentCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RC02ContentCatalog>();
            }
            var previous = new Dictionary<string, RC02CatalogEntry>();
            foreach (var entry in catalog.entries)
                if (entry != null && !string.IsNullOrEmpty(entry.id)) previous[entry.id] = entry;

            var merged = new List<RC02CatalogEntry>();
            var seenIds = new HashSet<string>();
            var seenKeys = new HashSet<string>();
            var packageNames = new List<string>();
            foreach (string path in PackagePaths())
            {
                if (!File.Exists(path))
                {
                    errors.Add("缺少清单 " + path);
                    continue;
                }
                Package package;
                try { package = JsonUtility.FromJson<Package>(File.ReadAllText(path)); }
                catch (Exception ex) { errors.Add(path + " JSON: " + ex.Message); continue; }
                if (package == null || package.schemaVersion != 1)
                {
                    errors.Add(path + " schemaVersion 必须是 1");
                    continue;
                }
                packageNames.Add(package.packageId);
                if (package.entries == null) continue;
                foreach (Entry raw in package.entries)
                    Accept(path, raw, previous, merged, seenIds, seenKeys, errors);
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, ReportPreview(merged, errors));
            if (errors.Count > 0)
                throw new InvalidOperationException("RC02_CATALOG_REFRESH_FAILED " + string.Join(" | ", errors));
            if (!AssetDatabase.Contains(catalog)) AssetDatabase.CreateAsset(catalog, CatalogPath);
            catalog.schemaVersion = 1;
            catalog.packages = string.Join(", ", packageNames);
            catalog.entries = merged;
            EditorUtility.SetDirty(catalog);
            File.WriteAllText(ReportPath, Report(catalog, errors));
            AssetDatabase.SaveAssets();
            Debug.Log("RC02_CATALOG_REFRESH_PASSED entries=" + merged.Count + " packages=" + catalog.packages);
        }

        static void Accept(string file, Entry raw, Dictionary<string, RC02CatalogEntry> previous, List<RC02CatalogEntry> merged, HashSet<string> seenIds, HashSet<string> seenKeys, List<string> errors)
        {
            if (raw == null || string.IsNullOrEmpty(raw.id))
            {
                errors.Add(file + " 有一条缺少 id");
                return;
            }
            if (!seenIds.Add(raw.id))
            {
                errors.Add(raw.id + " 重复");
                return;
            }
            var notes = new List<string>();
            if (raw.kind != "art" && raw.kind != "audio" && raw.kind != "music")
            {
                errors.Add(raw.id + " kind 非法");
                return;
            }
            if (!ContentIdOk(raw.contentId))
            {
                errors.Add(raw.id + " contentId 非法");
                return;
            }
            if (raw.kind == "music")
            {
                if (!string.IsNullOrEmpty(raw.eventType) || string.IsNullOrEmpty(raw.profileId))
                {
                    errors.Add(raw.id + " 音乐必须空 eventType 且有 profileId");
                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(raw.profileId) || !Enum.TryParse(raw.eventType, out PresentationEventType parsed) || !Enum.IsDefined(typeof(PresentationEventType), parsed))
                {
                    errors.Add(raw.id + " eventType 不是现有枚举");
                    return;
                }
            }
            string key = raw.kind + "|" + raw.eventType + "|" + raw.contentId + "|" + raw.profileId;
            if (!seenKeys.Add(key))
            {
                errors.Add(raw.id + " 与另一条匹配键重复");
                return;
            }
            if (!Enum.TryParse(raw.pausePolicy, true, out RC02PausePolicy policy))
            {
                errors.Add(raw.id + " pausePolicy 只能是 freeze 或 stop");
                return;
            }
            if (string.IsNullOrEmpty(raw.status) || string.IsNullOrEmpty(raw.source))
            {
                errors.Add(raw.id + " 缺少 status 或 source");
                return;
            }
            int maxInstances = raw.maxInstances > 0 ? raw.maxInstances : raw.kind == "art" ? 8 : raw.kind == "audio" ? 4 : 1;
            if (raw.maxInstances < 0 || raw.maxInstances > 128 || raw.minInterval < 0f || float.IsNaN(raw.minInterval) || float.IsInfinity(raw.minInterval))
            {
                errors.Add(raw.id + " minInterval 不能为负");
                return;
            }
            var parameters = new List<RC02Parameter>();
            var parameterKeys = new HashSet<string>();
            if (raw.parameters != null)
            {
                foreach (Param param in raw.parameters)
                {
                    if (param == null || string.IsNullOrEmpty(param.key)) continue;
                    if (!parameterKeys.Add(param.key) || float.IsNaN(param.value) || float.IsInfinity(param.value))
                    { errors.Add(raw.id + " 重复或非有限参数 " + param.key); return; }
                    if (!Allowed(raw.kind, param.key)) notes.Add("未知参数 " + param.key);
                    else if ((param.key == "bpm" || param.key == "beatsPerBar") && param.value > 0f)
                        notes.Add(param.key + " 已记录但不启用拍点对齐");
                    parameters.Add(new RC02Parameter { key = param.key, value = param.value });
                }
            }
            previous.TryGetValue(raw.id, out RC02CatalogEntry old);
            var assets = new List<RC02AssetRef>();
            if (raw.assets != null)
            {
                foreach (Asset item in raw.assets)
                {
                    if (item == null || string.IsNullOrEmpty(item.path)) continue;
                    if (!item.path.StartsWith("Assets/", StringComparison.Ordinal) || item.path.Contains(".."))
                    { errors.Add(raw.id + " 资源路径必须在Assets内 " + item.path); return; }
                    var bound = new RC02AssetRef { role = item.role, assetPath = item.path };
                    var loaded = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.path);
                    if (loaded == null)
                    {
                        bound.asset = OldAsset(old, item.role, item.path);
                        notes.Add(bound.asset == null ? "缺资源 " + item.path : "缺资源，沿用旧引用 " + item.path);
                    }
                    else if (!RoleOk(item.role, loaded))
                    {
                        errors.Add(raw.id + " 资源类型不符 " + item.path);
                        return;
                    }
                    else bound.asset = loaded;
                    assets.Add(bound);
                }
            }
            merged.Add(new RC02CatalogEntry
            {
                id = raw.id, kind = raw.kind, contentId = raw.contentId, eventType = raw.eventType ?? "",
                profileId = raw.profileId ?? "", assets = assets.ToArray(), parameters = parameters.ToArray(),
                maxInstances = maxInstances, minInterval = raw.minInterval, pausePolicy = policy,
                status = raw.status, source = raw.source, issues = string.Join("; ", notes)
            });
        }

        static IEnumerable<string> PackagePaths()
        {
            yield return "Assets/Game/Content/Manifests/art-manifest.json";
            yield return "Assets/Game/Content/Manifests/audio-manifest.json";
            yield return "Assets/Game/Content/Manifests/music-manifest.json";
        }

        static bool ContentIdOk(string id)
        {
            if (id == "*") return true;
            if (id == "P12_P13") return true;
            if (string.IsNullOrEmpty(id) || id.Length != 3 || !char.IsDigit(id[1]) || !char.IsDigit(id[2])) return false;
            int n = (id[1] - '0') * 10 + (id[2] - '0');
            if (id[0] == 'W' || id[0] == 'E' || id[0] == 'D') return n >= 1 && n <= 6;
            return id[0] == 'P' && n >= 1 && n <= 18;
        }

        static bool Allowed(string kind, string key)
        {
            if (kind == "art") return key == "width" || key == "scale" || key == "tailSeconds" || key == "intensity";
            if (kind == "audio") return key == "gainDb" || key == "pitchMin" || key == "pitchMax" || key == "spatialBlend"
                || key == "minDistance" || key == "maxDistance" || key == "priority";
            return key == "musicGainDb" || key == "fadeSeconds" || key == "bpm" || key == "beatsPerBar";
        }

        static bool RoleOk(string role, UnityEngine.Object asset)
        {
            if (role == "prefab") return asset is GameObject;
            if (role == "material") return asset is Material;
            if (role == "icon") return asset is Texture;
            return (role == "clip" || role == "combatLoop" || role == "levelUpLoop" || role == "stem") && asset is AudioClip;
        }

        static UnityEngine.Object OldAsset(RC02CatalogEntry old, string role, string path)
        {
            if (old == null || old.assets == null) return null;
            for (int i = 0; i < old.assets.Length; i++)
                if (old.assets[i] != null && old.assets[i].role == role)
                    return old.assets[i].asset;
            return null;
        }

        static string Report(RC02ContentCatalog catalog, List<string> errors)
        {
            var text = new StringBuilder();
            text.AppendLine("RC02 catalog refresh");
            text.AppendLine("errors: " + errors.Count);
            foreach (string error in errors) text.AppendLine("ERROR " + error);
            foreach (RC02CatalogEntry entry in catalog.entries)
            {
                text.AppendLine(entry.kind + " " + entry.contentId + " " + entry.eventType + " " + entry.id
                    + " instances=" + entry.maxInstances + " pause=" + entry.pausePolicy
                    + (string.IsNullOrEmpty(entry.issues) ? "" : " notes=" + entry.issues));
                if (entry.kind != "art")
                    text.AppendLine("  Consumed by the existing AudioDirector through RC02AudioConfiguration.");
            }
            return text.ToString();
        }

        static string ReportPreview(List<RC02CatalogEntry> entries, List<string> errors)
        {
            var text = new StringBuilder();
            text.AppendLine("RC02 validation: entries=" + entries.Count + " errors=" + errors.Count);
            foreach (var error in errors) text.AppendLine(error);
            return text.ToString();
        }
    }

    [CustomEditor(typeof(RC02ContentCatalog))]
    public class RC02CatalogInspector : Editor
    {
        int index;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var catalog = (RC02ContentCatalog)target;
            index = EditorGUILayout.IntField("预览序号", index);
            if (!GUILayout.Button("预览这一条并停掉上一条")) return;
            if (catalog.entries == null || catalog.entries.Count == 0) return;
            string scene = SceneManager.GetActiveScene().path;
            if (scene == RC02ContentPipeline.HarvestPath)
            {
                Debug.LogError("请在空场景里预览；主场景只用于实际游玩。");
                return;
            }
            index = Mathf.Clamp(index, 0, catalog.entries.Count - 1);
            var presenter = UnityEngine.Object.FindAnyObjectByType<RC02CatalogPresenter>();
            if (presenter == null)
            {
                var host = new GameObject("RC02_PreviewHost") { hideFlags = HideFlags.DontSave };
                presenter = host.AddComponent<RC02CatalogPresenter>();
            }
            presenter.catalog = catalog;
            presenter.Preview(catalog.entries[index]);
        }
    }
}
