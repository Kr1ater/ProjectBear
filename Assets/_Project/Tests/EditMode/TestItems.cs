using System.Collections.Generic;
using DeadAir.Items;
using UnityEditor;
using UnityEngine;

namespace DeadAir.Tests
{
    // Создаёт ItemDefinition в памяти. Поля приватные, поэтому заполняем через SerializedObject,
    // как это делает инспектор, — без тестовых сеттеров в рабочем коде.
    internal sealed class TestItems
    {
        private readonly List<ItemDefinition> created = new();

        public ItemDefinition Create(
            string id,
            ItemCategory category,
            float weight,
            int maxStack = 99,
            PickupMode pickupMode = PickupMode.Manual,
            params ItemAmount[] salvage)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.name = id;

            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("weight").floatValue = weight;
            so.FindProperty("maxStack").intValue = maxStack;
            so.FindProperty("pickupMode").enumValueIndex = (int)pickupMode;

            var list = so.FindProperty("salvageResult");
            list.arraySize = salvage.Length;
            for (int i = 0; i < salvage.Length; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = salvage[i].Item;
                element.FindPropertyRelative("amount").intValue = salvage[i].Amount;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            created.Add(item);
            return item;
        }

        public void DestroyAll()
        {
            foreach (var item in created)
                Object.DestroyImmediate(item);
            created.Clear();
        }
    }
}
