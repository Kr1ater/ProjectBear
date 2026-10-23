using System;
using System.Collections.Generic;
using DeadAir.InventorySystem;
using DeadAir.Items;
using DeadAir.World;
using UnityEngine;

namespace DeadAir.Player
{
    public sealed class LootSensor : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private InputReader input;
        [SerializeField] private Camera viewCamera;
        [Tooltip("Точка на рюкзаке, куда притягиваются подобранные предметы")]
        [SerializeField] private Transform backpack;
        [SerializeField] private LayerMask lootMask;
        [Tooltip("Что закрывает лут от камеры")]
        [SerializeField] private LayerMask occluderMask;

        [Header("Радиусы, м")]
        [SerializeField] private float markerRadius = 6f;
        [SerializeField] private float interactRadius = 2.5f;
        [SerializeField] private float autoPickupRadius = 1.5f;
        [Tooltip("На сколько отойти от выброшенного предмета, чтобы он снова подбирался сам")]
        [SerializeField] private float droppedItemReleaseDistance = 2.5f;

        [Tooltip("Как часто искать лут вокруг, с")]
        [SerializeField] private float scanInterval = 0.1f;

        private readonly Collider[] hits = new Collider[64];
        private readonly List<WorldItem> visibleItems = new();
        // Предметы, о перегрузе которых уже сообщили, — чтобы не повторять сообщение.
        private readonly HashSet<WorldItem> reportedOverweight = new();
        private float nextScanTime;

        public event Action<AddResult> PickupAttempted;

        public IReadOnlyList<WorldItem> VisibleItems => visibleItems;
        public WorldItem Focused { get; private set; }
        public float MarkerRadius => markerRadius;

        public bool IsVisible(WorldItem item) => visibleItems.Contains(item);

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
            
            int count = Physics.OverlapSphereNonAlloc(transform.position, markerRadius, hits, lootMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                WorldItem item = hits[i].GetComponentInParent<WorldItem>();

                if (IsLyingOnGround(item) && !visibleItems.Contains(item))
                    HandleNearbyItem(item);
            }

            reportedOverweight.RemoveWhere(item => item == null || DistanceTo(item) > autoPickupRadius);
        }

        private void HandleNearbyItem(WorldItem item)
        {
            float distance = DistanceTo(item);

            if (item.IsAutoPickupOnHold && distance > droppedItemReleaseDistance)
                item.ResumeAutoPickup();

            if (item.PickupMode == PickupMode.Auto && distance <= autoPickupRadius)
                TryAutoPickup(item);

            if (IsLyingOnGround(item) && IsOnScreen(item) && !IsBehindObstacle(item))
                visibleItems.Add(item);
        }

        private void TryAutoPickup(WorldItem item)
        {
            // О перегрузе уже сообщили — молча ждём, пока освободится место.
            if (reportedOverweight.Contains(item) && !playerInventory.Inventory.CanAdd(item.Item))
                return;

            reportedOverweight.Remove(item);
            if (PickUp(item).Status != AddStatus.Ok)
                reportedOverweight.Add(item);
        }

        private void PickUpFocused()
        {
            if (Focused != null)
                PickUp(Focused);
        }

        private AddResult PickUp(WorldItem item)
        {
            AddResult result = playerInventory.Inventory.TryAdd(item.Item, item.Amount);
            if (result.Added > 0)
                item.Take(result.Added, backpack);

            PickupAttempted?.Invoke(result);
            return result;
        }

        // Фокус — предмет с ручным подбором рядом, ближе всех к центру экрана и к игроку.
        private void UpdateFocus()
        {
            Focused = null;
            float bestScore = float.MaxValue;

            foreach (WorldItem item in visibleItems)
            {
                if (!IsLyingOnGround(item) || item.PickupMode != PickupMode.Manual)
                    continue;

                float distance = DistanceTo(item);
                if (distance > interactRadius)
                    continue;

                Vector2 screenPoint = viewCamera.WorldToViewportPoint(item.MarkerPosition);
                float score = Vector2.Distance(screenPoint, new Vector2(0.5f, 0.5f)) + 0.5f * distance / interactRadius;
                if (score < bestScore)
                {
                    bestScore = score;
                    Focused = item;
                }
            }
        }

        private static bool IsLyingOnGround(WorldItem item) => item != null && item.Amount > 0;

        private bool IsOnScreen(WorldItem item)
        {
            // Viewport: x и y от 0 до 1 — на экране, z <= 0 — за спиной камеры.
            Vector3 viewport = viewCamera.WorldToViewportPoint(item.MarkerPosition);
            return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        }

        // Ignore — триггеры других предметов луч не останавливают.
        private bool IsBehindObstacle(WorldItem item) =>
            Physics.Linecast(viewCamera.transform.position, item.MarkerPosition, occluderMask, QueryTriggerInteraction.Ignore);

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
