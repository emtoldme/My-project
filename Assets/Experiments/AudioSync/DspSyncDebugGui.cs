using UnityEngine;

namespace Reverb.Experiments.AudioSync
{
    /// <summary>
    /// E-03 实验独立快速调试与演示 GUI：
    /// 允许在单独场景或挂载到测试物体上进行一键测试：
    /// - 启动 DSP 锁步
    /// - 切换垂直分层（静默/低烈度/高烈度战斗）
    /// - 暂停/恢复测试
    /// - 实时显示当前采样漂移（Samples 与毫秒数）与循环次数
    /// </summary>
    [RequireComponent(typeof(DspSyncExperimentController))]
    public class DspSyncDebugGui : MonoBehaviour
    {
        private DspSyncExperimentController controller;

        private void Awake()
        {
            controller = GetComponent<DspSyncExperimentController>();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 380, 420), "E-03 双轨 DSP 同步混音实验", GUI.skin.window);

            GUILayout.Label($"播放状态: {(controller.IsPlaying ? (controller.IsPaused ? "已暂停" : "正在播放") : "未启动")}");
            GUILayout.Label($"完成循环次数: {controller.LoopCount}");
            
            float sampleRate = Mathf.Max(1, controller.SampleRate);
            float driftMs = (controller.CurrentSampleDrift / sampleRate) * 1000.0f;
            float maxDriftMs = (controller.MaxSampleDriftDetected / sampleRate) * 1000.0f;
            GUILayout.Label($"实时采样点漂移: {controller.CurrentSampleDrift} samples ({driftMs:F3} ms)");
            GUILayout.Label($"历史最大漂移: {controller.MaxSampleDriftDetected} samples ({maxDriftMs:F3} ms)");
            GUILayout.Label($"有效观察 {controller.ValidObservations}，丢弃 {controller.DiscardedObservations}（非硬件输出认证）");

            GUILayout.Space(10);
            GUILayout.Label($"Track A (基础底床) 增益: {controller.GainA:F2}");
            GUILayout.Label($"Track B (氛围高频) 增益: {controller.GainB:F2}");

            GUILayout.Space(10);
            if (!controller.IsPlaying)
            {
                if (GUILayout.Button("1. 启动 DSP 预定同步播放 (Start Sync)", GUILayout.Height(35)))
                {
                    controller.StartSyncPlayback();
                }
            }
            else
            {
                if (GUILayout.Button(controller.IsPaused ? "恢复播放 (Resume Sync)" : "暂停播放 (Pause Sync)", GUILayout.Height(30)))
                {
                    if (controller.IsPaused) controller.ResumeSync();
                    else controller.PauseSync();
                }

                GUILayout.Space(5);
                GUILayout.Label("垂直分层混音动态切换:");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("底床独奏\n(A:1.0, B:0.0)"))
                {
                    controller.SetLayerGains(1.0f, 0.0f);
                }
                if (GUILayout.Button("双轨融合\n(A:1.0, B:0.7)"))
                {
                    controller.SetLayerGains(1.0f, 0.7f);
                }
                if (GUILayout.Button("高潮爆发\n(A:0.8, B:1.0)"))
                {
                    controller.SetLayerGains(0.8f, 1.0f);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                if (GUILayout.Button("重置并停止 (Stop & Reset)", GUILayout.Height(30)))
                {
                    controller.StopAndReset();
                }
            }

            GUILayout.EndArea();
        }
    }
}
