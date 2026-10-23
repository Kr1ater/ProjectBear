using DeadAir.Items;

namespace DeadAir.InventorySystem
{
    public enum AddStatus
    {
        // Добавлено всё
        Ok,
        // Добавлена часть, остальное не влезло по весу
        Partial,
        // Не влезла ни одна единица
        Overweight
    }

    // Итог попытки добавить предмет
    public readonly struct AddResult
    {
        public AddResult(ItemDefinition item, int requested, int added, float freeWeight)
        {
            Item = item;
            Requested = requested;
            Added = added;
            FreeWeight = freeWeight;
        }

        public ItemDefinition Item { get; }
        public int Requested { get; }
        public int Added { get; }
        public int Rejected => Requested - Added;

        // Свободный вес до попытки.
        public float FreeWeight { get; }
        public float RequiredWeight => Item.Weight * Requested;

        public AddStatus Status =>
            Added == Requested ? AddStatus.Ok
            : Added > 0 ? AddStatus.Partial
            : AddStatus.Overweight;
    }
}
