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

        public ItemDefinition Item => item;
        public int Amount => amount;

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

        // Забирает часть кучки. Когда ничего не осталось, предмет исчезает из мира.
        public void Take(int count)
        {
            amount -= Mathf.Clamp(count, 0, amount);
            if (amount == 0)
                Destroy(gameObject);
        }
    }
}
