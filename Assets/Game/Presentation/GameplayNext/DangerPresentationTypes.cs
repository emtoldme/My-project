using System;
using UnityEngine;

namespace Reverb.Drafts.RC01.Presentation.GameplayNext
{
    public enum DangerShapeType
    {
        Circle,
        Capsule,
        Annulus
    }

    public enum DangerVisualState
    {
        Telegraphing, // 预警充能中（倒计时脉冲与边界汇聚）
        Active,       // 判定生效时刻（高亮闪烁、饱和填充）
        Resolved      // 判定结束消散（快速淡出）
    }

    /// <summary>
    /// 只读危险区域描述符，共享几何与时序，不推导伤害。
    /// </summary>
    public struct DangerAreaDescriptor
    {
        public int AttackId;
        public DangerShapeType Shape;
        public DangerVisualState State;

        // 世界坐标中心或起点
        public Vector3 Position;
        // 胶囊体方向或冲锋朝向（归一化）
        public Vector3 Direction;

        // 几何尺寸
        public float Radius;        // 圆形半径 / 胶囊体半宽 / 环形外半径
        public float InnerRadius;   // 环形内半径
        public float Length;        // 胶囊体长度（冲锋路径长度）

        // 时序进度 (0.0 ~ 1.0)
        public float Progress;      // 0 = 刚生成预警, 1 = 充能完毕生效
        public float RemainingTime; // 剩余倒计时秒数

        // 标签说明（用于辨识不同敌人的招式）
        public string SourceLabel;
    }

    /// <summary>
    /// 地图调谐目标只读快照
    /// </summary>
    public struct ObjectiveVisualSnapshot
    {
        public string ObjectiveId;
        public string Title;
        public string RewardSummary;
        public Vector3 WorldPosition;
        public float Radius;
        public float Progress;      // 0.0 ~ 1.0
        public float RemainingTime; // 剩余秒数
        public bool IsAvailable;
        public bool IsChanneling;
        public bool IsCompleted;
        public bool IsExpired;
    }

    /// <summary>
    /// 首领战只读快照
    /// </summary>
    public struct BossVisualSnapshot
    {
        public string BossId;
        public string BossName;
        public string CurrentMoveName;
        public int CurrentHp;
        public int MaxHp;
        public int Phase;
        public float DeadlineRemainingTime; // 距10:00截止剩余时间
        public bool IsWarning;
        public bool IsAlive;
        public bool IsDefeated;
    }

    /// <summary>
    /// 结算结果只读快照
    /// </summary>
    public struct RunOutcomeVisualSnapshot
    {
        public bool IsVictory;
        public string Reason;
        public float SurvivalDuration;
        public int Kills;
        public int Level;
        public int CompletedObjectives;
        public bool BossDefeated;
        public string BossResultText;
    }
}
