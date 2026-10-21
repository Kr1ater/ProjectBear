using System.Collections.Generic;
using DeadAir.Player;
using DeadAir.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Янтарные скобки над лутом. Это элементы Canvas, а не объекты в мире:
    // никаких обводок и свечения на самих предметах.
    // Скобка появляется плавно и «сходится» на предмет, при пропаже гаснет на последнем месте.
    public sealed class LootMarkers : MonoBehaviour
    {
        [SerializeField] private LootSensor sensor;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Image markerPrefab;
        [SerializeField] private RectTransform container;

        [Header("Подпись у предмета в фокусе")]
        [SerializeField] private RectTransform focusLabel;
        [SerializeField] private CanvasGroup focusLabelGroup;
        [SerializeField] private TMP_Text focusLabelText;
        [SerializeField] private Vector2 focusLabelOffset = new(0f, 18f);

        [Header("Вид")]
        [SerializeField] private float nearSize = 60f;
        [SerializeField] private float farSize = 30f;
        [Tooltip("Скобка предмета в фокусе крупнее и ярче остальных")]
        [SerializeField] private float focusedScale = 1.3f;
        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.4f;

        [Header("Анимация, с")]
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float focusDuration = 0.12f;
        [Tooltip("Во сколько раз скобка крупнее в момент появления")]
        [SerializeField] private float appearScale = 1.6f;

        // Состояние одной скобки: 0..1 насколько она видна и насколько в фокусе.
        private sealed class Marker
        {
            public WorldItem Item;
            public Image Image;
            public Vector3 ScreenPosition;
            public float Shown;
            public float Focus;
        }

        private readonly List<Marker> markers = new();
        private readonly Stack<Image> pool = new();
        private Marker labelMarker;
        private WorldItem labelItem;
        private int labelAmount;

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            foreach (var item in sensor.VisibleItems)
                if (IsAlive(item) && Find(item) == null)
                    markers.Add(new Marker { Item = item, Image = Rent() });

            for (int i = markers.Count - 1; i >= 0; i--)
            {
                var marker = markers[i];
                bool visible = IsAlive(marker.Item) && Contains(sensor.VisibleItems, marker.Item);
                bool focused = visible && marker.Item == sensor.Focused;
                marker.Shown = Mathf.MoveTowards(marker.Shown, visible ? 1f : 0f, dt / fadeDuration);
                marker.Focus = Mathf.MoveTowards(marker.Focus, focused ? 1f : 0f, dt / focusDuration);

                // Подобранный предмет улетает к рюкзаку — скобка гаснет там, где он лежал.
                if (IsAlive(marker.Item))
                    marker.ScreenPosition = viewCamera.WorldToScreenPoint(marker.Item.MarkerPosition);

                if (marker.Shown <= 0f)
                    Release(i);
                else
                    Apply(marker);
            }

            UpdateFocusLabel();
        }

        private void Apply(Marker marker)
        {
            if (marker.ScreenPosition.z <= 0f)
            {
                marker.Image.enabled = false;
                return;
            }

            float appear = Mathf.SmoothStep(0f, 1f, marker.Shown);
            float distance = Mathf.Clamp01(marker.ScreenPosition.z / sensor.MarkerRadius);
            float size = Mathf.Lerp(nearSize, farSize, distance)
                         * Mathf.Lerp(1f, focusedScale, marker.Focus)
                         * Mathf.Lerp(appearScale, 1f, appear);

            var rect = marker.Image.rectTransform;
            rect.position = new Vector3(marker.ScreenPosition.x, marker.ScreenPosition.y, 0f);
            rect.sizeDelta = new Vector2(size, size);
            marker.Image.enabled = true;
            marker.Image.color = UiPalette.Amber.WithAlpha(Mathf.Lerp(idleAlpha, 1f, marker.Focus) * appear);
        }

        private void UpdateFocusLabel()
        {
            var focused = sensor.Focused;
            var marker = focused != null ? Find(focused) : null;
            if (marker != null)
                labelMarker = marker;

            float alpha = labelMarker != null && labelMarker.Image.enabled
                ? labelMarker.Focus * Mathf.SmoothStep(0f, 1f, labelMarker.Shown)
                : 0f;
            focusLabelGroup.alpha = alpha;
            focusLabel.gameObject.SetActive(alpha > 0.01f);
            if (alpha <= 0.01f)
                return;

            // Текст пересобираем, только когда сменился предмет или его количество.
            if (IsAlive(labelMarker.Item) && (labelMarker.Item != labelItem || labelMarker.Item.Amount != labelAmount))
            {
                labelItem = labelMarker.Item;
                labelAmount = labelItem.Amount;
                string amount = labelAmount > 1 ? $" ×{labelAmount}" : string.Empty;
                string weight = UiText.Weight(labelItem.Item.Weight * labelAmount);
                focusLabelText.text =
                    $"<color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Amber)}>[E]</color> Подобрать: {labelItem.Item.DisplayName}{amount}" +
                    $"  <color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Muted)}>{weight}</color>";
            }

            var markerRect = labelMarker.Image.rectTransform;
            focusLabel.position = markerRect.position;
            focusLabel.anchoredPosition += new Vector2(0f, markerRect.sizeDelta.y * 0.5f) + focusLabelOffset;
        }

        private Marker Find(WorldItem item)
        {
            foreach (var marker in markers)
                if (ReferenceEquals(marker.Item, item))
                    return marker;
            return null;
        }

        private static bool Contains(IReadOnlyList<WorldItem> items, WorldItem item)
        {
            foreach (var candidate in items)
                if (ReferenceEquals(candidate, item))
                    return true;
            return false;
        }

        private static bool IsAlive(WorldItem item) => item != null && item.Amount > 0;

        private Image Rent()
        {
            var image = pool.Count > 0 ? pool.Pop() : Instantiate(markerPrefab, container);
            image.gameObject.SetActive(true);
            image.enabled = false;
            return image;
        }

        private void Release(int index)
        {
            var marker = markers[index];
            if (labelMarker == marker)
                labelMarker = null;
            marker.Image.gameObject.SetActive(false);
            pool.Push(marker.Image);
            markers.RemoveAt(index);
        }
    }
}
