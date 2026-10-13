using UnityEngine;
using Waffle;

namespace Reverb.Narrative.Presentation
{
    /// <summary>
    /// 车载无线电 HUD：将无线电通话以 80 年代模拟收音机（CRT/点阵发光二极管）质感呈现在屏幕左下方。
    /// 纯表现层，挂载即可生效，无侵入性。
    /// </summary>
    public class NarrativeRadioHud : MonoBehaviour
    {
        [SerializeField] private NarrativeDirector director;

        private NarrativeLine currentLine;
        private float typingProgress = 0f;
        private string displayedTranscript = "";
        private const float CHARACTERS_PER_SECOND = 24f; // 模拟电传打字机速度

        private void Awake()
        {
            if (director == null)
            {
                director = GetComponent<NarrativeDirector>() ?? FindAnyObjectByType<NarrativeDirector>();
            }
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.OnNarrativeLineStarted += HandleLineStarted;
                director.OnNarrativeLineFinished += HandleLineFinished;
            }
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.OnNarrativeLineStarted -= HandleLineStarted;
                director.OnNarrativeLineFinished -= HandleLineFinished;
            }
        }

        private void HandleLineStarted(NarrativeLine line)
        {
            currentLine = line;
            typingProgress = 0f;
            displayedTranscript = "";
        }

        private void HandleLineFinished(NarrativeLine line)
        {
            if (currentLine == line)
            {
                currentLine = null;
                displayedTranscript = "";
            }
        }

        private void Update()
        {
            if (currentLine == null || string.IsNullOrEmpty(currentLine.transcript)) return;

            typingProgress += Time.unscaledDeltaTime * CHARACTERS_PER_SECOND;
            int count = Mathf.Clamp(Mathf.FloorToInt(typingProgress), 0, currentLine.transcript.Length);
            displayedTranscript = currentLine.transcript.Substring(0, count);
        }

        private void OnGUI()
        {
            if (currentLine == null && string.IsNullOrEmpty(displayedTranscript)) return;

            ImguiScale.Apply();
            Color originalColor = GUI.color;
            Color speakerThemeColor = GetThemeColor(currentLine?.speaker ?? NarrativeSpeaker.Mara);

            // 外边框与面板底色
            GUI.color = speakerThemeColor;
            Rect radioRect = ImguiScale.Fit(new Rect(16, ImguiScale.Height - 215, 480, 95));
            GUI.Box(radioRect, "");

            GUILayout.BeginArea(radioRect);
            
            // 顶栏：FM 频段 + 发话者 + 信号状态
            GUILayout.BeginHorizontal();
            GUILayout.Label($"[FM {currentLine?.frequencyMHz:F1} MHz]", GUILayout.Width(110));
            GUILayout.Label($"▶ {currentLine?.speakerDisplayName}", GUILayout.Width(130));
            GUILayout.Label(GetSignalStatus(currentLine?.staticIntensity ?? 0f));
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            // 荧光白打字机文本
            GUI.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
            var style = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 13
            };
            GUILayout.Label(displayedTranscript, style);

            GUILayout.EndArea();

            GUI.color = originalColor;
        }

        private Color GetThemeColor(NarrativeSpeaker speaker)
        {
            switch (speaker)
            {
                case NarrativeSpeaker.Walter:
                    return new Color(0.35f, 0.75f, 1f, 0.95f); // 幽蓝（控制与技术）
                case NarrativeSpeaker.Mara:
                    return new Color(1f, 0.72f, 0.28f, 0.95f); // 暖橙（泥土与现实）
                case NarrativeSpeaker.Lucy:
                    return new Color(1f, 0.38f, 0.38f, 0.95f); // 铁锈暗红（历史与伤痕）
                case NarrativeSpeaker.Eli:
                    return new Color(0.35f, 1f, 0.65f, 0.95f); // 亮青绿（未来与科学）
                case NarrativeSpeaker.Protagonist:
                    return new Color(0.9f, 0.9f, 0.9f, 0.95f);  // 纯白（归乡者）
                default:
                    return new Color(0.7f, 0.7f, 0.7f, 0.95f);
            }
        }

        private string GetSignalStatus(float noise)
        {
            if (noise > 0.4f) return "STATIC: [!! DISTORTION !!]";
            if (noise > 0.2f) return "SIGNAL: [■■□□] WEAK";
            return "SIGNAL: [■■■■] TUNED";
        }
    }
}
