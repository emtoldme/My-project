using UnityEngine;

namespace Waffle
{
    /// <summary>固定斜俯视相机：只跟随位置、不旋转（§8.2）。在 LateUpdate 里执行。</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(-12f, 18f, -12f);

        private void LateUpdate()
        {
            if (target != null) transform.position = target.position + offset;
        }
    }
}
