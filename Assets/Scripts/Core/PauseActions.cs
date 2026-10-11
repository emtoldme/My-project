using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Survivor
{
    /// <summary>
    /// Task 1 的暂停输入。只启用自己的 Global 动作表，不触项目级 InputSystem_Actions。
    /// 运行时复制资源，避免改到 Project 里的原资产。
    /// </summary>
    public sealed class PauseActions : IDisposable
    {
        readonly InputActionAsset asset;
        readonly InputActionMap globalMap;

        public InputAction Pause { get; }

        public PauseActions()
        {
            var source = Resources.Load<InputActionAsset>("PauseActions");
            if (source == null)
            {
                throw new InvalidOperationException(
                    "未找到 Resources/PauseActions.inputactions。");
            }

            var copy = UnityEngine.Object.Instantiate(source);
            copy.name = source.name + " (Runtime)";
            InputActionMap map;
            InputAction pause;
            try
            {
                map = copy.FindActionMap("Global", true);
                pause = map.FindAction("Pause", true);
            }
            catch
            {
                UnityEngine.Object.Destroy(copy);
                throw;
            }

            asset = copy;
            globalMap = map;
            Pause = pause;
        }

        public void Enable() => globalMap.Enable();

        public void Disable()
        {
            if (globalMap != null && globalMap.enabled)
                globalMap.Disable();
        }

        public void Dispose()
        {
            Disable();
            if (asset != null)
                UnityEngine.Object.Destroy(asset);
        }
    }
}
