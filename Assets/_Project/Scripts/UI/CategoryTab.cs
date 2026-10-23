using DeadAir.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Вкладка-фильтр над списком инвентаря: «Все» или одна категория.
    public sealed class CategoryTab : MonoBehaviour
    {
        [SerializeField] private bool showAll;
        [SerializeField] private ItemCategory category;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Graphic underline;

        public Button Button => button;

        // Показывать ли этот предмет, когда выбрана эта вкладка.
        public bool Matches(ItemDefinition item)
        {
            if (showAll)
                return true;
            return item.Category == category;
        }

        public void SetSelected(bool selected)
        {
            if (selected)
                label.color = UiPalette.Text;
            else
                label.color = UiPalette.Muted;

            // Янтарная черта под выбранной вкладкой.
            underline.enabled = selected;
        }
    }
}
