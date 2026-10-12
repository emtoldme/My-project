namespace Waffle.RunContent
{
    public enum GrowthResultKind
    {
        WeaponAdded, WeaponRankUp, PassiveAdded, PassiveRankUp, Evolved,
        /// <summary>候选耗尽的兜底回复：请求宿主回复 HealFraction×最大HP（规则核心不改HP）。</summary>
        FallbackHeal,
        /// <summary>跳过：消费奖励，请求宿主回复 HealFraction×最大HP。</summary>
        Skipped
    }

    public enum RewardSource { LevelUp = 0, Elite = 1 }

    /// <summary>一次已接受命令的结果，供 Codex 适配器消费（加属性/替换武器/请求回复）。</summary>
    public readonly struct GrowthResult
    {
        public readonly GrowthResultKind Kind;
        public readonly RewardSource Source;
        public readonly string Id;          // 内容ID（进化时为进化ID）
        public readonly string ReplacedId;  // 进化时被替换的武器ID
        public readonly int Rank, Slot;
        public readonly float HealFraction;
        public GrowthResult(GrowthResultKind kind, RewardSource source, string id, int rank, int slot, string replacedId = null, float healFraction = 0f)
        { Kind = kind; Source = source; Id = id; Rank = rank; Slot = slot; ReplacedId = replacedId; HealFraction = healFraction; }
    }
}
