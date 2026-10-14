using UnityEngine;

namespace Waffle
{
    /// <summary>A copied, presentation-only snapshot. No rule objects escape into animation.</summary>
    public readonly struct EnemyVisualPose
    {
        public readonly int Serial;
        public readonly float Now, Hit;
        public readonly Vector3 Velocity;
        public readonly bool Recycled, Elite, Boss;
        public readonly string Archetype;
        public EnemyVisualPose(int serial, float now, float hit, Vector3 velocity,
            bool recycled, bool elite, bool boss, string archetype)
        { Serial=serial; Now=now; Hit=hit; Velocity=velocity; Recycled=recycled; Elite=elite; Boss=boss; Archetype=archetype; }
    }
    public interface IEnemyVisualPose
    {
        void Present(in EnemyVisualPose pose);
    }
}
