using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Reverb.UI;

namespace Reverb.UI.Editor
{
    /// <summary>
    /// 幂等独立预览装配工具。由 Codex 在 Unity 中执行。
    /// 只创建 Assets/UI/ 下的预览场景与 Prefab，不改其他系统。
    /// </summary>
    public static class UIPreviewSetup
    {
        private const string PrefabPath = "Assets/UI/Prefabs/ReverbHUD_Preview.prefab";
        private const string ScenePath = "Assets/UI/Scenes/UI_Preview.unity";

        [MenuItem("Reverb/UI/Create Preview (Idempotent)")]
        public static void CreatePreview()
        {
            EnsureFolders();
            var prefab = CreateOrUpdatePrefab();
            CreateOrUpdateScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Reverb.UI] Preview prefab & scene ready. Test input is ON only in this preview.");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI/Prefabs"))
                AssetDatabase.CreateFolder("Assets/UI", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/UI/Scenes"))
                AssetDatabase.CreateFolder("Assets/UI", "Scenes");
        }

        private static GameObject CreateOrUpdatePrefab()
        {
            var root = new GameObject("ReverbHUD_Preview");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.AddComponent<GraphicRaycaster>();

            var hudGo = new GameObject("HUD");
            hudGo.transform.SetParent(root.transform, false);
            var hud = hudGo.AddComponent<ReverbHUD>();
            hud.enableTestInput = true; // 仅预览开启

            // Health panel
            var healthPanel = CreatePanel(hudGo.transform, "HealthPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(320, 80));
            var frame = CreateImage(healthPanel.transform, "Frame", new Color(0.32f, 0.24f, 0.18f, 0.95f));
            var fill = CreateImage(healthPanel.transform, "Fill", new Color(0.85f, 0.25f, 0.45f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            var healthText = CreateText(healthPanel.transform, "HealthText", "100 / 100");

            hud.healthFrame = frame;
            hud.healthFill = fill;
            hud.healthText = healthText;

            // Mode panel
            var modePanel = CreatePanel(hudGo.transform, "ModePanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(220, 60));
            var glow = CreateImage(modePanel.transform, "Glow", new Color(0.55f, 0.5f, 0.45f, 0.8f));
            var modeText = CreateText(modePanel.transform, "ModeText", "NONE");
            hud.modeGlow = glow;
            hud.modeText = modeText;

            // Stats
            var stats = CreateText(hudGo.transform, "StatsText", "Controls: not ready");
            stats.rectTransform.anchorMin = new Vector2(0.5f, 1);
            stats.rectTransform.anchorMax = new Vector2(0.5f, 1);
            stats.rectTransform.anchoredPosition = new Vector2(0, -40);
            hud.statsText = stats;

            // Pause panel (hidden by default)
            var pause = CreatePanel(hudGo.transform, "PausePanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            pause.SetActive(false);
            var pauseBg = CreateImage(pause.transform, "Bg", new Color(0.1f, 0.08f, 0.06f, 0.85f));
            var pauseHint = CreateText(pause.transform, "Hint", "PREVIEW PAUSE\n(buttons call actions only)");
            hud.pausePanel = pause;

            // Simple mode buttons for preview
            CreateButton(hudGo.transform, "Btn_Echo", "Echo", new Vector2(-160, -120), () => hud.OnClickRestartEcho());
            CreateButton(hudGo.transform, "Btn_Prop", "Prop", new Vector2(0, -120), () => hud.OnClickRestartPropagation());
            CreateButton(hudGo.transform, "Btn_Pause", "Pause", new Vector2(160, -120), () => hud.OnClickTogglePause());

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateOrUpdateScene(GameObject prefab)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "ReverbHUD_Preview";

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return img;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.color = new Color(0.96f, 0.91f, 0.82f);
            tmp.alignment = TextAlignmentOptions.Center;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return tmp;
        }

        private static void CreateButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(140, 40);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.32f, 0.24f, 0.18f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.96f, 0.91f, 0.82f);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
        }
    }
}
