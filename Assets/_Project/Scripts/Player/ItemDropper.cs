using DeadAir.Items;
using DeadAir.World;
using UnityEngine;

namespace DeadAir.Player
{
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

        public bool CanDrop(ItemDefinition item)
        {
            if (item == null)
                return false;
            
            if (item.WorldPrefab == null)
                return false;
            
            return playerInventory.Inventory.CountOf(item) > 0;
        }

        public bool Drop(ItemDefinition item)
        {
            if (!CanDrop(item))
                return false;

            bool removed = playerInventory.Inventory.Remove(item, 1);
            if (!removed)
                return false;

            Vector3 dropPoint = FindDropPoint();
            Quaternion randomTurn = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            WorldItem dropped = WorldItem.SpawnDropped(item, 1, dropPoint, randomTurn);
            ItemFlight.Drop(dropped.gameObject, backpack.position, dropPoint);
            return true;
        }

        private Vector3 FindDropPoint()
        {
            // Точка перед игроком.
            Vector3 point = transform.position + transform.forward * dropDistance;

            // Небольшой случайный сдвиг чтобы предметы не ложились в одну точку.
            Vector2 randomShift = Random.insideUnitCircle * scatter;
            point += transform.right * randomShift.x;
            point += transform.forward * randomShift.y;

            // Луч сверху вниз находит поверхность под точкой: грунт, камень или ящик.
            Vector3 rayStart = point + Vector3.up * 2f;
            bool hitGround = Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 4f, groundMask, QueryTriggerInteraction.Ignore);
            if (hitGround)
                point = hit.point;

            return point;
        }
    }
}
