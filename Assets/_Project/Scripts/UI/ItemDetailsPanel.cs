using System.Collections.Generic;
using DeadAir.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Панель деталей выбранного предмета. Только показывает данные.
    // Что делать по нажатию кнопок «Разобрать» и «Выбросить», решает InventoryUI.
    public sealed class ItemDetailsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject content;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private GameObject salvageBlock;
        [SerializeField] private TMP_Text componentsText;
        [SerializeField] private Button salvageButton;
        [SerializeField] private Button dropButton;

        public Button SalvageButton => salvageButton;
        public Button DropButton => dropButton;

        public void Show(ItemDefinition item, int count, bool canSalvage, bool canDrop)
        {
            // Ничего не выбрано (например, инвентарь пуст) — прячем панель целиком.
            if (item == null)
            {
                content.SetActive(false);
                return;
            }
            content.SetActive(true);

            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;
            nameText.text = item.DisplayName;
            categoryText.text = UiText.Category(item.Category).ToUpperInvariant();
            descriptionText.text = item.Description;
            statsText.text = BuildStats(item, count);

            dropButton.interactable = canDrop;

            // Блок разборки и кнопка «Разобрать» есть только у хлама.
            salvageButton.gameObject.SetActive(item.IsSalvageable);
            salvageBlock.SetActive(item.IsSalvageable);
            if (item.IsSalvageable)
            {
                componentsText.text = BuildComponents(item);
                salvageButton.interactable = canSalvage;
            }
        }

        // Три строки: вес, количество, способ подбора.
        private static string BuildStats(ItemDefinition item, int count)
        {
            string weightLine = "Вес: " + UiText.Weight(item.Weight) + " за шт. · всего " + UiText.Weight(item.Weight * count);
            string countLine = "В инвентаре: " + count + " · стопка до " + item.MaxStack;
            string pickupLine = "Подбор: " + UiText.Pickup(item.PickupMode);
            return weightLine + "\n" + countLine + "\n" + pickupLine;
        }

        // По строке на каждый компонент разборки.
        private static string BuildComponents(ItemDefinition item)
        {
            var lines = new List<string>();
            foreach (ItemAmount part in item.SalvageResult)
                lines.Add(UiText.ComponentLine(part));
            return string.Join("\n", lines);
        }
    }
}
