using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Waffle;
using Reverb.Drafts.RC01.Presentation;

namespace Reverb.Drafts.RC01.Pipeline.EditorTools
{
    public static class ArtWorkspaceTools
    {
        public const string Main = "Assets/Game/Scenes/Main.unity";
        [MenuItem("Reverb/打开主场景")]
        public static void OpenMain()
        {
            if(Application.isPlaying) return;
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { EditorSceneManager.OpenScene(Main); Debug.Log("ART_WORKSPACE_MAIN_OPEN"); }
        }

        [MenuItem("Reverb/检查资源引用")]
        public static void Validate()
        {
            var issues=new List<string>();
            foreach(string path in AssetDatabase.GetAllAssetPaths())
            {
                if(!path.StartsWith("Assets/",StringComparison.Ordinal) || !File.Exists(path)) continue;
                if(!new[]{".unity",".prefab",".mat",".asset"}.Contains(Path.GetExtension(path))) continue;
                // Binary TerrainData is checked by Unity's imported dependency graph; YAML by GUID.
                using(var reader=new StreamReader(path)) if(reader.ReadLine()?.StartsWith("%YAML")!=true) continue;
                foreach(Match match in Regex.Matches(File.ReadAllText(path),@"guid: ([0-9a-fA-F]{32})"))
                {
                    string guid=match.Groups[1].Value;
                    if(guid.StartsWith("0000000000000000",StringComparison.Ordinal)) continue;
                    if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid))) issues.Add(path+" missing GUID "+guid);
                }
            }
            var catalog=AssetDatabase.LoadAssetAtPath<RC02ContentCatalog>(RC02ContentPipeline.CatalogPath);
            if(catalog==null || catalog.entries.Count!=33) issues.Add("Expected 33 content bindings");
            if(catalog!=null) foreach(var entry in catalog.entries) foreach(var asset in entry.assets)
                if(asset.asset==null || AssetDatabase.GetAssetPath(asset.asset)!=asset.assetPath) issues.Add("Invalid binding "+entry.id+" "+asset.assetPath);
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path==Main)
            {
                var loops=UnityEngine.Object.FindObjectsByType<GameLoop>();
                if(loops.Length!=1) issues.Add("Main must have one GameLoop");
                if(loops.Length==1)
                {
                    var loop=loops[0];
                    if(loop.playerView==null||loop.gameplayCamera==null) issues.Add("Player/camera binding missing");
                    if(loop.GetComponent<CombatViews>()?.enemyVisualPrefab==null) issues.Add("Enemy prefab missing");
                    if(loop.GetComponent<RC01BattleView>()==null||loop.GetComponent<RC02CatalogPresenter>()==null||loop.GetComponent<AudioDirector>()==null) issues.Add("Presentation binding missing");
                    if(loop.config.runContentAllowList.Length!=27) issues.Add("Playable allowlist changed");
                }
            }
            string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Reviews/ArtWorkspace-Cleanup-2026-10-13/references-after.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            File.WriteAllLines(report,issues.Count==0?new[]{"passed=true","catalogEntries=33","playableContent=6/14/3/4","missingAssetReferences=0"}:issues);
            if(issues.Count>0) throw new Exception(string.Join("\n",issues));
            Debug.Log("ART_WORKSPACE_VALIDATED missing=0 catalog=33 playable=6/14/3/4");
        }

        [MenuItem("Reverb/构建当前游戏")]
        public static void Build()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/ArtWorkspace/Reverb.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{Main}, locationPathName=output,
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-summary.json"),
                JsonUtility.ToJson(new BuildSummary {result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings},true));
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed");
            Debug.Log("ART_WORKSPACE_BUILD_PASSED errors="+report.summary.totalErrors);
        }
        [Serializable] class BuildSummary { public string result; public int errors,warnings; }
    }

    public class ArtWorkspaceWindow : EditorWindow
    {
        [MenuItem("Reverb/美术工作台")]
        static void Open() { GetWindow<ArtWorkspaceWindow>("美术工作台"); }
        void OnGUI()
        {
            GUILayout.Label("《麦田里的混响》美术编辑",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("先打开主场景。在停止运行时修改并保存；运行时生成的敌人和特效不会保存到工程。",MessageType.Info);
            if(GUILayout.Button("打开主场景")) ArtWorkspaceTools.OpenMain();
            if(GUILayout.Button("选择玩家外观"))
            {
                var loop=UnityEngine.Object.FindAnyObjectByType<GameLoop>();
                if(loop?.playerView!=null) Selection.activeTransform=loop.playerView.Find("Visual") ?? loop.playerView;
            }
            if(GUILayout.Button("选择战斗外观绑定（敌人、子弹、掉落）"))
            {
                var loop=UnityEngine.Object.FindAnyObjectByType<GameLoop>();
                if(loop!=null) { Selection.activeGameObject=loop.gameObject; EditorGUIUtility.PingObject(loop); }
            }
            Folder("角色 Prefab","Assets/Game/Art/Actors");
            Folder("场景环境与地形","Assets/Game/Environments");
            Folder("场景材质","Assets/Game/Art/Materials");
            Folder("技能特效与图标","Assets/Game/Art/Effects");
            Folder("原始地面、植被和武器","Assets/ReverbInTheRye");
            Folder("声音与音乐","Assets/Game/Audio");
            Folder("视听映射清单","Assets/Game/Content/Manifests");
            if(GUILayout.Button("打开美术修改说明"))
                EditorUtility.RevealInFinder(Path.GetFullPath("Docs/ART_EDITING.md"));
            EditorGUILayout.HelpBox("替换同一 Prefab 或材质可直接预览。改了清单里的路径或预算，再点 Reverb → 刷新视听目录。该菜单不会重建你的模型、地形或灯光。",MessageType.None);
        }
        static void Folder(string label,string path)
        {
            if(!GUILayout.Button(label)) return;
            EditorUtility.FocusProjectWindow(); Selection.activeObject=AssetDatabase.LoadMainAssetAtPath(path);
        }
    }
}
