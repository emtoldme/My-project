using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace Reverb.UI
{
    /// <summary>
    /// 锈蚀质感 HUD 控制器（可直接测试）。
    /// 挂在 Canvas 下的空物体上，拖入 UI 引用即可。
    /// 无外部贴图也能运行（使用纯色 + 渐变模拟锈蚀金属 + 信号光）。
    /// 
    /// 测试快捷键（Play 模式下）：
    /// - H / J : 增减生命
    /// - M : 切换模式 (None → Echo → Propagation → Combined)
    /// - Esc : 暂停/继续
    /// </summary>
    public class ReverbHUD : MonoBehaviour
    {
        [Header("Health / Signal")]
        public Image healthFill;
        public TextMeshProUGUI healthText;
        public Image healthFrame; // 可选，用于边框颜色

        [Header("Mode Indicator")]
        public TextMeshProUGUI modeText;
        public Image modeGlow;

        [Header("Pause")]
        public GameObject pausePanel;
        public TextMeshProUGUI pauseHint;

        [Header("Test Settings")]
        public bool enableTestInput = true;
        public float maxHealth = 100f;

        private float currentHealth = 100f;
        private int modeIndex = 0;
        private readonly string[] modes = { "None", "Echo", "Propagation", "Combined" };

        private Gradient signalGradient;

        private void Awake()
        {
            signalGradient = new Gradient();
            signalGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.0f, 0.94f, 1.0f), 0f),   // Cyan
                    new GradientColorKey(new Color(1.0f, 0.0f, 0.67f), 1f)    // Magenta
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
        }

        private void Start()
        {
            currentHealth = maxHealth;
            UpdateHealth(currentHealth);
            SetMode(modes[modeIndex]);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (pauseHint != null) pauseHint.text = "Esc 暂停 / 继续";
        }

        private void Update()
        {
            if (!enableTestInput) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.hKey.wasPressedThisFrame)
                UpdateHealth(currentHealth - 10f);
            if (keyboard.jKey.wasPressedThisFrame)
                UpdateHealth(currentHealth + 10f);
            if (keyboard.mKey.wasPressedThisFrame)
                CycleMode();
            if (keyboard.escapeKey.wasPressedThisFrame)
                TogglePause(!pausePanel.activeSelf);
        }

        public void UpdateHealth(float current)
        {
            currentHealth = Mathf.Clamp(current, 0f, maxHealth);
            float t = currentHealth / maxHealth;

            if (healthFill != null)
            {
                healthFill.fillAmount = t;
                healthFill.color = signalGradient.Evaluate(1f - t);
            }

            if (healthText != null)
                healthText.text = $"{Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)}";

            if (healthFrame != null)
                healthFrame.color = new Color(0.35f, 0.25f, 0.18f, 0.95f); // 锈铁色
        }

        public void SetMode(string mode)
        {
            if (modeText != null)
                modeText.text = mode.ToUpper();

            if (modeGlow != null)
            {
                Color c = mode switch
                {
                    "Echo" => new Color(0f, 0.94f, 1f, 0.9f),
                    "Propagation" => new Color(1f, 0f, 0.67f, 0.9f),
                    "Combined" => new Color(0.85f, 0.4f, 1f, 0.95f),
                    _ => new Color(0.5f, 0.5f, 0.5f, 0.6f)
                };
                modeGlow.color = c;
            }
        }

        public void CycleMode()
        {
            modeIndex = (modeIndex + 1) % modes.Length;
            SetMode(modes[modeIndex]);
        }

        public void TogglePause(bool show)
        {
            if (pausePanel != null)
                pausePanel.SetActive(show);
            Time.timeScale = show ? 0f : 1f;
        }

        // 外部战斗系统可调用
        public void OnPlayerDamaged(float newHealth) => UpdateHealth(newHealth);
        public void OnModeChanged(string mode) => SetMode(mode);
    }
}
