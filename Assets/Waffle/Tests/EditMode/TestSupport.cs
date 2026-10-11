using System.Collections.Generic;

namespace Waffle.Tests
{
    /// <summary>测试用日志：收集到列表里，不写 Unity Console（避免 LogError 让测试失败）。</summary>
    public sealed class ListLog : IRunLog
    {
        public readonly List<string> Infos = new List<string>();
        public readonly List<string> Errors = new List<string>();
        public void Info(string message) { Infos.Add(message); }
        public void Error(string message) { Errors.Add(message); }
    }

    /// <summary>测试小工具：用固定 dt 推进，统一管理帧号和 unscaled 时间。</summary>
    public sealed class SimDriver
    {
        public readonly RunSimulation Sim;
        public readonly ListLog Log = new ListLog();
        public long Frame;
        public double RealTime;

        public SimDriver(RunConfig config = null)
        {
            Sim = new RunSimulation(config ?? new RunConfig(), Log);
        }

        public void Tick(float dt, InputSnapshot input)
        {
            Frame++;
            RealTime += dt;
            Sim.Tick(dt, input, Frame, RealTime);
        }

        public void Tick(float dt = 1f / 60f) { Tick(dt, InputSnapshot.None); }

        public void PressPause() { var s = InputSnapshot.None; s.PausePressed = true; Tick(1f / 60f, s); }

        /// <summary>等过 0.3 s 防误触，再提交选择（新的一帧）。</summary>
        public bool ChooseAfterGuard(int index)
        {
            RealTime += 0.31;
            Frame++;
            Sim.BeginFrame(Frame, RealTime);
            return Sim.ChooseUpgrade(index);
        }
    }
}
