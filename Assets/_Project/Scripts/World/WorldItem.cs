using DeadAir.Items;
using UnityEngine;

namespace DeadAir.World
{
    // Предмет на земле: ссылка на ассет + количество. Коллайдер — триггер на слое Loot,
    // чтобы сенсор его видел, а игрок проходил сквозь.
    public sealed class WorldItem : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount = 1;
        [Tooltip("Насколько выше коллайдера висит скобка, м")]
        [SerializeField] private float markerHeight = 0.2f;

        private Collider bounds;
        private bool droppedByPlayer;

        public ItemDefinition Item => item;
        public int Amount => amount;

        // Выброшенное игроком подбирается только по клавише: иначе вернулось бы в инвентарь с первым же шагом.
        public PickupMode PickupMode => droppedByPlayer ? PickupMode.Manual : item.PickupMode;

        public Vector3 MarkerPosition
        {
            get
            {
                var box = bounds.bounds;
                return new Vector3(box.center.x, box.max.y + markerHeight, box.center.z);
            }
        }

        private void Awake()
        {
            bounds = GetComponent<Collider>();
        }

        // Кладёт в мир модель предмета из его ассета. Используется при выбросе из инвентаря.
        public static WorldItem SpawnDropped(ItemDefinition item, int amount, Vector3 position, Quaternion rotation)
        {
            var spawned = Instantiate(item.WorldPrefab, position, rotation).GetComponent<WorldItem>();
            spawned.amount = amount;
            spawned.droppedByPlayer = true;
            return spawned;
        }

        // Забирает часть кучки и отправляет её в полёт к рюкзаку. Если забрано всё, улетает сам объект,
        // иначе — его копия, а кучка остаётся с меньшим количеством.
        public void Take(int count, Transform collector)
        {
            count = Mathf.Clamp(count, 0, amount);
            if (count == 0)
                return;

            amount -= count;
            var flying = amount == 0 ? gameObject : Instantiate(gameObject, transform.position, transform.rotation);
            ItemFlight.ToCollector(flying, collector);
        }
    }
}
