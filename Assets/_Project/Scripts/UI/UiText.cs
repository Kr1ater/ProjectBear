using System;
using System.Globalization;
using System.Linq;
using DeadAir.InventorySystem;
using DeadAir.Items;

namespace DeadAir.UI
{
    // Все тексты интерфейса, которые собираются из данных. Логика инвентаря строк не знает.
    public static class UiText
    {
        public static string Weight(float weight) => weight.ToString("0.0", CultureInfo.InvariantCulture);

        // Свободное место округляем вниз, чтобы не обещать больше, чем есть.
        public static string FreeWeight(float weight) => Weight((float)Math.Floor(weight * 10f + 0.001f) / 10f);

        public static string Category(ItemCategory category) => category switch
        {
            ItemCategory.Consumable => "Расходник",
            ItemCategory.RepairResource => "Ресурс для ремонта",
            ItemCategory.Junk => "Хлам",
            ItemCategory.Weapon => "Оружие",
            _ => category.ToString()
        };

        public static string CategoryTab(ItemCategory category) => category switch
        {
            ItemCategory.Consumable => "Расходники",
            ItemCategory.RepairResource => "Ремонт",
            ItemCategory.Junk => "Хлам",
            ItemCategory.Weapon => "Оружие",
            _ => category.ToString()
        };

        public static string Pickup(PickupMode mode) => mode switch
        {
            PickupMode.Auto => "автоматически",
            PickupMode.Manual => "по клавише E",
            _ => mode.ToString()
        };

        public static string PickupResult(AddResult result) => result.Status switch
        {
            AddStatus.Ok => $"+{result.Added} {result.Item.DisplayName}",
            AddStatus.Partial => $"+{result.Added} {result.Item.DisplayName} (не хватает места для {result.Rejected})",
            _ => $"Перегруз: {result.Item.DisplayName} — нужно {Weight(result.RequiredWeight)}, свободно {FreeWeight(result.FreeWeight)}"
        };

        public static string SalvageResult(SalvageResult result) => result.Status switch
        {
            SalvageStatus.Ok => $"Разобрано: {result.Item.DisplayName} → {Components(result.Item)}",
            SalvageStatus.NotSalvageable => $"{result.Item.DisplayName}: разбирать нечего",
            SalvageStatus.NotInInventory => $"{result.Item.DisplayName}: нет в инвентаре",
            _ => $"{result.Item.DisplayName}: компоненты не поместятся по весу"
        };

        public static string Dropped(ItemDefinition item, int amount) => $"−{amount} {item.DisplayName}";

        public static string Components(ItemDefinition item) =>
            string.Join(", ", item.SalvageResult.Select(part => $"{part.Amount} {part.Item.DisplayName}"));
    }
}
