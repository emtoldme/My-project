using System;
using Waffle.Combat;
using Waffle.GameplayNext;

namespace Waffle.Objectives
{
    /// <summary>
    /// 两次可选调谐。位置由构造时传入的锚点决定，不查找 Transform。
    /// 不改 HP / XP。完成只提交 D02 或 D03，等宿主确认后封账。
    /// </summary>
    public sealed class TuningObjectives : IObjectiveModule
    {
        public const float RadiusMeters = 2f;
        public const float ChannelSeconds = 4f;
        public const float LifetimeSeconds = 60f;
        public const float FirstAt = 150f;
        public const float SecondAt = 390f;
        public const string FirstId = "tune-1";
        public const string SecondId = "tune-2";
        public const string FirstReward = "D02";
        public const string SecondReward = "D03";

        readonly Vec2 firstAnchor;
        readonly Vec2 secondAnchor;
        Guid runId;
        bool haveRun;
        int active = -1;
        Slot[] slots = MakeSlots();

        struct Slot
        {
            public string Id, RewardId;
            public float AppearAt, Age, Channel, StartedAt;
            public bool Seen, Pending, Closed;
        }

        public TuningObjectives(Vec2 firstAnchor, Vec2 secondAnchor)
        {
            this.firstAnchor = firstAnchor;
            this.secondAnchor = secondAnchor;
        }

        public void Step(GameplayContext context, GameplayCommandBuffer commands)
        {
            if (!haveRun)
            {
                runId = context.RunId;
                haveRun = true;
            }
            if (context.RunId != runId) return;
            float dt = Math.Max(0f, context.Dt);
            if (active < 0) TrySpawn(context);
            if (active >= 0) Advance(context, dt, commands);
        }

        public void ConfirmReward(Guid claimedRunId, string objectiveId, bool granted)
        {
            if (!haveRun || claimedRunId != runId || active < 0) return;
            ref Slot slot = ref slots[active];
            if (!slot.Pending || slot.Id != objectiveId) return;
            slot.Pending = false;
            slot.Closed = true;
            active = -1;
        }

        public void Clear()
        {
            haveRun = false;
            runId = Guid.Empty;
            active = -1;
            slots = MakeSlots();
        }

        void Advance(GameplayContext context, float dt, GameplayCommandBuffer commands)
        {
            ref Slot slot = ref slots[active];
            if (slot.Pending)
            {
                Publish(commands, slot, ObjectiveStatus.Completed, 1f);
                commands.TryReward(new ObjectiveRewardIntent(runId, slot.Id, slot.RewardId));
                return;
            }
            slot.Age += dt;
            if (Inside(context, Anchor(active))) slot.Channel += dt;
            if (slot.Channel >= ChannelSeconds)
            {
                slot.Channel = ChannelSeconds;
                slot.Pending = true;
                Publish(commands, slot, ObjectiveStatus.Completed, 1f);
                commands.TryReward(new ObjectiveRewardIntent(runId, slot.Id, slot.RewardId));
                return;
            }
            if (slot.Age >= LifetimeSeconds)
            {
                Publish(commands, slot, ObjectiveStatus.Expired, Progress(slot));
                slot.Closed = true;
                active = -1;
                return;
            }
            var status = slot.Channel > 0f ? ObjectiveStatus.Channeling : ObjectiveStatus.Available;
            Publish(commands, slot, status, Progress(slot));
        }

        void TrySpawn(GameplayContext context)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Closed || slots[i].Seen) continue;
                if (context.Now < slots[i].AppearAt) return;
                slots[i].Seen = true;
                slots[i].StartedAt = context.Now;
                active = i;
                return;
            }
        }

        void Publish(GameplayCommandBuffer commands, Slot slot, ObjectiveStatus status, float progress)
        {
            commands.PublishObjective(new ObjectiveSnapshot(slot.Id, Anchor(active < 0 ? 0 : active), RadiusMeters,
                status, progress, slot.StartedAt + LifetimeSeconds, slot.RewardId));
        }

        Vec2 Anchor(int index) { return index == 0 ? firstAnchor : secondAnchor; }

        bool Inside(GameplayContext context, Vec2 anchor)
        {
            return Vec2.SqrDistance(context.PlayerPosition, anchor) <= RadiusMeters * RadiusMeters;
        }

        static float Progress(Slot slot)
        {
            return Math.Min(1f, slot.Channel / ChannelSeconds);
        }

        static Slot[] MakeSlots()
        {
            return new[]
            {
                new Slot { Id = FirstId, RewardId = FirstReward, AppearAt = FirstAt },
                new Slot { Id = SecondId, RewardId = SecondReward, AppearAt = SecondAt }
            };
        }
    }
}
