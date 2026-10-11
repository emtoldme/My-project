namespace Waffle
{
    /// <summary>一局游戏的五种状态（§0.1）。</summary>
    public enum RunPhase
    {
        Playing,
        Paused,
        LevelUp,
        Victory,
        Defeat
    }

    public static class RunPhaseExtensions
    {
        /// <summary>Victory / Defeat 是终态，只有重开能离开。</summary>
        public static bool IsTerminal(this RunPhase phase)
        {
            return phase == RunPhase.Victory || phase == RunPhase.Defeat;
        }
    }
}
