namespace Survivor
{
    /// <summary>
    /// Task 1 只允许 Init、Playing、Paused 之间的三条转换。
    /// 其余枚举先占位，避免下一阶段再改已经被调用方引用的类型。
    /// </summary>
    public enum GameState
    {
        Init = 0,
        Playing = 1,
        Paused = 2,
        LevelUp = 3,
        Defeat = 4,
        Victory = 5
    }
}
