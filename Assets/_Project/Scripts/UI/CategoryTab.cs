using DeadAir.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Вкладка-фильтр списка: «Все» или одна категория.
    public sealed class CategoryTab : MonoBehaviour
    {
        [SerializeField] private bool showAll;
        [SerializeField] private ItemCategory category;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Graphic underline;

        public Button Button => button;

        public bool Matches(ItemDefinition item) => showAll || item.Category == category;

        public void SetSelected(bool selected)
        {
            label.color = selected ? UiPalette.Text : UiPalette.Muted;
            underline.enabled = selected;
        }
    }
}
