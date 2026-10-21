using System;
using DeadAir.Items;
using UnityEngine;

namespace DeadAir.EditorTools
{
    // Стартовый набор предметов из дизайн-документа и их модели из примитивов.
    // Источник правды после сборки — ассеты в Data/Items; этот список нужен только генератору.
    internal static class DemoItems
    {
        internal sealed class Spec
        {
            public string Id;
            public string File;
            public string Name;
            public string Description;
            public ItemCategory Category;
            public float Weight;
            public int MaxStack;
            public PickupMode Mode;
            public (string id, int amount)[] Salvage = Array.Empty<(string, int)>();
            public Action<Transform> Model;
        }

        public static Spec[] All => new[]
        {
            new Spec
            {
                Id = "bolts", File = "Bolts", Name = "Болты",
                Description = "Крепёж, свинченный с обшивки. Уходит на любой ремонт.",
                Category = ItemCategory.RepairResource, Weight = 0.1f, MaxStack = 50, Mode = PickupMode.Auto,
                Model = Bolts
            },
            new Spec
            {
                Id = "duct_tape", File = "DuctTape", Name = "Изолента",
                Description = "Серая армированная лента. Временно закрывает пробой скафандра.",
                Category = ItemCategory.RepairResource, Weight = 0.2f, MaxStack = 10, Mode = PickupMode.Auto,
                Model = DuctTape
            },
            new Spec
            {
                Id = "wiring", File = "Wiring", Name = "Проводка",
                Description = "Медный провод в потрескавшейся изоляции.",
                Category = ItemCategory.RepairResource, Weight = 0.3f, MaxStack = 20, Mode = PickupMode.Auto,
                Model = Wiring
            },
            new Spec
            {
                Id = "microchip", File = "Microchip", Name = "Микросхема",
                Description = "Плата управления. Контакты окислились, но она ещё работает.",
                Category = ItemCategory.RepairResource, Weight = 0.1f, MaxStack = 20, Mode = PickupMode.Auto,
                Model = Microchip
            },
            new Spec
            {
                Id = "metal_plate", File = "MetalPlate", Name = "Металлическая пластина",
                Description = "Кусок обшивки с отверстиями под заклёпки.",
                Category = ItemCategory.RepairResource, Weight = 0.5f, MaxStack = 20, Mode = PickupMode.Auto,
                Model = MetalPlate
            },
            new Spec
            {
                Id = "medkit", File = "Medkit", Name = "Медпакет",
                Description = "Стандартная аптечка ЧВК: бинт, гемостатик, обезболивающее.",
                Category = ItemCategory.Consumable, Weight = 0.5f, MaxStack = 5, Mode = PickupMode.Auto,
                Model = Medkit
            },
            new Spec
            {
                Id = "oxygen_cartridge", File = "OxygenCartridge", Name = "Кислородный картридж",
                Description = "Сменный картридж системы жизнеобеспечения. Запас на несколько часов.",
                Category = ItemCategory.Consumable, Weight = 1f, MaxStack = 4, Mode = PickupMode.Manual,
                Model = OxygenCartridge
            },
            new Spec
            {
                Id = "old_radio", File = "OldRadio", Name = "Старая рация",
                Description = "Армейская рация старого образца. Не включается.",
                Category = ItemCategory.Junk, Weight = 3f, MaxStack = 1, Mode = PickupMode.Manual,
                Salvage = new[] { ("wiring", 2), ("microchip", 1), ("bolts", 2) },
                Model = OldRadio
            },
            new Spec
            {
                Id = "broken_pump", File = "BrokenPump", Name = "Сломанный насос",
                Description = "Насос контура охлаждения с треснувшим корпусом.",
                Category = ItemCategory.Junk, Weight = 5f, MaxStack = 1, Mode = PickupMode.Manual,
                Salvage = new[] { ("bolts", 4), ("metal_plate", 2) },
                Model = BrokenPump
            },
            new Spec
            {
                Id = "empty_canister", File = "EmptyCanister", Name = "Пустой баллон",
                Description = "Газовый баллон без давления. Годится только на металл.",
                Category = ItemCategory.Junk, Weight = 2f, MaxStack = 3, Mode = PickupMode.Manual,
                Salvage = new[] { ("metal_plate", 1) },
                Model = EmptyCanister
            },
            new Spec
            {
                Id = "carbine", File = "Carbine", Name = "Карабин",
                Description = "Короткий карабин охраны. Тяжёлый, без глушителя.",
                Category = ItemCategory.Weapon, Weight = 6f, MaxStack = 1, Mode = PickupMode.Manual,
                Model = Carbine
            },
        };

        // ---- Модели. Начало координат — на земле, под центром предмета.

        private static void Bolts(Transform root)
        {
            var offsets = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0.09f, 0f, 0.05f), new Vector3(-0.07f, 0f, 0.08f),
                new Vector3(0.04f, 0f, -0.09f), new Vector3(-0.1f, 0f, -0.04f), new Vector3(0.02f, 0.035f, 0.02f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                var bolt = DemoContentBuilder.Group(root, "Bolt", offsets[i] + Vector3.up * 0.018f, new Vector3(0f, i * 61f, 90f));
                DemoContentBuilder.Part(bolt, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.025f, 0.05f, 0.025f), "MetalGray");
                DemoContentBuilder.Part(bolt, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.045f, 0.008f, 0.045f), "MetalDark");
            }
        }

        private static void DuctTape(Transform root)
        {
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.025f, 0f), new Vector3(0.16f, 0.025f, 0.16f), "TapeGray");
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.026f, 0f), new Vector3(0.09f, 0.0255f, 0.09f), "PaintOchre");
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0.14f, 0.075f, 0.04f), new Vector3(0.16f, 0.025f, 0.16f), "TapeGray", new Vector3(70f, 20f, 0f));
        }

        private static void Wiring(Transform root)
        {
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.012f, 0f), new Vector3(0.022f, 0.2f, 0.022f), "Rubber", new Vector3(90f, 20f, 0f));
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0.03f, 0.03f, 0.02f), new Vector3(0.022f, 0.18f, 0.022f), "Copper", new Vector3(90f, -35f, 0f));
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(-0.02f, 0.045f, -0.02f), new Vector3(0.022f, 0.16f, 0.022f), "HazardOrange", new Vector3(90f, 75f, 0f));
        }

        private static void Microchip(Transform root)
        {
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.008f, 0f), new Vector3(0.18f, 0.012f, 0.12f), "Circuit");
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(-0.03f, 0.022f, 0f), new Vector3(0.06f, 0.016f, 0.06f), "Rubber");
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0.05f, 0.02f, 0.03f), new Vector3(0.03f, 0.012f, 0.02f), "MetalGray");
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.016f, -0.055f), new Vector3(0.16f, 0.004f, 0.01f), "Copper");
        }

        private static void MetalPlate(Transform root)
        {
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.015f, 0f), new Vector3(0.4f, 0.02f, 0.28f), "MetalGray", new Vector3(0f, 0f, 3f));
            foreach (var corner in new[] { new Vector2(0.17f, 0.11f), new Vector2(-0.17f, 0.11f), new Vector2(0.17f, -0.11f), new Vector2(-0.17f, -0.11f) })
                DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(corner.x, 0.028f, corner.y), new Vector3(0.025f, 0.004f, 0.025f), "MetalDark");
        }

        private static void Medkit(Transform root)
        {
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.07f, 0f), new Vector3(0.32f, 0.14f, 0.22f), "PaintWhite");
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.07f, 0f), new Vector3(0.325f, 0.04f, 0.225f), "HazardOrange");
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(0.12f, 0.02f, 0.03f), "Rubber");
        }

        private static void OxygenCartridge(Transform root)
        {
            var body = DemoContentBuilder.Group(root, "Body", new Vector3(0f, 0.065f, 0f), new Vector3(0f, 0f, 90f));
            DemoContentBuilder.Part(body, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.13f, 0.17f, 0.13f), "PaintWhite");
            DemoContentBuilder.Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.135f, 0.02f, 0.135f), "HazardOrange");
            DemoContentBuilder.Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.19f, 0f), new Vector3(0.06f, 0.03f, 0.06f), "MetalDark");
        }

        private static void OldRadio(Transform root)
        {
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.13f, 0f), new Vector3(0.38f, 0.26f, 0.16f), "PaintOchre");
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.14f, 0.081f), new Vector3(0.32f, 0.18f, 0.01f), "MetalDark");
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(-0.08f, 0.16f, 0.09f), new Vector3(0.06f, 0.012f, 0.06f), "PaintWhite", new Vector3(90f, 0f, 0f));
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0.06f, 0.16f, 0.09f), new Vector3(0.04f, 0.012f, 0.04f), "PaintWhite", new Vector3(90f, 0f, 0f));
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.09f), new Vector3(0.24f, 0.03f, 0.01f), "Rubber");
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0.15f, 0.42f, -0.03f), new Vector3(0.012f, 0.18f, 0.012f), "MetalGray", new Vector3(0f, 0f, -12f));
        }

        private static void BrokenPump(Transform root)
        {
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.17f, 0f), new Vector3(0.32f, 0.24f, 0.32f), "MetalDark", new Vector3(0f, 0f, 90f));
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0.1f, 0.17f, 0f), new Vector3(0.335f, 0.03f, 0.335f), "HazardOrange", new Vector3(0f, 0f, 90f));
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(-0.32f, 0.13f, 0f), new Vector3(0.18f, 0.22f, 0.24f), "MetalGray");
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0.05f, 0.36f, 0f), new Vector3(0.07f, 0.06f, 0.07f), "MetalGray");
            DemoContentBuilder.Part(root, PrimitiveType.Cylinder, new Vector3(0.3f, 0.1f, 0.08f), new Vector3(0.06f, 0.08f, 0.06f), "MetalGray", new Vector3(0f, 0f, 90f));
            DemoContentBuilder.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.6f, 0.04f, 0.3f), "Rubber");
        }

        private static void EmptyCanister(Transform root)
        {
            var body = DemoContentBuilder.Group(root, "Body", new Vector3(0f, 0.11f, 0f), new Vector3(0f, 25f, 90f));
            DemoContentBuilder.Part(body, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.22f, 0.3f, 0.22f), "PaintWhite");
            DemoContentBuilder.Part(body, PrimitiveType.Sphere, new Vector3(0f, 0.3f, 0f), new Vector3(0.22f, 0.1f, 0.22f), "PaintWhite");
            DemoContentBuilder.Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.225f, 0.03f, 0.225f), "HazardOrange");
            DemoContentBuilder.Part(body, PrimitiveType.Cylinder, new Vector3(0f, 0.38f, 0f), new Vector3(0.05f, 0.04f, 0.05f), "MetalDark");
        }

        private static void Carbine(Transform root)
        {
            // Модель стоит «вертикально» (магазин вниз), группа кладёт её на бок.
            var gun = DemoContentBuilder.Group(root, "Gun", new Vector3(0f, 0.035f, 0f), new Vector3(0f, 0f, 90f));
            DemoContentBuilder.Part(gun, PrimitiveType.Cube, Vector3.zero, new Vector3(0.06f, 0.08f, 0.42f), "MetalDark");
            DemoContentBuilder.Part(gun, PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0.42f), new Vector3(0.03f, 0.22f, 0.03f), "MetalGray", new Vector3(90f, 0f, 0f));
            DemoContentBuilder.Part(gun, PrimitiveType.Cube, new Vector3(0f, -0.01f, -0.36f), new Vector3(0.05f, 0.1f, 0.3f), "PaintOchre");
            DemoContentBuilder.Part(gun, PrimitiveType.Cube, new Vector3(0f, -0.09f, 0.06f), new Vector3(0.04f, 0.14f, 0.06f), "Rubber");
            DemoContentBuilder.Part(gun, PrimitiveType.Cube, new Vector3(0f, -0.08f, -0.1f), new Vector3(0.035f, 0.11f, 0.04f), "Rubber");
            DemoContentBuilder.Part(gun, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.05f), new Vector3(0.03f, 0.04f, 0.18f), "MetalGray");
        }
    }
}
