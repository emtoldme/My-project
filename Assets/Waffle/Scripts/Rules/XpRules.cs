namespace Waffle
{
    /// <summary>
    /// 经验公式（§1.5）：从 n 级升到 n+1 级需要 5 + 3(n−1) XP。
    /// 只有纯函数，没有任何可变 static 状态。
    /// </summary>
    public static class XpRules
    {
        public const int BaseXp = 5;
        public const int StepXp = 3;

        /// <summary>当前等级升到下一级所需 XP。</summary>
        public static int XpToNext(int level, float extraStepAfterFour = 0)
        {
            return BaseXp + StepXp * (level - 1) + (int)System.Math.Ceiling(System.Math.Max(0, level - 4) * System.Math.Max(0, extraStepAfterFour));
        }

        /// <summary>从 1 级 0 XP 升到 targetLevel 需要的累计 XP：5(L−1) + 1.5(L−1)(L−2)。</summary>
        public static int CumulativeXp(int targetLevel)
        {
            int l = targetLevel - 1;
            return BaseXp * l + StepXp * l * (l - 1) / 2;
        }

        /// <summary>
        /// 加 XP，处理溢出与连续升级。
        /// 返回本次升级的次数；level / xp 被就地更新。
        /// </summary>
        public static int AddXp(ref int level, ref int xp, int gain, float extraStepAfterFour = 0)
        {
            if (gain <= 0) return 0;
            xp += gain;
            int levelsGained = 0;
            while (xp >= XpToNext(level, extraStepAfterFour))
            {
                xp -= XpToNext(level, extraStepAfterFour);
                level++;
                levelsGained++;
            }
            return levelsGained;
        }
    }
}
