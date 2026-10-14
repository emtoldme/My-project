using System;

namespace Waffle.Homecoming.Relics
{
    public enum ResonancePattern { Unlinked, Backstitch, ThreadLure, HearthGather }

    public static class ResonancePatterns
    {
        public static string Id(ResonancePattern pattern) => pattern == ResonancePattern.Backstitch ? "combo.backstitch"
            : pattern == ResonancePattern.ThreadLure ? "combo.thread-lure" : pattern == ResonancePattern.HearthGather ? "combo.hearth-gather" : null;
        public static string Name(ResonancePattern pattern) => pattern == ResonancePattern.Backstitch ? "回针"
            : pattern == ResonancePattern.ThreadLure ? "牵弦" : pattern == ResonancePattern.HearthGather ? "归巢" : "独奏";
        public static string Recipe(ResonancePattern pattern) => pattern == ResonancePattern.Backstitch ? "针线盒 ＋ 旧吉他"
            : pattern == ResonancePattern.ThreadLure ? "工作手套 ＋ 旧吉他" : "针线盒 ＋ 工作手套";
        public static string Description(ResonancePattern pattern) => pattern == ResonancePattern.Backstitch
            ? "首次闪避留针，下一次共鸣花1碎音返回旧位置并发出残响。缝线限24米、24秒；离得太远会断。"
            : pattern == ResonancePattern.ThreadLure
            ? "闪避残响花1碎音牵引最多12个普通追击者，朝旧声源走2.5秒。特殊敌人与首领不受影响；无碎音时仍是普通残响。"
            : "闪避花1碎音，让8米内最多12份已有经验进入磁吸。经验仍须实际拾取，不能凭空生成；没有吉他也可使用。";
        public static bool Available(RelicInventory bag, ResonancePattern pattern)
        {
            if (bag == null) return false;
            bool tin = bag.IsEquipped(RelicInventory.SewingTin), gloves = bag.IsEquipped(RelicInventory.WorkGloves), guitar = bag.IsEquipped(RelicInventory.OldGuitar);
            return pattern == ResonancePattern.Unlinked || (pattern == ResonancePattern.Backstitch && tin && guitar)
                || (pattern == ResonancePattern.ThreadLure && gloves && guitar) || (pattern == ResonancePattern.HearthGather && tin && gloves);
        }
    }
}
