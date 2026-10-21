using System;
using System.Collections.Generic;
using DeadAir.Items;

namespace DeadAir.InventorySystem
{
    // Инвентарь, ограниченный условным весом. Чистый C#: ничего не знает о сцене и UI.
    public sealed class Inventory
    {
        // Допуск на ошибку float: 0.1 + 0.2 != 0.3.
        private const float WeightEpsilon = 0.0001f;

        private readonly List<ItemStack> stacks = new();

        public Inventory(float maxWeight)
        {
            if (maxWeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxWeight), "Лимит веса должен быть больше 0");
            MaxWeight = maxWeight;
        }

        public event Action OnChanged;

        public float MaxWeight { get; }
        public float CurrentWeight { get; private set; }
        public float FreeWeight => Math.Max(0f, MaxWeight - CurrentWeight);
        public IReadOnlyList<ItemStack> Stacks => stacks;

        public int CountOf(ItemDefinition item)
        {
            int count = 0;
            foreach (var stack in stacks)
                if (stack.Item == item)
                    count += stack.Count;
            return count;
        }

        public int HowManyFit(ItemDefinition item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            return (int)Math.Floor((FreeWeight + WeightEpsilon) / item.Weight);
        }

        public bool CanAdd(ItemDefinition item, int amount = 1) => HowManyFit(item) >= amount;

        // Добавляет столько, сколько влезает по весу. Причина отказа — в результате.
        public AddResult TryAdd(ItemDefinition item, int amount)
        {
            if (amount < 1)
                throw new ArgumentOutOfRangeException(nameof(amount));

            float freeBefore = FreeWeight;
            int added = Math.Min(amount, HowManyFit(item));
            if (added > 0)
            {
                AddToStacks(item, added);
                Commit();
            }
            return new AddResult(item, amount, added, freeBefore);
        }

        public bool Remove(ItemDefinition item, int amount)
        {
            if (amount < 1)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (CountOf(item) < amount)
                return false;

            RemoveFromStacks(item, amount);
            Commit();
            return true;
        }

        public bool CanExchange(ItemAmount remove, IReadOnlyList<ItemAmount> add)
        {
            if (remove.Item == null || CountOf(remove.Item) < remove.Amount)
                return false;

            float weightAfter = CurrentWeight - remove.Weight;
            foreach (var part in add)
                weightAfter += part.Weight;
            return weightAfter <= MaxWeight + WeightEpsilon;
        }

        // Убирает remove и добавляет add одной операцией: либо всё, либо ничего.
        // OnChanged срабатывает один раз.
        public bool TryExchange(ItemAmount remove, IReadOnlyList<ItemAmount> add)
        {
            if (!CanExchange(remove, add))
                return false;

            RemoveFromStacks(remove.Item, remove.Amount);
            foreach (var part in add)
                AddToStacks(part.Item, part.Amount);
            Commit();
            return true;
        }

        // Сначала доливаем неполные стопки, остаток кладём новыми стопками по MaxStack.
        private void AddToStacks(ItemDefinition item, int amount)
        {
            for (int i = 0; i < stacks.Count && amount > 0; i++)
            {
                var stack = stacks[i];
                if (stack.Item != item || stack.IsFull)
                    continue;

                int moved = Math.Min(amount, item.MaxStack - stack.Count);
                stacks[i] = stack.WithCount(stack.Count + moved);
                amount -= moved;
            }

            while (amount > 0)
            {
                int moved = Math.Min(amount, item.MaxStack);
                stacks.Add(new ItemStack(item, moved));
                amount -= moved;
            }
        }

        // Забираем с конца, чтобы первыми уходили неполные стопки.
        private void RemoveFromStacks(ItemDefinition item, int amount)
        {
            for (int i = stacks.Count - 1; i >= 0 && amount > 0; i--)
            {
                var stack = stacks[i];
                if (stack.Item != item)
                    continue;

                int taken = Math.Min(amount, stack.Count);
                amount -= taken;
                if (taken == stack.Count)
                    stacks.RemoveAt(i);
                else
                    stacks[i] = stack.WithCount(stack.Count - taken);
            }
        }

        // Вес пересчитываем из стопок, а не копим дельтами, чтобы не накапливать ошибку float.
        private void Commit()
        {
            float total = 0f;
            foreach (var stack in stacks)
                total += stack.Weight;
            CurrentWeight = total;
            OnChanged?.Invoke();
        }
    }
}
