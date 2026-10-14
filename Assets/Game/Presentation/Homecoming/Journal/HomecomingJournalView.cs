using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Waffle;
using Waffle.Homecoming;
using Waffle.Homecoming.Relics;

namespace Reverb.Drafts.RC01.Presentation.Homecoming.Journal
{
    public enum JournalTab
    {
        Goals,      // 风味目标
        Relics,     // 遗物行囊（2~4槽）
        Archive,    // 记忆图鉴与叙事详情
        Profile,
        Resonance   // 局外成长与修补升级
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

        GameLoop Host => viewSourceComponent as GameLoop;
        public bool IsOpen => _isOpen;
        string viewedNarrative;

        private void Start()
        {
            if (Host != null && Host.BeforeDeparture) { _isOpen = true; _currentTab = JournalTab.Profile; }
        }
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

            if ((_viewSource == null || _commands == null) && isPreviewMode)
            {
                _mockHost = new MockHomecomingHost();
                _viewSource = _mockHost;
                _commands = _mockHost;
                isPreviewMode = true;
            }
        }

        private void Update()
        {
            if (Host != null) _isOpen = Host.HomecomingJournalOpen;
            // Toggle Journal with 'J' key
            if ((Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame))
            {
                ToggleJournal();
            }

            // Close on Escape if open
            if (_isOpen && (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                CloseJournal();
            }

            // F8 Scenic View toggle
            if ((Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame))
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
                SetFeedback("现在无法打开日志，请先完成成长选择");
            }
        }

        public void CloseJournal()
        {
            if (Host != null && Host.BeforeDeparture) return;
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
            if (scenicView && Host != null && !Host.BeforeDeparture && !Host.Sim.State.Phase.IsTerminal()) return;
            if (_viewSource == null || _commands == null) return;
            EnsureStyles();
            var oldMatrix = GUI.matrix; ImguiScale.Apply();

            // 1. Draw In-Game Nearby POI Prompt if active and not in journal
            if (!_isOpen && showPoiPrompt)
            {
                DrawPoiPromptOverlay();
                DrawJournalShortcutHint();
                DrawFieldDirections();
                DrawResonanceOverlay();
            }

            // 2. Draw Full Journal Dialog when open
            if (_isOpen)
            {
                DrawJournalModal();
            }
            GUI.matrix = oldMatrix;
        }

        private void DrawFieldDirections()
        {
            if (Host?.Sim?.Places == null || Host.Sim.State.Phase != RunPhase.Playing) return;
            float y = ImguiScale.Height - 300;
            bool hasGuideBackground = false;
            foreach (var site in Waffle.Homecoming.Places.HomecomingPlacesLayout.UniqueSites(Host.Sim.HomecomingWorldSeed))
            {
                if (Host.Sim.Places.StatusOf(site.SiteId) == PoiStatus.Claimed) continue;
                var position = new Vector3(site.Position.X, 0, site.Position.Y);
                float distance = Vector2.Distance(new Vector2(Host.Sim.PlayerX, Host.Sim.PlayerZ), new Vector2(site.Position.X, site.Position.Y));
                if (!hasGuideBackground) { DrawSolidRect(new Rect(16, y - 4, 290, 80), ColorInk); hasGuideBackground = true; }
                Vector3 delta = position - new Vector3(Host.Sim.PlayerX, 0, Host.Sim.PlayerZ);
                Vector2 direction = Camera.main != null ? new Vector2(Vector3.Dot(delta, Camera.main.transform.right), Vector3.Dot(delta, Camera.main.transform.up)) : new Vector2(delta.x, delta.z);
                string[] arrows = { "→", "↗", "↑", "↖", "←", "↙", "↓", "↘" };
                int index = ((Mathf.RoundToInt(Mathf.Atan2(direction.y, direction.x) * 4 / Mathf.PI) % 8) + 8) % 8;
                string name = _viewSource.ReadNarrative(site.NarrativeId)?.Title ?? "田间旧地";
                GUI.Label(new Rect(24, y, 270, 24), arrows[index] + "  " + name + " · " + Mathf.RoundToInt(distance) + " 米", _styleSmall); y += 24;
            }
        }

        private void DrawResonanceOverlay()
        {
            if (Host?.Sim?.Relics == null || Host.BeforeDeparture || Host.Sim.State.Phase.IsTerminal()) return;
            var sim = Host.Sim;
            Rect panel = new Rect(ImguiScale.Width - 240, 154, 220, 62);
            DrawSolidRect(panel, ColorInk);
            GUI.Label(new Rect(panel.x + 8, panel.y + 4, 204, 22), "碎音 " + sim.FieldFragments + "/3  ·  " + ResonancePatterns.Name(sim.Resonance), _styleBody);
            string hint = sim.ReturnNeedleActive ? "留针" + Mathf.CeilToInt(sim.ReturnNeedleUntil - sim.State.Now) + "秒 · 共鸣" + sim.Relics.DodgeEchoRemaining(sim.State.Now).ToString("F1") + "秒"
                : sim.HearthGuard ? "灯下的一针 · 可挡一次伤害" : "拾取6经验得1碎音 · 受伤散落1份";
            GUI.Label(new Rect(panel.x + 8, panel.y + 30, 204, 24), hint, _styleTiny);
        }

        private void DrawTabResonance(Rect area)
        {
            if (Host?.Sim?.Relics == null) { GUI.Label(new Rect(area.x + 16, area.y + 16, area.width - 32, 40), "出发后在旧地调弦", _styleBody); return; }
            var sim = Host.Sim;
            GUI.Label(new Rect(area.x + 16, area.y + 10, area.width - 140, 24), "共鸣架 · 三选一，共鸣共享8秒冷却", _styleSubtitle);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && Host.CanRetuneHomecoming;
            if (GUI.Button(new Rect(area.xMax - 112, area.y + 10, 96, 26), "回到独奏", _styleActionButton)) Host.TryRetuneHomecoming(ResonancePattern.Unlinked);
            GUI.enabled = previousEnabled;
            var patterns = new[] { ResonancePattern.Backstitch, ResonancePattern.ThreadLure, ResonancePattern.HearthGather };
            for (int i = 0; i < patterns.Length; i++)
            {
                var pattern = patterns[i]; bool available = ResonancePatterns.Available(sim.Relics, pattern);
                Rect card = new Rect(area.x + 12, area.y + 44 + i * 88, area.width - 24, 80);
                DrawSolidRect(card, new Color(.04f, .08f, .10f, .9f)); DrawBorder(card, sim.Resonance == pattern ? ColorJade : ColorMuted, 1);
                GUI.Label(new Rect(card.x + 12, card.y + 7, card.width - 130, 22), ResonancePatterns.Name(pattern) + " · " + ResonancePatterns.Recipe(pattern), _styleSubtitle);
                GUI.Label(new Rect(card.x + 12, card.y + 31, card.width - 142, 44), ResonancePatterns.Description(pattern), _styleBody);
                GUI.enabled = previousEnabled && available && Host.CanRetuneHomecoming;
                if (GUI.Button(new Rect(card.xMax - 116, card.y + 24, 100, 30), sim.Resonance == pattern ? "重系此调" : available ? "调弦" : "需装备两件", _styleActionButton))
                    SetFeedback(Host.TryRetuneHomecoming(pattern) ? "已调为" + ResonancePatterns.Name(pattern) + "，共鸣冷却保持" : "请靠近已熟悉的旧地再调弦");
                GUI.enabled = previousEnabled;
            }
            float y = area.y + 316;
            GUI.Label(new Rect(area.x + 16, y, area.width - 32, 22), "田间碎音 " + sim.FieldFragments + "/3 · 已拾取经验不消耗 · 受伤散落1份", _styleBody);
            GUI.Label(new Rect(area.x + 16, y + 26, area.width - 174, 48), Host.CanRetuneHomecoming
                ? "返灯：交回3碎音，补回15%生命并缝一针护衣。30秒后可再来，护衣最多一针。" : "靠近已收取的老屋/筒仓/吉他，或已发现的田间旧地6米内，可以换调与返灯。", _styleSmall);
            GUI.enabled = previousEnabled && Host.CanRetuneHomecoming && sim.FieldFragments >= 3 && sim.State.Now >= sim.CampReadyAt && (!sim.HearthGuard || sim.State.Hp < sim.State.MaxHp);
            string campLabel = sim.State.Now < sim.CampReadyAt ? "返灯 · " + Mathf.CeilToInt(sim.CampReadyAt - sim.State.Now) + "秒" : "返灯 · 3碎音";
            if (GUI.Button(new Rect(area.xMax - 154, y + 26, 138, 32), campLabel, _styleActionButton)) SetFeedback(Host.TryRekindleHomecoming() ? "旧地亮起了灯，衣襟里留下一针" : "此刻无法返灯");
            GUI.enabled = previousEnabled;
        }

        private void DrawJournalShortcutHint()
        {
            // Discreet prompt at top right
            Rect rect = new Rect(ImguiScale.Width - 240, 122, 220, 28);
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
            float x = (ImguiScale.Width - width) * 0.5f;
            float y = ImguiScale.Height - 180f; // Above weapon bar, avoids center card
            Rect panel = new Rect(x, y, width, height);

            DrawSolidRect(panel, ColorInkDark);
            DrawBorder(panel, poi.Status == PoiStatus.Available ? ColorBrass : ColorMuted, 2);

            string title = "已发现旧地：" + poi.Site.TemplateId;
            if (_viewSource.ReadNarrative(poi.Site.NarrativeId) is NarrativeEntry ne)
                title = ne.Title;

            GUI.Label(new Rect(x + 14, y + 8, width - 28, 22), title, _styleSubtitle);
            string service = Host != null && Host.CanRetuneHomecoming ? "J → 旧物共鸣：调弦或携3碎音返灯" : null;
            GUI.Label(new Rect(x + 14, y + 30, width - 28, 20), service ?? poi.RewardDescription ?? "靠近即可自动收取并认出旧地", _styleSmall);

            if (poi.Status == PoiStatus.Available)
            {
                Rect btn = new Rect(x + width - 110, y + 42, 96, 24);
                if (GUI.Button(btn, "收取 [E]", _styleActionButton))
                {
                    _commands?.TryInteract((Host?.Sim?.RunId ?? Guid.Empty), poi.Site.SiteId);
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
            DrawSolidRect(new Rect(0, 0, ImguiScale.Width, ImguiScale.Height), new Color(0, 0, 0, 0.65f));

            // Dialog panel
            float modalW = Mathf.Clamp(ImguiScale.Width * 0.78f, 860f, 1180f);
            float modalH = Mathf.Clamp(ImguiScale.Height * 0.82f, 540f, 760f);
            float modalX = (ImguiScale.Width - modalW) * 0.5f;
            float modalY = (ImguiScale.Height - modalH) * 0.5f;
            Rect modalRect = new Rect(modalX, modalY, modalW, modalH);

            DrawSolidRect(modalRect, ColorInk);
            DrawBorder(modalRect, ColorBrass, 2);

            // Header Title
            GUI.Label(new Rect(modalX + 24, modalY + 16, 400, 32), "故乡记忆与行囊日志", _styleTitle);

            // Close Button
            if (GUI.Button(new Rect(modalX + modalW - 90, modalY + 16, 68, 28), (Host != null && Host.BeforeDeparture) ? "出发" : "关闭 [Esc]", _styleActionButton))
            {
                if (Host != null && Host.BeforeDeparture) { if (Host.StartHomecomingJourney()) _isOpen = false; }
                else CloseJournal();
            }
            if (Host != null && Host.Sim.State.Phase.IsTerminal() && GUI.Button(new Rect(modalX + modalW - 230, modalY + 16, 128, 28), "再沿田间路出发", _styleActionButton)) Host.Restart();

            // Tab Buttons
            float tabY = modalY + 54;
            float tabW = (modalW - 48) / 5.0f;
            string[] tabNames = { "风味目标", "遗物行囊 (2~4槽)", "记忆图鉴与叙事", "局外成长与修补", "旧物共鸣" };

            for (int i = 0; i < tabNames.Length; i++)
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
                case JournalTab.Resonance:
                    DrawTabResonance(contentRect);
                    break;
                case JournalTab.Profile:
                    DrawTabProfile(contentRect);
                    break;
            }

            // Footer Feedback bar
            Rect footerRect = new Rect(modalX + 24, modalY + modalH - 40, modalW - 48, 28);
            if (Host != null)
            {
                string saved = Host.HomecomingSaveMessage;
                if (Host.HomecomingReceipt.HasValue) { var receipt = Host.HomecomingReceipt.Value; saved = "此行带回 " + receipt.ExperienceGained + " 余响经验，" + receipt.PointsGained + " 修补点 · " + saved; }
                GUI.Label(new Rect(footerRect.x + 4, footerRect.y + 4, footerRect.width - 100, 22), string.IsNullOrEmpty(_statusFeedback) ? saved : _statusFeedback, _styleSmall);
                if (Host.HomecomingSaveFailed && GUI.Button(new Rect(footerRect.xMax - 88, footerRect.y, 84, 26), "重试保存", _styleActionButton)) Host.FlushHomecoming();
            }
            else if (!string.IsNullOrEmpty(_statusFeedback))
            {
                DrawSolidRect(footerRect, new Color(0.12f, 0.22f, 0.24f, 0.9f));
                GUI.Label(new Rect(footerRect.x + 12, footerRect.y + 4, footerRect.width - 24, 20), _statusFeedback, _styleSmall);
            }
            else
            {
                GUI.Label(new Rect(footerRect.x + 4, footerRect.y + 6, footerRect.width - 8, 20), "提示：使用鼠标点击或键盘操作，旧物的故事会留在日志里", _styleTiny);
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
                    GUI.Label(new Rect(slotRect.x + 8, slotRect.y + 45, slotRect.width - 16, 30), "待探索解锁\n(发现老屋/老筒仓)", _styleTiny);
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

                    if (Host?.Sim?.Relics != null && Host.Sim.Relics.TryReadAbility(equipped.ItemId, Host.Sim.State.Now, out var ability))
                        GUI.Label(new Rect(slotRect.x + 8, slotRect.y + 62, slotRect.width - 16, 32), equipped.ItemId == "item.old-guitar" ? "闪避残响 · 冷却 " + ability.EchoCooldownRemaining.ToString("F1") + " 秒" : equipped.ItemId == "item.sewing-tin" ? "最大生命 +8%" : "拾取范围 +15%", _styleSmall);
                    if (GUI.Button(new Rect(slotRect.x + 8, slotRect.y + slotH - 32, slotRect.width - 16, 24), "卸装入库", _styleActionButton))
                    {
                        if (_commands != null)
                        {
                            bool success = _commands.TryUnequip((Host?.Sim?.RunId ?? Guid.Empty), i);
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
                            bool success = _commands.TryEquip((Host?.Sim?.RunId ?? Guid.Empty), itemId, emptySlot);
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
                    if (Event.current.type == EventType.Repaint && viewedNarrative != entry.Id) { Host?.TryViewHomecomingNarrative(entry.Id); viewedNarrative = entry.Id; }
                    GUI.Label(new Rect(detailRect.x + 16, detailRect.y + 14, detailRect.width - 32, 28), entry.Title, _styleTitle);
                    GUI.Label(new Rect(detailRect.x + 16, detailRect.y + 44, 200, 20), "类别：" + (entry.Kind == NarrativeKind.Place ? "旧地" : entry.Kind == NarrativeKind.Item ? "随身旧物" : entry.Kind == NarrativeKind.Goal ? "故乡心愿" : entry.Kind == NarrativeKind.Combination ? "旧物共鸣" : "行前修补"), _styleSmall);

                    _scrollNarrativeDetail = GUI.BeginScrollView(
                        new Rect(detailRect.x + 12, detailRect.y + 70, detailRect.width - 24, detailRect.height - 82),
                        _scrollNarrativeDetail,
                        new Rect(0, 0, detailRect.width - 50, Mathf.Max(260, _styleBodyLong.CalcHeight(new GUIContent(entry.Body), detailRect.width - 66) + 20))
                    );

                    GUI.Label(new Rect(8, 8, detailRect.width - 66, Mathf.Max(240, _styleBodyLong.CalcHeight(new GUIContent(entry.Body), detailRect.width - 66))), entry.Body, _styleBodyLong);

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

                    string benefit = up.Id == "meta.mended-coat" ? "每级最大生命 +2%" : up.Id == "meta.travel-light" ? "每级移动速度 +1%" : "每级拾取范围 +3%";
                    string desc = benefit + " · 下次出发生效\n" + (ne != null ? ne.Body : "条目内容缺失");
                    GUI.Label(new Rect(card.x + 14, card.y + 34, card.width - 150, 48), desc, _styleBody);

                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = oldEnabled && (Host == null || Host.CanPurchaseHomecoming) && profile.AvailablePoints >= up.NextCost && up.NextCost > 0;
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
                    GUI.enabled = oldEnabled;
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
