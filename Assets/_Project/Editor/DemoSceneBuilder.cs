using System.Linq;
using DeadAir.Items;
using DeadAir.Player;
using DeadAir.UI;
using DeadAir.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static DeadAir.EditorTools.DemoContentBuilder;

namespace DeadAir.EditorTools
{
    // Собирает сцену LootDemo: кусок кратера 30×30 м, место крушения модуля, лут «дорожкой», игрок и UI.
    public static class DemoSceneBuilder
    {
        public const string ScenePath = Root + "/Scenes/LootDemo.unity";

        private const int DefaultMask = 1 << 0;
        private static readonly Color Sky = Hex(0x9A6E55);

        [MenuItem("Tools/Dead Air/Rebuild Demo Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLighting(new GameObject("Lighting").transform);
            BuildEnvironment(new GameObject("Environment").transform);
            BuildLoot(new GameObject("Loot").transform);

            var player = BuildPlayer(out var cameraTarget);
            var camera = BuildCamera(player.transform);
            WirePlayer(player, camera, cameraTarget);
            BuildUi(player, camera);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"Dead Air: сцена собрана — {ScenePath}");
        }

        // ---- Свет: белое солнце даёт жёсткие тени, красное подкрашивает тени тёплым.

        private static void SetupLighting(Transform parent)
        {
            var white = AddSun(parent, "Sun_White", Hex(0xFFF2DE), 2f, new Vector3(42f, -35f, 0f), LightShadows.Hard);
            AddSun(parent, "Sun_Red", Hex(0xB8432A), 0.5f, new Vector3(14f, 150f, 0f), LightShadows.None);

            RenderSettings.sun = white;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex(0x6A4A3C);
            RenderSettings.ambientEquatorColor = Hex(0x5A382A);
            RenderSettings.ambientGroundColor = Hex(0x2A1812);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Sky;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 70f;
        }

        private static Light AddSun(Transform parent, string name, Color color, float intensity, Vector3 euler, LightShadows shadows)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.eulerAngles = euler;
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows;
            light.shadowStrength = 1f;
            return light;
        }

        // ---- Окружение

        private static void BuildEnvironment(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            ground.GetComponent<Renderer>().sharedMaterial = Mat("Ground");

            var dunes = Group(parent, "Dunes", Vector3.zero, Vector3.zero);
            AddDune(dunes, new Vector3(-10f, -0.35f, 8f), new Vector3(9f, 1.2f, 7f));
            AddDune(dunes, new Vector3(11f, -0.3f, -6f), new Vector3(8f, 1f, 10f));
            AddDune(dunes, new Vector3(-11f, -0.4f, -9f), new Vector3(7f, 1.1f, 6f));
            AddDune(dunes, new Vector3(9f, -0.45f, 13f), new Vector3(10f, 1.2f, 5f));

            // Камни по краю кратера; сид фиксирован, чтобы сцена собиралась одинаково.
            var rocks = Group(parent, "Rocks", Vector3.zero, Vector3.zero);
            var random = new System.Random(1979);
            for (int i = 0; i < 16; i++)
            {
                float angle = i / 16f * Mathf.PI * 2f + (float)random.NextDouble() * 0.3f;
                float radius = 12f + (float)random.NextDouble() * 2.5f;
                var scale = new Vector3(Range(random, 1.2f, 2.8f), Range(random, 0.8f, 1.9f), Range(random, 1f, 2.4f));
                var position = new Vector3(Mathf.Cos(angle) * radius, scale.y * 0.3f, Mathf.Sin(angle) * radius);
                var euler = new Vector3(Range(random, -12f, 12f), Range(random, 0f, 360f), Range(random, -12f, 12f));
                Solid(rocks, PrimitiveType.Cube, position, scale, i % 3 == 0 ? "RockOchre" : "Rock", euler);
            }
            // Пара камней у маршрута: за ними лут не подсвечивается.
            Solid(rocks, PrimitiveType.Cube, new Vector3(3.6f, 0.45f, -3f), new Vector3(1.6f, 1.3f, 1.4f), "Rock", new Vector3(5f, 30f, -4f));
            Solid(rocks, PrimitiveType.Cube, new Vector3(-4f, 0.5f, 4f), new Vector3(1.8f, 1.5f, 1.5f), "RockOchre", new Vector3(-6f, 70f, 3f));

            BuildCrashSite(Group(parent, "CrashSite", Vector3.zero, Vector3.zero));
            BuildBounds(Group(parent, "Bounds", Vector3.zero, Vector3.zero));
        }

        private static void BuildCrashSite(Transform parent)
        {
            // Грузовой модуль: выцветший белый корпус, оранжевые полосы опасности, клёпаные швы.
            var module = Group(parent, "CargoModule", new Vector3(3f, 0f, 11.5f), new Vector3(0f, 18f, 0f));
            var hull = Group(module, "Hull", new Vector3(0f, 1.2f, 0f), new Vector3(0f, 0f, 7f));
            Solid(hull, PrimitiveType.Cube, Vector3.zero, new Vector3(7.5f, 3f, 3.4f), "PaintWhite");
            foreach (float x in new[] { -2.2f, 2.2f })
                Part(hull, PrimitiveType.Cube, new Vector3(x, 0f, 0f), new Vector3(0.45f, 3.04f, 3.44f), "HazardOrange");
            foreach (float x in new[] { -3.8f, 3.8f })
                Part(hull, PrimitiveType.Cube, new Vector3(x, 0f, 0f), new Vector3(0.12f, 2.6f, 3f), "MetalDark");
            foreach (var seam in new[] { new Vector3(0f, 1.5f, 1.7f), new Vector3(0f, 1.5f, -1.7f), new Vector3(0f, -1.2f, 1.7f) })
                Part(hull, PrimitiveType.Cube, seam, new Vector3(7.5f, 0.06f, 0.06f), "MetalGray");
            Part(hull, PrimitiveType.Cube, new Vector3(0.6f, 0f, 1.71f), new Vector3(1.4f, 1.8f, 0.05f), "MetalGray");

            // Обломки и ящики вокруг.
            Solid(parent, PrimitiveType.Cube, new Vector3(-1.8f, 0.06f, 9f), new Vector3(2f, 0.08f, 1.3f), "MetalGray", new Vector3(6f, 35f, 3f));
            Solid(parent, PrimitiveType.Cube, new Vector3(7f, 0.9f, 10.5f), new Vector3(2.2f, 0.08f, 1.2f), "PaintWhite", new Vector3(0f, -20f, 65f));
            Solid(parent, PrimitiveType.Cube, new Vector3(5.4f, 0.4f, 7.4f), new Vector3(1f, 0.8f, 0.8f), "PaintOchre", new Vector3(0f, 15f, 0f));
            Solid(parent, PrimitiveType.Cube, new Vector3(6.4f, 0.35f, 8.6f), new Vector3(0.9f, 0.7f, 0.7f), "PaintOchre", new Vector3(0f, -8f, 0f));

            // Ящик, на котором лежит карабин.
            var crate = Group(parent, "CarbineCrate", new Vector3(0.3f, 0f, 6f), new Vector3(0f, 5f, 0f));
            Solid(crate, PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(1.1f, 0.5f, 0.65f), "MetalDark");
            Part(crate, PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f), new Vector3(1.11f, 0.05f, 0.66f), "HazardOrange");
        }

        private static void BuildBounds(Transform parent)
        {
            const float half = 15f;
            foreach (var (center, size) in new[]
                     {
                         (new Vector3(0f, 2f, half + 0.5f), new Vector3(2f * half + 2f, 4f, 1f)),
                         (new Vector3(0f, 2f, -half - 0.5f), new Vector3(2f * half + 2f, 4f, 1f)),
                         (new Vector3(half + 0.5f, 2f, 0f), new Vector3(1f, 4f, 2f * half + 2f)),
                         (new Vector3(-half - 0.5f, 2f, 0f), new Vector3(1f, 4f, 2f * half + 2f)),
                     })
            {
                var wall = new GameObject("Wall").AddComponent<BoxCollider>();
                wall.transform.SetParent(parent, false);
                wall.transform.localPosition = center;
                wall.size = size;
            }
        }

        private static void AddDune(Transform parent, Vector3 position, Vector3 scale)
        {
            var dune = Part(parent, PrimitiveType.Sphere, position, scale, "Ground");
            dune.name = "Dune";
            dune.AddComponent<MeshCollider>();
        }

        private static GameObject Solid(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, string material, Vector3 euler = default)
        {
            var solid = Part(parent, type, position, scale, material, euler);
            solid.AddComponent<BoxCollider>();
            return solid;
        }

        private static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        // ---- Лут. Маршрут видео идёт по оси Z от старта к ящику с карабином.

        private static void BuildLoot(Transform parent)
        {
            // Автоподбор: 10 болтов, 3 изоленты, 2 медпакета = 2.6
            PlaceLoot(parent, "Bolts", 5, new Vector3(0.35f, 0f, -7.5f), 20f);
            PlaceLoot(parent, "Bolts", 5, new Vector3(-0.3f, 0f, -6f), 140f);
            PlaceLoot(parent, "DuctTape", 3, new Vector3(0.25f, 0f, -4.6f), 60f);
            PlaceLoot(parent, "Medkit", 2, new Vector3(-0.25f, 0f, -3.2f), 200f);
            // По клавише: рация, насос и 2 картриджа = ещё 10.0, итого 12.6 из 15
            PlaceLoot(parent, "OldRadio", 1, new Vector3(-1.9f, 0f, -0.6f), 110f);
            PlaceLoot(parent, "BrokenPump", 1, new Vector3(1.9f, 0f, 0.6f), 30f);
            PlaceLoot(parent, "OxygenCartridge", 2, new Vector3(-0.7f, 0f, 2.4f), 75f);
            // Карабин 6.0 не влезает, пока не разобран хлам
            PlaceLoot(parent, "Carbine", 1, new Vector3(0.3f, 0.5f, 6f), 95f);

            // В стороне от маршрута — остальные предметы и большая куча болтов для частичного подбора.
            PlaceLoot(parent, "EmptyCanister", 2, new Vector3(-8f, 0f, 1.5f), 10f);
            PlaceLoot(parent, "Bolts", 40, new Vector3(-7f, 0f, -2f), 0f);
            PlaceLoot(parent, "Wiring", 3, new Vector3(7.5f, 0f, -1.5f), 45f);
            PlaceLoot(parent, "Microchip", 2, new Vector3(8.2f, 0f, 1f), 0f);
            PlaceLoot(parent, "MetalPlate", 2, new Vector3(7.2f, 0f, 3.5f), 80f);
        }

        private static void PlaceLoot(Transform parent, string file, int amount, Vector3 position, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ItemPrefabsDir}/{file}.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = amount > 1 ? $"{file} ×{amount}" : file;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Wire(instance.GetComponent<WorldItem>(), ("amount", amount));
        }

        // ---- Игрок: капсула-скафандр с контроллером от третьего лица

        private static GameObject BuildPlayer(out Transform cameraTarget)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0f, -10f);

            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.04f;

            var model = Group(player.transform, "Model", Vector3.zero, Vector3.zero);
            Part(model, PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(0.72f, 0.85f, 0.6f), "Suit");
            Part(model, PrimitiveType.Sphere, new Vector3(-0.36f, 1.35f, 0f), new Vector3(0.26f, 0.22f, 0.3f), "Suit");
            Part(model, PrimitiveType.Sphere, new Vector3(0.36f, 1.35f, 0f), new Vector3(0.26f, 0.22f, 0.3f), "Suit");
            Part(model, PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0.02f), new Vector3(0.46f, 0.44f, 0.46f), "Suit");
            Part(model, PrimitiveType.Cube, new Vector3(0f, 1.63f, 0.21f), new Vector3(0.24f, 0.1f, 0.06f), "Visor");
            Part(model, PrimitiveType.Cube, new Vector3(0f, 1.15f, -0.33f), new Vector3(0.5f, 0.62f, 0.26f), "MetalGray");
            Part(model, PrimitiveType.Cube, new Vector3(0f, 1.32f, -0.465f), new Vector3(0.5f, 0.07f, 0.01f), "HazardOrange");
            Part(model, PrimitiveType.Cylinder, new Vector3(-0.14f, 1.12f, -0.5f), new Vector3(0.12f, 0.26f, 0.12f), "PaintWhite");
            Part(model, PrimitiveType.Cylinder, new Vector3(0.14f, 1.12f, -0.5f), new Vector3(0.12f, 0.26f, 0.12f), "PaintWhite");

            cameraTarget = new GameObject("CameraTarget").transform;
            cameraTarget.SetParent(player.transform, false);
            cameraTarget.localPosition = new Vector3(0f, 1.6f, 0f);

            // Центр рюкзака: сюда притягивается подобранное и отсюда вылетает выброшенное.
            var backpack = new GameObject("BackpackAnchor").transform;
            backpack.SetParent(player.transform, false);
            backpack.localPosition = new Vector3(0f, 1.15f, -0.35f);

            player.AddComponent<InputReader>();
            player.AddComponent<PlayerInventory>();
            player.AddComponent<LootSensor>();
            player.AddComponent<ItemDropper>();
            player.AddComponent<PlayerController>();
            SetLayerRecursively(player, PlayerLayer);
            return player;
        }

        private static Camera BuildCamera(Transform player)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(player.position + new Vector3(0.55f, 2.2f, -3.2f), Quaternion.Euler(12f, 0f, 0f));
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 150f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Sky;
            go.AddComponent<AudioListener>();
            go.AddComponent<ThirdPersonCamera>();
            return camera;
        }

        private static void WirePlayer(GameObject player, Camera camera, Transform cameraTarget)
        {
            var input = player.GetComponent<InputReader>();
            var backpack = player.transform.Find("BackpackAnchor");
            Wire(input,
                ("move", InputAction("Move")), ("look", InputAction("Look")), ("interact", InputAction("Interact")),
                ("toggleInventory", InputAction("ToggleInventory")), ("salvage", InputAction("Salvage")),
                ("drop", InputAction("Drop")));
            Wire(player.GetComponent<PlayerInventory>(), ("maxWeight", 15f));
            Wire(player.GetComponent<LootSensor>(),
                ("playerInventory", player.GetComponent<PlayerInventory>()), ("input", input), ("viewCamera", camera),
                ("backpack", backpack), ("lootMask", 1 << LootLayer), ("occluderMask", DefaultMask));
            Wire(player.GetComponent<ItemDropper>(),
                ("playerInventory", player.GetComponent<PlayerInventory>()), ("backpack", backpack), ("groundMask", DefaultMask));
            Wire(player.GetComponent<PlayerController>(), ("input", input), ("cameraTransform", camera.transform));
            Wire(camera.GetComponent<ThirdPersonCamera>(),
                ("input", input), ("target", cameraTarget), ("obstacleMask", DefaultMask));
        }

        // ---- Интерфейс

        private static void BuildUi(GameObject player, Camera camera)
        {
            var playerInventory = player.GetComponent<PlayerInventory>();
            var hudText = AssetDatabase.LoadAssetAtPath<Material>(HudTextMaterialPath);

            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = UiLayer
            };
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGo.transform;

            var feed = BuildHud(root, player, camera, hudText);
            BuildInventoryScreen(root, playerInventory, player.GetComponent<ItemDropper>(), player.GetComponent<InputReader>(), feed);
            // Лента поверх затемнения инвентаря: сообщения о разборке видны при открытом экране.
            feed.transform.SetParent(root, false);
            feed.transform.SetAsLastSibling();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static MessageFeed BuildHud(RectTransform root, GameObject player, Camera camera, Material hudText)
        {
            var hud = UiFactory.Rect("HUD", root).Stretch();
            var markers = UiFactory.Rect("Markers", hud).Stretch();

            UiFactory.Image(UiFactory.Rect("Crosshair", hud).Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, 4f)),
                UiPalette.Text.WithAlpha(0.5f));

            // Подпись предмета в фокусе: плашка подстраивается под длину текста.
            var focusLabel = UiFactory.Rect("FocusLabel", hud).Anchor(Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300f, 36f));
            UiFactory.Image(focusLabel, UiPalette.Background.WithAlpha(0.75f));
            var focusLayout = focusLabel.gameObject.AddComponent<HorizontalLayoutGroup>();
            focusLayout.padding = new RectOffset(12, 12, 5, 5);
            focusLayout.childControlWidth = true;
            focusLayout.childControlHeight = true;
            focusLayout.childForceExpandWidth = false;
            focusLayout.childForceExpandHeight = false;
            var focusFitter = focusLabel.gameObject.AddComponent<ContentSizeFitter>();
            focusFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            focusFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var focusText = UiFactory.Text(focusLabel, "Text", "[E] Подобрать", 22f, UiPalette.Text, TextAlignmentOptions.MidlineLeft);
            var focusGroup = focusLabel.gameObject.AddComponent<CanvasGroup>();
            focusGroup.alpha = 0f;
            focusGroup.blocksRaycasts = false;
            focusGroup.interactable = false;

            var messagesRect = UiFactory.Rect("Messages", hud).Anchor(Vector2.zero, Vector2.zero, new Vector2(48f, 116f), new Vector2(1000f, 126f));
            var messagesLayout = messagesRect.gameObject.AddComponent<VerticalLayoutGroup>();
            messagesLayout.childAlignment = TextAnchor.LowerLeft;
            messagesLayout.spacing = 2f;
            messagesLayout.childControlWidth = true;
            messagesLayout.childControlHeight = true;
            messagesLayout.childForceExpandWidth = true;
            messagesLayout.childForceExpandHeight = false;
            var feed = messagesRect.gameObject.AddComponent<MessageFeed>();
            Wire(feed,
                ("linePrefab", AssetDatabase.LoadAssetAtPath<TextMeshProUGUI>(MessageLinePrefabPath)),
                ("container", messagesRect));

            var weightRect = UiFactory.Rect("Weight", hud).Anchor(Vector2.zero, Vector2.zero, new Vector2(48f, 56f), new Vector2(300f, 44f));
            var weightBar = BuildWeightBar(weightRect, player.GetComponent<PlayerInventory>(), hudText);

            var hint = UiFactory.Text(hud, "Hint", $"<color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Amber)}>[Tab]</color> Инвентарь",
                20f, UiPalette.Muted, TextAlignmentOptions.BottomRight, hudText);
            hint.rectTransform.Anchor(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 56f), new Vector2(400f, 30f));

            var lootMarkers = hud.gameObject.AddComponent<LootMarkers>();
            Wire(lootMarkers,
                ("sensor", player.GetComponent<LootSensor>()), ("viewCamera", camera),
                ("markerPrefab", AssetDatabase.LoadAssetAtPath<Image>(MarkerPrefabPath)), ("container", markers),
                ("focusLabel", focusLabel), ("focusLabelGroup", focusGroup), ("focusLabelText", focusText));

            var hudUi = hud.gameObject.AddComponent<HudUI>();
            Wire(hudUi, ("sensor", player.GetComponent<LootSensor>()), ("messages", feed), ("weightBar", weightBar));

            return feed;
        }

        // «ВЕС  12.6 / 15.0» и полоса под ним.
        private static WeightBar BuildWeightBar(RectTransform block, PlayerInventory playerInventory, Material textMaterial)
        {
            UiFactory.Text(block, "Label", "ВЕС", 16f, UiPalette.Muted, TextAlignmentOptions.TopLeft, textMaterial).rectTransform.Stretch(0f, 0f, 0f, 14f);
            var value = UiFactory.Text(block, "Value", "0.0 / 15.0", 20f, UiPalette.Text, TextAlignmentOptions.TopRight, textMaterial);
            value.rectTransform.Stretch(0f, 0f, 0f, 14f);

            var track = UiFactory.Rect("Track", block);
            track.anchorMin = Vector2.zero;
            track.anchorMax = new Vector2(1f, 0f);
            track.pivot = new Vector2(0.5f, 0f);
            track.anchoredPosition = Vector2.zero;
            track.sizeDelta = new Vector2(0f, 8f);
            UiFactory.Image(track, UiPalette.Background.WithAlpha(0.8f));

            var fill = UiFactory.Rect("Fill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.5f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var fillImage = UiFactory.Image(fill, UiPalette.Text);

            var bar = block.gameObject.AddComponent<WeightBar>();
            Wire(bar, ("playerInventory", playerInventory), ("fill", fill), ("fillGraphic", fillImage), ("label", value));
            return bar;
        }

        private static void BuildInventoryScreen(RectTransform root, PlayerInventory playerInventory, ItemDropper dropper,
            InputReader input, MessageFeed feed)
        {
            var inventoryRoot = UiFactory.Rect("Inventory", root).Stretch();
            var screen = UiFactory.Rect("Screen", inventoryRoot).Stretch();
            UiFactory.Image(screen, UiPalette.Background.WithAlpha(0.82f), raycast: true);

            // Панель чуть выше центра, чтобы под ней оставалось место для ленты сообщений.
            var panel = UiFactory.Rect("Panel", screen).Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1180f, 660f));
            UiFactory.Image(panel, UiPalette.Panel, raycast: true);

            var title = UiFactory.Text(panel, "Title", "ИНВЕНТАРЬ", 30f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            title.characterSpacing = 6f;
            title.rectTransform.TopLeft(32f, 26f, 400f, 40f);

            var weightRect = UiFactory.Rect("Weight", panel).Anchor(Vector2.one, Vector2.one, new Vector2(-32f, -24f), new Vector2(360f, 44f));
            var weightBar = BuildWeightBar(weightRect, playerInventory, null);

            var tabs = new CategoryTab[5];
            for (int i = 0; i < tabs.Length; i++)
            {
                bool all = i == 0;
                var category = all ? ItemCategory.Consumable : (ItemCategory)(i - 1);
                tabs[i] = BuildTab(panel, all ? "Все" : UiText.CategoryTab(category), all, category, 32f + i * 158f);
            }

            UiFactory.Image(UiFactory.Rect("Divider", panel).TopLeft(32f, 124f, 1116f, 1f), UiPalette.Muted.WithAlpha(0.25f));

            // Список слева
            var list = UiFactory.Rect("List", panel).TopLeft(32f, 136f, 640f, 460f);
            UiFactory.Text(list, "HeaderName", "ПРЕДМЕТ", 14f, UiPalette.Muted, TextAlignmentOptions.MidlineLeft).rectTransform.TopLeft(76f, 0f, 200f, 24f);
            UiFactory.Text(list, "HeaderCount", "КОЛ-ВО", 14f, UiPalette.Muted, TextAlignmentOptions.MidlineRight).rectTransform
                .Anchor(Vector2.one, Vector2.one, new Vector2(-110f, 0f), new Vector2(90f, 24f));
            UiFactory.Text(list, "HeaderWeight", "ВЕС", 14f, UiPalette.Muted, TextAlignmentOptions.MidlineRight).rectTransform
                .Anchor(Vector2.one, Vector2.one, new Vector2(-16f, 0f), new Vector2(80f, 24f));

            var scroll = UiFactory.Rect("Scroll", list).Stretch(0f, 28f, 0f, 0f);
            var viewport = UiFactory.Rect("Viewport", scroll).Stretch();
            UiFactory.Image(viewport, Color.clear, raycast: true);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            var empty = UiFactory.Text(list, "Empty", "Пусто", 20f, UiPalette.Muted, TextAlignmentOptions.Center);
            empty.rectTransform.Stretch(0f, 28f, 0f, 0f);

            var details = BuildDetails(panel);

            var hint = UiFactory.Text(panel, "Hint",
                $"{Key("Tab")} закрыть     {Key("ЛКМ")} выбрать     {Key("X")} разобрать     {Key("G")} выбросить",
                18f, UiPalette.Muted, TextAlignmentOptions.BottomLeft);
            hint.rectTransform.Anchor(Vector2.zero, Vector2.zero, new Vector2(32f, 22f), new Vector2(800f, 28f));

            var inventoryUi = inventoryRoot.gameObject.AddComponent<InventoryUI>();
            Wire(inventoryUi,
                ("playerInventory", playerInventory), ("dropper", dropper), ("input", input), ("messages", feed),
                ("screen", screen.gameObject), ("weightBar", weightBar), ("tabs", tabs),
                ("rowPrefab", AssetDatabase.LoadAssetAtPath<InventoryRow>(RowPrefabPath)), ("rowContainer", content),
                ("emptyText", empty), ("details", details));

            // В редакторе экран закрыт, чтобы не мешал смотреть сцену. InventoryUI всё равно прячет его в Awake.
            screen.gameObject.SetActive(false);
        }

        private static CategoryTab BuildTab(RectTransform panel, string label, bool all, ItemCategory category, float x)
        {
            var rect = UiFactory.Rect($"Tab_{label}", panel).TopLeft(x, 76f, 150f, 44f);
            UiFactory.Image(rect, Color.clear, raycast: true);
            var hover = UiFactory.Image(UiFactory.Rect("Hover", rect).Stretch(), Color.white.WithAlpha(0.03f));
            var button = rect.gameObject.AddComponent<Button>();
            UiFactory.SetupButton(button, hover);

            var text = UiFactory.Text(rect, "Label", label, 20f, UiPalette.Muted, TextAlignmentOptions.Center);
            text.rectTransform.Stretch();
            var underline = UiFactory.Rect("Underline", rect);
            underline.anchorMin = Vector2.zero;
            underline.anchorMax = new Vector2(1f, 0f);
            underline.pivot = new Vector2(0.5f, 0f);
            underline.anchoredPosition = Vector2.zero;
            underline.sizeDelta = new Vector2(0f, 3f);
            var underlineImage = UiFactory.Image(underline, UiPalette.Amber);

            var tab = rect.gameObject.AddComponent<CategoryTab>();
            Wire(tab, ("showAll", all), ("category", category), ("button", button), ("label", text), ("underline", underlineImage));
            return tab;
        }

        private static ItemDetailsPanel BuildDetails(RectTransform panel)
        {
            var details = UiFactory.Rect("Details", panel).TopLeft(704f, 136f, 444f, 460f);
            var content = UiFactory.Rect("Content", details).Stretch();

            var iconFrame = UiFactory.Rect("IconFrame", content).TopLeft(0f, 0f, 120f, 120f);
            UiFactory.Image(iconFrame, UiPalette.Background);
            var icon = UiFactory.Image(UiFactory.Rect("Icon", iconFrame).Stretch(8f, 8f, 8f, 8f), Color.white);
            icon.preserveAspect = true;

            var categoryText = UiFactory.Text(content, "Category", "ХЛАМ", 16f, UiPalette.Muted, TextAlignmentOptions.TopLeft);
            categoryText.characterSpacing = 4f;
            categoryText.rectTransform.TopLeft(136f, 6f, 308f, 22f);
            var nameText = UiFactory.Text(content, "Name", "Название", 28f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            nameText.textWrappingMode = TextWrappingModes.Normal;
            nameText.rectTransform.TopLeft(136f, 32f, 308f, 84f);

            var description = UiFactory.Text(content, "Description", "Описание", 20f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            description.textWrappingMode = TextWrappingModes.Normal;
            description.rectTransform.TopLeft(0f, 136f, 444f, 72f);
            var stats = UiFactory.Text(content, "Stats", "Вес", 18f, UiPalette.Muted, TextAlignmentOptions.TopLeft);
            stats.textWrappingMode = TextWrappingModes.Normal;
            stats.lineSpacing = 10f;
            stats.rectTransform.TopLeft(0f, 214f, 444f, 86f);

            var salvageBlock = UiFactory.Rect("Salvage", content).TopLeft(0f, 304f, 444f, 104f);
            var salvageHeader = UiFactory.Text(salvageBlock, "Header", "ПРИ РАЗБОРКЕ", 16f, UiPalette.Muted, TextAlignmentOptions.TopLeft);
            salvageHeader.characterSpacing = 4f;
            salvageHeader.rectTransform.TopLeft(0f, 0f, 444f, 22f);
            var components = UiFactory.Text(salvageBlock, "Components", "2 × Проводка", 20f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            components.rectTransform.TopLeft(0f, 28f, 444f, 76f);

            // Ряд кнопок: «Разобрать» есть только у хлама, и когда она скрыта, «Выбросить» встаёт на её место.
            var buttons = UiFactory.Rect("Buttons", content).TopLeft(0f, 414f, 444f, 44f);
            var buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 12f;
            buttonsLayout.childControlWidth = true;
            buttonsLayout.childControlHeight = true;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = true;
            // Акцентная кнопка — тёмный янтарный фон, нейтральная — на ступень светлее панели.
            var salvageButton = BuildActionButton(buttons, "SalvageButton", "Разобрать", "X", UiPalette.Amber,
                Hex(0x3A2E1C), Hex(0x4A3A22), Hex(0x56432A));
            var dropButton = BuildActionButton(buttons, "DropButton", "Выбросить", "G", UiPalette.Text,
                UiPalette.PanelRaised, Hex(0x3A3732), Hex(0x45413B));

            var panelView = details.gameObject.AddComponent<ItemDetailsPanel>();
            Wire(panelView,
                ("content", content.gameObject), ("icon", icon), ("nameText", nameText), ("categoryText", categoryText),
                ("descriptionText", description), ("statsText", stats), ("salvageBlock", salvageBlock.gameObject),
                ("componentsText", components), ("salvageButton", salvageButton), ("dropButton", dropButton));
            return panelView;
        }

        // Кнопка с непрозрачными цветами: полупрозрачные в линейном цветовом пространстве выходят слишком яркими.
        private static Button BuildActionButton(RectTransform parent, string name, string label, string key, Color textColor,
            Color normal, Color highlighted, Color pressed)
        {
            var rect = UiFactory.Rect(name, parent);
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 216f;
            var background = UiFactory.Image(rect, Color.white, raycast: true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = normal;
            colors.disabledColor = Hex(0x2A2622);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            UiFactory.Text(rect, "Label", $"{label}   {Key(key)}", 20f, textColor, TextAlignmentOptions.Center).rectTransform.Stretch();
            return button;
        }

        private static string Key(string key) => $"<color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Amber)}>[{key}]</color>";

        private static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
