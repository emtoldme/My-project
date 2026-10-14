using System;
using System.Collections.Generic;
using Waffle.Homecoming;

namespace Reverb.Drafts.RC01.Presentation.Homecoming.Journal
{
    /// <summary>
    /// Preview adapter for testing HC-D Presentation without needing the fully integrated GameLoop.
    /// Provides mock locations, relics, goals, profile upgrades, and rich narrative entries.
    /// Can simulate claim, equip, unequip, purchase, open/close commands.
    /// </summary>
    public sealed class MockHomecomingHost : IHomecomingViewSource, IHomecomingCommands
    {
        public Guid CurrentRunId { get; set; } = Guid.NewGuid();
        public bool IsJournalOpen { get; set; } = false;
        public string LastFeedbackMessage { get; private set; } = "";
        public bool SimulateSaveFailure { get; set; } = false;

        private readonly Dictionary<string, NarrativeEntry> _narratives = new Dictionary<string, NarrativeEntry>(StringComparer.Ordinal);
        private readonly List<PoiView> _nearbyPois = new List<PoiView>();
        private readonly List<RelicSlotView> _relicSlots = new List<RelicSlotView>();
        private readonly List<string> _storedItems = new List<string>();
        private readonly List<GoalView> _goals = new List<GoalView>();
        private readonly List<MetaUpgradeView> _upgrades = new List<MetaUpgradeView>();
        private readonly List<string> _discoveredNarratives = new List<string>();

        public int RelicCapacity { get; set; } = 2;
        public long ProfileXp { get; set; } = 150;
        public int ProfileLevel { get; set; } = 2;
        public int AvailablePoints { get; set; } = 3;

        public MockHomecomingHost()
        {
            SeedMockData();
        }

        private void SeedMockData()
        {
            // 1. Narratives
            AddNarrative(new NarrativeEntry("poi.old-home", "主角家的老房子", "门框上最后一道身高线旁写着“等秋收”。门锁换了，厨房窗台上的针线盒却还在。晚风穿过门廊，旧门轴偶尔发出一声轻响。", NarrativeKind.Place));
            AddNarrative(new NarrativeEntry("poi.old-silo", "老筒仓", "父亲用粉笔记粮高，最矮那道是你够得着的位置。如今风穿过空仓，仍有一次迟到的回声。", NarrativeKind.Place));
            AddNarrative(new NarrativeEntry("poi.old-guitar", "老吉他所在的角落", "第三根弦系着你打的结。那首没学完的歌，如今只剩风替你接下一小节。", NarrativeKind.Place));

            AddNarrative(new NarrativeEntry("item.sewing-tin", "母亲的针线盒", "补过的衣服比新的更贴身。盒盖上生了一层细黄铜锈，内衬还垫着半块磨损的蓝布。（最大生命值 +8%）", NarrativeKind.Item));
            AddNarrative(new NarrativeEntry("item.work-gloves", "粗布工作手套", "手掌认得田里每一种分量。虎口处磨出了厚厚的硬茧印痕。（拾取半径 +15%）", NarrativeKind.Item));
            AddNarrative(new NarrativeEntry("item.old-guitar", "旧吉他", "从一次躲闪中把断掉的拍子接回来。琴箱上有雨水干透留下的白痕。（成功闪避后释放一次音波残响）", NarrativeKind.Item));

            AddNarrative(new NarrativeEntry("goal.homeward", "门框上的最后一道线", "回到旧屋门前，认出门框上的刻痕并取回窗台的针线盒。这一次，你没有在门口转身。", NarrativeKind.Goal));
            AddNarrative(new NarrativeEntry("goal.harvest-memory", "空仓也记得秋收", "重访高耸的老筒仓，拾起粗布工作手套并戴上它。田间的泥土与粮尘在指缝中依然清晰。", NarrativeKind.Goal));
            AddNarrative(new NarrativeEntry("goal.unfinished-song", "把最后一小节弹完", "在旧秋千旁拾起老吉他，装备于遗物槽中，在战斗中用一次漂亮的闪避弹响那断弦的回响。", NarrativeKind.Goal));

            AddNarrative(new NarrativeEntry("meta.mended-coat", "补好的外套", "粗针脚缝严了迎风的领口。虽然旧了，却比崭新的装甲更能挡住深秋的麦芒与湿冷。（每级生命上限 +2%）", NarrativeKind.Upgrade));
            AddNarrative(new NarrativeEntry("meta.travel-light", "熟路的脚步", "走过上百遍的垄沟与田埂，闭着眼睛也不会踩空碎石。（每级移动速度 +1%）", NarrativeKind.Upgrade));
            AddNarrative(new NarrativeEntry("meta.sorted-satchel", "整理行囊", "将工具与线团按次序收纳，伸手就能摸到需要的物件。（每级拾取范围 +3%）", NarrativeKind.Upgrade));

            // Default nearby POI
            _nearbyPois.Add(new PoiView(
                new PoiSite("hc1:1001:2:3:poi.old-home", "poi.old-home", "poi.old-home", new Waffle.Combat.Vec2(12f, 8f), 2.4f),
                PoiStatus.Available,
                "老房子门廊（可拾取：母亲的针线盒，遗物槽 +1）"
            ));

            // Relic slots
            _relicSlots.Add(new RelicSlotView(0, "item.sewing-tin", "item.sewing-tin"));
            _relicSlots.Add(new RelicSlotView(1, null, null));
            _storedItems.Add("item.work-gloves");
            _storedItems.Add("item.old-guitar");

            // Goals
            _goals.Add(new GoalView("goal.homeward", "goal.homeward", "已达成：在老屋窗台认出了针线盒", true));
            _goals.Add(new GoalView("goal.harvest-memory", "goal.harvest-memory", "已取得工作手套，待装备入槽 (2/3)", false));
            _goals.Add(new GoalView("goal.unfinished-song", "goal.unfinished-song", "寻觅旧秋千旁的残缺吉他 (0/3)", false));

            // Upgrades
            _upgrades.Add(new MetaUpgradeView("meta.mended-coat", "meta.mended-coat", 1, 5, 2));
            _upgrades.Add(new MetaUpgradeView("meta.travel-light", "meta.travel-light", 0, 5, 1));
            _upgrades.Add(new MetaUpgradeView("meta.sorted-satchel", "meta.sorted-satchel", 2, 5, 3));

            _discoveredNarratives.Add("poi.old-home");
            _discoveredNarratives.Add("item.sewing-tin");
            _discoveredNarratives.Add("item.work-gloves");
            _discoveredNarratives.Add("item.old-guitar");
            _discoveredNarratives.Add("goal.homeward");
            _discoveredNarratives.Add("meta.mended-coat");
        }

        public void AddNarrative(NarrativeEntry entry)
        {
            _narratives[entry.Id] = entry;
        }

        // --- IHomecomingViewSource ---
        public IReadOnlyList<PoiView> ReadNearby() => _nearbyPois.AsReadOnly();
        public RelicInventoryView ReadRelics() => new RelicInventoryView(RelicCapacity, _relicSlots, _storedItems);
        public IReadOnlyList<GoalView> ReadGoals() => _goals.AsReadOnly();
        public HomecomingProfileView ReadProfile() => new HomecomingProfileView(ProfileXp, ProfileLevel, AvailablePoints, _upgrades, _discoveredNarratives);

        public NarrativeEntry ReadNarrative(string id)
        {
            if (id != null && _narratives.TryGetValue(id, out var entry))
                return entry;
            return null;
        }

        // --- IHomecomingCommands ---
        public bool TryInteract(Guid runId, string siteId)
        {
            if (runId != CurrentRunId)
            {
                LastFeedbackMessage = "操作拒绝：运行局标识不匹配";
                return false;
            }
            LastFeedbackMessage = "已交互：拾取遗物并认出旧地";
            return true;
        }

        public bool TryEquip(Guid runId, string itemId, int slot)
        {
            if (runId != CurrentRunId)
            {
                LastFeedbackMessage = "装备失败：非当前探索局";
                return false;
            }
            if (slot < 0 || slot >= RelicCapacity)
            {
                LastFeedbackMessage = "装备失败：槽位未解锁或超出容量";
                return false;
            }

            // Remove item from stored
            _storedItems.Remove(itemId);

            // If slot already occupied, unequip old to stored
            var existing = _relicSlots.Find(s => s.Slot == slot);
            if (!string.IsNullOrEmpty(existing.ItemId))
            {
                _storedItems.Add(existing.ItemId);
                _relicSlots.Remove(existing);
            }

            _relicSlots.Add(new RelicSlotView(slot, itemId, itemId));
            LastFeedbackMessage = $"已装备：{itemId} 到槽位 {slot + 1}";
            return true;
        }

        public bool TryUnequip(Guid runId, int slot)
        {
            if (runId != CurrentRunId)
            {
                LastFeedbackMessage = "卸装失败：非当前探索局";
                return false;
            }
            var idx = _relicSlots.FindIndex(s => s.Slot == slot);
            if (idx >= 0)
            {
                var item = _relicSlots[idx].ItemId;
                _relicSlots.RemoveAt(idx);
                if (!string.IsNullOrEmpty(item))
                    _storedItems.Add(item);
                LastFeedbackMessage = $"已收纳槽位 {slot + 1} 的遗物";
                return true;
            }
            LastFeedbackMessage = "卸装失败：指定槽位为空";
            return false;
        }

        public bool TryPurchaseUpgrade(string upgradeId)
        {
            if (SimulateSaveFailure)
            {
                LastFeedbackMessage = "保存失败：磁盘写入未确认，升级回滚，请重试";
                return false;
            }

            var idx = _upgrades.FindIndex(u => u.Id == upgradeId);
            if (idx < 0)
            {
                LastFeedbackMessage = "购买失败：未找到指定升级";
                return false;
            }

            var up = _upgrades[idx];
            if (up.Rank >= up.MaxRank)
            {
                LastFeedbackMessage = "购买失败：已达到最高等级";
                return false;
            }
            if (AvailablePoints < up.NextCost)
            {
                LastFeedbackMessage = "修补点不足，无法升级";
                return false;
            }

            AvailablePoints -= up.NextCost;
            int nextCost = (up.Rank + 1 >= up.MaxRank) ? 0 : up.NextCost + 1;
            _upgrades[idx] = new MetaUpgradeView(up.Id, up.NarrativeId, up.Rank + 1, up.MaxRank, nextCost);
            LastFeedbackMessage = $"升级成功：{up.Id} 提升至 {up.Rank + 1} 级";
            return true;
        }

        public bool TryOpenJournal()
        {
            IsJournalOpen = true;
            return true;
        }

        public void CloseJournal()
        {
            IsJournalOpen = false;
        }
    }
}
