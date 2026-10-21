using System;
using DeadAir.InventorySystem;
using DeadAir.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Строка списка: иконка, название, количество, вес строки.
    public sealed class InventoryRow : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Graphic selectionMark;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text weightText;

        private ItemDefinition item;
        private Action<ItemDefinition> clicked;

        private void Awake()
        {
            button.onClick.AddListener(() => clicked?.Invoke(item));
        }

        public void Bind(ItemStack stack, bool selected, Action<ItemDefinition> onClick)
        {
            item = stack.Item;
            clicked = onClick;

            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;
            nameText.text = item.DisplayName;
            countText.text = stack.Count > 1 ? $"×{stack.Count}" : string.Empty;
            weightText.text = UiText.Weight(stack.Weight);

            // Непрозрачный цвет: полупрозрачный белый в линейном цветовом пространстве выходит слишком ярким.
            background.color = selected ? UiPalette.PanelRaised : Color.clear;
            selectionMark.enabled = selected;
        }
    }
}
