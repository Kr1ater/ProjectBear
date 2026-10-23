using DeadAir.InventorySystem;
using UnityEngine;

namespace DeadAir.Player
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float maxWeight = 15f;

        private Inventory inventory;
        private SalvageService salvage;
        
        public Inventory Inventory
        {
            get
            {
                if (inventory == null)
                    inventory = new Inventory(maxWeight);
                return inventory;
            }
        }

        public SalvageService Salvage
        {
            get
            {
                if (salvage == null)
                    salvage = new SalvageService(Inventory);
                return salvage;
            }
        }
    }
}
