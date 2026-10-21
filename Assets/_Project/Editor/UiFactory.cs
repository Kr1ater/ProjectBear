using DeadAir.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.EditorTools
{
    // Короткие помощники для сборки uGUI из кода генератора.
    internal static class UiFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = DemoContentBuilder.UiLayer };
            var rect = (RectTransform)go.transform;
            if (parent != null)
                rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(this RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Anchor(this RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // Прямоугольник от левого верхнего угла родителя: x вправо, y вниз.
        public static RectTransform TopLeft(this RectTransform rect, float x, float y, float width, float height) =>
            rect.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(width, height));

        public static Image Image(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment, Material material = null)
        {
            var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            if (material != null)
                label.fontSharedMaterial = material;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        // Подсветка при наведении через отдельный полупрозрачный слой: фон строки остаётся под данными.
        public static void SetupButton(Button button, Graphic hover)
        {
            button.targetGraphic = hover;
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white.WithAlpha(0f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white.WithAlpha(0f);
            colors.disabledColor = Color.white.WithAlpha(0f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }
    }
}
