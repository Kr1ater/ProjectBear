using DeadAir.Items;
using UnityEngine;

namespace DeadAir.World
{
    public sealed class WorldItem : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount = 1;
        [Tooltip("Насколько выше коллайдера висит скобка, м")]
        [SerializeField] private float markerHeight = 0.2f;

        private Collider bounds;
        
        private bool autoPickupOnHold;

        public ItemDefinition Item => item;
        public int Amount => amount;
        public bool IsAutoPickupOnHold => autoPickupOnHold;

        public PickupMode PickupMode
        {
            get
            {
                if (autoPickupOnHold)
                    return PickupMode.Manual;
                return item.PickupMode;
            }
        }
        
        public Vector3 MarkerPosition
        {
            get
            {
                Bounds box = bounds.bounds;
                return new Vector3(box.center.x, box.max.y + markerHeight, box.center.z);
            }
        }

        private void Awake()
        {
            bounds = GetComponent<Collider>();
        }
        
        public static WorldItem SpawnDropped(ItemDefinition item, int amount, Vector3 position, Quaternion rotation)
        {
            GameObject instance = Instantiate(item.WorldPrefab, position, rotation);
            WorldItem dropped = instance.GetComponent<WorldItem>();
            dropped.amount = amount;
            dropped.autoPickupOnHold = true;
            return dropped;
        }

        public void ResumeAutoPickup()
        {
            autoPickupOnHold = false;
        }

        // Забирает count единиц из кучки и отправляет их в полёт к рюкзаку.
        public void Take(int count, Transform collector)
        {
            count = Mathf.Clamp(count, 0, amount);
            if (count == 0)
                return;

            amount -= count;

            if (amount == 0)
            {
                // Забрали всё — к рюкзаку улетает сам предмет.
                ItemFlight.ToCollector(gameObject, collector);
            }
            else
            {
                // Забрали часть — улетает копия, а кучка остаётся на месте с меньшим количеством.
                GameObject copy = Instantiate(gameObject, transform.position, transform.rotation);
                ItemFlight.ToCollector(copy, collector);
            }
        }
    }
}
