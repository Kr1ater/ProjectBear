using System.Collections.Generic;
using DeadAir.InventorySystem;
using DeadAir.Items;
using DeadAir.Player;
using TMPro;
using UnityEngine;

namespace DeadAir.UI
{
    // Экран инвентаря: список стопок, вкладки по категориям, детали, разборка и выброс.
    // Данные сам не меняет — только через Inventory, SalvageService и ItemDropper.
    // Перерисовывается по событию Inventory.OnChanged, то есть после любого изменения инвентаря.
    public sealed class InventoryUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private ItemDropper dropper;
        [SerializeField] private InputReader input;
        [SerializeField] private MessageFeed messages;

        [Header("Экран")]
        [SerializeField] private GameObject screen;
        [SerializeField] private WeightBar weightBar;
        [SerializeField] private CategoryTab[] tabs;
        [Tooltip("Готовые строки списка на сцене. Сколько их, столько стопок помещается в список")]
        [SerializeField] private InventoryRow[] rows;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private ItemDetailsPanel details;

        // Стопки, которые проходят фильтр текущей вкладки.
        private readonly List<ItemStack> visibleStacks = new();

        private CategoryTab activeTab;
        private ItemDefinition selected;
        private int selectedIndex;

        private Inventory Inventory => playerInventory.Inventory;

        public bool IsOpen => screen.activeSelf;

        private void Awake()
        {
            foreach (CategoryTab tab in tabs)
                tab.Button.onClick.AddListener(() => SelectTab(tab));

            details.SalvageButton.onClick.AddListener(SalvageSelected);
            details.DropButton.onClick.AddListener(DropSelected);

            activeTab = tabs[0];
            screen.SetActive(false);
        }

        private void OnEnable()
        {
            input.ToggleInventoryPressed += Toggle;
            input.SalvagePressed += SalvageSelected;
            input.DropPressed += DropSelected;
            Inventory.OnChanged += Refresh;
        }

        private void OnDisable()
        {
            input.ToggleInventoryPressed -= Toggle;
            input.SalvagePressed -= SalvageSelected;
            input.DropPressed -= DropSelected;
            Inventory.OnChanged -= Refresh;
        }

        public void Toggle()
        {
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            screen.SetActive(open);

            // Пока инвентарь открыт, герой стоит, камера не крутится, курсор свободен.
            input.SetGameplayEnabled(!open);

            if (open)
                Refresh();
        }

        private void SelectTab(CategoryTab tab)
        {
            activeTab = tab;
            selected = null;
            selectedIndex = 0;
            Refresh();
        }

        private void SelectItem(ItemDefinition item)
        {
            selected = item;
            Refresh();
        }

        // X или кнопка «Разобрать».
        private void SalvageSelected()
        {
            if (!IsOpen || selected == null || !selected.IsSalvageable)
                return;

            SalvageResult result = playerInventory.Salvage.Salvage(selected);
            bool failed = result.Status != SalvageStatus.Ok;
            messages.Show(UiText.SalvageResult(result), failed);

            if (failed)
                weightBar.Flash();
        }

        // G или кнопка «Выбросить»: выбрасывает одну единицу выбранного предмета.
        private void DropSelected()
        {
            if (!IsOpen || selected == null)
                return;

            ItemDefinition item = selected;
            bool dropped = dropper.Drop(item);
            if (dropped)
                messages.Show(UiText.Dropped(item, 1));
        }

        private void Refresh()
        {
            // Закрытый экран не перерисовываем: при открытии SetOpen сам вызовет Refresh.
            if (!IsOpen)
                return;

            HighlightActiveTab();
            CollectVisibleStacks();
            KeepSelectionValid();
            DrawRows();
            DrawDetails();
        }

        private void HighlightActiveTab()
        {
            foreach (CategoryTab tab in tabs)
                tab.SetSelected(tab == activeTab);
        }

        private void CollectVisibleStacks()
        {
            visibleStacks.Clear();
            foreach (ItemStack stack in Inventory.Stacks)
            {
                if (activeTab.Matches(stack.Item))
                    visibleStacks.Add(stack);
            }
        }

        // Следит, чтобы выбранный предмет был в списке.
        private void KeepSelectionValid()
        {
            int index = FindVisibleIndex(selected);
            if (index >= 0)
            {
                selectedIndex = index;
                return;
            }

            // Выбранного предмета больше нет: разобран, выброшен или скрыт вкладкой.
            if (visibleStacks.Count == 0)
            {
                selected = null;
                selectedIndex = 0;
                return;
            }

            // Выбираем строку, которая встала на его место. Так хлам можно разбирать подряд одной клавишей X.
            if (selectedIndex > visibleStacks.Count - 1)
                selectedIndex = visibleStacks.Count - 1;
            selected = visibleStacks[selectedIndex].Item;
        }

        private int FindVisibleIndex(ItemDefinition item)
        {
            for (int i = 0; i < visibleStacks.Count; i++)
            {
                if (visibleStacks[i].Item == item)
                    return i;
            }
            return -1;
        }

        // Заполняет готовые строки на сцене: по строке на стопку, лишние строки выключает.
        // Если стопок больше, чем строк, в списке показываются первые rows.Length стопок.
        private void DrawRows()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                InventoryRow row = rows[i];
                if (i < visibleStacks.Count)
                {
                    // Сначала включаем: выключенная строка ещё не получила Awake и не подписалась на клик.
                    row.gameObject.SetActive(true);
                    ItemStack stack = visibleStacks[i];
                    bool isSelected = stack.Item == selected;
                    row.Bind(stack, isSelected, SelectItem);
                }
                else
                {
                    row.gameObject.SetActive(false);
                }
            }

            emptyText.gameObject.SetActive(visibleStacks.Count == 0);
        }

        private void DrawDetails()
        {
            int count = 0;
            if (selected != null)
                count = Inventory.CountOf(selected);

            bool canSalvage = playerInventory.Salvage.Check(selected) == SalvageStatus.Ok;
            bool canDrop = dropper.CanDrop(selected);
            details.Show(selected, count, canSalvage, canDrop);
        }
    }
}
