using UnityEngine;

namespace Waffle
{
    /// <summary>
    /// 固定斜俯视相机：只跟随位置、不旋转（§8.2）。在 LateUpdate 里执行。
    /// 默认值按主美规范（Docs/Art/01 §四，用户已确认）：正交、Size 9、Pitch 55°、Yaw 45°。
    /// 执行顺序早于 GameLoop，保证 GameLoop.Awake 计算移动方向时相机朝向已经是最终值。
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public bool orthographic = true;
        public float orthographicSize = 9f;
        [Tooltip("俯仰角（度）")] public float pitch = 55f;
        [Tooltip("水平朝向（度）")] public float yaw = 45f;
        [Tooltip("相机到目标的距离（正交时只影响裁剪，不影响画面大小）")] public float distance = 30f;

        private void Awake()
        {
            Apply();
        }

        /// <summary>把投影和角度写到相机上，并放到目标位置。编辑器菜单也会调用。</summary>
        public void Apply()
        {
            var cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = orthographic;
                if (orthographic) cam.orthographicSize = orthographicSize;
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, distance + 60f);
            }
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            Follow();
        }

        private void LateUpdate()
        {
            Follow();
        }

        private void Follow()
        {
            Vector3 center = target != null ? target.position : Vector3.zero;
            transform.position = center - transform.rotation * Vector3.forward * distance;
        }
    }
}
