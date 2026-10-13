using NUnit.Framework;

namespace Waffle.Objectives.Tests
{
    public class TuningObjectivesTests
    {
        [Test]
        public void ChannelPausesOutsideAndCompletesOnce()
        {
            var module = New();
            module.Advance(At(150f, 0f, 0f, 9f));
            Assert.That(module.TryRead(out var view), Is.True);
            Assert.That(view.Status, Is.EqualTo(TuningStatus.Available));
            Assert.That(view.RadiusMeters, Is.EqualTo(2f));

            module.Advance(At(151f, 1f, 0f, 0f));
            module.Advance(At(152.5f, 1.5f, 0f, 0f));
            module.Advance(At(153f, 0.5f, 9f, 0f));
            module.TryRead(out view);
            Assert.That(view.Progress, Is.EqualTo(0.625f).Within(0.0001f));
            Assert.That(module.TryPeekReward(out _), Is.False);

            module.Advance(At(154.5f, 1.5f, 0f, 0f));
            Assert.That(module.TryPeekReward(out var reward), Is.True);
            Assert.That(module.TryPeekReward(out var again), Is.True);
            Assert.That(again.ObjectiveId, Is.EqualTo(reward.ObjectiveId));
            Assert.That(reward.RewardId, Is.EqualTo("D02"));
            Assert.That(module.ConfirmReward(reward.RunId, reward.ObjectiveId), Is.True);
            Assert.That(module.ConfirmReward(reward.RunId, reward.ObjectiveId), Is.False);
            Assert.That(module.TryPeekReward(out _), Is.False);
        }

        [Test]
        public void BoundaryUsesPlayerCenter()
        {
            var module = New();
            module.Advance(At(150f, 0f, 0f, 0f));
            module.Advance(At(154f, 4f, 2f, 0f));
            Assert.That(module.TryPeekReward(out _), Is.True);
            module = New();
            module.Advance(At(150f, 0f, 0f, 0f));
            module.Advance(At(154f, 4f, 2.01f, 0f));
            Assert.That(module.TryPeekReward(out _), Is.False);
        }

        [Test]
        public void PauseAndExpiryDoNotPay()
        {
            var module = New();
            module.Advance(At(150f, 0f, 0f, 0f));
            var paused = At(151f, 4f, 0f, 0f);
            paused.Playing = false;
            module.Advance(paused);
            Assert.That(module.TryPeekReward(out _), Is.False);
            module.TryRead(out var view);
            Assert.That(view.RemainingSeconds, Is.EqualTo(60f).Within(0.0001f));

            module.Advance(At(210f, 60f, 9f, 0f));
            Assert.That(module.TryRead(out _), Is.False);
            Assert.That(module.TryPeekReward(out _), Is.False);
        }

        [Test]
        public void DeathOnTheCompletionTickPaysNothing()
        {
            var module = New();
            module.Advance(At(150f, 0f, 0f, 0f));
            var dead = At(154f, 4f, 0f, 0f);
            dead.Alive = false;
            module.Advance(dead);
            Assert.That(module.TryPeekReward(out _), Is.False);
            Assert.That(module.ConfirmReward(7, TuningObjectives.FirstId), Is.False);
        }

        [Test]
        public void SecondTuneUsesExistingMagnetAndOldRunCannotConfirm()
        {
            var module = FinishFirst();
            module.Advance(At(390f, 0f, 0f, 0f));
            module.Advance(At(394f, 4f, 0f, 0f));
            Assert.That(module.TryPeekReward(out var reward), Is.True);
            Assert.That(reward.RewardId, Is.EqualTo("D03"));
            Assert.That(reward.ObjectiveId, Is.EqualTo(TuningObjectives.SecondId));
            Assert.That(module.ConfirmReward(6, reward.ObjectiveId), Is.False);
            Assert.That(module.ConfirmReward(7, reward.ObjectiveId), Is.True);
        }

        [Test]
        public void RestartDropsThePreviousReward()
        {
            var module = New();
            module.Advance(At(150f, 0f, 0f, 0f));
            module.Advance(At(154f, 4f, 0f, 0f));
            module.BeginRun(8);
            Assert.That(module.TryPeekReward(out _), Is.False);
            Assert.That(module.ConfirmReward(7, TuningObjectives.FirstId), Is.False);
            Assert.That(module.TryRead(out _), Is.False);
        }

        static TuningObjectives FinishFirst()
        {
            var module = New();
            module.Advance(At(150f, 0f, 0f, 0f));
            module.Advance(At(154f, 4f, 0f, 0f));
            Assert.That(module.ConfirmReward(7, TuningObjectives.FirstId), Is.True);
            return module;
        }

        static TuningObjectives New()
        {
            var module = new TuningObjectives();
            module.BeginRun(7);
            return module;
        }

        static TuningTick At(float now, float dt, float x, float z)
        {
            return new TuningTick
            {
                RunId = 7, Now = now, Dt = dt, Playing = true, Alive = true,
                AnchorValid = true, PlayerX = x, PlayerZ = z, AnchorX = 0f, AnchorZ = 0f
            };
        }
    }
}
