using System.Collections.Generic;
using DeadAir.Items;
using DeadAir.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Янтарные скобки над лутом. Это элементы Canvas, а не объекты в мире:
    // никаких обводок и свечения на самих предметах.
    public sealed class LootMarkers : MonoBehaviour
    {
        [SerializeField] private LootSensor sensor;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Image markerPrefab;
        [SerializeField] private RectTransform container;

        [Header("Подпись у предмета в фокусе")]
        [SerializeField] private RectTransform focusLabel;
        [SerializeField] private TMP_Text focusLabelText;
        [SerializeField] private Vector2 focusLabelOffset = new(0f, 18f);

        [Header("Вид")]
        [SerializeField] private float nearSize = 60f;
        [SerializeField] private float farSize = 30f;
        [Tooltip("Скобка предмета в фокусе крупнее и ярче остальных")]
        [SerializeField] private float focusedScale = 1.3f;
        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.4f;

        private readonly List<Image> pool = new();

        private void LateUpdate()
        {
            int used = 0;
            Image focusedMarker = null;

            foreach (var item in sensor.VisibleItems)
            {
                if (item == null || item.Amount == 0)
                    continue;

                Vector3 screen = viewCamera.WorldToScreenPoint(item.MarkerPosition);
                if (screen.z <= 0f)
                    continue;

                var marker = GetMarker(used++);
                bool focused = item == sensor.Focused;
                float t = Mathf.Clamp01(screen.z / sensor.MarkerRadius);
                float size = Mathf.Lerp(nearSize, farSize, t) * (focused ? focusedScale : 1f);
                marker.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
                marker.rectTransform.sizeDelta = new Vector2(size, size);
                marker.color = focused ? UiPalette.Amber : UiPalette.Amber.WithAlpha(idleAlpha);
                if (focused)
                    focusedMarker = marker;
            }

            for (int i = used; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);

            UpdateFocusLabel(focusedMarker);
        }

        private void UpdateFocusLabel(Image marker)
        {
            var focused = sensor.Focused;
            bool show = marker != null && focused != null;
            focusLabel.gameObject.SetActive(show);
            if (!show)
                return;

            ItemDefinition item = focused.Item;
            string amount = focused.Amount > 1 ? $" ×{focused.Amount}" : string.Empty;
            string weight = UiText.Weight(item.Weight * focused.Amount);
            focusLabelText.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Amber)}>[E]</color> Подобрать: {item.DisplayName}{amount}" +
                $"  <color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Muted)}>{weight}</color>";

            var markerRect = marker.rectTransform;
            focusLabel.position = markerRect.position;
            focusLabel.anchoredPosition += new Vector2(0f, markerRect.sizeDelta.y * 0.5f) + focusLabelOffset;
        }

        private Image GetMarker(int index)
        {
            if (index == pool.Count)
                pool.Add(Instantiate(markerPrefab, container));

            var marker = pool[index];
            marker.gameObject.SetActive(true);
            return marker;
        }
    }
}
