using UnityEngine;
using Waffle.Homecoming;

namespace Reverb.Homecoming.Places
{
    /// <summary>挂在地点Prefab根上。只根据PoiStatus改灯与遗物点缀，不写任何规则状态，无碰撞体。</summary>
    [DisallowMultipleComponent]
    public sealed class HomecomingPlaceView : MonoBehaviour
    {
        public string templateId;
        [Tooltip("有实体灯罩的暖色灯；发现后点亮，领取后保持低亮的“有人回来过”。")]
        public Light[] warmLamps;
        public float unseenIntensity = 0.15f, discoveredIntensity = 1.2f, availableIntensity = 1.8f, claimedIntensity = 0.6f;
        [Tooltip("可领取遗物的实体模型（针线盒/手套/吉他）；领取后隐藏。")]
        public GameObject relicProp;
        public float lampEase = 3f;
        public PoiStatus Status { get; private set; }
        float target;
        Waffle.GameLoop loop;
        void Awake() { loop = GetComponentInParent<Waffle.GameLoop>(); }

        public void Apply(PoiStatus status)
        {
            Status = status;
            target = status == PoiStatus.Claimed ? claimedIntensity : status == PoiStatus.Available ? availableIntensity
                   : status == PoiStatus.Discovered ? discoveredIntensity : unseenIntensity;
            if (relicProp != null) relicProp.SetActive(status != PoiStatus.Claimed);
        }

        void Update()
        {
            if (warmLamps == null) return;
            float dt = loop?.Sim != null ? (loop.Sim.State.Phase == Waffle.RunPhase.Playing ? loop.Sim.LastDt : 0) : Time.deltaTime;
            float k = 1f - Mathf.Exp(-lampEase * dt);
            foreach (var l in warmLamps) if (l != null) l.intensity = Mathf.Lerp(l.intensity, target, k);
        }
    }
}
