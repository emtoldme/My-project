using UnityEngine;

namespace Waffle.S2
{
    /// <summary>
    /// S2 样板的动效参数。占位数值，不是已验收的美术强度。
    /// </summary>
    [CreateAssetMenu(fileName = "S2FxProfile", menuName = "Waffle/S2 Fx Profile")]
    public class S2FxProfile : ScriptableObject
    {
        [Header("收束 → 错位展开 → 复位（秒）")]
        public float contractSeconds = 0.04f;
        public float unfoldSeconds = 0.06f;
        public float recoverSeconds = 0.08f;
        [Range(0f, 0.45f)] public float contract = 0.14f;
        public float unfoldMeters = 0.05f;
        public float unfoldYaw = 7f;

        [Header("精英击杀环 / 拾取碎片。只在 Playing 用逻辑 dt")]
        public float eliteRingSeconds = 0.42f;
        public float eliteRingRadius = 1.7f;
        public float pickupSeconds = 0.16f;
        public Color eliteColor = new Color(1f, 0.27f, 0f, 1f);
        public Color pickupColor = new Color(0.93f, 0.74f, 0.32f, 1f);
    }
}
