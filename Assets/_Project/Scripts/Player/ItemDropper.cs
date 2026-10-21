using DeadAir.Items;
using DeadAir.World;
using UnityEngine;

namespace DeadAir.Player
{
    // Выброс предмета из инвентаря: убирает одну единицу и кладёт её модель на землю перед игроком.
    public sealed class ItemDropper : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [Tooltip("Откуда вылетает выброшенный предмет")]
        [SerializeField] private Transform backpack;
        [SerializeField] private LayerMask groundMask;
        [Tooltip("Насколько впереди игрока ложится предмет, м")]
        [SerializeField] private float dropDistance = 1.1f;
        [Tooltip("Случайный разброс точки падения, чтобы предметы не ложились друг в друга, м")]
        [SerializeField] private float scatter = 0.35f;

        public bool CanDrop(ItemDefinition item) =>
            item != null && item.WorldPrefab != null && playerInventory.Inventory.CountOf(item) > 0;

        public bool Drop(ItemDefinition item)
        {
            if (!CanDrop(item) || !playerInventory.Inventory.Remove(item, 1))
                return false;

            Vector2 offset = Random.insideUnitCircle * scatter;
            Vector3 spot = transform.position + transform.forward * dropDistance + transform.right * offset.x + transform.forward * offset.y;
            if (Physics.Raycast(spot + Vector3.up * 2f, Vector3.down, out var hit, 4f, groundMask, QueryTriggerInteraction.Ignore))
                spot = hit.point;

            var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var dropped = WorldItem.SpawnDropped(item, 1, spot, rotation);
            ItemFlight.Drop(dropped.gameObject, backpack.position, spot);
            return true;
        }
    }
}
