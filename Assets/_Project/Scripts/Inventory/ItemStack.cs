using DeadAir.Items;

namespace DeadAir.InventorySystem
{
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
