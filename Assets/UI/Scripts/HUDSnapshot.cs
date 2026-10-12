using System;
using UnityEngine;

namespace Reverb.UI
{
    /// <summary>
    /// HUD 只读快照。由外部适配层（Codex）从真实 GameLoop / Combat / IMechanismPlaytestControls 填充。
    /// UI 不持有、不推进任何局内状态。
    /// </summary>
    [Serializable]
    public struct HUDSnapshot
    {
        public float CurrentHealth;
        public float MaxHealth;
        public string ModeName;          // "None" / "Echo" / "Propagation" / "Combined"
        public bool IsPaused;
        public int EchoHits;
        public int PropagationHits;
        public int PendingCount;
        public bool ControlsReady;

        public static HUDSnapshot Empty => new HUDSnapshot
        {
            CurrentHealth = 0f,
            MaxHealth = 1f,
            ModeName = "None",
            IsPaused = false,
            ControlsReady = false
        };
    }

    /// <summary>
    /// 最小按钮回调接口。正式接线时由 Codex 适配到唯一 GameLoop / IMechanismPlaytestControls。
    /// HUD 自身不调用 RestartWithMode 或 TogglePause。
    /// </summary>
    public interface IHUDActions
    {
        void OnRestartWithMode(string modeName);
        void OnTogglePause();
    }
}
