using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeadAir.Items;
using DeadAir.Player;
using DeadAir.UI;
using DeadAir.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadAir.EditorTools
{
    // Генератор демо-контента: материалы, ассеты предметов, префабы из примитивов, иконки и UI-префабы.
    // Повторный запуск перезаписывает файлы на тех же путях, GUID ассетов сохраняются.
    // Сцену собирает DemoSceneBuilder.
    public static class DemoContentBuilder
    {
        public const string Root = "Assets/_Project";
        public const string MaterialsDir = Root + "/Art/Materials";
        public const string IconsDir = Root + "/Art/Icons";
        public const string UiArtDir = Root + "/Art/UI";
        public const string ItemsDir = Root + "/Data/Items";
        public const string ItemPrefabsDir = Root + "/Prefabs/Items";
        public const string UiPrefabsDir = Root + "/Prefabs/UI";
        public const string BracketSpritePath = UiArtDir + "/LootBracket.png";
        public const string HudTextMaterialPath = UiArtDir + "/HudText.mat";
        public const string MarkerPrefabPath = UiPrefabsDir + "/LootMarker.prefab";
        public const string MessageLinePrefabPath = UiPrefabsDir + "/MessageLine.prefab";
        public const string RowPrefabPath = UiPrefabsDir + "/InventoryRow.prefab";
        public const string UiFontPath = Root + "/Art/Fonts/Inter-Regular.ttf";
        public const string UiFontAssetPath = Root + "/Art/Fonts/Inter-Regular SDF.asset";
        private const string FallbackFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        public const int LootLayer = 6;
        public const int PlayerLayer = 7;
        public const int UiLayer = 5;

        private static readonly (string name, int rgb, float metallic, float smoothness)[] MaterialSpecs =
        {
            ("Ground", 0x6B3322, 0f, 0.05f),
            ("Rock", 0x5C463A, 0f, 0.1f),
            ("RockOchre", 0x8A6238, 0f, 0.1f),
            ("MetalGray", 0x6E6C66, 0.6f, 0.3f),
            ("MetalDark", 0x34322F, 0.5f, 0.25f),
            ("PaintWhite", 0xBFB8A8, 0f, 0.2f),
            ("PaintOchre", 0x9C7436, 0f, 0.2f),
            ("HazardOrange", 0xB8652A, 0f, 0.2f),
            ("Rubber", 0x1F1E1C, 0f, 0.1f),
            ("TapeGray", 0x7D7A73, 0f, 0.25f),
            ("Copper", 0x8A5530, 0.7f, 0.3f),
            ("Circuit", 0x3D4532, 0f, 0.2f),
            ("Suit", 0xB5AE9E, 0f, 0.15f),
            ("Visor", 0x1E2224, 0.2f, 0.45f),
        };

        private static Dictionary<string, Material> materials;

        [MenuItem("Tools/Dead Air/Rebuild Demo Content")]
        public static void BuildAll()
        {
            BuildAssets();
            DemoSceneBuilder.Build();
        }

        public static void BuildAssets()
        {
            foreach (var dir in new[] { MaterialsDir, IconsDir, UiArtDir, ItemsDir, ItemPrefabsDir, UiPrefabsDir, Root + "/Scenes" })
                EnsureFolder(dir);

            SetupUiFont();
            CreateMaterials();
            var specs = DemoItems.All;
            var items = CreateItems(specs);
            CreateItemPrefabs(specs, items);
            RenderIcons(specs, items);
            CreateUiAssets();
            AssetDatabase.SaveAssets();
            Debug.Log($"Dead Air: собрано {items.Count} предметов, префабы, иконки и UI-ассеты");
        }

        // ---- Общие помощники, ими пользуются DemoItems и DemoSceneBuilder.

        public static Material Mat(string name)
        {
            if (materials == null)
                CreateMaterials();
            return materials[name];
        }

        public static Transform Group(Transform parent, string name, Vector3 localPosition, Vector3 localEuler)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = localPosition;
            group.localEulerAngles = localEuler;
            return group;
        }

        // Деталь модели без коллайдера.
        public static GameObject Part(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale,
            string material, Vector3 localEuler = default)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = $"{type}_{material}";
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localEulerAngles = localEuler;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = Mat(material);
            return part;
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            foreach (var child in go.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        public static Bounds RendererBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        // Заполняет сериализованные поля по именам: ссылки задаются как в инспекторе, без кода в рантайме.
        public static void Wire(Object target, params (string field, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var property = so.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"{target.GetType().Name}: нет поля {field}");
                    continue;
                }

                switch (value)
                {
                    case Object reference: property.objectReferenceValue = reference; break;
                    case int number: property.intValue = number; break;
                    case float number: property.floatValue = number; break;
                    case bool flag: property.boolValue = flag; break;
                    case string text: property.stringValue = text; break;
                    case System.Enum option: property.intValue = System.Convert.ToInt32(option); break;
                    case Object[] array:
                        property.arraySize = array.Length;
                        for (int i = 0; i < array.Length; i++)
                            property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                        break;
                    case null: property.objectReferenceValue = null; break;
                    default: Debug.LogError($"{field}: тип {value.GetType().Name} не поддержан"); break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        public static InputActionReference InputAction(string name) =>
            AssetDatabase.LoadAllAssetRepresentationsAtPath(Root + "/Input/DeadAirInput.inputactions")
                .OfType<InputActionReference>()
                .Single(reference => reference.action.name == name);

        // ---- Шрифт интерфейса

        // Inter из гайда проекта. Font Asset создаётся штатным меню TMP (Create → TextMeshPro → Font Asset → SDF),
        // динамический: кириллица и символы добавляются в атлас по мере надобности.
        // Inter становится шрифтом по умолчанию, LiberationSans остаётся запасным для редких символов.
        private static void SetupUiFont()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontAssetPath);
            if (fontAsset == null)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);
                EditorApplication.ExecuteMenuItem("Assets/Create/TextMeshPro/Font Asset/SDF");
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontAssetPath);
            }

            var settings = new SerializedObject(TMP_Settings.instance);
            settings.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
            var fallbacks = settings.FindProperty("m_fallbackFontAssets");
            fallbacks.arraySize = 1;
            fallbacks.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontAssetPath);
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        // ---- Материалы

        private static void CreateMaterials()
        {
            EnsureFolder(MaterialsDir);
            materials = new Dictionary<string, Material>();
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var (name, rgb, metallic, smoothness) in MaterialSpecs)
            {
                string path = $"{MaterialsDir}/M_{name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(lit);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.SetColor("_BaseColor", Hex(rgb));
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(material);
                materials[name] = material;
            }
        }

        // ---- Предметы

        private static Dictionary<string, ItemDefinition> CreateItems(DemoItems.Spec[] specs)
        {
            var items = new Dictionary<string, ItemDefinition>();
            foreach (var spec in specs)
            {
                string path = $"{ItemsDir}/{spec.File}.asset";
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<ItemDefinition>();
                    AssetDatabase.CreateAsset(item, path);
                }
                items[spec.Id] = item;
            }

            // Второй проход: список разборки ссылается на другие ассеты.
            foreach (var spec in specs)
            {
                var so = new SerializedObject(items[spec.Id]);
                so.FindProperty("id").stringValue = spec.Id;
                so.FindProperty("displayName").stringValue = spec.Name;
                so.FindProperty("description").stringValue = spec.Description;
                so.FindProperty("category").intValue = (int)spec.Category;
                so.FindProperty("weight").floatValue = spec.Weight;
                so.FindProperty("maxStack").intValue = spec.MaxStack;
                so.FindProperty("pickupMode").intValue = (int)spec.Mode;

                var salvage = so.FindProperty("salvageResult");
                salvage.arraySize = spec.Salvage.Length;
                for (int i = 0; i < spec.Salvage.Length; i++)
                {
                    var element = salvage.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("item").objectReferenceValue = items[spec.Salvage[i].id];
                    element.FindPropertyRelative("amount").intValue = spec.Salvage[i].amount;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(items[spec.Id]);
            }
            return items;
        }

        private static void CreateItemPrefabs(DemoItems.Spec[] specs, Dictionary<string, ItemDefinition> items)
        {
            foreach (var spec in specs)
            {
                var root = new GameObject(spec.File);
                spec.Model(root.transform);

                // Триггер по габаритам модели, не меньше 30 см: мелочь иначе трудно поймать сенсором.
                var bounds = RendererBounds(root);
                var trigger = root.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = bounds.center;
                trigger.size = Vector3.Max(bounds.size + Vector3.one * 0.05f, Vector3.one * 0.3f);

                var worldItem = root.AddComponent<WorldItem>();
                Wire(worldItem, ("item", items[spec.Id]), ("amount", 1));
                SetLayerRecursively(root, LootLayer);

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{ItemPrefabsDir}/{spec.File}.prefab");
                Object.DestroyImmediate(root);
                Wire(items[spec.Id], ("worldPrefab", prefab));
            }
        }

        // Иконки рендерятся из тех же моделей отдельной камерой в preview-сцене.
        private static void RenderIcons(DemoItems.Spec[] specs, Dictionary<string, ItemDefinition> items)
        {
            const int size = 256;
            var scene = EditorSceneManager.NewPreviewScene();
            var target = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(size, size, TextureFormat.RGBA32, false);
            try
            {
                var camera = new GameObject("IconCamera").AddComponent<Camera>();
                SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.fieldOfView = 20f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;
                camera.targetTexture = target;

                AddPreviewLight(scene, Hex(0xFFF2DE), 1.6f, new Vector3(45f, -40f, 0f));
                AddPreviewLight(scene, Hex(0xC8B8A8), 0.7f, new Vector3(20f, 140f, 0f));

                foreach (var spec in specs)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ItemPrefabsDir}/{spec.File}.prefab");
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    var bounds = RendererBounds(instance);

                    // Смотрим спереди-справа: лицевая сторона моделей обращена к +Z.
                    var direction = Quaternion.Euler(32f, 215f, 0f) * Vector3.forward;
                    float distance = bounds.extents.magnitude / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    camera.transform.SetPositionAndRotation(bounds.center - direction * distance, Quaternion.LookRotation(direction));
                    camera.Render();

                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                    pixels.Apply();
                    RenderTexture.active = null;
                    Object.DestroyImmediate(instance);

                    string path = $"{IconsDir}/{spec.File}.png";
                    File.WriteAllBytes(path, pixels.EncodeToPNG());
                    Wire(items[spec.Id], ("icon", ImportSprite(path, Vector4.zero)));
                }
            }
            finally
            {
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(pixels);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void AddPreviewLight(Scene scene, Color color, float intensity, Vector3 euler)
        {
            var light = new GameObject("Light").AddComponent<Light>();
            SceneManager.MoveGameObjectToScene(light.gameObject, scene);
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.transform.eulerAngles = euler;
        }

        private static Sprite ImportSprite(string path, Vector4 border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---- UI-ассеты: спрайт скобки, материал текста HUD, префабы маркера, строки ленты и строки инвентаря.

        private static void CreateUiAssets()
        {
            var bracket = CreateBracketSprite();
            var hudText = CreateHudTextMaterial();

            var marker = UiFactory.Rect("LootMarker", null);
            var markerImage = UiFactory.Image(marker, UiPalette.Amber);
            markerImage.sprite = bracket;
            markerImage.type = UnityEngine.UI.Image.Type.Sliced;
            markerImage.pixelsPerUnitMultiplier = 2f;
            marker.sizeDelta = new Vector2(40f, 40f);
            SavePrefab(marker.gameObject, MarkerPrefabPath);

            var line = UiFactory.Text(null, "MessageLine", "+5 Болты", 24f, UiPalette.Text, TextAlignmentOptions.BottomLeft, hudText);
            SavePrefab(line.gameObject, MessageLinePrefabPath);

            SavePrefab(BuildInventoryRow(), RowPrefabPath);
        }

        private static Sprite CreateBracketSprite()
        {
            // Четыре угла «⌜ ⌝ ⌞ ⌟», белые: цвет задаёт Image. Середина прозрачная, края режутся 9-slice.
            const int size = 64, thickness = 4, arm = 20;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool vertical = (x < thickness || x >= size - thickness) && (y < arm || y >= size - arm);
                    bool horizontal = (y < thickness || y >= size - thickness) && (x < arm || x >= size - arm);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(vertical || horizontal ? 255 : 0));
                }
            }
            texture.SetPixels32(pixels);
            File.WriteAllBytes(BracketSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return ImportSprite(BracketSpritePath, new Vector4(24f, 24f, 24f, 24f));
        }

        // Мягкая тень под текстом HUD, чтобы он читался на светлом грунте. Это не свечение: тень тёмная.
        private static Material CreateHudTextMaterial()
        {
            var font = TMP_Settings.defaultFontAsset;
            var material = AssetDatabase.LoadAssetAtPath<Material>(HudTextMaterialPath);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, HudTextMaterialPath);
            }
            material.CopyPropertiesFromMaterial(font.material);
            material.shader = font.material.shader;
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.65f));
            material.SetFloat("_UnderlayOffsetX", 0.4f);
            material.SetFloat("_UnderlayOffsetY", -0.4f);
            material.SetFloat("_UnderlaySoftness", 0.4f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject BuildInventoryRow()
        {
            var row = UiFactory.Rect("InventoryRow", null);
            row.sizeDelta = new Vector2(620f, 56f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            var background = UiFactory.Image(row, Color.clear, raycast: true);
            var hover = UiFactory.Image(UiFactory.Rect("Hover", row).Stretch(), Color.white.WithAlpha(0.03f));
            var button = row.gameObject.AddComponent<Button>();
            UiFactory.SetupButton(button, hover);

            var mark = UiFactory.Image(UiFactory.Rect("SelectionMark", row).Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 56f)), UiPalette.Amber);

            var iconFrame = UiFactory.Image(UiFactory.Rect("IconFrame", row).Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(44f, 44f)), UiPalette.Background);
            var icon = UiFactory.Image(UiFactory.Rect("Icon", iconFrame.rectTransform).Stretch(2f, 2f, 2f, 2f), Color.white);
            icon.preserveAspect = true;

            var nameText = UiFactory.Text(row, "Name", "Название", 22f, UiPalette.Text, TextAlignmentOptions.MidlineLeft);
            nameText.rectTransform.Stretch(76f, 0f, 200f, 0f);
            var countText = UiFactory.Text(row, "Count", "×5", 20f, UiPalette.Muted, TextAlignmentOptions.MidlineRight);
            countText.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-110f, 0f), new Vector2(90f, 40f));
            var weightText = UiFactory.Text(row, "Weight", "0.0", 20f, UiPalette.Text, TextAlignmentOptions.MidlineRight);
            weightText.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(80f, 40f));

            var divider = UiFactory.Image(UiFactory.Rect("Divider", row).Anchor(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero), UiPalette.Muted.WithAlpha(0.15f));
            divider.rectTransform.anchorMin = new Vector2(0f, 0f);
            divider.rectTransform.anchorMax = new Vector2(1f, 0f);
            divider.rectTransform.sizeDelta = new Vector2(0f, 1f);

            var view = row.gameObject.AddComponent<InventoryRow>();
            Wire(view,
                ("button", button), ("background", background), ("selectionMark", mark), ("icon", icon),
                ("nameText", nameText), ("countText", countText), ("weightText", weightText));
            return row.gameObject;
        }

        private static void SavePrefab(GameObject go, string path)
        {
            SetLayerRecursively(go, UiLayer);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
