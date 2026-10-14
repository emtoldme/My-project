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
        }

        RelicInventory Open()
        {
            var bag = new RelicInventory();
            bag.Reset(run, HomecomingContract.StartingRelicSlots);
            return bag;
        }

        static PoiReward Reward(Guid run, string claim, string item, int slots)
        {
            int cell = 0; foreach (char c in claim) cell = unchecked(cell * 31 + c);
            string template = item == RelicInventory.SewingTin ? "poi.old-home" : item == RelicInventory.WorkGloves ? "poi.old-silo" : "poi.old-guitar";
            return new PoiReward(run, HomecomingContract.SiteId(14104, cell, 0, template), item, slots);
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
