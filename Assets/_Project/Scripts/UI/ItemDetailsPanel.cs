using System.Text;
using DeadAir.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Панель деталей выбранного предмета. Только показывает: разборку и выброс запускает InventoryUI.
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
            content.SetActive(item != null);
            if (item == null)
                return;

            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;
            nameText.text = item.DisplayName;
            categoryText.text = UiText.Category(item.Category).ToUpperInvariant();
            descriptionText.text = item.Description;
            statsText.text =
                $"Вес: {UiText.Weight(item.Weight)} за шт. · всего {UiText.Weight(item.Weight * count)}\n" +
                $"В инвентаре: {count} · стопка до {item.MaxStack}\n" +
                $"Подбор: {UiText.Pickup(item.PickupMode)}";

            dropButton.interactable = canDrop;
            salvageButton.gameObject.SetActive(item.IsSalvageable);
            salvageBlock.SetActive(item.IsSalvageable);
            if (!item.IsSalvageable)
                return;

            var parts = new StringBuilder();
            foreach (var part in item.SalvageResult)
                parts.AppendLine($"{part.Amount} × {part.Item.DisplayName}  <color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Muted)}>{UiText.Weight(part.Weight)}</color>");
            componentsText.text = parts.ToString().TrimEnd();
            salvageButton.interactable = canSalvage;
        }
    }
}
