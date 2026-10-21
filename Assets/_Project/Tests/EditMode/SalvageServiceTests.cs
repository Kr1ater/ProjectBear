using DeadAir.InventorySystem;
using DeadAir.Items;
using NUnit.Framework;

namespace DeadAir.Tests
{
    public class SalvageServiceTests
    {
        private TestItems items;
        private ItemDefinition bolts;
        private ItemDefinition wiring;
        private ItemDefinition chip;
        private ItemDefinition radio;

        [SetUp]
        public void SetUp()
        {
            items = new TestItems();
            bolts = items.Create("bolts", ItemCategory.RepairResource, 0.1f, maxStack: 50);
            wiring = items.Create("wiring", ItemCategory.RepairResource, 0.3f, maxStack: 20);
            chip = items.Create("chip", ItemCategory.RepairResource, 0.1f, maxStack: 20);
            radio = items.Create("radio", ItemCategory.Junk, 3f, maxStack: 1, PickupMode.Manual,
                new ItemAmount(wiring, 2), new ItemAmount(chip, 1), new ItemAmount(bolts, 2));
        }

        [TearDown]
        public void TearDown() => items.DestroyAll();

        [Test]
        public void Salvage_Junk_ReplacesOneUnitWithComponents()
        {
            var inventory = new Inventory(15f);
            inventory.TryAdd(radio, 1);
            var salvage = new SalvageService(inventory);

            var result = salvage.Salvage(radio);

            Assert.AreEqual(SalvageStatus.Ok, result.Status);
            Assert.AreEqual(0, inventory.CountOf(radio));
            Assert.AreEqual(2, inventory.CountOf(wiring));
            Assert.AreEqual(1, inventory.CountOf(chip));
            Assert.AreEqual(2, inventory.CountOf(bolts));
            Assert.AreEqual(0.9f, inventory.CurrentWeight, 0.0001f);
        }

        [Test]
        public void Salvage_NotJunk_IsRefused()
        {
            var inventory = new Inventory(15f);
            inventory.TryAdd(wiring, 1);
            var salvage = new SalvageService(inventory);

            Assert.AreEqual(SalvageStatus.NotSalvageable, salvage.Salvage(wiring).Status);
            Assert.AreEqual(1, inventory.CountOf(wiring));
        }

        [Test]
        public void Salvage_ItemNotInInventory_IsRefused()
        {
            var salvage = new SalvageService(new Inventory(15f));

            Assert.AreEqual(SalvageStatus.NotInInventory, salvage.Salvage(radio).Status);
        }

        [Test]
        public void Salvage_ComponentsDoNotFit_ChangesNothing()
        {
            // Данные, которые валидация не пропустила бы: компоненты тяжелее хлама.
            var plate = items.Create("plate", ItemCategory.RepairResource, 2f);
            var badJunk = items.Create("bad_junk", ItemCategory.Junk, 1f, maxStack: 1, PickupMode.Manual,
                new ItemAmount(plate, 3));
            var inventory = new Inventory(6f);
            inventory.TryAdd(badJunk, 1);
            inventory.TryAdd(plate, 2);
            int calls = 0;
            inventory.OnChanged += () => calls++;

            var result = new SalvageService(inventory).Salvage(badJunk);

            Assert.AreEqual(SalvageStatus.Overweight, result.Status);
            Assert.AreEqual(1, inventory.CountOf(badJunk));
            Assert.AreEqual(2, inventory.CountOf(plate));
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Salvage_RaisesOnChangedOnce()
        {
            var inventory = new Inventory(15f);
            inventory.TryAdd(radio, 1);
            int calls = 0;
            inventory.OnChanged += () => calls++;

            new SalvageService(inventory).Salvage(radio);

            Assert.AreEqual(1, calls);
        }
    }
}
