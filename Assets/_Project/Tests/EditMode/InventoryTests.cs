using DeadAir.InventorySystem;
using DeadAir.Items;
using NUnit.Framework;

namespace DeadAir.Tests
{
    public class InventoryTests
    {
        private TestItems items;
        private ItemDefinition bolts;
        private ItemDefinition carbine;

        [SetUp]
        public void SetUp()
        {
            items = new TestItems();
            bolts = items.Create("bolts", ItemCategory.RepairResource, 0.1f, maxStack: 50, PickupMode.Auto);
            carbine = items.Create("carbine", ItemCategory.Weapon, 6f, maxStack: 1);
        }

        [TearDown]
        public void TearDown() => items.DestroyAll();

        [Test]
        public void TryAdd_WithinLimit_AddsAllAndUpdatesWeight()
        {
            var inventory = new Inventory(15f);

            var result = inventory.TryAdd(bolts, 10);

            Assert.AreEqual(AddStatus.Ok, result.Status);
            Assert.AreEqual(10, inventory.CountOf(bolts));
            Assert.AreEqual(1f, inventory.CurrentWeight, 0.0001f);
        }

        [Test]
        public void TryAdd_OverLimit_AddsNothingAndReportsWhy()
        {
            var inventory = new Inventory(15f);
            var heavy = items.Create("heavy", ItemCategory.Junk, 12.6f);
            inventory.TryAdd(heavy, 1);

            var result = inventory.TryAdd(carbine, 1);

            Assert.AreEqual(AddStatus.Overweight, result.Status);
            Assert.AreEqual(0, inventory.CountOf(carbine));
            Assert.AreEqual(6f, result.RequiredWeight, 0.0001f);
            Assert.AreEqual(2.4f, result.FreeWeight, 0.0001f);
            Assert.AreEqual(12.6f, inventory.CurrentWeight, 0.0001f);
        }

        [Test]
        public void TryAdd_StackDoesNotFit_TakesOnlyWhatFits()
        {
            var inventory = new Inventory(15f);
            var heavy = items.Create("heavy", ItemCategory.Junk, 14.4f);
            inventory.TryAdd(heavy, 1);

            var result = inventory.TryAdd(bolts, 10);

            Assert.AreEqual(AddStatus.Partial, result.Status);
            Assert.AreEqual(6, result.Added);
            Assert.AreEqual(4, result.Rejected);
            Assert.AreEqual(6, inventory.CountOf(bolts));
        }

        [Test]
        public void TryAdd_ManySmallItems_FillsLimitExactlyDespiteFloatError()
        {
            var inventory = new Inventory(15f);

            for (int i = 0; i < 150; i++)
                Assert.AreEqual(AddStatus.Ok, inventory.TryAdd(bolts, 1).Status, $"единица {i + 1}");

            Assert.AreEqual(AddStatus.Overweight, inventory.TryAdd(bolts, 1).Status);
        }

        [Test]
        public void TryAdd_FillsExistingStackBeforeOpeningNewOne()
        {
            var inventory = new Inventory(15f);

            inventory.TryAdd(bolts, 30);
            inventory.TryAdd(bolts, 30);

            Assert.AreEqual(2, inventory.Stacks.Count);
            Assert.AreEqual(50, inventory.Stacks[0].Count);
            Assert.AreEqual(10, inventory.Stacks[1].Count);
        }

        [Test]
        public void Remove_TakesFromLastStackFirst()
        {
            var inventory = new Inventory(15f);
            inventory.TryAdd(bolts, 60);

            bool removed = inventory.Remove(bolts, 15);

            Assert.IsTrue(removed);
            Assert.AreEqual(1, inventory.Stacks.Count);
            Assert.AreEqual(45, inventory.Stacks[0].Count);
            Assert.AreEqual(4.5f, inventory.CurrentWeight, 0.0001f);
        }

        [Test]
        public void Remove_MoreThanHeld_ChangesNothing()
        {
            var inventory = new Inventory(15f);
            inventory.TryAdd(bolts, 5);

            Assert.IsFalse(inventory.Remove(bolts, 6));
            Assert.AreEqual(5, inventory.CountOf(bolts));
        }

        [Test]
        public void OnChanged_FiresOnlyWhenContentsChange()
        {
            var inventory = new Inventory(5f);
            int calls = 0;
            inventory.OnChanged += () => calls++;

            inventory.TryAdd(bolts, 10);
            inventory.TryAdd(carbine, 1);

            Assert.AreEqual(1, calls);
        }
    }
}
