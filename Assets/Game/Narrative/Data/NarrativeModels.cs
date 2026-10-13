using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reverb.Narrative
{
    /// <summary>
    /// 发话主体：决定 UI 显示样式、频段标签与色彩
    /// </summary>
    public enum NarrativeSpeaker
    {
        Protagonist,   // 归乡者阿瑟：微光白，无电台杂音
        Mara,          // 玛拉（机械师/现实中留下的人）：暖橙琥珀，对讲机短波
        Eli,           // 伊莱（工程师/相信未来的人）：冷绿青色，广播塔内线
        Lucy,          // 露西（记录员/记住过去的人）：铁锈暗红，发霉卡带感
        Walter,        // 沃尔特（广播塔核心工程师/反派）：幽冷纯蓝，伪官方安全播报
        UnknownStatic  // 未知频段/强杂音震颤
    }

    /// <summary>
    /// 叙事事件优先级：确保关键剧情与紧急救援抢占环境闲聊，且不丢失重要物证信息
    /// </summary>
    public enum NarrativePriority
    {
        Ambient = 0,    // 广播背景天气预报 / 待机闲谈
        StoryBeat = 1,  // 阶段推进 / 探索地标 / 异常初现
        Urgent = 2,     // 危险警告 / Boss 降临 / 现实篡改
        Milestone = 3   // 击败 Boss 结算 / 核心历史物证揭露
    }

    /// <summary>
    /// 复合触发条件判定器
    /// </summary>
    [Serializable]
    public class NarrativeTriggerCondition
    {
        public float minRunSeconds = 0f;
        public float maxRunSeconds = float.MaxValue;
        public int minKills = 0;
        public float maxHpFraction = 1f;       // 玩家生命值百分比低于此值时触发
        public string requiredContentId = "";  // 需要装备的武器或被动模块 ID（如 W05, P18）
        public string requiredEventId = "";    // 需要前置触发过的 CueId
        public bool requireBossSpawned = false;
        public bool requireBossDefeated = false;
    }

    /// <summary>
    /// 单句无线电对白原子
    /// </summary>
    [Serializable]
    public class NarrativeLine
    {
        public NarrativeSpeaker speaker;
        public string speakerDisplayName;
        [TextArea(2, 4)]
        public string transcript;           // 字幕文字
        public string audioClipId;          // 音频资产 ID（预留对接 AudioDirector）
        public float duration = 4.5f;       // 显示与播放基础时长
        public float frequencyMHz = 98.5f;  // 所处 FM 调频刻度
        [Range(0f, 1f)]
        public float staticIntensity = 0.1f;// 杂音强度（0=清澈，1=严重雪花失真）

        public NarrativeLine() { }

        public NarrativeLine(NarrativeSpeaker speaker, string name, string text, float freq, float dur, float noise = 0.1f, string audioId = "")
        {
            this.speaker = speaker;
            this.speakerDisplayName = name;
            this.transcript = text;
            this.frequencyMHz = freq;
            this.duration = dur;
            this.staticIntensity = noise;
            this.audioClipId = audioId;
        }
    }

    /// <summary>
    /// 一组无线电叙事广播事件定义（Cue）
    /// </summary>
    [Serializable]
    public class NarrativeCueDefinition
    {
        public string cueId;
        public NarrativePriority priority = NarrativePriority.StoryBeat;
        public NarrativeTriggerCondition condition = new NarrativeTriggerCondition();
        public NarrativeLine[] lines = Array.Empty<NarrativeLine>();
        public bool triggerOnce = true;
        public string environmentalGlitchTarget = ""; // 触发时联动的环境异变标记（如老屋秋千消失）
        public string unlockTapeId = "";              // 解锁的磁带存根 ID
    }

    /// <summary>
    /// 磁带物证存根数据结构（历史碎片）
    /// </summary>
    [Serializable]
    public class CassetteArtifact
    {
        public string tapeId;
        public string title;
        public string recordedDate; // e.g. "1968.10.14"
        public string location;     // e.g. "第四粮仓主控机房"
        [TextArea(3, 6)]
        public string transcript;
        public string narrativeNote; // 主角的感悟与分析
    }
}
