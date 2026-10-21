using System.Collections.Generic;
using UnityEngine;

namespace DeadAir.Items
{
    // Описание предмета. Код не знает про конкретные предметы:
    // новый предмет — это новый ассет, без правок кода.
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

        // Правила данных. Их же проверяет Edit Mode тест по всем ассетам в проекте.
        public void Validate(List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(id))
                errors.Add("не задан id");
            if (weight <= 0f)
                errors.Add("вес должен быть больше 0");
            if (maxStack < 1)
                errors.Add("размер стопки должен быть не меньше 1");

            if (category != ItemCategory.Junk)
            {
                if (salvageResult.Count > 0)
                    errors.Add("разбирать можно только хлам, список разборки должен быть пустым");
                return;
            }

            if (salvageResult.Count == 0)
                errors.Add("у хлама должен быть список компонентов разборки");

            foreach (var part in salvageResult)
            {
                if (part.Item == null)
                    errors.Add("в списке разборки пустой предмет");
                else if (part.Item == this)
                    errors.Add("предмет не может разбираться сам на себя");
                if (part.Amount < 1)
                    errors.Add("количество компонента должно быть не меньше 1");
            }

            // Разборка всегда освобождает вес — на этом держится правило «всё или ничего».
            if (SalvageWeight >= weight)
                errors.Add($"компоненты весят {SalvageWeight:0.##}, это не меньше веса предмета {weight:0.##}");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var errors = new List<string>();
            Validate(errors);
            foreach (var error in errors)
                Debug.LogWarning($"{name}: {error}", this);
        }
#endif
    }
}
