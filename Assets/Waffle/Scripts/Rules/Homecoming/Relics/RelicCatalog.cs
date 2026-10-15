using Waffle.Homecoming;

namespace Waffle.Homecoming.Relics
{
    public sealed class RelicDetail
    {
        public readonly string ItemId, Name, Ability, Material, SfxId, VfxId, ArtPath;
        public readonly int AdditionalSlots;
        public readonly HomecomingModifiers Modifier;

        public RelicDetail(string itemId, string name, string ability, string material, string sfxId, string vfxId,
            string artPath, int additionalSlots, HomecomingModifiers modifier)
        {
            ItemId = itemId; Name = name; Ability = ability; Material = material; SfxId = sfxId; VfxId = vfxId;
            ArtPath = artPath; AdditionalSlots = additionalSlots; Modifier = modifier;
        }
    }

    public static class RelicCatalog
    {
        public const string ArtFolder = "Assets/Waffle/Scripts/Rules/Homecoming/Relics/Art/";

        public static readonly RelicDetail SewingTin = new RelicDetail(
            "item.sewing-tin", "针线盒",
            "最大生命 +8%。这一局只能挡一次致命伤，挡下后留在四分之一生命。拿到的时候不补血。",
            "生锈的椭圆铁盒，盖子翘着，里面是奶油色的线和一根针，花纸已经起皮。",
            "sfx.relic.sewing-tin", "vfx.relic.mend-stitch", ArtFolder + "sewing-tin.jpg",
            1, new HomecomingModifiers(maxHealth: 0.08f));

        public static readonly RelicDetail WorkGloves = new RelicDetail(
            "item.work-gloves", "工作手套",
            "拾取范围 +15%。每 12 秒，真正捡起一颗经验时，再把 2.5 米内最多 6 颗已有的经验揽过来。不是全图。",
            "开裂的旧皮手套，指节上补过一块，缝里还嵌着麦壳。",
            "sfx.relic.work-gloves", "vfx.relic.glove-tug", ArtFolder + "work-gloves.jpg",
            1, new HomecomingModifiers(pickupRadius: 0.15f));

        public static readonly RelicDetail OldGuitar = new RelicDetail(
            "item.old-guitar", "旧吉他",
            "成功闪避后打出一次弦响：冷却 8 秒，半径 3 米，基础伤害的一半，最多 12 个目标。同时戴着钟钥匙时，冷却改成 6.5 秒。",
            "晒白的小吉他，第三根弦断了，打了个结。不是电击圆，也不是闪避放电。",
            "sfx.relic.old-guitar", "vfx.relic.string-ring", ArtFolder + "old-guitar.jpg",
            0, new HomecomingModifiers(dodgeEcho: true));

        public static readonly RelicDetail LanternGlass = new RelicDetail(
            "item.lantern-glass", "裂罩灯",
            "移速 +6%。连续走动满 4 秒后，灯芯旺 3 秒，这 3 秒再 +4% 移速。停下会慢慢凉下来。",
            "砸过的铜灯，玻璃罩有一道裂纹，里面只剩一小粒暖火，罩上有烟。",
            "sfx.relic.lantern-glass", "vfx.relic.wick-flare", ArtFolder + "lantern-glass.jpg",
            0, new HomecomingModifiers(moveSpeed: 0.06f));

        public static readonly RelicDetail ClockKey = new RelicDetail(
            "item.clock-key", "钟钥匙",
            "闪避冷却缩短 8%。自己不造成伤害。和旧吉他一起装备时，只把弦响的冷却从 8 秒收成 6.5 秒。",
            "一把发绿的铁质上弦钥匙，环上穿着磨毛的奶油色带子。",
            "sfx.relic.clock-key", "vfx.relic.key-tick", ArtFolder + "clock-key.jpg",
            0, new HomecomingModifiers(dodgeCooldownReduction: 0.08f));

        public static readonly RelicDetail RainCup = new RelicDetail(
            "item.rain-cup", "搪瓷杯",
            "每 16 秒，只有还没满血时才抿一口，回复 6% 最大生命。满血不会浪费这一口。",
            "掉了瓷的奶油色搪瓷杯，内壁有一圈茶渍，杯沿沾着一根干麦秆。",
            "sfx.relic.rain-cup", "vfx.relic.cup-sip", ArtFolder + "rain-cup.jpg",
            0, new HomecomingModifiers());

        public static readonly RelicDetail[] All =
        {
            SewingTin, WorkGloves, OldGuitar, LanternGlass, ClockKey, RainCup
        };

        public static bool TryGet(string itemId, out RelicDetail detail)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].ItemId == itemId) { detail = All[i]; return true; }
            detail = null;
            return false;
        }
    }
}
