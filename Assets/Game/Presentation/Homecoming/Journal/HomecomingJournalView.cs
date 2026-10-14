using System;
using System.Collections.Generic;
using UnityEngine;
using Waffle.Homecoming;

namespace Reverb.Drafts.RC01.Presentation.Homecoming.Journal
{
    public enum JournalTab
    {
        Goals,      // 风味目标
        Relics,     // 遗物行囊（2~4槽）
        Archive,    // 记忆图鉴与叙事详情
        Profile     // 局外成长与修补升级
    }

    /// <summary>
    /// HC-D: 風味目标、叙事查看与成长界面呈现组件。
    /// 遵循墨蓝 / 旧黄铜 / 粉白、尚有余温的故乡美学基调；
    /// 严格只读调用 IHomecomingViewSource，命令通过 IHomecomingCommands 提交；
    /// 适配 1280x720 与 1600x900 布局，长文本支持滚动查看，F8 观景模式完全隐藏。
    /// </summary>
    public sealed class HomecomingJournalView : MonoBehaviour
    {
        [Header("宿主与数据源绑定（正式由 Codex 注入，空时自动使用 Mock 预览）")]
        public Component viewSourceComponent;
        public Component commandsComponent;

        [Header("调试与预览模式")]
        public bool isPreviewMode = false;
        public bool scenicView = false;
        public bool showPoiPrompt = true;

        private IHomecomingViewSource _viewSource;
        private IHomecomingCommands _commands;
        private MockHomecomingHost _mockHost;

        private bool _isOpen = false;
        private JournalTab _currentTab = JournalTab.Goals;
        private string _selectedNarrativeId = null;
        private Vector2 _scrollNarrativeList = Vector2.zero;
        private Vector2 _scrollNarrativeDetail = Vector2.zero;
        private Vector2 _scrollGoals = Vector2.zero;
        private Vector2 _scrollProfile = Vector2.zero;

        private string _statusFeedback = "";
        private float _statusFeedbackTimer = 0f;

        // Visual Palette: Ink / Brass / PowderWhite / Muted / Jade / Coral
        private static readonly Color ColorInk = new Color(0.035f, 0.065f, 0.085f, 0.94f);
        private static readonly Color ColorInkDark = new Color(0.018f, 0.035f, 0.045f, 0.96f);
        private static readonly Color ColorBrass = new Color(0.88f, 0.72f, 0.42f, 1.0f);
        private static readonly Color ColorPowderWhite = new Color(0.94f, 0.95f, 0.96f, 1.0f);
        private static readonly Color ColorMuted = new Color(0.58f, 0.68f, 0.70f, 1.0f);
        private static readonly Color ColorJade = new Color(0.38f, 0.82f, 0.74f, 1.0f);
        private static readonly Color ColorWarnRed = new Color(0.92f, 0.32f, 0.28f, 1.0f);

        private Font _font;
        private GUIStyle _styleTitle;
        private GUIStyle _styleSubtitle;
        private GUIStyle _styleBody;
        private GUIStyle _styleBodyLong;
        private GUIStyle _styleSmall;
        private GUIStyle _styleTiny;
        private GUIStyle _styleTabButton;
        private GUIStyle _styleTabButtonActive;
        private GUIStyle _styleActionButton;
        private GUIStyle _styleCard;

        private Texture2D _texWhite;

        private void Awake()
        {
            InitializeDataSources();
        }

        private void InitializeDataSources()
        {
            if (viewSourceComponent is IHomecomingViewSource vs)
                _viewSource = vs;
            if (commandsComponent is IHomecomingCommands cmd)
                _commands = cmd;

            if (_viewSource == null || _commands == null)
            {
                _mockHost = new MockHomecomingHost();
                _viewSource = _mockHost;
                _commands = _mockHost;
                isPreviewMode = true;
            }
        }

        private void Update()
        {
            // Toggle Journal with 'J' key
            if (Input.GetKeyDown(KeyCode.J))
            {
                ToggleJournal();
            }

            // Close on Escape if open
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseJournal();
            }

            // F8 Scenic View toggle
            if (Input.GetKeyDown(KeyCode.F8))
            {
                scenicView = !scenicView;
            }

            if (_statusFeedbackTimer > 0f)
            {
                _statusFeedbackTimer -= Time.unscaledDeltaTime;
                if (_statusFeedbackTimer <= 0f)
                    _statusFeedback = "";
            }
        }

        public void ToggleJournal()
        {
            if (_isOpen)
                CloseJournal();
            else
                OpenJournal();
        }

        public void OpenJournal()
        {
            if (_commands != null && _commands.TryOpenJournal())
            {
                _isOpen = true;
            }
            else
            {
                // Fallback for mock preview
                _isOpen = true;
            }
        }

        public void CloseJournal()
        {
            _isOpen = false;
            if (_commands != null)
                _commands.CloseJournal();
        }

        private void EnsureStyles()
        {
            if (_styleBody != null) return;

            _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "SimHei", "Arial" }, 16);
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _texWhite = Texture2D.whiteTexture;

            _styleTitle = new GUIStyle(GUI.skin.label)
            {
                font = _font,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = ColorBrass }
            };

            _styleSubtitle = new GUIStyle(GUI.skin.label)
            {
                font = _font,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = ColorPowderWhite }
            };

            _styleBody = new GUIStyle(GUI.skin.label)
            {
                font = _font,
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = ColorPowderWhite }
            };

            _styleBodyLong = new GUIStyle(GUI.skin.label)
            {
                font = _font,
                fontSize = 14,
                wordWrap = true,
                lineSpacing = 1.3f,
                normal = { textColor = new Color(0.85f, 0.88f, 0.90f) }
            };

            _styleSmall = new GUIStyle(GUI.skin.label)
            {
                font = _font,
                fontSize = 12,
                normal = { textColor = ColorMuted }
            };

            _styleTiny = new GUIStyle(GUI.skin.label)
            {
                font = _font,
                fontSize = 11,
                normal = { textColor = ColorMuted }
            };

            _styleTabButton = new GUIStyle(GUI.skin.button)
            {
                font = _font,
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ColorMuted }
            };

            _styleTabButtonActive = new GUIStyle(GUI.skin.button)
            {
                font = _font,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ColorBrass }
            };

            _styleActionButton = new GUIStyle(GUI.skin.button)
            {
                font = _font,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ColorPowderWhite }
            };
        }

        private void OnGUI()
        {
            if (scenicView) return;
            EnsureStyles();

            // 1. Draw In-Game Nearby POI Prompt if active and not in journal
            if (!_isOpen && showPoiPrompt)
            {
                DrawPoiPromptOverlay();
                DrawJournalShortcutHint();
            }

            // 2. Draw Full Journal Dialog when open
            if (_isOpen)
            {
                DrawJournalModal();
            }
        }

        private void DrawJournalShortcutHint()
        {
            // Discreet prompt at top right
            Rect rect = new Rect(Screen.width - 240, 16, 220, 28);
            DrawSolidRect(rect, ColorInk);
            DrawBorder(rect, ColorBrass, 1);
            GUI.Label(new Rect(rect.x + 8, rect.y + 4, rect.width - 16, 20), "按 [J] 键翻开故乡回忆日志", _styleSmall);
        }

        private void DrawPoiPromptOverlay()
        {
            if (_viewSource == null) return;
            var pois = _viewSource.ReadNearby();
            if (pois == null || pois.Count == 0) return;

            // Display nearest available POI
            var poi = pois[0];
            if (poi.Status == PoiStatus.Unseen) return;

            float width = 380f;
            float height = 76f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 180f; // Above weapon bar, avoids center card
            Rect panel = new Rect(x, y, width, height);

            DrawSolidRect(panel, ColorInkDark);
            DrawBorder(panel, poi.Status == PoiStatus.Available ? ColorBrass : ColorMuted, 2);

            string title = "已发现旧地：" + poi.Site.TemplateId;
            if (_viewSource.ReadNarrative(poi.Site.NarrativeId) is NarrativeEntry ne)
                title = ne.Title;

            GUI.Label(new Rect(x + 14, y + 8, width - 28, 22), title, _styleSubtitle);
            GUI.Label(new Rect(x + 14, y + 30, width - 28, 20), poi.RewardDescription ?? "靠近即可自动收取并认出旧地", _styleSmall);

            if (poi.Status == PoiStatus.Available)
            {
                Rect btn = new Rect(x + width - 110, y + 42, 96, 24);
                if (GUI.Button(btn, "收取 [E]", _styleActionButton))
                {
                    _commands?.TryInteract(Guid.Empty, poi.Site.SiteId);
                }
            }
            else if (poi.Status == PoiStatus.Claimed)
            {
                GUI.Label(new Rect(x + width - 110, y + 42, 96, 24), "✓ 已收取", _styleSmall);
            }
        }

        private void DrawJournalModal()
        {
            // Dim background
            DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, 0.65f));

            // Dialog panel
            float modalW = Mathf.Clamp(Screen.width * 0.78f, 860f, 1180f);
            float modalH = Mathf.Clamp(Screen.height * 0.82f, 540f, 760f);
            float modalX = (Screen.width - modalW) * 0.5f;
            float modalY = (Screen.height - modalH) * 0.5f;
            Rect modalRect = new Rect(modalX, modalY, modalW, modalH);

            DrawSolidRect(modalRect, ColorInk);
            DrawBorder(modalRect, ColorBrass, 2);

            // Header Title
            GUI.Label(new Rect(modalX + 24, modalY + 16, 400, 32), "故乡记忆与行囊日志", _styleTitle);

            // Close Button
            if (GUI.Button(new Rect(modalX + modalW - 90, modalY + 16, 68, 28), "关闭 [Esc]", _styleActionButton))
            {
                CloseJournal();
            }

            // Tab Buttons
            float tabY = modalY + 54;
            float tabW = (modalW - 48) / 4.0f;
            string[] tabNames = { "风味目标", "遗物行囊 (2~4槽)", "记忆图鉴与叙事", "局外成长与修补" };

            for (int i = 0; i < 4; i++)
            {
                Rect tabRect = new Rect(modalX + 24 + i * tabW, tabY, tabW - 4, 32);
                bool isActive = ((int)_currentTab == i);
                if (GUI.Button(tabRect, tabNames[i], isActive ? _styleTabButtonActive : _styleTabButton))
                {
                    _currentTab = (JournalTab)i;
                }
            }

            // Content Area
            Rect contentRect = new Rect(modalX + 24, tabY + 42, modalW - 48, modalH - 146);
            DrawSolidRect(contentRect, new Color(0.02f, 0.04f, 0.05f, 0.85f));
            DrawBorder(contentRect, new Color(0.2f, 0.3f, 0.35f, 0.6f), 1);

            switch (_currentTab)
            {
                case JournalTab.Goals:
                    DrawTabGoals(contentRect);
                    break;
                case JournalTab.Relics:
                    DrawTabRelics(contentRect);
                    break;
                case JournalTab.Archive:
                    DrawTabArchive(contentRect);
                    break;
                case JournalTab.Profile:
                    DrawTabProfile(contentRect);
                    break;
            }

            // Footer Feedback bar
            Rect footerRect = new Rect(modalX + 24, modalY + modalH - 40, modalW - 48, 28);
            if (!string.IsNullOrEmpty(_statusFeedback))
            {
                DrawSolidRect(footerRect, new Color(0.12f, 0.22f, 0.24f, 0.9f));
                GUI.Label(new Rect(footerRect.x + 12, footerRect.y + 4, footerRect.width - 24, 20), _statusFeedback, _styleSmall);
            }
            else
            {
                GUI.Label(new Rect(footerRect.x + 4, footerRect.y + 6, footerRect.width - 8, 20), "提示：使用鼠标点击或键盘操作，探索所得均来自真实确认快照", _styleTiny);
            }
        }

        private void DrawTabGoals(Rect area)
        {
            var goals = _viewSource?.ReadGoals();
            if (goals == null || goals.Count == 0)
            {
                GUI.Label(new Rect(area.x + 20, area.y + 20, 400, 30), "暂无目标数据", _styleBody);
                return;
            }

            _scrollGoals = GUI.BeginScrollView(
                new Rect(area.x + 8, area.y + 8, area.width - 16, area.height - 16),
                _scrollGoals,
                new Rect(0, 0, area.width - 36, goals.Count * 130 + 20)
            );

            float itemY = 10f;
            for (int i = 0; i < goals.Count; i++)
            {
                var g = goals[i];
                NarrativeEntry ne = _viewSource.ReadNarrative(g.NarrativeId);

                Rect card = new Rect(10, itemY, area.width - 56, 116);
                DrawSolidRect(card, g.Completed ? new Color(0.08f, 0.16f, 0.14f, 0.9f) : new Color(0.04f, 0.08f, 0.10f, 0.9f));
                DrawBorder(card, g.Completed ? ColorJade : ColorBrass, 1);

                string title = (ne != null) ? ne.Title : g.Id;
                string badge = g.Completed ? "【已达成】" : "【进行中】";
                GUI.Label(new Rect(card.x + 14, card.y + 10, 400, 22), badge + " " + title, _styleSubtitle);

                string desc = (ne != null) ? ne.Body : "暂无愿望叙事";
                GUI.Label(new Rect(card.x + 14, card.y + 36, card.width - 28, 44), desc, _styleBody);

                GUI.Label(new Rect(card.x + 14, card.y + 84, card.width - 28, 22), "进度：" + g.ProgressText, _styleSmall);

                itemY += 126f;
            }

            GUI.EndScrollView();
        }

        private void DrawTabRelics(Rect area)
        {
            var relics = _viewSource?.ReadRelics();
            if (relics == null)
            {
                GUI.Label(new Rect(area.x + 20, area.y + 20, 400, 30), "行囊数据未就绪", _styleBody);
                return;
            }

            // Top: Equipment Slots (Capacity: 2 ~ 4)
            GUI.Label(new Rect(area.x + 16, area.y + 12, 400, 24), $"装备槽位（当前容量：{relics.Capacity} / 4 槽）", _styleSubtitle);

            float slotW = (area.width - 64) / 4.0f;
            float slotH = 130f;
            float slotY = area.y + 42;

            for (int i = 0; i < 4; i++)
            {
                Rect slotRect = new Rect(area.x + 16 + i * (slotW + 10), slotY, slotW, slotH);
                bool isUnlocked = (i < relics.Capacity);

                DrawSolidRect(slotRect, isUnlocked ? new Color(0.05f, 0.10f, 0.12f, 0.9f) : new Color(0.02f, 0.03f, 0.04f, 0.8f));
                DrawBorder(slotRect, isUnlocked ? ColorBrass : ColorMuted, 1);

                if (!isUnlocked)
                {
                    GUI.Label(new Rect(slotRect.x + 8, slotRect.y + 45, slotRect.width - 16, 30), "🔒 待探索解锁\n(发现老屋/老筒仓)", _styleTiny);
                    continue;
                }

                // Check if equipped
                RelicSlotView equipped = default;
                bool hasItem = false;
                if (relics.Slots != null)
                {
                    for (int s = 0; s < relics.Slots.Count; s++)
                    {
                        if (relics.Slots[s].Slot == i && !string.IsNullOrEmpty(relics.Slots[s].ItemId))
                        {
                            equipped = relics.Slots[s];
                            hasItem = true;
                            break;
                        }
                    }
                }

                GUI.Label(new Rect(slotRect.x + 8, slotRect.y + 8, slotRect.width - 16, 20), $"槽位 {i + 1}", _styleSmall);

                if (hasItem)
                {
                    NarrativeEntry ne = _viewSource.ReadNarrative(equipped.NarrativeId);
                    string name = ne != null ? ne.Title : equipped.ItemId;
                    GUI.Label(new Rect(slotRect.x + 8, slotRect.y + 32, slotRect.width - 16, 40), name, _styleSubtitle);

                    if (GUI.Button(new Rect(slotRect.x + 8, slotRect.y + slotH - 32, slotRect.width - 16, 24), "卸装入库", _styleActionButton))
                    {
                        if (_commands != null)
                        {
                            bool success = _commands.TryUnequip(Guid.Empty, i);
                            SetFeedback(success ? "卸装成功" : "卸装失败：请在安全或暂停时换装");
                        }
                    }
                }
                else
                {
                    GUI.Label(new Rect(slotRect.x + 8, slotRect.y + 50, slotRect.width - 16, 30), "【空槽位】", _styleMutedText());
                }
            }

            // Bottom: Stored Items
            float storeY = slotY + slotH + 18;
            GUI.Label(new Rect(area.x + 16, storeY, 400, 24), "已收纳遗物（点击装备至首个可用槽）：", _styleSubtitle);

            float itemBoxY = storeY + 30;
            if (relics.StoredItems == null || relics.StoredItems.Count == 0)
            {
                GUI.Label(new Rect(area.x + 20, itemBoxY, 400, 30), "收纳库暂无闲置遗物", _styleSmall);
            }
            else
            {
                for (int j = 0; j < relics.StoredItems.Count; j++)
                {
                    string itemId = relics.StoredItems[j];
                    NarrativeEntry ne = _viewSource.ReadNarrative(itemId);
                    string name = ne != null ? ne.Title : itemId;

                    Rect itemRect = new Rect(area.x + 16 + j * 240, itemBoxY, 220, 68);
                    DrawSolidRect(itemRect, new Color(0.06f, 0.12f, 0.14f, 0.9f));
                    DrawBorder(itemRect, ColorJade, 1);

                    GUI.Label(new Rect(itemRect.x + 8, itemRect.y + 6, itemRect.width - 16, 22), name, _styleSubtitle);

                    if (GUI.Button(new Rect(itemRect.x + 8, itemRect.y + 36, itemRect.width - 16, 24), "装备至空槽", _styleActionButton))
                    {
                        // Find first empty slot
                        int emptySlot = -1;
                        for (int k = 0; k < relics.Capacity; k++)
                        {
                            bool occupied = false;
                            for (int s = 0; s < relics.Slots.Count; s++)
                            {
                                if (relics.Slots[s].Slot == k && !string.IsNullOrEmpty(relics.Slots[s].ItemId))
                                {
                                    occupied = true;
                                    break;
                                }
                            }
                            if (!occupied)
                            {
                                emptySlot = k;
                                break;
                            }
                        }

                        if (emptySlot >= 0 && _commands != null)
                        {
                            bool success = _commands.TryEquip(Guid.Empty, itemId, emptySlot);
                            SetFeedback(success ? $"已装备 {name} 至槽位 {emptySlot + 1}" : "装备失败：非安全探索状态");
                        }
                        else
                        {
                            SetFeedback("装备失败：无可用空槽位，请先卸装现有遗物");
                        }
                    }
                }
            }
        }

        private void DrawTabArchive(Rect area)
        {
            var profile = _viewSource?.ReadProfile();
            var discovered = profile?.DiscoveredNarrativeIds ?? Array.Empty<string>();

            float splitW = 280f;

            // Left List: Discovered Narrative Items
            Rect listRect = new Rect(area.x + 8, area.y + 8, splitW, area.height - 16);
            DrawSolidRect(listRect, new Color(0.03f, 0.05f, 0.06f, 0.8f));
            DrawBorder(listRect, new Color(0.2f, 0.3f, 0.35f, 0.5f), 1);

            _scrollNarrativeList = GUI.BeginScrollView(
                new Rect(listRect.x + 4, listRect.y + 4, listRect.width - 8, listRect.height - 8),
                _scrollNarrativeList,
                new Rect(0, 0, listRect.width - 28, discovered.Count * 40 + 10)
            );

            for (int i = 0; i < discovered.Count; i++)
            {
                string id = discovered[i];
                NarrativeEntry ne = _viewSource.ReadNarrative(id);
                string title = ne != null ? ne.Title : id;

                Rect btnRect = new Rect(4, i * 40 + 4, listRect.width - 36, 32);
                bool isSelected = (_selectedNarrativeId == id);
                if (GUI.Button(btnRect, title, isSelected ? _styleTabButtonActive : _styleTabButton))
                {
                    _selectedNarrativeId = id;
                }
            }
            GUI.EndScrollView();

            // Right Detail: Full narrative body with scrolling
            Rect detailRect = new Rect(area.x + splitW + 18, area.y + 8, area.width - splitW - 26, area.height - 16);
            DrawSolidRect(detailRect, new Color(0.02f, 0.04f, 0.05f, 0.9f));
            DrawBorder(detailRect, ColorBrass, 1);

            if (string.IsNullOrEmpty(_selectedNarrativeId))
            {
                if (discovered.Count > 0)
                    _selectedNarrativeId = discovered[0];
            }

            if (!string.IsNullOrEmpty(_selectedNarrativeId))
            {
                NarrativeEntry entry = _viewSource.ReadNarrative(_selectedNarrativeId);
                if (entry != null)
                {
                    GUI.Label(new Rect(detailRect.x + 16, detailRect.y + 14, detailRect.width - 32, 28), entry.Title, _styleTitle);
                    GUI.Label(new Rect(detailRect.x + 16, detailRect.y + 44, 200, 20), $"类别：{entry.Kind}", _styleSmall);

                    _scrollNarrativeDetail = GUI.BeginScrollView(
                        new Rect(detailRect.x + 12, detailRect.y + 70, detailRect.width - 24, detailRect.height - 82),
                        _scrollNarrativeDetail,
                        new Rect(0, 0, detailRect.width - 50, 260)
                    );

                    GUI.Label(new Rect(8, 8, detailRect.width - 66, 240), entry.Body, _styleBodyLong);

                    GUI.EndScrollView();
                }
                else
                {
                    GUI.Label(new Rect(detailRect.x + 16, detailRect.y + 20, 300, 24), "【条目内容缺失】", _styleBody);
                }
            }
        }

        private void DrawTabProfile(Rect area)
        {
            var profile = _viewSource?.ReadProfile();
            if (profile == null)
            {
                GUI.Label(new Rect(area.x + 20, area.y + 20, 400, 30), "局外档案未就绪", _styleBody);
                return;
            }

            // Top Status Bar: Level, XP, Available Points
            Rect statusBar = new Rect(area.x + 16, area.y + 12, area.width - 32, 48);
            DrawSolidRect(statusBar, new Color(0.06f, 0.12f, 0.14f, 0.9f));
            DrawBorder(statusBar, ColorBrass, 1);

            string statusText = $"故乡行者 等级: {profile.Level}   |   总余响经验: {profile.TotalExperience}   |   可用修补点: {profile.AvailablePoints}";
            GUI.Label(new Rect(statusBar.x + 16, statusBar.y + 12, statusBar.width - 32, 24), statusText, _styleSubtitle);

            // Upgrades List
            _scrollProfile = GUI.BeginScrollView(
                new Rect(area.x + 16, area.y + 70, area.width - 32, area.height - 80),
                _scrollProfile,
                new Rect(0, 0, area.width - 56, (profile.Upgrades?.Count ?? 0) * 110 + 20)
            );

            if (profile.Upgrades != null)
            {
                for (int i = 0; i < profile.Upgrades.Count; i++)
                {
                    var up = profile.Upgrades[i];
                    NarrativeEntry ne = _viewSource.ReadNarrative(up.NarrativeId);

                    Rect card = new Rect(4, i * 110 + 4, area.width - 64, 98);
                    DrawSolidRect(card, new Color(0.04f, 0.08f, 0.10f, 0.9f));
                    DrawBorder(card, ColorBrass, 1);

                    string name = ne != null ? ne.Title : up.Id;
                    GUI.Label(new Rect(card.x + 14, card.y + 10, 300, 22), $"{name} (等级: {up.Rank}/{up.MaxRank})", _styleSubtitle);

                    string desc = ne != null ? ne.Body : "暂无说明";
                    GUI.Label(new Rect(card.x + 14, card.y + 34, card.width - 150, 48), desc, _styleBody);

                    // Upgrade action button
                    if (up.Rank < up.MaxRank)
                    {
                        Rect btn = new Rect(card.x + card.width - 130, card.y + 24, 116, 32);
                        string btnText = $"修补 ({up.NextCost} 点)";
                        if (GUI.Button(btn, btnText, _styleActionButton))
                        {
                            if (_commands != null)
                            {
                                bool success = _commands.TryPurchaseUpgrade(up.Id);
                                SetFeedback(success ? $"已完成 {name} 升级" : "升级失败：修补点不足或磁盘保存拒绝");
                            }
                        }
                    }
                    else
                    {
                        GUI.Label(new Rect(card.x + card.width - 130, card.y + 30, 116, 24), "【已达上限】", _styleMutedText());
                    }
                }
            }

            GUI.EndScrollView();
        }

        private GUIStyle _styleMutedText()
        {
            return new GUIStyle(_styleSmall) { normal = { textColor = ColorMuted }, alignment = TextAnchor.MiddleCenter };
        }

        private void SetFeedback(string msg)
        {
            _statusFeedback = msg;
            _statusFeedbackTimer = 3.5f;
        }

        private void DrawSolidRect(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _texWhite);
            GUI.color = old;
        }

        private void DrawBorder(Rect r, Color c, int thickness)
        {
            DrawSolidRect(new Rect(r.x, r.y, r.width, thickness), c);
            DrawSolidRect(new Rect(r.x, r.y + r.height - thickness, r.width, thickness), c);
            DrawSolidRect(new Rect(r.x, r.y, thickness, r.height), c);
            DrawSolidRect(new Rect(r.x + r.width - thickness, r.y, thickness, r.height), c);
        }
    }
}
