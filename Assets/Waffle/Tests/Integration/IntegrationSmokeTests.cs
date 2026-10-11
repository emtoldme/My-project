using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Waffle.Tests.Integration
{
    /// <summary>
    /// 占位冒烟测试：只证明 Waffle.Tests.Integration 程序集能编译、能在 PlayMode 下运行。
    /// 本目录之后由 1 号（V-01）负责编写真正的集成测试。
    /// </summary>
    public class IntegrationSmokeTests
    {
        [UnityTest]
        public IEnumerator GameLoop_CanBeCreated_AndStartsPlaying()
        {
            var go = new GameObject("GameLoop_Smoke");
            var loop = go.AddComponent<GameLoop>();
            yield return null; // 让 Awake / 第一帧 Update 执行

            Assert.IsNotNull(loop.Sim);
            Assert.AreSame(loop.config, loop.Sim.Config, "GameLoop 序列化的 config 就是 RunSimulation 使用的 config");
            Assert.AreEqual(RunPhase.Playing, loop.Sim.State.Phase);

            Object.Destroy(go);
            Time.timeScale = 1f;
            yield return null;
        }
    }
}
