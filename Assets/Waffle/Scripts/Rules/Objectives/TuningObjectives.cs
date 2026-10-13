using System;

namespace Waffle.Objectives
{
    public enum TuningStatus
    {
        Available,
        Channeling,
        Completed,
        Expired
    }

    public struct TuningTick
    {
        public long RunId;
        public float Now;
        public float Dt;
        public bool Playing;
        public bool Alive;
        public bool Terminal;
        public bool AnchorValid;
        public float PlayerX, PlayerZ;
        public float AnchorX, AnchorZ;
    }

    public struct TuningView
    {
        public string ObjectiveId;
        public float X, Z, RadiusMeters;
        public TuningStatus Status;
        public float Progress;
        public float RemainingSeconds;
        public string RewardText;
    }

    public struct TuningRewardRequest
    {
        public long RunId;
        public string ObjectiveId;
        public string RewardId;
        public float CompletedAt;
    }

    /// <summary>
    /// 两次可选调谐。不改 HP / XP，不查找场景物体。
    /// 达成之后要等宿主确认，确认过一次就封账。
    /// </summary>
    public sealed class TuningObjectives
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

        long runId;
        int active = -1;
        float shownX, shownZ;
        Slot[] slots;

        struct Slot
        {
            public string Id, RewardId, RewardText;
            public float AppearAt, Age, Channel, CompletedAt;
            public bool Seen, Pending, Sealed, Closed;
            public TuningStatus Status;
        }

        public void BeginRun(long runId)
        {
            this.runId = runId;
            active = -1;
            slots = new[]
            {
                NewSlot(FirstId, FirstReward, "回复20%最大生命", FirstAt),
                NewSlot(SecondId, SecondReward, "吸走场上已有的经验", SecondAt)
            };
        }

        public void Advance(TuningTick tick)
        {
            if (slots == null || tick.RunId != runId) return;
            float dt = tick.Playing && !tick.Terminal ? Math.Max(0f, tick.Dt) : 0f;
            if (!tick.Alive || tick.Terminal) CancelPending();
            if (active < 0) TrySpawn(tick);
            if (active >= 0) Step(tick, dt);
        }

        public bool TryRead(out TuningView view)
        {
            view = default;
            if (active < 0) return false;
            Slot slot = slots[active];
            view = new TuningView
            {
                ObjectiveId = slot.Id,
                X = shownX,
                Z = shownZ,
                RadiusMeters = RadiusMeters,
                Status = slot.Status,
                Progress = ChannelSeconds <= 0f ? 1f : Math.Min(1f, slot.Channel / ChannelSeconds),
                RemainingSeconds = Math.Max(0f, LifetimeSeconds - slot.Age),
                RewardText = slot.RewardText
            };
            return true;
        }

        public bool TryPeekReward(out TuningRewardRequest request)
        {
            request = default;
            if (active < 0 || !slots[active].Pending) return false;
            Slot slot = slots[active];
            request = new TuningRewardRequest
            {
                RunId = runId,
                ObjectiveId = slot.Id,
                RewardId = slot.RewardId,
                CompletedAt = slot.CompletedAt
            };
            return true;
        }

        public bool ConfirmReward(long claimedRunId, string objectiveId)
        {
            if (slots == null || claimedRunId != runId || active < 0) return false;
            ref Slot slot = ref slots[active];
            if (!slot.Pending || slot.Sealed || slot.Id != objectiveId) return false;
            slot.Pending = false;
            slot.Sealed = true;
            slot.Closed = true;
            slot.Status = TuningStatus.Completed;
            active = -1;
            return true;
        }

        void Step(TuningTick tick, float dt)
        {
            ref Slot slot = ref slots[active];
            if (slot.Pending) return;
            slot.Age += dt;
            if (tick.AnchorValid)
            {
                shownX = tick.AnchorX;
                shownZ = tick.AnchorZ;
            }
            bool inside = tick.AnchorValid && Inside(tick);
            if (inside && tick.Alive && !tick.Terminal) slot.Channel += dt;
            if (slot.Channel >= ChannelSeconds && tick.Alive && !tick.Terminal)
            {
                slot.Channel = ChannelSeconds;
                slot.Pending = true;
                slot.CompletedAt = tick.Now;
                slot.Status = TuningStatus.Completed;
                return;
            }
            if (slot.Age >= LifetimeSeconds)
            {
                slot.Status = TuningStatus.Expired;
                slot.Closed = true;
                active = -1;
                return;
            }
            slot.Status = slot.Channel > 0f ? TuningStatus.Channeling : TuningStatus.Available;
        }

        void TrySpawn(TuningTick tick)
        {
            if (!tick.Playing || tick.Terminal || !tick.Alive || !tick.AnchorValid) return;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Closed || slots[i].Seen) continue;
                if (tick.Now < slots[i].AppearAt) return;
                slots[i].Seen = true;
                slots[i].Status = TuningStatus.Available;
                shownX = tick.AnchorX;
                shownZ = tick.AnchorZ;
                active = i;
                return;
            }
        }

        void CancelPending()
        {
            if (active < 0 || !slots[active].Pending) return;
            slots[active].Pending = false;
            slots[active].Closed = true;
            slots[active].Status = TuningStatus.Expired;
            active = -1;
        }

        bool Inside(TuningTick tick)
        {
            float dx = tick.PlayerX - tick.AnchorX;
            float dz = tick.PlayerZ - tick.AnchorZ;
            return dx * dx + dz * dz <= RadiusMeters * RadiusMeters;
        }

        static Slot NewSlot(string id, string rewardId, string text, float at)
        {
            return new Slot { Id = id, RewardId = rewardId, RewardText = text, AppearAt = at, Status = TuningStatus.Available };
        }
    }
}
