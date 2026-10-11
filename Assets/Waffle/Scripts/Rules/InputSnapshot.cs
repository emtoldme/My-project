namespace Waffle
{
    /// <summary>
    /// T0 输入快照：每帧只读一次输入，之后的步骤只用这个快照（§0.3）。
    /// 不依赖 UnityEngine，方便在测试里直接构造。
    /// </summary>
    public struct InputSnapshot
    {
        /// <summary>移动输入（摇杆/WASD），x = 右，y = 前，长度未限制。</summary>
        public float MoveX;
        public float MoveY;
        /// <summary>闪避键"本帧按下"（Gameplay Map）。</summary>
        public bool DodgePressed;
        /// <summary>暂停键"本帧按下"（Playing 时来自 Gameplay Map，其他状态来自 UI Map）。</summary>
        public bool PausePressed;
        /// <summary>升级面板：本帧按下的选项序号 0/1/2，没有则为 -1。</summary>
        public int ChoosePressed;

        public static InputSnapshot None
        {
            get { return new InputSnapshot { ChoosePressed = -1 }; }
        }
    }
}
