using System.Collections.Generic;
using DeadAir.InventorySystem;
using DeadAir.Items;
using DeadAir.Player;
using TMPro;
using UnityEngine;

namespace DeadAir.UI
{
    // Экран инвентаря: список стопок, фильтр по категориям, детали, разборка и выброс.
    // Данные не меняет сам — только через Inventory, SalvageService и ItemDropper.
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
        [SerializeField] private InventoryRow rowPrefab;
        [SerializeField] private RectTransform rowContainer;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private ItemDetailsPanel details;

        private readonly List<InventoryRow> rows = new();
        private readonly List<ItemStack> visibleStacks = new();
        private CategoryTab activeTab;
        private ItemDefinition selected;
        private int selectedIndex;

        private Inventory Inventory => playerInventory.Inventory;

        public bool IsOpen => screen.activeSelf;

        private void Awake()
        {
            foreach (var tab in tabs)
            {
                var captured = tab;
                tab.Button.onClick.AddListener(() => SelectTab(captured));
            }
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

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool open)
        {
            screen.SetActive(open);
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

        private void SalvageSelected()
        {
            if (!IsOpen || selected == null || !selected.IsSalvageable)
                return;

            var result = playerInventory.Salvage.Salvage(selected);
            bool failed = result.Status != SalvageStatus.Ok;
            messages.Show(UiText.SalvageResult(result), attention: failed);
            if (failed)
                weightBar.Flash();
        }

        // Выбрасывает одну единицу выбранного предмета.
        private void DropSelected()
        {
            if (!IsOpen || selected == null)
                return;

            var item = selected;
            if (dropper.Drop(item))
                messages.Show(UiText.Dropped(item, 1));
        }

        private void Refresh()
        {
            if (!IsOpen)
                return;

            foreach (var tab in tabs)
                tab.SetSelected(tab == activeTab);

            visibleStacks.Clear();
            foreach (var stack in Inventory.Stacks)
                if (activeTab.Matches(stack.Item))
                    visibleStacks.Add(stack);

            // Выбранный предмет пропал (разобран последний) — выбираем строку, что встала на его место:
            // так хлам можно разбирать подряд, не возвращаясь мышью к списку.
            int index = visibleStacks.FindIndex(stack => stack.Item == selected);
            if (index < 0)
            {
                index = Mathf.Min(selectedIndex, visibleStacks.Count - 1);
                selected = index >= 0 ? visibleStacks[index].Item : null;
            }
            selectedIndex = Mathf.Max(index, 0);

            for (int i = 0; i < visibleStacks.Count; i++)
                GetRow(i).Bind(visibleStacks[i], visibleStacks[i].Item == selected, SelectItem);
            for (int i = visibleStacks.Count; i < rows.Count; i++)
                rows[i].gameObject.SetActive(false);

            emptyText.gameObject.SetActive(visibleStacks.Count == 0);
            int count = selected != null ? Inventory.CountOf(selected) : 0;
            details.Show(selected, count,
                canSalvage: playerInventory.Salvage.Check(selected) == SalvageStatus.Ok,
                canDrop: dropper.CanDrop(selected));
        }

        private InventoryRow GetRow(int index)
        {
            if (index == rows.Count)
                rows.Add(Instantiate(rowPrefab, rowContainer));

            var row = rows[index];
            row.gameObject.SetActive(true);
            return row;
        }
    }
}
