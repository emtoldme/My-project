using UnityEngine;
using UnityEngine.InputSystem;

namespace Waffle
{
    /// <summary>
    /// 输入封装（§8.4）：Playing 时只开 Player（Gameplay）Map，其他状态只开 UI Map。
    /// 用轮询（ReadValue / WasPressedThisFrame），在 T0 读一次，不订阅回调。
    /// 不是 MonoBehaviour，由 GameLoop 持有，重开时随场景一起销毁。
    /// </summary>
    public class GameInput
    {
        private readonly InputActionMap playerMap;
        private readonly InputActionMap uiMap;
        private readonly InputAction move, dodge, pauseGameplay;
        private readonly InputAction pauseUi, choose1, choose2, choose3;
        private bool? gameplayEnabled;

        public bool IsValid { get; private set; }

        public GameInput(InputActionAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("[Waffle] 没有指定 InputActionAsset（GameLoop.actions）");
                return;
            }
            playerMap = asset.FindActionMap("Player", false);
            uiMap = asset.FindActionMap("UI", false);
            if (playerMap == null || uiMap == null)
            {
                Debug.LogError("[Waffle] InputActionAsset 里缺少 Player 或 UI Map");
                return;
            }
            move = playerMap.FindAction("Move", false);
            dodge = playerMap.FindAction("Dodge", false);
            pauseGameplay = playerMap.FindAction("Pause", false);
            pauseUi = uiMap.FindAction("Pause", false);
            choose1 = uiMap.FindAction("Choose1", false);
            choose2 = uiMap.FindAction("Choose2", false);
            choose3 = uiMap.FindAction("Choose3", false);
            IsValid = move != null && dodge != null && pauseGameplay != null && pauseUi != null;
            if (!IsValid) Debug.LogError("[Waffle] 缺少 Move / Dodge / Pause 动作，请检查 InputSystem_Actions");

            // 项目级 Actions 会在启动时把所有 Map 打开，这里统一先关掉，再按状态打开
            asset.Disable();
        }

        public bool GameplayMapEnabled { get { return playerMap != null && playerMap.enabled; } }
        public bool UiMapEnabled { get { return uiMap != null && uiMap.enabled; } }

        /// <summary>按状态切换 Map（只在变化时切换）。</summary>
        public void ApplyPhase(RunPhase phase)
        {
            if (!IsValid) return;
            bool wantGameplay = phase == RunPhase.Playing;
            if (gameplayEnabled == wantGameplay) return;
            gameplayEnabled = wantGameplay;
            if (wantGameplay) { uiMap.Disable(); playerMap.Enable(); }
            else { playerMap.Disable(); uiMap.Enable(); }
        }

        /// <summary>T0：读一次输入快照。只读当前打开的那个 Map。</summary>
        public InputSnapshot Read(RunPhase phase)
        {
            var s = InputSnapshot.None;
            if (!IsValid) return s;
            if (phase == RunPhase.Playing)
            {
                Vector2 m = move.ReadValue<Vector2>();
                s.MoveX = m.x;
                s.MoveY = m.y;
                s.DodgePressed = dodge.WasPressedThisFrame();
                s.PausePressed = pauseGameplay.WasPressedThisFrame();
            }
            else
            {
                s.PausePressed = pauseUi.WasPressedThisFrame();
                if (choose1 != null && choose1.WasPressedThisFrame()) s.ChoosePressed = 0;
                else if (choose2 != null && choose2.WasPressedThisFrame()) s.ChoosePressed = 1;
                else if (choose3 != null && choose3.WasPressedThisFrame()) s.ChoosePressed = 2;
            }
            return s;
        }

        /// <summary>重开前调用：关闭两个 Map，避免跨场景残留。</summary>
        public void DisableAll()
        {
            if (playerMap != null) playerMap.Disable();
            if (uiMap != null) uiMap.Disable();
            gameplayEnabled = null;
        }
    }
}
