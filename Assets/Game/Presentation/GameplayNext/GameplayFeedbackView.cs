using System;
using System.Collections.Generic;
using UnityEngine;
using Waffle;
using Waffle.Combat;
using Waffle.GameplayNext;

namespace Reverb.Drafts.RC01.Presentation.GameplayNext
{
    /// <summary>
    /// AI-D 核心反馈与危险可读性呈现组件。
    /// 遵循 GP00 契约与严格规范：
    /// 1. 不扣血、不产生伤害、不发奖、不修改 GameLoop。
    /// 2. 所有正式显示严格来自宿主 IGameplayViewSource (RunGameplayView & RunOutcome)。
    /// 3. 支持 isPreviewMode 在隔离测试场景中接收外部预览快照。
    /// 4. 升级与暂停时冻结动画与倒计时。
    /// 5. 适配 1280x720 与 1600x900 布局，绝不遮挡三选一卡片（中心大面板）与武器栏（底部）。
    /// </summary>
    public sealed class GameplayFeedbackView : MonoBehaviour
    {
        [Header("宿主引用")]
        public GameLoop loop;
        public DangerIndicatorPool dangerPool;

        [Header("预览模式开关（仅在隔离测试场景或未接线时生效）")]
        public bool isPreviewMode = false;

        // UI 风格定义
        static readonly Color Ink = new Color(.035f, .065f, .075f, .94f);
        static readonly Color Jade = new Color(.44f, .87f, .78f);
        static readonly Color Muted = new Color(.62f, .72f, .72f);
        static readonly Color Gold = new Color(.95f, .77f, .43f);
        static readonly Color DangerOrange = new Color(1.0f, .45f, .15f);
        static readonly Color BossHealthRed = new Color(.92f, .24f, .28f);

        Font font;
        GUIStyle titleStyle, bodyStyle, smallStyle, tinyStyle, centerStyle, buttonStyle;
        Texture2D whiteTex;

        // 当前只读状态（来自宿主聚合或预览）
        readonly List<DangerAreaDescriptor> activeDangers = new List<DangerAreaDescriptor>(16);
        ObjectiveVisualSnapshot? currentObjective;
        BossVisualSnapshot? currentBoss;
        RunOutcomeVisualSnapshot? currentOutcome;

        void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (dangerPool == null) dangerPool = GetComponentInChildren<DangerIndicatorPool>();
            if (dangerPool == null)
            {
                var poolGo = new GameObject("DangerIndicatorPool");
                poolGo.transform.SetParent(transform, false);
                dangerPool = poolGo.AddComponent<DangerIndicatorPool>();
            }

            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }

        void InitStyles()
        {
            if (bodyStyle != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 22);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            titleStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            bodyStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 15, normal = { textColor = Color.white } };
            smallStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 13, normal = { textColor = Muted } };
            smallStyle.wordWrap = true;
            tinyStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 11, normal = { textColor = Muted } };
            centerStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            buttonStyle = new GUIStyle(GUI.skin.button) { font = font, fontSize = 16 };
        }

        static void RectFill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Panel(Rect r, Color accent)
        {
            RectFill(new Rect(r.x + 2, r.y + 3, r.width, r.height), new Color(0, 0, 0, .2f));
            RectFill(r, Ink);
            RectFill(new Rect(r.x, r.y, r.width, 2), accent);
        }

        // ================= 外部只读快照推送接口（供预览器使用） =================

        public void SetDangerDescriptors(IReadOnlyList<DangerAreaDescriptor> descriptors)
        {
            activeDangers.Clear();
            if (descriptors != null)
            {
                for (int i = 0; i < descriptors.Count; i++) activeDangers.Add(descriptors[i]);
            }
        }

        public void SetObjectiveSnapshot(ObjectiveVisualSnapshot? snapshot)
        {
            currentObjective = snapshot;
        }

        public void SetBossSnapshot(BossVisualSnapshot? snapshot)
        {
            currentBoss = snapshot;
        }

        public void SetRunOutcome(RunOutcomeVisualSnapshot? outcome)
        {
            currentOutcome = outcome;
        }

        public void ClearAll()
        {
            activeDangers.Clear();
            currentObjective = null;
            currentBoss = null;
            currentOutcome = null;
            if (dangerPool != null) dangerPool.Clear();
        }

        // ================= 帧更新与宿主只读快照接入 =================

        void LateUpdate()
        {
            // 暂停或升级三选一时，冻结动画，不推进几何更新
            if (loop != null && loop.Sim != null)
            {
                var phase = loop.Sim.State.Phase;
                if (phase == RunPhase.Paused || phase == RunPhase.LevelUp)
                {
                    return;
                }
                if (phase.IsTerminal())
                {
                    dangerPool.Clear();
                    currentObjective = null; currentBoss = null;
                    // 终局时读取真实 Outcome
                    if (!isPreviewMode)
                    {
                        ReadHostOutcome();
                    }
                    return;
                }
            }

            // 正式运行时：从宿主 IGameplayViewSource 同步真实快照
            if (!isPreviewMode && loop != null)
            {
                SyncFromHost();
            }

            // 同步物理/世界空间中的危险区域网格
            dangerPool.SyncIndicators(activeDangers);
        }

        /// <summary>
        /// 从 GP00 的 IGameplayViewSource 提取真实只读快照
        /// </summary>
        void SyncFromHost()
        {
            var viewSource = loop as IGameplayViewSource;
            var view = viewSource?.ReadGameplayView();
            if (view == null) return;

            // 1. 同步危险区域（来自宿主 view.Attacks 与 view.SpawnWarnings）
            activeDangers.Clear();
            float now = view.Now;

            // 1.1 正在生效或充能的攻击危险区
            if (view.Attacks != null)
            {
                for (int i = 0; i < view.Attacks.Count; i++)
                {
                    var a = view.Attacks[i];
                    var intent = a.Attack;
                    var shape = intent.Shape;

                    DangerShapeType shapeType;
                    switch (shape.Kind)
                    {
                        case AttackShapeKind.Capsule:
                            shapeType = DangerShapeType.Capsule;
                            break;
                        case AttackShapeKind.Annulus:
                            shapeType = DangerShapeType.Annulus;
                            break;
                        case AttackShapeKind.Circle:
                        default:
                            shapeType = DangerShapeType.Circle;
                            break;
                    }

                    DangerVisualState state;
                    float progress;
                    float remaining;

                    if (now < intent.ActivateAt)
                    {
                        state = DangerVisualState.Telegraphing;
                        float totalWarn = Mathf.Max(0.001f, intent.ActivateAt - a.CreatedAt);
                        progress = Mathf.Clamp01((now - a.CreatedAt) / totalWarn);
                        remaining = intent.ActivateAt - now;
                    }
                    else if (now <= intent.EndAt)
                    {
                        state = a.Spent ? DangerVisualState.Resolved : DangerVisualState.Active;
                        progress = 1.0f;
                        remaining = intent.EndAt - now;
                    }
                    else
                    {
                        state = DangerVisualState.Resolved;
                        progress = 1.0f;
                        remaining = 0;
                    }

                    Vector3 center = new Vector3(shape.Center.X, 0, shape.Center.Y);
                    Vector3 end = new Vector3(shape.End.X, 0, shape.End.Y);
                    Vector3 dir = (end - center);
                    float length = dir.magnitude;
                    if (length > 0.001f) dir /= length;
                    else dir = Vector3.forward;

                    // 计算扩散环在当前的实际外径与内径
                    float currentRadius = shape.Radius;
                    float currentInnerRadius = shape.InnerRadius;
                    if (shape.Kind == AttackShapeKind.Annulus && shape.ExpansionSpeed > 0 && now > intent.ActivateAt)
                    {
                        float age = now - intent.ActivateAt;
                        currentRadius += shape.ExpansionSpeed * age;
                        currentInnerRadius += shape.ExpansionSpeed * age;
                    }

                    activeDangers.Add(new DangerAreaDescriptor
                    {
                        AttackId = (int)intent.Sequence,
                        Shape = shapeType,
                        State = state,
                        Position = center,
                        Direction = dir,
                        Radius = currentRadius,
                        InnerRadius = currentInnerRadius,
                        Length = length,
                        Progress = progress,
                        RemainingTime = remaining,
                        SourceLabel = $"Seq#{intent.Sequence}"
                    });
                }
            }

            // 1.2 出生预警（如冲锋者、落波者等生成预警）
            if (view.SpawnWarnings != null)
            {
                for (int i = 0; i < view.SpawnWarnings.Count; i++)
                {
                    var sw = view.SpawnWarnings[i];
                    float totalWarn = Mathf.Max(0.001f, sw.Spawn.WarningSeconds);
                    float progress = Mathf.Clamp01(1.0f - (sw.SpawnAt - now) / totalWarn);
                    float remaining = Mathf.Max(0, sw.SpawnAt - now);

                    activeDangers.Add(new DangerAreaDescriptor
                    {
                        AttackId = (int)sw.Key.Sequence + 10000,
                        IsSpawnWarning = true,
                        Shape = DangerShapeType.Circle,
                        State = DangerVisualState.Telegraphing,
                        Position = new Vector3(sw.Spawn.Position.X, 0, sw.Spawn.Position.Y),
                        Radius = sw.Spawn.Spec.Radius,
                        Progress = progress,
                        RemainingTime = remaining,
                        SourceLabel = $"出生预警 {sw.Spawn.Spec.ArchetypeId}"
                    });
                }
            }

            // 2. 同步地图调谐目标（来自宿主 view.Objective）
            var obj = view.Objective;
            if (obj.Status != ObjectiveStatus.None)
            {
                currentObjective = new ObjectiveVisualSnapshot
                {
                    ObjectiveId = obj.Id,
                    Title = obj.Id == "tune-1" ? "第一声学装置" : "第二声学装置",
                    RewardSummary = obj.RewardId == "D02" ? "恢复 20% 生命值" : obj.RewardId == "D03" ? "全场经验宝石磁吸" : "调谐完成",
                    WorldPosition = new Vector3(obj.Position.X, 0, obj.Position.Y),
                    Radius = obj.Radius,
                    Progress = obj.Progress,
                    RemainingTime = Mathf.Max(0, obj.ExpiresAt - now),
                    IsAvailable = (obj.Status == ObjectiveStatus.Available || obj.Status == ObjectiveStatus.Channeling),
                    IsChanneling = (obj.Status == ObjectiveStatus.Channeling),
                    IsCompleted = (obj.Status == ObjectiveStatus.Completed),
                    IsExpired = (obj.Status == ObjectiveStatus.Expired)
                };
            }
            else
            {
                currentObjective = null;
            }

            // 3. 同步首领状态（来自宿主 view.Finale）
            var finale = view.Finale;
            if (finale.Spawned && !finale.Defeated)
            {
                currentBoss = new BossVisualSnapshot
                {
                    BossId = $"Boss_{finale.Boss.Serial}",
                    BossName = "收割者",
                    CurrentMoveName = string.IsNullOrEmpty(finale.AttackLabel) ? "常规压迫" : finale.AttackLabel,
                    CurrentHp = Mathf.RoundToInt(finale.Hp),
                    MaxHp = Mathf.RoundToInt(finale.MaxHp),
                    Phase = (finale.Hp <= finale.MaxHp * 0.5f) ? 2 : 1,
                    DeadlineRemainingTime = Mathf.Max(0, finale.Deadline - now),
                    IsWarning = false,
                    IsAlive = true,
                    IsDefeated = false
                };
            }
            else
            {
                currentBoss = null;
                foreach (var warning in view.SpawnWarnings)
                    if (warning.Spawn.Spec.IsBoss)
                    {
                        currentBoss = new BossVisualSnapshot { IsWarning = true, BossName = "收割者", DeadlineRemainingTime = Mathf.Max(0f, warning.SpawnAt - now) };
                        break;
                    }
            }
        }

        void ReadHostOutcome()
        {
            var viewSource = loop as IGameplayViewSource;
            var outcome = viewSource?.ReadRunOutcome();
            if (outcome != null)
            {
                currentOutcome = new RunOutcomeVisualSnapshot
                {
                    IsVictory = (outcome.Phase == RunPhase.Victory),
                    Reason = OutcomeReason(outcome.Reason),
                    SurvivalDuration = outcome.Duration,
                    Kills = outcome.Kills,
                    Level = outcome.Level,
                    CompletedObjectives = outcome.CompletedObjectives,
                    BossDefeated = outcome.BossDefeated,
                    BossResultText = outcome.BossDefeated ? "首领已被击破" : "未击败首领",
                    WeaponsText = LoadoutText(outcome.Weapons), PassivesText = LoadoutText(outcome.Passives)
                };
            }
        }

        static string OutcomeReason(string reason)
        {
            switch (reason)
            {
                case "boss-defeated": return "终局首领已击败";
                case "boss-deadline": return "十分钟内未击败首领";
                case "hp<=0": return "生命值耗尽";
                case "timeout": return "已守住声场";
                default: return "本局战斗结束";
            }
        }

        static string LoadoutText(IReadOnlyList<Waffle.RunContent.RunContentOwnedView> items)
        {
            var parts = new List<string>(items.Count);
            foreach (var item in items) if (!string.IsNullOrEmpty(item.Id)) parts.Add(item.DisplayName + " " + item.Rank);
            return parts.Count > 0 ? string.Join(" · ", parts) : "无";
        }

        // ================= 界面绘制与安全布局 =================

        void OnGUI()
        {
            InitStyles();
            var oldMatrix = GUI.matrix;
            ImguiScale.Apply();

            float width = ImguiScale.Width;
            float height = ImguiScale.Height;

            bool isLevelUpOrPaused = false;
            if (loop != null && loop.Sim != null)
            {
                var phase = loop.Sim.State.Phase;
                isLevelUpOrPaused = (phase == RunPhase.LevelUp || phase == RunPhase.Paused);
            }

            // 升级卡片弹窗出现时，弱化或隐藏次要 HUD，防止视觉信息杂乱冲突
            if (!isLevelUpOrPaused)
            {
                // 1. 顶部首领状态条（位于顶部正中 10:00 计时器下方）
                if (currentBoss.HasValue && (currentBoss.Value.IsAlive || currentBoss.Value.IsWarning))
                {
                    DrawBossHealthBar(currentBoss.Value, width);
                }

                // 2. 地图调谐目标提示（位于屏幕左侧 24, 140 处，避开生命值条）
                if (currentObjective.HasValue && currentObjective.Value.IsAvailable)
                {
                    DrawObjectivePanel(currentObjective.Value, 24, 140);
                }

                // 3. 屏幕边缘越界方向指示器（Off-screen indicator）
                if (currentObjective.HasValue && currentObjective.Value.IsAvailable)
                {
                    DrawOffscreenIndicator(currentObjective.Value, width, height);
                }
            }

            // 4. 结算画面详细战果（仅在终局触发）
            if (currentOutcome.HasValue)
            {
                DrawOutcomeSettlement(currentOutcome.Value, width, height);
            }

            GUI.matrix = oldMatrix;
        }

        void DrawBossHealthBar(BossVisualSnapshot boss, float screenWidth)
        {
            float barWidth = 420;
            float left = (screenWidth - barWidth) / 2f;
            float top = 92; // 位于秋收时钟（22~86）正下方

            Panel(new Rect(left, top, barWidth, 68), BossHealthRed);

            if (boss.IsWarning)
            {
                GUI.Label(new Rect(left + 16, top + 6, barWidth - 32, 20), $"⚠ 警告：终局共鸣聚合体正在接近！", smallStyle);
                GUI.Label(new Rect(left + 16, top + 27, barWidth - 32, 18), $"收割者将在 {boss.DeadlineRemainingTime:F1} 秒后入场", smallStyle);
                return;
            }

            // 首领名称与招式
            GUI.Label(new Rect(left + 16, top + 5, 200, 20), $"{boss.BossName} (阶段 {boss.Phase})", bodyStyle);
            GUI.Label(new Rect(left + 220, top + 6, barWidth - 236, 18), $"招式：{boss.CurrentMoveName}", smallStyle);

            // 血条与数值
            float hpPct = (float)boss.CurrentHp / Mathf.Max(1, boss.MaxHp);
            RectFill(new Rect(left + 16, top + 28, barWidth - 32, 10), new Color(.16f, .22f, .22f));
            RectFill(new Rect(left + 16, top + 28, (barWidth - 32) * Mathf.Clamp01(hpPct), 10), BossHealthRed);
            GUI.Label(new Rect(left + barWidth - 110, top + 26, 94, 16), $"{boss.CurrentHp} / {boss.MaxHp}", tinyStyle);
            GUI.Label(new Rect(left + 16, top + 45, barWidth - 32, 18), $"击败收割者结束本局 · 剩余 {boss.DeadlineRemainingTime:F1} 秒", smallStyle);
        }

        void DrawObjectivePanel(ObjectiveVisualSnapshot obj, float left, float top)
        {
            float panelWidth = 260;
            float panelHeight = 78;

            Panel(new Rect(left, top, panelWidth, panelHeight), Gold);

            GUI.Label(new Rect(left + 14, top + 8, panelWidth - 28, 20), $"声学调谐 · {obj.Title}", bodyStyle);
            GUI.Label(new Rect(left + 14, top + 30, panelWidth - 28, 18), $"站入光圈调谐 4 秒 · {Mathf.FloorToInt(obj.Progress * 100)}% · {obj.RemainingTime:F0}s", smallStyle);

            // 进度条
            RectFill(new Rect(left + 14, top + 54, panelWidth - 28, 6), new Color(.16f, .22f, .22f));
            RectFill(new Rect(left + 14, top + 54, (panelWidth - 28) * Mathf.Clamp01(obj.Progress), 6), Gold);

            GUI.Label(new Rect(left + 14, top + 62, panelWidth - 28, 14), obj.RewardSummary, tinyStyle);
        }

        void DrawOffscreenIndicator(ObjectiveVisualSnapshot obj, float screenWidth, float screenHeight)
        {
            if (Camera.main == null) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(obj.WorldPosition);
            bool isOffscreen = screenPos.z < 0 || screenPos.x < 40 || screenPos.x > Screen.width - 40 || screenPos.y < 40 || screenPos.y > Screen.height - 40;

            if (!isOffscreen) return;

            // 转换为 IMGUI 坐标系 (Y 轴反转)
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 targetDir = (new Vector2(screenPos.x, screenPos.y) - screenCenter).normalized;
            if (screenPos.z < 0) targetDir = -targetDir;

            // 限制在安全边界内
            float margin = 50f;
            float clampX = Mathf.Clamp(screenCenter.x + targetDir.x * (screenCenter.x - margin), margin, Screen.width - margin);
            float clampY = Mathf.Clamp(screenCenter.y + targetDir.y * (screenCenter.y - margin), margin, Screen.height - margin);

            // 缩放到 ImguiScale
            float guiX = clampX * (ImguiScale.Width / Screen.width);
            float guiY = (Screen.height - clampY) * (ImguiScale.Height / Screen.height);

            Rect indicatorRect = new Rect(guiX - 35, guiY - 14, 70, 28);
            Panel(indicatorRect, Gold);
            GUI.Label(indicatorRect, "调谐 ⮞", centerStyle);
        }

        void DrawOutcomeSettlement(RunOutcomeVisualSnapshot outcome, float screenWidth, float screenHeight)
        {
            RectFill(new Rect(0, 0, screenWidth, screenHeight), new Color(0, 0, 0, 0.75f));

            float w = 600;
            float h = 400;
            Rect r = ImguiScale.Fit(new Rect((screenWidth - w) / 2f, (screenHeight - h) / 2f, w, h));

            Panel(r, outcome.IsVictory ? Jade : BossHealthRed);

            GUI.Label(new Rect(r.x + 30, r.y + 24, w - 60, 36), outcome.IsVictory ? "声场已稳定" : "本局结束", titleStyle);
            GUI.Label(new Rect(r.x + 30, r.y + 68, w - 60, 24), $"原因：{outcome.Reason}", bodyStyle);

            float lineY = r.y + 110;
            GUI.Label(new Rect(r.x + 30, lineY, 200, 22), $"存活时长: {outcome.SurvivalDuration:F1} 秒", smallStyle);
            GUI.Label(new Rect(r.x + 260, lineY, 200, 22), $"击杀总数: {outcome.Kills}", smallStyle);

            lineY += 32;
            GUI.Label(new Rect(r.x + 30, lineY, 200, 22), $"最终角色等级: Lv.{outcome.Level}", smallStyle);
            GUI.Label(new Rect(r.x + 260, lineY, 200, 22), $"完成调谐目标: {outcome.CompletedObjectives} 次", smallStyle);

            lineY += 32;
            GUI.Label(new Rect(r.x + 30, lineY, w - 60, 22), $"首领决战: {outcome.BossResultText}", smallStyle);
            GUI.Label(new Rect(r.x + 30, r.y + 208, w - 60, 42), "武器：" + outcome.WeaponsText, smallStyle);
            GUI.Label(new Rect(r.x + 30, r.y + 254, w - 60, 54), "被动：" + outcome.PassivesText, smallStyle);

            if (GUI.Button(new Rect(r.x + 30, r.y + h - 60, w - 60, 40), "重新开始", buttonStyle))
            {
                ClearAll();
                if (loop != null) loop.Restart();
            }
        }

        void OnDestroy()
        {
            if (whiteTex != null) Destroy(whiteTex);
            if (font != null && font.name != "LegacyRuntime") Destroy(font);
        }
    }
}
