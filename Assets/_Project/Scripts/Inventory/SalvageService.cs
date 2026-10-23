using System.Collections.Generic;
using DeadAir.Items;

namespace DeadAir.InventorySystem
{
    public enum SalvageStatus
    {
        Ok,
        NotSalvageable,
        NotInInventory,
        Overweight
    }

    public readonly struct SalvageResult
    {
        public SalvageResult(SalvageStatus status, ItemDefinition item)
        {
            Status = status;
            Item = item;
        }

        public SalvageStatus Status { get; }
        public ItemDefinition Item { get; }
        public IReadOnlyList<ItemAmount> Components => Item.SalvageResult;
    }
    
    public sealed class SalvageService
    {
        private readonly Inventory inventory; 

        public SalvageService(Inventory inventory)
        {
            this.inventory = inventory;
        }

        public SalvageStatus Check(ItemDefinition item)
        {
            if (item == null || !item.IsSalvageable)
                return SalvageStatus.NotSalvageable;
            if (inventory.CountOf(item) < 1)
                return SalvageStatus.NotInInventory;
            if (!inventory.CanExchange(new ItemAmount(item, 1), item.SalvageResult))
                return SalvageStatus.Overweight;
            return SalvageStatus.Ok;
        }

        public SalvageResult Salvage(ItemDefinition item)
        {
            var status = Check(item);
            if (status == SalvageStatus.Ok && !inventory.TryExchange(new ItemAmount(item, 1), item.SalvageResult))
                status = SalvageStatus.Overweight;
            return new SalvageResult(status, item);
        }
    }
}
