using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Waffle;

namespace Reverb.Narrative.Presentation
{
    /// <summary>
    /// 磁带物证回放与整理界面（局外汽车旅馆 / 局内结算档案）。
    /// 纯表现层，展示收集到的录音带与历史调查笔记。
    /// </summary>
    public class CassetteArchiveViewer : MonoBehaviour
    {
        [SerializeField] private NarrativeDirector director;
        public bool isVisible = false;
        private Vector2 scrollPos;
        private CassetteArtifact selectedArtifact;

        private void Awake()
        {
            if (director == null)
            {
                director = GetComponent<NarrativeDirector>() ?? FindAnyObjectByType<NarrativeDirector>();
            }
        }

        public void ToggleVisibility() => isVisible = !isVisible;

        private void OnGUI()
        {
            if (!isVisible || director == null || director.Database == null) return;

            ImguiScale.Apply();
            Rect modalRect = ImguiScale.Fit(new Rect((ImguiScale.Width - 800) / 2, (ImguiScale.Height - 500) / 2, 800, 500));
            GUI.Box(modalRect, "");

            GUILayout.BeginArea(modalRect);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("【卡式录音带存根 · 1968-1988】", GUI.skin.box);
            if (GUILayout.Button("关闭 [ESC/Tab]", GUILayout.Width(120))) isVisible = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            // 左侧磁带列表
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(260));
            GUILayout.Label("已截获录音：");
            foreach (var artifact in director.Database.artifacts)
            {
                bool unlocked = director.UnlockedTapes.Contains(artifact.tapeId);
                string title = unlocked ? $"▶ {artifact.title}" : "[未破译的噪波频段]";
                
                GUI.enabled = unlocked;
                if (GUILayout.Button(title, GUILayout.Height(32)))
                {
                    selectedArtifact = artifact;
                }
                GUI.enabled = true;
            }
            GUILayout.EndVertical();

            // 右侧磁带转录文本与感悟
            GUILayout.BeginVertical(GUI.skin.box);
            if (selectedArtifact != null)
            {
                GUILayout.Label($"标题: {selectedArtifact.title}");
                GUILayout.Label($"记录时间: {selectedArtifact.recordedDate}  |  地点: {selectedArtifact.location}");
                GUILayout.Space(6);
                GUILayout.Label("--- 录音转录 (Transcript) ---");
                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(180));
                GUILayout.Label(selectedArtifact.transcript);
                GUILayout.EndScrollView();
                GUILayout.Space(6);
                GUILayout.Label("--- 阿瑟的调查备忘 (Arthur's Notes) ---");
                GUILayout.Label(selectedArtifact.narrativeNote);
            }
            else
            {
                GUILayout.Label("在左侧选择一盒录音带以回放历史存根。");
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
