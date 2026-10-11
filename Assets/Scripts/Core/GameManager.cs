using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Survivor
{
    /// <summary>
    /// S1 单场景的状态与时间入口。
    /// 只有这个组件可以写 Time.timeScale。输入回调不依赖缩放时间，因此暂停后仍能恢复。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Init;

        /// <summary>
        /// 参数是切换前、切换后。只在转换真正成功时触发一次。
        /// 使用实例事件，避免在 OnDestroy 里清空全局委托。
        /// </summary>
        public event Action<GameState, GameState> StateChanged;

        PauseActions actions;
        bool isOwner;
        bool inputHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticState()
        {
            // 关闭 Domain Reload 时静态字段会留下来。普通场景重载不会走这里。
            Instance = null;
            Time.timeScale = 1f;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                isOwner = false;
                Debug.LogWarning("[GameManager] 场景里有重复实例，后出现的对象已忽略。", this);
                Destroy(gameObject);
                return;
            }

            try
            {
                actions = new PauseActions();
            }
            catch (Exception exception)
            {
                isOwner = false;
                Debug.LogException(exception, this);
                enabled = false;
                return;
            }

            Instance = this;
            isOwner = true;
            State = GameState.Init;
            ApplyTimeScale(GameState.Init);
            Debug.Log("[GameManager] 进入状态 Init。timeScale=1。", this);
        }

        void OnEnable()
        {
            if (!isOwner || actions == null)
                return;
            HookInput();
        }

        void Start()
        {
            if (!isOwner)
                return;

            // 放在 Start，保证其他对象已经在 OnEnable 里订阅。
            TryChangeState(GameState.Playing);
        }

        void OnDisable()
        {
            UnhookInput();
        }

        void OnDestroy()
        {
            UnhookInput();

            if (actions != null)
            {
                actions.Dispose();
                actions = null;
            }

            if (Instance == this)
            {
                Instance = null;
                // 退出 Play Mode 或重载场景时，不能把 0 留到下一次。
                Time.timeScale = 1f;
            }
        }

        public bool TryChangeState(GameState next)
        {
            if (!isOwner)
                return false;

            if (next == State)
                return false;

            if (!IsAllowed(State, next))
            {
                Debug.LogWarning(
                    $"[GameManager] 拒绝状态切换 {State} -> {next}。Task 1 只允许 Init->Playing、Playing->Paused、Paused->Playing。",
                    this);
                return false;
            }

            var previous = State;
            State = next;
            ApplyTimeScale(next);
            Debug.Log($"[GameManager] 状态切换 {previous} -> {next}。timeScale={Time.timeScale}。", this);
            StateChanged?.Invoke(previous, next);
            return true;
        }

        public static bool IsAllowed(GameState current, GameState next)
        {
            return (current == GameState.Init && next == GameState.Playing)
                || (current == GameState.Playing && next == GameState.Paused)
                || (current == GameState.Paused && next == GameState.Playing);
        }

        void HookInput()
        {
            if (inputHooked)
                return;

            actions.Enable();
            actions.Pause.performed += OnPausePerformed;
            inputHooked = true;
        }

        void UnhookInput()
        {
            if (!inputHooked || actions == null)
                return;

            actions.Pause.performed -= OnPausePerformed;
            actions.Disable();
            inputHooked = false;
        }

        void OnPausePerformed(InputAction.CallbackContext context)
        {
            if (!isOwner || Instance != this)
                return;

            if (State == GameState.Playing)
                TryChangeState(GameState.Paused);
            else if (State == GameState.Paused)
                TryChangeState(GameState.Playing);
            else
                Debug.Log($"[GameManager] 当前状态 {State} 忽略 Pause。", this);
        }

        static void ApplyTimeScale(GameState state)
        {
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
        }

        [ContextMenu("调试/请求进入 LevelUp（应当被拒绝）")]
        void DebugRequestLevelUp()
        {
            TryChangeState(GameState.LevelUp);
        }
    }
}
