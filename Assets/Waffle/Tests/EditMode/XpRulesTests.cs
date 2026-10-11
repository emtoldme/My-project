using NUnit.Framework;

namespace Waffle.Tests
{
    /// <summary>表 X-1 / X-2（§1.5）：经验公式、溢出与连续升级。</summary>
    public class XpRulesTests
    {
        [TestCase(1, 5)]
        [TestCase(2, 8)]
        [TestCase(4, 14)]
        [TestCase(13, 41)]
        [TestCase(14, 44)]
        [TestCase(15, 47)]
        public void XpToNext_FollowsFormula(int level, int expected)
        {
            Assert.AreEqual(expected, XpRules.XpToNext(level));
        }

        [TestCase(2, 5)]
        [TestCase(5, 38)]
        [TestCase(12, 220)]
        [TestCase(13, 258)]
        [TestCase(14, 299)]
        [TestCase(15, 343)]
        [TestCase(16, 390)]
        public void Cumulative_MatchesTableX1(int targetLevel, int expected)
        {
            Assert.AreEqual(expected, XpRules.CumulativeXp(targetLevel));
            // 累计表也必须等于逐级相加
            int sum = 0;
            for (int n = 1; n < targetLevel; n++) sum += XpRules.XpToNext(n);
            Assert.AreEqual(expected, sum);
        }

        // 初始等级, 初始XP, 获得, 期望等级, 期望XP, 期望升级次数
        [TestCase(1, 0, 5, 2, 0, 1, TestName = "A-60 1级0XP+5 → 2级 0/8")]
        [TestCase(1, 0, 4, 1, 4, 0, TestName = "边界 1级0XP+4 → 1级 4/5")]
        [TestCase(1, 0, 30, 4, 6, 3, TestName = "A-61 1级0XP+30 → 4级 6/14")]
        [TestCase(1, 0, 38, 5, 0, 4, TestName = "A-61b 1级0XP+38 → 5级 0/17")]
        [TestCase(2, 3, 13, 3, 8, 1, TestName = "A-61c 2级3XP+13 → 3级 8/11")]
        [TestCase(5, 10, 10, 6, 3, 1, TestName = "精英 5级10XP+10 → 6级 3/20")]
        [TestCase(1, 0, 300, 14, 1, 13, TestName = "调试 1级0XP+300 → 14级 1/44")]
        public void AddXp_TableX2(int level, int xp, int gain, int expLevel, int expXp, int expUps)
        {
            int ups = XpRules.AddXp(ref level, ref xp, gain);
            Assert.AreEqual(expLevel, level, "等级");
            Assert.AreEqual(expXp, xp, "剩余 XP");
            Assert.AreEqual(expUps, ups, "升级次数");
        }

        [Test]
        public void AddXp_ZeroOrNegative_DoesNothing()
        {
            int level = 3, xp = 2;
            Assert.AreEqual(0, XpRules.AddXp(ref level, ref xp, 0));
            Assert.AreEqual(0, XpRules.AddXp(ref level, ref xp, -5));
            Assert.AreEqual(3, level);
            Assert.AreEqual(2, xp);
        }
    }
}
