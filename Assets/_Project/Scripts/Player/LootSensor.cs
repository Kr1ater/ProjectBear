using System;
using System.Collections.Generic;
using DeadAir.InventorySystem;
using DeadAir.Items;
using DeadAir.World;
using UnityEngine;

namespace DeadAir.Player
{
    // Сенсор лута вокруг игрока: находит предметы, подбирает Auto сам, Manual — по клавише,
    // и отдаёт HUD список видимого лута и предмет в фокусе.
    public sealed class LootSensor : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private InputReader input;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private LayerMask lootMask;
        [Tooltip("Что закрывает лут от камеры: предметы за стеной не подсвечиваются")]
        [SerializeField] private LayerMask occluderMask;

        [Header("Радиусы, м")]
        [SerializeField] private float markerRadius = 6f;
        [SerializeField] private float interactRadius = 2.5f;
        [SerializeField] private float autoPickupRadius = 1.5f;

        [Tooltip("Как часто искать лут вокруг, с")]
        [SerializeField] private float scanInterval = 0.1f;

        private readonly Collider[] hits = new Collider[64];
        private readonly List<WorldItem> visibleItems = new();
        // Auto-предметы, о перегрузе которых уже сообщили. Очищается, когда игрок отходит.
        private readonly HashSet<WorldItem> reportedOverweight = new();
        private float nextScanTime;

        // Любая попытка подбора: успешная, частичная или отказ по весу.
        public event Action<AddResult> PickupAttempted;

        public IReadOnlyList<WorldItem> VisibleItems => visibleItems;
        public WorldItem Focused { get; private set; }
        public float MarkerRadius => markerRadius;

        private void OnEnable() => input.InteractPressed += PickUpFocused;

        private void OnDisable() => input.InteractPressed -= PickUpFocused;

        private void Update()
        {
            if (Time.time >= nextScanTime)
            {
                nextScanTime = Time.time + scanInterval;
                Scan();
            }
            UpdateFocus();
        }

        private void Scan()
        {
            visibleItems.Clear();
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, markerRadius, hits, lootMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                var item = hits[i].GetComponentInParent<WorldItem>();
                if (item == null || item.Amount == 0 || visibleItems.Contains(item))
                    continue;

                // Автоподбор не зависит от того, куда смотрит камера.
                if (item.Item.PickupMode == PickupMode.Auto && DistanceTo(item) <= autoPickupRadius)
                {
                    TryAutoPickup(item);
                    if (item.Amount == 0)
                        continue;
                }

                if (IsOnScreenAndVisible(item))
                    visibleItems.Add(item);
            }

            reportedOverweight.RemoveWhere(item => item == null || DistanceTo(item) > autoPickupRadius);
        }

        private void TryAutoPickup(WorldItem item)
        {
            if (reportedOverweight.Contains(item))
            {
                // Молча пробуем снова: вдруг место освободилось после разборки.
                if (!playerInventory.Inventory.CanAdd(item.Item))
                    return;
                reportedOverweight.Remove(item);
            }

            var result = PickUp(item);
            if (result.Status != AddStatus.Ok)
                reportedOverweight.Add(item);
        }

        private void PickUpFocused()
        {
            if (Focused != null)
                PickUp(Focused);
        }

        private AddResult PickUp(WorldItem item)
        {
            var result = playerInventory.Inventory.TryAdd(item.Item, item.Amount);
            if (result.Added > 0)
                item.Take(result.Added);
            PickupAttempted?.Invoke(result);
            return result;
        }

        // В фокусе — Manual-предмет в радиусе взаимодействия, ближайший к центру экрана и к игроку.
        private void UpdateFocus()
        {
            Focused = null;
            float bestScore = float.MaxValue;

            foreach (var item in visibleItems)
            {
                if (item == null || item.Amount == 0 || item.Item.PickupMode != PickupMode.Manual)
                    continue;

                float distance = DistanceTo(item);
                if (distance > interactRadius)
                    continue;

                Vector3 viewport = viewCamera.WorldToViewportPoint(item.MarkerPosition);
                float offCenter = Vector2.Distance(viewport, new Vector2(0.5f, 0.5f));
                float score = offCenter + 0.5f * distance / interactRadius;
                if (score < bestScore)
                {
                    bestScore = score;
                    Focused = item;
                }
            }
        }

        private bool IsOnScreenAndVisible(WorldItem item)
        {
            Vector3 marker = item.MarkerPosition;
            Vector3 viewport = viewCamera.WorldToViewportPoint(marker);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                return false;

            return !Physics.Linecast(viewCamera.transform.position, marker, occluderMask, QueryTriggerInteraction.Ignore);
        }

        private float DistanceTo(WorldItem item) => Vector3.Distance(transform.position, item.transform.position);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.92f, 0.63f, 0.22f);
            Gizmos.DrawWireSphere(transform.position, autoPickupRadius);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
            Gizmos.color = new Color(0.55f, 0.52f, 0.47f);
            Gizmos.DrawWireSphere(transform.position, markerRadius);
        }
    }
}
