using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Reverb.UI
{
    /// <summary>
    /// 锈蚀质感 HUD 视图（只读）。
    /// - 只暴露 Refresh(HUDSnapshot) 与按钮回调。
    /// - 不持有第二套局内状态，不写 Time.timeScale，不监听正式 Esc。
    /// - 测试输入默认关闭，仅独立预览显式开启。
    /// - 颜色与已接入反馈统一：回声紫红、传播金黄。
    /// - 缺引用 / MaxHealth==0 不会抛异常。
    /// </summary>
    public class ReverbHUD : MonoBehaviour
    {
        [Header("Health")]
        public Image healthFill;
        public TextMeshProUGUI healthText;
        public Image healthFrame;

        [Header("Mode")]
        public TextMeshProUGUI modeText;
        public Image modeGlow;

        [Header("Stats (optional)")]
        public TextMeshProUGUI statsText;

        [Header("Pause Panel (optional)")]
        public GameObject pausePanel;

        [Header("Test Preview Only")]
        [Tooltip("仅独立预览场景开启。正常试玩必须关闭。")]
        public bool enableTestInput = false;

        private IHUDActions _actions;
        private HUDSnapshot _last;

        // 与已接入反馈统一的颜色（紫红回声 / 金黄传播）
        private static readonly Color EchoColor = new Color(0.85f, 0.25f, 0.45f, 1f);       // 紫红
        private static readonly Color PropagationColor = new Color(1f, 0.82f, 0.25f, 1f);  // 金黄
        private static readonly Color CombinedColor = new Color(0.9f, 0.55f, 0.35f, 1f);
        private static readonly Color NeutralColor = new Color(0.55f, 0.5f, 0.45f, 0.8f);
        private static readonly Color FrameColor = new Color(0.32f, 0.24f, 0.18f, 0.95f);   // 锈铁

        public void BindActions(IHUDActions actions)
        {
            _actions = actions;
        }

        public void Refresh(HUDSnapshot snapshot)
        {
            _last = snapshot;
            ApplyHealth(snapshot);
            ApplyMode(snapshot.ModeName);
            ApplyStats(snapshot);
            ApplyPause(snapshot.IsPaused);
        }

        private void ApplyHealth(HUDSnapshot s)
        {
            float max = Mathf.Max(s.MaxHealth, 0.0001f); // 防除零
            float t = Mathf.Clamp01(s.CurrentHealth / max);

            if (healthFill != null)
            {
                healthFill.fillAmount = t;
                // 低血偏向紫红，高血偏向金黄（仍属同一语义体系）
                healthFill.color = Color.Lerp(EchoColor, PropagationColor, t);
            }

            if (healthText != null)
                healthText.text = $"{Mathf.RoundToInt(s.CurrentHealth)} / {Mathf.RoundToInt(max)}";

            if (healthFrame != null)
                healthFrame.color = FrameColor;
        }

        private void ApplyMode(string modeName)
        {
            if (modeText != null)
                modeText.text = string.IsNullOrEmpty(modeName) ? "NONE" : modeName.ToUpperInvariant();

            if (modeGlow != null)
            {
                modeGlow.color = modeName switch
                {
                    "Echo" => EchoColor,
                    "Propagation" => PropagationColor,
                    "Combined" => CombinedColor,
                    _ => NeutralColor
                };
            }
        }

        private void ApplyStats(HUDSnapshot s)
        {
            if (statsText == null) return;
            if (!s.ControlsReady)
            {
                statsText.text = "Controls: not ready";
                return;
            }
            statsText.text = $"Echo {s.EchoHits}  Prop {s.PropagationHits}  Pend {s.PendingCount}";
        }

        private void ApplyPause(bool paused)
        {
            if (pausePanel != null)
                pausePanel.SetActive(paused);
        }

        // 按钮回调（由 UI Button OnClick 或预览工具调用）
        public void OnClickRestartNone() => _actions?.OnRestartWithMode("None");
        public void OnClickRestartEcho() => _actions?.OnRestartWithMode("Echo");
        public void OnClickRestartPropagation() => _actions?.OnRestartWithMode("Propagation");
        public void OnClickRestartCombined() => _actions?.OnRestartWithMode("Combined");
        public void OnClickTogglePause() => _actions?.OnTogglePause();

        // 仅预览用：本地模拟快照，不驱动真实系统
        private void Update()
        {
            if (!enableTestInput) return;

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            if (kb.hKey.wasPressedThisFrame)
                SimulateHealth(-10f);
            if (kb.jKey.wasPressedThisFrame)
                SimulateHealth(+10f);
            if (kb.mKey.wasPressedThisFrame)
                SimulateCycleMode();
            // 注意：不监听 Esc，避免与正式输入竞争
        }

        private void SimulateHealth(float delta)
        {
            var s = _last;
            s.MaxHealth = Mathf.Max(s.MaxHealth, 1f);
            s.CurrentHealth = Mathf.Clamp(s.CurrentHealth + delta, 0f, s.MaxHealth);
            Refresh(s);
        }

        private void SimulateCycleMode()
        {
            var s = _last;
            s.ModeName = s.ModeName switch
            {
                "None" => "Echo",
                "Echo" => "Propagation",
                "Propagation" => "Combined",
                _ => "None"
            };
            Refresh(s);
        }
    }
}
