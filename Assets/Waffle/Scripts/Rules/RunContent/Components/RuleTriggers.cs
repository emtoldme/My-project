using System;
using Waffle.Combat;

namespace Waffle.RunContent.Components
{
    /// <summary>触发：冷却。cooldown 由调用方传入（已经过 GrowthStats.FinalCooldown）。</summary>
    public sealed class CooldownTrigger
    {
        float timer;
        /// <summary>推进 dt；就绪且 hasTarget 时返回 true 并重新计时。无目标时保持就绪（与旧武器一致）。</summary>
        public bool Advance(float dt, float cooldown, bool hasTarget)
        {
            timer -= Math.Max(0f, dt);
            if (timer > 0f) return false;
            if (!hasTarget) { timer = 0f; return false; }
            timer = Math.Max(GrowthStats.MinCooldown, cooldown);
            return true;
        }
        public void Reset() { timer = 0f; }
    }

    /// <summary>触发：实际移动累计。只累计合法位移；传送/重开/调试跳点必须调用 Rebase。</summary>
    public sealed class DistanceAccumulator
    {
        Vec2 last; bool hasLast; float carried;
        public float Carried => carried;
        public void Rebase(Vec2 pos) { last = pos; hasLast = true; carried = 0f; }
        /// <summary>返回本 tick 跨过 spacing 的次数（上限 maxEmits，余量丢弃以防爆发）。</summary>
        public int Feed(Vec2 pos, float maxLegalStep, float spacing, int maxEmits)
        {
            if (!hasLast) { Rebase(pos); return 0; }
            float d = Vec2.Distance(last, pos);
            last = pos;
            if (spacing <= 0f || d <= 0f || float.IsNaN(d)) return 0;
            if (maxLegalStep > 0f && d > maxLegalStep) { carried = 0f; return 0; } // 传送式位移不积攒
            carried += d;
            int n = 0;
            while (carried >= spacing && n < maxEmits) { carried -= spacing; n++; }
            if (carried >= spacing) carried = 0f; // 超出本 tick 上限的距离作废
            return n;
        }
        public void Clear() { hasLast = false; carried = 0f; }
    }

    /// <summary>触发：真正发动的闪避。只由 RunSimulation 在 T1 闪避实际开始时通知；按键失败不通知。</summary>
    public sealed class DodgeTrigger
    {
        bool pending; Vec2 at; float readyAt = float.NegativeInfinity;
        public void Notify(Vec2 pos) { pending = true; at = pos; }
        /// <summary>消费待处理闪避；throttle&gt;0 时独立节流。返回是否触发。</summary>
        public bool Consume(float now, float throttle, out Vec2 pos)
        {
            pos = at;
            if (!pending) return false;
            pending = false;
            if (now < readyAt) return false;
            readyAt = now + Math.Max(0f, throttle);
            return true;
        }
        /// <summary>同一次闪避给多个消费者时使用：只看不清。</summary>
        public bool Peek(out Vec2 pos) { pos = at; return pending; }
        public void Clear() { pending = false; readyAt = float.NegativeInfinity; }
    }
}
