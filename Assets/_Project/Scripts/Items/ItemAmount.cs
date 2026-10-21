using System;
using UnityEngine;

namespace DeadAir.Items
{
    // Предмет + количество: компонент разборки, содержимое кучки лута.
    [Serializable]
    public struct ItemAmount
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount;

        public ItemAmount(ItemDefinition item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }

        public ItemDefinition Item => item;
        public int Amount => amount;
        public float Weight => item != null ? item.Weight * amount : 0f;
    }
}
