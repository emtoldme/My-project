namespace Waffle.Experiments.Mechanism.Presentation
{
    /// <summary>
    /// Codex 每帧给的回声预备快照。Progress 为 0 到 1，由规则时间算出。视图不保存这份列表。
    /// </summary>
    public struct M01EchoCue
    {
        public int Serial;
        public int Generation;
        public float X;
        public float Z;
        public float Progress;

        public M01EchoCue(int serial, int generation, float x, float z, float progress)
        {
            Serial = serial;
            Generation = generation;
            X = x;
            Z = z;
            Progress = progress;
        }
    }
}
