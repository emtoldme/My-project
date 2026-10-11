using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Waffle.Tests.Integration
{
    /// <summary>
    /// 独立组件冒烟测试，显式提供输入依赖，不依赖其他用例保留项目级 Actions。
    /// </summary>
    public class IntegrationSmokeTests
    {
        [UnityTest]
        public IEnumerator GameLoop_CanBeCreated_AndStartsPlaying()
        {
            var go = new GameObject("GameLoop_Smoke");
            go.SetActive(false);
            var loop = go.AddComponent<GameLoop>();
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var player = actions.AddActionMap("Player");
            player.AddAction("Move", InputActionType.Value);
            player.AddAction("Dodge", InputActionType.Button);
            player.AddAction("Pause", InputActionType.Button);
            actions.AddActionMap("UI").AddAction("Pause", InputActionType.Button);
            loop.actions = actions;
            go.SetActive(true);
            yield return null; // 让 Awake / 第一帧 Update 执行

            Assert.IsNotNull(loop.Sim);
            Assert.AreSame(loop.config, loop.Sim.Config, "GameLoop 序列化的 config 就是 RunSimulation 使用的 config");
            Assert.AreEqual(RunPhase.Playing, loop.Sim.State.Phase);

            Object.Destroy(go);
            Time.timeScale = 1f;
            yield return null;
            Object.Destroy(actions);
        }
    }
}
