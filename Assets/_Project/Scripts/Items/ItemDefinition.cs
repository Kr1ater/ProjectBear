using System.Collections.Generic;
using UnityEngine;

namespace DeadAir.Items
{
    [CreateAssetMenu(fileName = "Item", menuName = "Dead Air/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;

        [Header("Правила")]
        [SerializeField] private ItemCategory category;
        [SerializeField, Min(0.01f)] private float weight = 1f;
        [SerializeField, Min(1)] private int maxStack = 1;
        [SerializeField] private PickupMode pickupMode = PickupMode.Manual;

        [Header("Разборка (только хлам)")]
        [SerializeField] private List<ItemAmount> salvageResult = new();

        [Header("Мир")]
        [SerializeField] private GameObject worldPrefab;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemCategory Category => category;
        public float Weight => weight;
        public int MaxStack => maxStack;
        public PickupMode PickupMode => pickupMode;
        public IReadOnlyList<ItemAmount> SalvageResult => salvageResult;
        public GameObject WorldPrefab => worldPrefab;

        public bool IsSalvageable => category == ItemCategory.Junk && salvageResult.Count > 0;

        public float SalvageWeight
        {
            get
            {
                float total = 0f;
                foreach (var part in salvageResult)
                    total += part.Weight;
                return total;
            }
        }
    }
}