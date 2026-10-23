using System;
using DeadAir.InventorySystem;
using DeadAir.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Одна строка списка инвентаря: иконка, название, количество, вес строки.
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

        // Что вызвать при клике. Передаёт InventoryUI, чтобы строка сама не решала, что делать.
        private Action<ItemDefinition> clicked;

        private void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        // Заполняет строку данными стопки. Строки переиспользуются, поэтому вызывается при каждой перерисовке.
        public void Bind(ItemStack stack, bool selected, Action<ItemDefinition> onClick)
        {
            item = stack.Item;
            clicked = onClick;

            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;
            nameText.text = item.DisplayName;
            weightText.text = UiText.Weight(stack.Weight);

            // Количество показываем, только если предметов больше одного.
            if (stack.Count > 1)
                countText.text = "×" + stack.Count;
            else
                countText.text = "";

            // Выбранная строка чуть светлее фона и с янтарной полоской слева.
            // Цвет непрозрачный: полупрозрачный белый в линейном цветовом пространстве Unity выходит слишком ярким.
            if (selected)
                background.color = UiPalette.PanelRaised;
            else
                background.color = Color.clear;
            selectionMark.enabled = selected;
        }

        private void OnButtonClicked()
        {
            if (clicked != null)
                clicked(item);
        }
    }
}
