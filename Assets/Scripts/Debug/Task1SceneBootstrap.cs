using UnityEngine;
using UnityEngine.SceneManagement;

namespace Survivor
{
    /// <summary>
    /// 空模板场景没有可安全手写的对象。这里只在 SampleScene 缺少 GameManager 时创建测试对象。
    /// 不修改 SampleScene.unity。Task 1 验收后应删掉这个自动生成。
    /// </summary>
    public static class Task1SceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "SampleScene")
                return;
            if (GameManager.Instance != null)
                return;

            var managerObject = new GameObject("Task1_GameManager");
            managerObject.AddComponent<GameManager>();

            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            probe.name = "Task1_PauseProbe";
            probe.transform.position = new Vector3(0f, 0.5f, 0f);
            probe.AddComponent<ScaledMotionProbe>();
        }
    }
}
