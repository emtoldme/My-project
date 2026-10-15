using System;
using NUnit.Framework;
using Waffle.Homecoming;
using Waffle.Homecoming.Relics;

namespace Waffle.Tests
{
    public class HomecomingRelicsTests
    {
        readonly Guid run = Guid.NewGuid();

        [Test]
        public void SlotsGrowFromTwoToFourAndARepeatDoesNot()
        {
            var bag = Open();
            Assert.That(bag.TryGrant(Reward(run, "home-a", RelicInventory.SewingTin, 1)), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "home-a", RelicInventory.SewingTin, 1)), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "home-b", RelicInventory.SewingTin, 1)), Is.False);
            Assert.That(bag.Read().Capacity, Is.EqualTo(3));
            Assert.That(bag.TryGrant(Reward(run, "silo-a", RelicInventory.WorkGloves, 1)), Is.True);
            Assert.That(bag.Read().Capacity, Is.EqualTo(4));
            Assert.That(bag.TryGrant(Reward(run, "silo-b", RelicInventory.WorkGloves, 1)), Is.False);
            Assert.That(bag.Read().Capacity, Is.EqualTo(4));
            Assert.That(Count(bag, RelicInventory.SewingTin), Is.EqualTo(1));
        }

        [Test]
        public void ForgedCombosAndOldRunsLeaveTheBagUntouched()
        {
            var bag = Open();
            Assert.That(bag.TryGrant(Reward(run, "bad-guitar", RelicInventory.OldGuitar, 1)), Is.False);
            Assert.That(bag.TryGrant(Reward(run, "bad-tin", RelicInventory.SewingTin, 0)), Is.False);
            Assert.That(bag.TryGrant(Reward(Guid.NewGuid(), "home-a", RelicInventory.SewingTin, 1)), Is.False);
            Assert.That(bag.TryGrant(Reward(run, "nope", "item.unknown", 0)), Is.False);
            Assert.That(bag.Read().Capacity, Is.EqualTo(2));
            Assert.That(bag.Read().StoredItems, Is.Empty);
            Assert.That(bag.Read().Slots[0].ItemId, Is.Null);
        }

        [Test]
        public void FullSlotsStoreAndSwapsDoNotDoubleTheBonus()
        {
            var bag = Open();
            bag.TryGrant(Reward(run, "guitar", RelicInventory.OldGuitar, 0));
            bag.TryGrant(Reward(run, "home", RelicInventory.SewingTin, 1));
            bag.TryGrant(Reward(run, "silo", RelicInventory.WorkGloves, 1));
            Assert.That(bag.TryEquip(run, RelicInventory.OldGuitar, 0), Is.True);
            Assert.That(bag.TryEquip(run, RelicInventory.SewingTin, 1), Is.True);
            Assert.That(bag.TryEquip(run, RelicInventory.WorkGloves, 2), Is.True);
            Assert.That(bag.TryUnequip(run, 0), Is.True);
            var away = bag.Read();
            Assert.That(away.StoredItems, Does.Contain(RelicInventory.OldGuitar));
            Assert.That(bag.ReadModifiers().DodgeEcho, Is.False);
            Assert.That(bag.ReadModifiers().MaxHealth, Is.EqualTo(0.08f).Within(0.0001f));
            Assert.That(bag.TryEquip(run, RelicInventory.OldGuitar, 1), Is.True);
            Assert.That(bag.Read().StoredItems, Does.Contain(RelicInventory.SewingTin));
            Assert.That(bag.ReadModifiers().MaxHealth, Is.EqualTo(0f));
            Assert.That(bag.ReadModifiers().DodgeEcho, Is.True);
            Assert.That(Count(bag, RelicInventory.OldGuitar), Is.EqualTo(1));
            bag.Reset(Guid.NewGuid(), HomecomingContract.StartingRelicSlots);
            Assert.That(bag.Read().Capacity, Is.EqualTo(2));
            Assert.That(bag.ReadModifiers().DodgeEcho, Is.False);
        }

        [Test]
        public void SnapshotDoesNotFollowLaterChanges()
        {
            var bag = Open();
            bag.TryGrant(Reward(run, "home", RelicInventory.SewingTin, 1));
            var before = bag.Read();
            bag.TryUnequip(run, 0);
            Assert.That(before.Slots[0].ItemId, Is.EqualTo(RelicInventory.SewingTin));
            Assert.That(before.Capacity, Is.EqualTo(3));
        }

        [Test]
        public void EchoConsumesOneDodgeAndFreezesWhilePaused()
        {
            var bag = Open();
            bag.TryGrant(Reward(run, "guitar", RelicInventory.OldGuitar, 0));
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 10f, out var pulse), Is.True);
            Assert.That(pulse.RadiusMeters, Is.EqualTo(3f));
            Assert.That(pulse.DamageFactor, Is.EqualTo(0.5f));
            Assert.That(pulse.MaxTargets, Is.EqualTo(12));
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 10f, out _), Is.False);
            Assert.That(bag.TryConsumeDodgeEcho(false, true, 30f, out _), Is.False);
            Assert.That(bag.TryConsumeDodgeEcho(true, false, 18f, out _), Is.False);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 18f, out _), Is.True);
            bag.TryUnequip(run, 0);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 40f, out _), Is.False);
            Assert.That(bag.TryReadAbility(RelicInventory.OldGuitar, 40f, out var view), Is.True);
            Assert.That(view.Equipped, Is.False);
            Assert.That(view.Owned, Is.True);
            Assert.That(view.Modifier.DodgeEcho, Is.True);
            Assert.That(view.EchoCooldownRemaining, Is.EqualTo(0f));
            Assert.That(view.ArtPath, Does.EndWith("old-guitar.jpg"));
        }

        [Test]
        public void FourSlotsCannotHoldEveryRelic()
        {
            var bag = Open();
            Assert.That(bag.TryGrant(Reward(run, "home", RelicInventory.SewingTin, 1)), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "silo", RelicInventory.WorkGloves, 1)), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "guitar", RelicInventory.OldGuitar, 0)), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "lamp", RelicInventory.LanternGlass, 0)), Is.False);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 0f, out _), Is.True);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 8f, out _), Is.True);
            Assert.That(bag.TryConsumeTug(true, true, 0f, out _, out _), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "lamp", RelicInventory.LanternGlass, 0)), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "key", RelicInventory.ClockKey, 0)), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "cup", RelicInventory.RainCup, 0)), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "lamp-fake", RelicInventory.LanternGlass, 1)), Is.False);
            var view = bag.Read();
            Assert.That(view.Capacity, Is.EqualTo(4));
            Assert.That(view.StoredItems.Count, Is.EqualTo(2));
            Assert.That(bag.ReadModifiers().MoveSpeed, Is.EqualTo(0.06f).Within(0.0001f));
            bag.TryUnequip(run, 3);
            Assert.That(bag.TryEquip(run, RelicInventory.ClockKey, 3), Is.True);
            Assert.That(bag.ReadModifiers().MoveSpeed, Is.EqualTo(0f));
            Assert.That(bag.ReadModifiers().DodgeCooldownReduction, Is.EqualTo(0.08f).Within(0.0001f));
        }

        [Test]
        public void MendTugSipAndLanternStayOnThePlayingClock()
        {
            var bag = Open();
            bag.TryGrant(Reward(run, "home", RelicInventory.SewingTin, 1));
            bag.TryGrant(Reward(run, "silo", RelicInventory.WorkGloves, 1));
            bag.TryGrant(Reward(run, "guitar", RelicInventory.OldGuitar, 0));
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 0f, out _), Is.True);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 8f, out _), Is.True);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 16f, out _), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "cup", RelicInventory.RainCup, 0)), Is.True);
            Assert.That(bag.TryConsumeMend(true, true, false, out _), Is.False);
            Assert.That(bag.TryConsumeMend(true, true, true, out float mend), Is.True);
            Assert.That(mend, Is.EqualTo(0.25f));
            Assert.That(bag.TryConsumeMend(true, true, true, out _), Is.False);
            Assert.That(bag.TryConsumeTug(true, true, 0f, out float radius, out int gems), Is.True);
            Assert.That(radius, Is.EqualTo(2.5f));
            Assert.That(gems, Is.EqualTo(6));
            Assert.That(bag.TryConsumeTug(false, true, 20f, out _, out _), Is.False);
            Assert.That(bag.TryConsumeTug(true, true, 12f, out _, out _), Is.True);
            Assert.That(bag.TryConsumeSip(true, true, false, 0f, out _), Is.False);
            Assert.That(bag.TryConsumeSip(true, true, true, 0f, out float sip), Is.True);
            Assert.That(sip, Is.EqualTo(0.06f));
            Assert.That(bag.TryConsumeSip(true, true, true, 16f, out _), Is.True);
            Assert.That(bag.TryUnequip(run, 3), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "lamp", RelicInventory.LanternGlass, 0)), Is.True);
            bag.Advance(3f, false, true, true);
            Assert.That(bag.ReadModifiers().MoveSpeed, Is.EqualTo(0.06f).Within(0.0001f));
            bag.Advance(4f, true, true, true);
            Assert.That(bag.ReadModifiers().MoveSpeed, Is.EqualTo(0.10f).Within(0.0001f));
            bag.TryUnequip(run, 3);
            Assert.That(bag.ReadModifiers().MoveSpeed, Is.EqualTo(0f));
        }

        [Test]
        public void ClockKeyShortensOnlyTheGuitarEcho()
        {
            var bag = Open();
            bag.TryGrant(Reward(run, "guitar", RelicInventory.OldGuitar, 0));
            bag.TryGrant(Reward(run, "home", RelicInventory.SewingTin, 1));
            Assert.That(bag.TryGrant(Reward(run, "key", RelicInventory.ClockKey, 0)), Is.False);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 0f, out _), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "lamp", RelicInventory.LanternGlass, 0)), Is.True);
            bag.Advance(4f, true, true, true);
            Assert.That(bag.TryUnequip(run, 2), Is.True);
            Assert.That(bag.TryGrant(Reward(run, "key", RelicInventory.ClockKey, 0)), Is.True);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 8f, out var pulse), Is.True);
            Assert.That(pulse.ReadyAgainAt, Is.EqualTo(14.5f).Within(0.0001f));
            Assert.That(pulse.DamageFactor, Is.EqualTo(0.5f));
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 14.4f, out _), Is.False);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 14.5f, out _), Is.True);
        }

        [Test]
        public void GuitarTearThenChromaticArriveOnlyAfterUse()
        {
            var bag = Open();
            bag.TryGrant(Reward(run, "guitar", RelicInventory.OldGuitar, 0));
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 0f, out var first), Is.True);
            Assert.That(bag.TryDequeueFx(out var cue), Is.True);
            Assert.That(cue.Kind, Is.EqualTo(RelicFxKind.StringRing));
            Assert.That(cue.TearMeters, Is.EqualTo(0f));
            Assert.That(cue.ChromaticPixels, Is.EqualTo(0f));
            Assert.That(first.DamageFactor, Is.EqualTo(0.5f));
            bag.TryConsumeDodgeEcho(true, true, 8f, out _);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 16f, out var torn), Is.True);
            Assert.That(bag.TryDequeueFx(out cue), Is.True);
            Assert.That(bag.TryDequeueFx(out cue), Is.True);
            Assert.That(cue.Kind, Is.EqualTo(RelicFxKind.StringTear));
            Assert.That(cue.TearMeters, Is.EqualTo(1.2f));
            Assert.That(torn.DamageFactor, Is.EqualTo(0.6f));
            bag.TryConsumeDodgeEcho(true, true, 24f, out _);
            Assert.That(bag.TryConsumeDodgeEcho(true, true, 32f, out var split), Is.True);
            while (bag.TryDequeueFx(out cue)) { }
            Assert.That(cue.Kind, Is.EqualTo(RelicFxKind.ChromaticHit));
            Assert.That(cue.ChromaticPixels, Is.EqualTo(1.6f));
            Assert.That(cue.TearMeters, Is.EqualTo(1.2f));
            Assert.That(split.RadiusMeters, Is.EqualTo(3.4f));
            Assert.That(split.DamageFactor, Is.EqualTo(0.65f));
            Assert.That(bag.UnlockHint(RelicInventory.LanternGlass), Is.EqualTo("可以领取"));
        }

        RelicInventory Open()
        {
            var bag = new RelicInventory();
            bag.Reset(run, HomecomingContract.StartingRelicSlots);
            return bag;
        }

        static PoiReward Reward(Guid run, string claim, string item, int slots)
        {
            return new PoiReward(run, claim, item, slots);
        }

        static int Count(RelicInventory bag, string item)
        {
            int n = 0;
            var view = bag.Read();
            for (int i = 0; i < view.Slots.Count; i++) if (view.Slots[i].ItemId == item) n++;
            for (int i = 0; i < view.StoredItems.Count; i++) if (view.StoredItems[i] == item) n++;
            return n;
        }
    }
}
