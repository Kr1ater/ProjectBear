using System.Collections.Generic;
using System.Linq;
using DeadAir.InventorySystem;
using DeadAir.Items;
using NUnit.Framework;
using UnityEditor;

namespace DeadAir.Tests
{
    // Проверяет настоящие ассеты предметов, а не тестовые.
    public class ItemDataTests
    {
        private const string ItemsFolder = "Assets/_Project/Data/Items";

        private static List<ItemDefinition> LoadAllItems() =>
            AssetDatabase.FindAssets("t:ItemDefinition", new[] { ItemsFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();

        private static ItemDefinition Find(List<ItemDefinition> all, string id) =>
            all.Single(item => item.Id == id);

        [Test]
        public void AllItems_PassValidation()
        {
            var all = LoadAllItems();
            Assert.IsNotEmpty(all);

            var problems = new List<string>();
            foreach (var item in all)
            {
                var errors = new List<string>();
                item.Validate(errors);
                problems.AddRange(errors.Select(error => $"{item.name}: {error}"));
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void AllItems_HaveUniqueIds()
        {
            var duplicates = LoadAllItems()
                .GroupBy(item => item.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            Assert.IsEmpty(duplicates);
        }

        [Test]
        public void AllCategories_AreUsed()
        {
            var used = LoadAllItems().Select(item => item.Category).Distinct();

            CollectionAssert.AreEquivalent(System.Enum.GetValues(typeof(ItemCategory)), used);
        }

        // Числа из сценария видео в дизайн-документе.
        [Test]
        public void DemoScenario_CarbineFitsOnlyAfterSalvage()
        {
            var all = LoadAllItems();
            var inventory = new Inventory(15f);
            var salvage = new SalvageService(inventory);

            inventory.TryAdd(Find(all, "bolts"), 10);
            inventory.TryAdd(Find(all, "duct_tape"), 3);
            inventory.TryAdd(Find(all, "medkit"), 2);
            Assert.AreEqual(2.6f, inventory.CurrentWeight, 0.001f);

            inventory.TryAdd(Find(all, "old_radio"), 1);
            inventory.TryAdd(Find(all, "broken_pump"), 1);
            inventory.TryAdd(Find(all, "oxygen_cartridge"), 2);
            Assert.AreEqual(12.6f, inventory.CurrentWeight, 0.001f);

            var carbine = Find(all, "carbine");
            Assert.AreEqual(AddStatus.Overweight, inventory.TryAdd(carbine, 1).Status);

            Assert.AreEqual(SalvageStatus.Ok, salvage.Salvage(Find(all, "old_radio")).Status);
            Assert.AreEqual(SalvageStatus.Ok, salvage.Salvage(Find(all, "broken_pump")).Status);
            Assert.AreEqual(6.9f, inventory.CurrentWeight, 0.001f);

            Assert.AreEqual(AddStatus.Ok, inventory.TryAdd(carbine, 1).Status);
        }
    }
}
