using System.Collections.Generic;
using System.Globalization;
using DeadAir.InventorySystem;
using DeadAir.Items;
using UnityEngine;

namespace DeadAir.UI
{
    // Все тексты интерфейса, которые собираются из данных. Логика инвентаря строк не знает:
    // она возвращает статусы и числа, а здесь из них получаются сообщения.
    public static class UiText
    {
        // Вес с одним знаком после точки: 2.6, 15.0.
        // InvariantCulture — чтобы разделителем всегда была точка, даже на русской системе.
        public static string Weight(float weight)
        {
            return weight.ToString("0.0", CultureInfo.InvariantCulture);
        }

        // Свободное место округляем вниз до десятых, чтобы не обещать больше, чем есть.
        // Добавка 0.001 компенсирует неточность float: 2.4 может храниться как 2.39999.
        public static string FreeWeight(float weight)
        {
            float tenths = Mathf.Floor(weight * 10f + 0.001f);
            return Weight(tenths / 10f);
        }

        // Красит часть текста. TextMeshPro понимает теги вида <color=#EBA138>текст</color>.
        public static string Colored(string text, Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGB(color);
            return "<color=#" + hex + ">" + text + "</color>";
        }

        public static string Category(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Consumable: return "Расходник";
                case ItemCategory.RepairResource: return "Ресурс для ремонта";
                case ItemCategory.Junk: return "Хлам";
                case ItemCategory.Weapon: return "Оружие";
                default: return category.ToString();
            }
        }

        public static string Pickup(PickupMode mode)
        {
            switch (mode)
            {
                case PickupMode.Auto: return "автоматически";
                case PickupMode.Manual: return "по клавише E";
                default: return mode.ToString();
            }
        }

        // «+5 Болты», «+6 Болты (не хватает места для 4)», «Перегруз: Карабин — нужно 6.0, свободно 2.4».
        public static string PickupResult(AddResult result)
        {
            string name = result.Item.DisplayName;
            switch (result.Status)
            {
                case AddStatus.Ok:
                    return "+" + result.Added + " " + name;
                case AddStatus.Partial:
                    return "+" + result.Added + " " + name + " (не хватает места для " + result.Rejected + ")";
                default:
                    return "Перегруз: " + name + " — нужно " + Weight(result.RequiredWeight) +
                           ", свободно " + FreeWeight(result.FreeWeight);
            }
        }

        // «Разобрано: Старая рация → 2 Проводка, 1 Микросхема, 2 Болты» или причина отказа.
        public static string SalvageResult(SalvageResult result)
        {
            string name = result.Item.DisplayName;
            switch (result.Status)
            {
                case SalvageStatus.Ok:
                    return "Разобрано: " + name + " → " + Components(result.Item);
                case SalvageStatus.NotSalvageable:
                    return name + ": разбирать нечего";
                case SalvageStatus.NotInInventory:
                    return name + ": нет в инвентаре";
                default:
                    return name + ": компоненты не поместятся по весу";
            }
        }

        // «−1 Старая рация»
        public static string Dropped(ItemDefinition item, int amount)
        {
            return "−" + amount + " " + item.DisplayName;
        }

        // «2 Проводка, 1 Микросхема, 2 Болты»
        public static string Components(ItemDefinition item)
        {
            var parts = new List<string>();
            foreach (ItemAmount part in item.SalvageResult)
                parts.Add(part.Amount + " " + part.Item.DisplayName);
            return string.Join(", ", parts);
        }

        // Строка компонента в панели деталей: «2 × Проводка  0.6», вес серым.
        public static string ComponentLine(ItemAmount part)
        {
            return part.Amount + " × " + part.Item.DisplayName + "  " + Colored(Weight(part.Weight), UiPalette.Muted);
        }

        // Подпись над предметом в фокусе: «[E] Подобрать: Болты ×5  0.5».
        public static string PickupHint(ItemDefinition item, int amount)
        {
            string text = Colored("[E]", UiPalette.Amber) + " Подобрать: " + item.DisplayName;
            if (amount > 1)
                text += " ×" + amount;
            text += "  " + Colored(Weight(item.Weight * amount), UiPalette.Muted);
            return text;
        }
    }
}
