using System.Collections.Generic;
using UnityEngine;

namespace Reverb.Drafts.RC01.Presentation.GameplayNext
{
    /// <summary>
    /// D1 隔离预览器：仅用于在测试场景或编辑器下验证 Circle/Capsule/Annulus 预警几何、首领血条与目标面板。
    /// 严格隔离：不挂载在正式 Main 场景，不修改 GameLoop。
    /// </summary>
    [ExecuteAlways]
    public sealed class GameplayFeedbackPreview : MonoBehaviour
    {
        public GameplayFeedbackView feedbackView;

        [Header("预览参数控制")]
        [Range(0f, 1f)] public float simulatedProgress = 0.5f;
        public bool showCircle = true;
        public bool showCapsule = true;
        public bool showAnnulus = true;
        public bool showBossBar = true;
        public bool showObjective = true;
        public bool showSettlement = false;

        readonly List<DangerAreaDescriptor> previewDescriptors = new List<DangerAreaDescriptor>();

        void Update()
        {
            if (feedbackView == null) feedbackView = GetComponent<GameplayFeedbackView>();
            if (feedbackView == null) return;

            feedbackView.isPreviewMode = true;
            previewDescriptors.Clear();

            // 1. Circle 危险区（如落波者预警）
            if (showCircle)
            {
                previewDescriptors.Add(new DangerAreaDescriptor
                {
                    AttackId = 101,
                    Shape = DangerShapeType.Circle,
                    State = simulatedProgress >= 0.95f ? DangerVisualState.Active : DangerVisualState.Telegraphing,
                    Position = transform.position + new Vector3(3f, 0, 2f),
                    Radius = 2.5f,
                    Progress = simulatedProgress,
                    RemainingTime = (1f - simulatedProgress) * 2.0f,
                    SourceLabel = "远距离落波"
                });
            }

            // 2. Capsule 危险区（如冲锋者轨迹）
            if (showCapsule)
            {
                previewDescriptors.Add(new DangerAreaDescriptor
                {
                    AttackId = 102,
                    Shape = DangerShapeType.Capsule,
                    State = simulatedProgress >= 0.95f ? DangerVisualState.Active : DangerVisualState.Telegraphing,
                    Position = transform.position + new Vector3(-4f, 0, -1f),
                    Direction = new Vector3(1f, 0, 0.5f).normalized,
                    Radius = 1.2f,
                    Length = 7.0f,
                    Progress = simulatedProgress,
                    RemainingTime = (1f - simulatedProgress) * 1.5f,
                    SourceLabel = "冲锋轨迹"
                });
            }

            // 3. Annulus 环形扩散危险区（如首领声波震荡）
            if (showAnnulus)
            {
                previewDescriptors.Add(new DangerAreaDescriptor
                {
                    AttackId = 103,
                    Shape = DangerShapeType.Annulus,
                    State = simulatedProgress >= 0.95f ? DangerVisualState.Active : DangerVisualState.Telegraphing,
                    Position = transform.position + new Vector3(0, 0, -4f),
                    InnerRadius = 2.0f + simulatedProgress * 1.5f,
                    Radius = 4.0f + simulatedProgress * 2.0f,
                    Progress = simulatedProgress,
                    RemainingTime = (1f - simulatedProgress) * 2.5f,
                    SourceLabel = "首领声环"
                });
            }

            feedbackView.SetDangerDescriptors(previewDescriptors);

            // 4. 首领状态快照预览
            if (showBossBar)
            {
                feedbackView.SetBossSnapshot(new BossVisualSnapshot
                {
                    BossId = "B01_RESONANCE_GOLIATH",
                    BossName = "终局首领 · 谐振巨构",
                    CurrentMoveName = "全域声环扩散",
                    CurrentHp = Mathf.RoundToInt(3500 * (1f - simulatedProgress * 0.5f)),
                    MaxHp = 3500,
                    Phase = 2,
                    DeadlineRemainingTime = 45f,
                    IsWarning = false,
                    IsAlive = true,
                    IsDefeated = false
                });
            }
            else
            {
                feedbackView.SetBossSnapshot(null);
            }

            // 5. 目标面板快照预览
            if (showObjective)
            {
                feedbackView.SetObjectiveSnapshot(new ObjectiveVisualSnapshot
                {
                    ObjectiveId = "OBJ_01_RESONANCE_SPIRE",
                    Title = "麦田声学调谐站 α",
                    RewardSummary = "收益: 瞬间满经验 + 全场吸附",
                    WorldPosition = transform.position + new Vector3(15f, 0, 10f),
                    Radius = 4.0f,
                    Progress = simulatedProgress,
                    RemainingTime = 25f,
                    IsAvailable = true,
                    IsChanneling = true,
                    IsCompleted = false,
                    IsExpired = false
                });
            }
            else
            {
                feedbackView.SetObjectiveSnapshot(null);
            }

            // 6. 结算面板预览
            if (showSettlement)
            {
                feedbackView.SetRunOutcome(new RunOutcomeVisualSnapshot
                {
                    IsVictory = true,
                    Reason = "成功击败终局首领并在时限内净化声场",
                    SurvivalDuration = 582.4f,
                    Kills = 428,
                    Level = 18,
                    CompletedObjectives = 2,
                    BossDefeated = true,
                    BossResultText = "谐振巨构已被击溃 (耗时 54.2s)"
                });
            }
            else
            {
                feedbackView.SetRunOutcome(null);
            }
        }
    }
}
