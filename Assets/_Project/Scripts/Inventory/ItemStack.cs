using DeadAir.Items;

namespace DeadAir.InventorySystem
{
    // Запись в инвентаре. Неизменяемая: снаружи стопку не поменять, только через Inventory.
    public readonly struct ItemStack
    {
        public ItemStack(ItemDefinition item, int count)
        {
            Item = item;
            Count = count;
        }

        public ItemDefinition Item { get; }
        public int Count { get; }
        public float Weight => Item.Weight * Count;
        public bool IsFull => Count >= Item.MaxStack;

        internal ItemStack WithCount(int count) => new(Item, count);
    }
}
