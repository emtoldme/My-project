using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Waffle;

namespace Reverb.Narrative.Tests
{
    [TestFixture]
    public class NarrativeDirectorTests
    {
        [Test]
        public void NarrativeDirector_TriggersIntroCue_AtEarlyRunTime()
        {
            var db = HarvestRoadNarrativeScript.CreateHarvestRoadDatabase();
            var go = new GameObject("TestNarrativeDirector");
            var director = go.AddComponent<NarrativeDirector>();
            
            // 手动注入测试数据
            var field = typeof(NarrativeDirector).GetField("narrativeDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(director, db);

            var config = new RunConfig();
            var sim = new RunSimulation(config, null);

            // 模拟时钟推移到 5 秒
            sim.Tick(1.0f, default, 1, 1.0);
            sim.State.Now = 5.0f;

            director.EvaluateConditions(sim);

            Assert.IsTrue(director.HasTriggered("HR_01_Intro"), "第一关开局对白应在第 5 秒被成功触发");

            // 验证对白队列与当前对白输出
            director.TickPlayback(0.1f);
            Assert.IsNotNull(director.CurrentActiveLine, "当前应激活第一句台词");
            Assert.AreEqual(NarrativeSpeaker.Mara, director.CurrentActiveLine.speaker);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NarrativeDirector_PriorityPreemption_WorksCorrectly()
        {
            var go = new GameObject("TestNarrativeDirector");
            var director = go.AddComponent<NarrativeDirector>();

            var lowPriorityCue = new NarrativeCueDefinition
            {
                cueId = "LowPriority",
                priority = NarrativePriority.Ambient,
                lines = new[] { new NarrativeLine(NarrativeSpeaker.Mara, "玛拉", "闲聊", 88.5f, 10f) }
            };

            var urgentCue = new NarrativeCueDefinition
            {
                cueId = "UrgentDanger",
                priority = NarrativePriority.Urgent,
                lines = new[] { new NarrativeLine(NarrativeSpeaker.Walter, "沃尔特", "紧急警报", 107.9f, 5f) }
            };

            director.TriggerCue(lowPriorityCue);
            director.TickPlayback(0.1f);
            Assert.AreEqual("低优先级闲聊被激活", "玛拉", director.CurrentActiveLine.speakerDisplayName);

            // 触发高优先级，立刻抢占打断
            director.TriggerCue(urgentCue);
            director.TickPlayback(0.1f);
            Assert.AreEqual("高优先级紧急对白成功抢占", "沃尔特", director.CurrentActiveLine.speakerDisplayName);

            Object.DestroyImmediate(go);
        }
    }
}
