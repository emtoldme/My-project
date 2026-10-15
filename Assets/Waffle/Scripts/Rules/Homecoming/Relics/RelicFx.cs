namespace Waffle.Homecoming.Relics
{
    public enum RelicFxKind
    {
        MendStitch,
        GloveTug,
        StringRing,
        StringTear,
        ChromaticHit,
        WickFlare,
        KeyTick,
        CupSip
    }

    /// <summary>
    /// 一次遗物动作的视听规格。规则只描述，不播放，也不改画面。
    /// 色差像素按 1080p 高度算。撕裂长度是世界上的米，不是屏幕伤口。
    /// </summary>
    public readonly struct RelicFxCue
    {
        public readonly string ItemId;
        public readonly string SfxId;
        public readonly RelicFxKind Kind;
        public readonly int Rank;
        public readonly float Duration;
        public readonly float Radius;
        public readonly float ChromaticPixels;
        public readonly float TearMeters;
        public readonly float R, G, B;

        public RelicFxCue(string itemId, string sfxId, RelicFxKind kind, int rank, float duration, float radius,
            float chromaticPixels, float tearMeters, float r, float g, float b)
        {
            ItemId = itemId; SfxId = sfxId; Kind = kind; Rank = rank; Duration = duration; Radius = radius;
            ChromaticPixels = chromaticPixels; TearMeters = tearMeters; R = r; G = g; B = b;
        }
    }
}
