using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Waffle
{
    /// <summary>
    /// 场景里唯一有玩法 Update 的脚本（§0.4 方案 A）。
    /// 每帧：读输入（T0）→ RunSimulation.Tick 按 T0–T13 推进 → 同步 timeScale、输入 Map、表现层。
    /// 其他玩法对象都不写 Update。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameLoop : MonoBehaviour
    {
        [Tooltip("一局的参数（设计初值）")]
        public RunConfig config = new RunConfig();

        [Tooltip("Assets/InputSystem_Actions.inputactions；为空时使用项目级 Actions")]
        public InputActionAsset actions;

        [Tooltip("玩家的表现物体（只同步位置）")]
        public Transform playerView;

        [Tooltip("用来计算相机相对移动方向；为空时用 Camera.main")]
        public Camera gameplayCamera;

        public RunSimulation Sim { get; private set; }
        public GameInput Controls { get; private set; }

        /// <summary>
        /// 表现事件分发（音效/特效订阅）。每次 Tick 之后、以及 UI 按钮/失焦/重开改变状态之后各分发一次，然后清空。
        /// 订阅者只读，不能改玩法状态。实例事件，随场景销毁。
        /// </summary>
        public event Action<IReadOnlyList<PresentationEvent>> PresentationDispatched;

        private RunPhase lastPhase;
        private bool restarting;

        private void Awake()
        {
            Time.timeScale = 1f;
            Sim = new RunSimulation(config, new UnityRunLog());

            InputActionAsset asset = actions != null ? actions : InputSystem.actions;
            Controls = new GameInput(asset);

            Camera cam = gameplayCamera != null ? gameplayCamera : Camera.main;
            if (cam != null)
            {
                // 相机固定，开局算一次投影到 XZ 的右/前方向（§8.2）
                Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
                Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
                Sim.SetMoveBasis(r.x, r.z, f.x, f.z);
            }

            lastPhase = Sim.State.Phase;
            Controls.ApplyPhase(lastPhase);
            Debug.Log("[Waffle] 开局 seed=" + config.seed + " frame=" + Time.frameCount);
        }

        private void Update()
        {
            if (restarting) return;
            RunPhase phase = Sim.State.Phase;
            InputSnapshot snapshot = Controls.Read(phase);
            // 用 unscaled 帧时间：逻辑是否推进只看 GameLoop 的门控，不依赖 timeScale（§0.1 规则 4）
            Sim.Tick(Time.unscaledDeltaTime, snapshot, Time.frameCount, Time.unscaledTimeAsDouble);
            SyncAfterStateChange();
            DispatchPresentation();

            if (playerView != null)
                playerView.position = new Vector3(Sim.PlayerX, playerView.position.y, Sim.PlayerZ);
        }

        /// <summary>把本帧表现事件交给订阅者，然后清空。</summary>
        public void DispatchPresentation()
        {
            if (Sim == null || Sim.Presentation.Count == 0) return;
            var handler = PresentationDispatched;
            if (handler != null) handler(Sim.Presentation.Events);
            Sim.Presentation.Clear();
        }

        /// <summary>状态变化后同步 timeScale 和输入 Map。UI 按钮改变状态后也要调用。</summary>
        public void SyncAfterStateChange()
        {
            RunPhase phase = Sim.State.Phase;
            Time.timeScale = RunState.TimeScaleFor(phase);
            if (phase != lastPhase)
            {
                Controls.ApplyPhase(phase);
                lastPhase = phase;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && Sim != null && !restarting)
            {
                Sim.BeginFrame(Time.frameCount, Time.unscaledTimeAsDouble);
                Sim.RequestPause("失去焦点");
                SyncAfterStateChange();
                DispatchPresentation();
            }
        }

        /// <summary>UI 提交升级选择（按钮）。</summary>
        public void Choose(int index)
        {
            Sim.BeginFrame(Time.frameCount, Time.unscaledTimeAsDouble);
            Sim.ChooseUpgrade(index);
            SyncAfterStateChange();
            DispatchPresentation();
        }

        /// <summary>UI 的"继续"按钮。</summary>
        public void TogglePause()
        {
            Sim.BeginFrame(Time.frameCount, Time.unscaledTimeAsDouble);
            Sim.TogglePause("按钮");
            SyncAfterStateChange();
            DispatchPresentation();
        }

        /// <summary>重开 = 重新加载当前场景（§1.7）。重开前显式把 timeScale 设为 1。</summary>
        public void Restart()
        {
            if (restarting) return;
            restarting = true;
            Debug.Log(string.Format("[Waffle] 重开：从 {0} now={1:F3} frame={2}", Sim.State.Phase, Sim.State.Now, Time.frameCount));
            Sim.NotifyRestart();
            DispatchPresentation(); // 让音效/特效先清场
            Time.timeScale = 1f;
            Controls.DisableAll();

            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
            }
            else
            {
#if UNITY_EDITOR
                // 场景没加进 Build Settings 时，编辑器里也能重开
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
                Debug.LogError("[Waffle] 场景不在 Build Settings 里，无法重开");
#endif
            }
        }

        private void OnDestroy()
        {
            if (Controls != null) Controls.DisableAll();
        }
    }
}
