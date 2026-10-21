using DeadAir.InventorySystem;
using UnityEngine;

namespace DeadAir.Player
{
    // Точка входа в инвентарь на сцене: лимит в инспекторе, создаёт Inventory и SalvageService
    // и отдаёт их сенсору и UI.
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float maxWeight = 15f;

        private Inventory inventory;
        private SalvageService salvage;

        // Создаются при первом обращении, поэтому порядок Awake у подписчиков не важен.
        public Inventory Inventory => inventory ??= new Inventory(maxWeight);
        public SalvageService Salvage => salvage ??= new SalvageService(Inventory);
    }
}
