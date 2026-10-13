using System.Collections.Generic;
using UnityEngine;

namespace Reverb.Narrative
{
    /// <summary>
    /// 提供第一关【秋收之路】全套开箱即用的标准叙事剧本与初始历史磁带存根
    /// </summary>
    public static class HarvestRoadNarrativeScript
    {
        public static NarrativeDatabase CreateHarvestRoadDatabase()
        {
            var db = ScriptableObject.CreateInstance<NarrativeDatabase>();

            // 1. 开局对白：抵达墓园与墓碑消失
            db.cues.Add(new NarrativeCueDefinition
            {
                cueId = "HR_01_Intro",
                priority = NarrativePriority.StoryBeat,
                condition = new NarrativeTriggerCondition { minRunSeconds = 2.0f, maxRunSeconds = 30.0f },
                lines = new[]
                {
                    new NarrativeLine(NarrativeSpeaker.Mara, "玛拉", "……喂？听得见吗，阿瑟？旧皮卡水箱开锅了，我正试着拆火花塞。你到墓园没有？", 88.5f, 4.5f, 0.15f),
                    new NarrativeLine(NarrativeSpeaker.Protagonist, "阿瑟", "我到了。但是……父亲的墓碑不见了。这一排全是一样的平地，像从未有人埋在这里。", 88.5f, 5.0f, 0.05f),
                    new NarrativeLine(NarrativeSpeaker.Mara, "玛拉", "记错排数了吧？等等……你那边什么声音？麦田里怎么会有脱粒机启动的动静？", 88.5f, 4.5f, 0.2f)
                }
            });

            // 2. 异常升级：首批异化农机与稻草人
            db.cues.Add(new NarrativeCueDefinition
            {
                cueId = "HR_02_StaticAwakening",
                priority = NarrativePriority.StoryBeat,
                condition = new NarrativeTriggerCondition { minRunSeconds = 90.0f, minKills = 25 },
                lines = new[]
                {
                    new NarrativeLine(NarrativeSpeaker.Walter, "收音机镇气象广播", "农业安全局通告：监测到第7农区存在未授权历史震颤。系统正在执行平滑净化，请勿注视麦浪中的蓝光。", 98.0f, 5.0f, 0.35f),
                    new NarrativeLine(NarrativeSpeaker.Mara, "玛拉", "广播塔的电弧顺着输电线打过来了！机器在自己动！拿稳你爸留下的调频盒，红色开关是过载波！", 98.0f, 4.5f, 0.25f)
                }
            });

            // 3. 经过老屋：童年记忆被现实擦除
            db.cues.Add(new NarrativeCueDefinition
            {
                cueId = "HR_03_HomeVanished",
                priority = NarrativePriority.Urgent,
                condition = new NarrativeTriggerCondition { minRunSeconds = 240.0f },
                environmentalGlitchTarget = "HomePorch",
                lines = new[]
                {
                    new NarrativeLine(NarrativeSpeaker.Mara, "玛拉", "阿瑟，你顺路经过你家老宅了吗？帮我看看门廊下那个红色的应急油桶还在不在……", 92.4f, 4.0f, 0.2f),
                    new NarrativeLine(NarrativeSpeaker.Protagonist, "阿瑟", "……没有门廊。房子变成了一整片生锈的铁丝网。秋千、台阶，全都不见了。", 92.4f, 4.5f, 0.1f),
                    new NarrativeLine(NarrativeSpeaker.Mara, "玛拉", "……你说什么？阿瑟，别吓我。那可是你住了十一年的家啊！", 92.4f, 4.0f, 0.3f)
                }
            });

            // 4. 沃尔特的哲学宣告：消灭痛苦的诱惑
            db.cues.Add(new NarrativeCueDefinition
            {
                cueId = "HR_04_WalterManifesto",
                priority = NarrativePriority.StoryBeat,
                condition = new NarrativeTriggerCondition { minRunSeconds = 380.0f },
                lines = new[]
                {
                    new NarrativeLine(NarrativeSpeaker.Walter, "沃尔特", "年轻的埃尔德里奇，我知道你怀里抱着什么。痛苦是无序的副产物。只要忘记大火与离别，一切都会回到平静的星期日下午。", 104.5f, 6.0f, 0.4f),
                    new NarrativeLine(NarrativeSpeaker.Protagonist, "阿瑟", "如果是为了抹去痛苦，那你凭什么连父亲的名字一起抹掉？", 104.5f, 4.5f, 0.1f),
                    new NarrativeLine(NarrativeSpeaker.Walter, "沃尔特", "因为他拒绝遗忘。而拒绝遗忘的人，注定会被留在大火里。", 104.5f, 5.0f, 0.3f)
                }
            });

            // 5. 关卡终局 Boss 降临：除垢者
            db.cues.Add(new NarrativeCueDefinition
            {
                cueId = "HR_05_BossArrival",
                priority = NarrativePriority.Urgent,
                condition = new NarrativeTriggerCondition { minRunSeconds = 540.0f },
                lines = new[]
                {
                    new NarrativeLine(NarrativeSpeaker.Mara, "玛拉", "高压电网彻底过载了！北面麦田有台巨型联合收割机朝你冲过去了！喇叭里全是白噪音！", 107.9f, 4.5f, 0.5f),
                    new NarrativeLine(NarrativeSpeaker.Walter, "收音机镇气象广播", "正在清除不可调和之异常节点。祝您度过宁静祥和的一天。", 107.9f, 4.0f, 0.2f)
                }
            });

            // 6. Boss 击败结算与物证掉落
            db.cues.Add(new NarrativeCueDefinition
            {
                cueId = "HR_06_VictoryTape",
                priority = NarrativePriority.Milestone,
                condition = new NarrativeTriggerCondition { minRunSeconds = 580.0f },
                unlockTapeId = "TAPE_1968_OCT14",
                lines = new[]
                {
                    new NarrativeLine(NarrativeSpeaker.Mara, "玛拉", "机器停下来了！天啊……阿瑟，你从收割机的控制盒里扯出了什么？一盒老磁带？", 88.5f, 4.5f, 0.1f),
                    new NarrativeLine(NarrativeSpeaker.Lucy, "露西", "……调频截获成功。这是……第四粮仓二十年前的总控室黑匣子录音？！阿瑟，快把它收好！", 88.5f, 5.0f, 0.2f)
                }
            });

            // 初始磁带物证
            db.artifacts.Add(new CassetteArtifact
            {
                tapeId = "TAPE_1968_OCT14",
                title = "第四粮仓夜班调度记录（残片）",
                recordedDate = "1968.10.14 23:42",
                location = "第四粮仓·主控室二楼",
                transcript = "“沃尔特，谷尘浓度传感器已经过载了！快拉闸放风！……等等，你为什么要反锁总控阀门？沃尔特！外面的工友还在脱粒线上！把门打开！——[激烈撞门声与剧烈电流爆音]”",
                narrativeNote = "这盒磁带被父亲焊死在收割机继电器内侧。沃尔特当年不仅在场，而且是他亲自切断了安全通道。所谓‘麦收骚乱’根本不是意外。"
            });

            return db;
        }
    }
}
