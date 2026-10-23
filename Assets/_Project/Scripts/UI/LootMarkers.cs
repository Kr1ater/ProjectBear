using System.Collections.Generic;
using DeadAir.Player;
using DeadAir.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Янтарные скобки над лутом. Это картинки на Canvas, а не объекты в мире:
    // по ТЗ на самих предметах никаких обводок и свечения.
    // Скобка плавно появляется и «сходится» на предмет. Когда предмет пропал, гаснет на последнем месте.
    // Скобки заранее лежат на сцене выключенными. Скрипт ничего не создаёт: берёт свободную скобку,
    // ставит её над предметом, а когда она погасла — выключает и возвращает в список свободных.
    public sealed class LootMarkers : MonoBehaviour
    {
        [SerializeField] private LootSensor sensor;
        [SerializeField] private Camera viewCamera;
        [Tooltip("Готовые скобки на сцене. Сколько их, столько предметов подсвечивается одновременно")]
        [SerializeField] private Image[] markerImages;

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

        // Одна скобка над одним предметом.
        private class Marker
        {
            public WorldItem Item;
            public Image Image;
            // x, y — позиция на экране в пикселях, z — расстояние от камеры.
            public Vector3 ScreenPosition;
            // 0 — скобки не видно, 1 — видна полностью.
            public float Visibility;
            // 0 — обычная скобка, 1 — полностью в фокусе.
            public float FocusAmount;
        }

        private readonly List<Marker> markers = new();
        // Скобки, которые сейчас не заняты ни одним предметом.
        private readonly Stack<Image> freeImages = new();

        // Скобка, над которой сейчас висит подпись «[E] Подобрать».
        private Marker labelMarker;
        // Предмет и количество, для которых последний раз собирался текст подписи.
        private WorldItem labelItem;
        private int labelAmount;

        private void Awake()
        {
            foreach (Image image in markerImages)
            {
                image.gameObject.SetActive(false);
                freeImages.Push(image);
            }
        }

        // LateUpdate: рисуем после того, как за кадр сдвинулись герой и камера.
        private void LateUpdate()
        {
            AddMarkersForNewItems();
            UpdateMarkers();
            UpdateFocusLabel();
        }

        // Заводит скобку для каждого видимого предмета, у которого её ещё нет.
        private void AddMarkersForNewItems()
        {
            foreach (WorldItem item in sensor.VisibleItems)
            {
                if (!IsLyingOnGround(item))
                    continue;
                if (FindMarker(item) != null)
                    continue;
                // Все скобки заняты — остальные предметы останутся без подсветки, пока какая-нибудь не освободится.
                if (freeImages.Count == 0)
                    return;

                Marker marker = new Marker();
                marker.Item = item;
                marker.Image = TakeFreeImage();
                markers.Add(marker);
            }
        }

        private void UpdateMarkers()
        {
            // Время без учёта Time.timeScale, чтобы интерфейс не замедлялся вместе с игрой.
            float deltaTime = Time.unscaledDeltaTime;

            // Идём с конца списка, потому что внутри цикла можем удалять скобки.
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                Marker marker = markers[i];

                bool shouldBeVisible = IsLyingOnGround(marker.Item) && sensor.IsVisible(marker.Item);
                bool shouldBeFocused = shouldBeVisible && marker.Item == sensor.Focused;

                // MoveTowards двигает значение к цели не больше чем на шаг за кадр — так получается плавность.
                // Шаг deltaTime / fadeDuration означает: путь от 0 до 1 займёт fadeDuration секунд.
                float visibilityTarget = shouldBeVisible ? 1f : 0f;
                float focusTarget = shouldBeFocused ? 1f : 0f;
                marker.Visibility = Mathf.MoveTowards(marker.Visibility, visibilityTarget, deltaTime / fadeDuration);
                marker.FocusAmount = Mathf.MoveTowards(marker.FocusAmount, focusTarget, deltaTime / focusDuration);

                // Пока предмет лежит, скобка следует за ним. Подобранный улетает к рюкзаку — скобка остаётся где была.
                if (IsLyingOnGround(marker.Item))
                    marker.ScreenPosition = viewCamera.WorldToScreenPoint(marker.Item.MarkerPosition);

                if (marker.Visibility <= 0f)
                    RemoveMarker(i);
                else
                    DrawMarker(marker);
            }
        }

        private void DrawMarker(Marker marker)
        {
            // Предмет за спиной камеры — скобку не рисуем.
            bool isBehindCamera = marker.ScreenPosition.z <= 0f;
            if (isBehindCamera)
            {
                marker.Image.enabled = false;
                return;
            }

            // Плавная кривая вместо прямой: появление мягче в начале и в конце.
            float appear = Mathf.SmoothStep(0f, 1f, marker.Visibility);

            // Размер складывается из трёх множителей.
            // 1. Расстояние: вблизи скобка nearSize, на краю радиуса — farSize.
            float distanceFactor = Mathf.Clamp01(marker.ScreenPosition.z / sensor.MarkerRadius);
            float size = Mathf.Lerp(nearSize, farSize, distanceFactor);
            // 2. Фокус: скобка в фокусе крупнее.
            size *= Mathf.Lerp(1f, focusedScale, marker.FocusAmount);
            // 3. Появление: сначала скобка крупная и «сходится» к обычному размеру.
            size *= Mathf.Lerp(appearScale, 1f, appear);

            // Прозрачность: обычная скобка полупрозрачная, в фокусе — непрозрачная. При появлении — от нуля.
            float alpha = Mathf.Lerp(idleAlpha, 1f, marker.FocusAmount) * appear;

            RectTransform rect = marker.Image.rectTransform;
            rect.position = new Vector3(marker.ScreenPosition.x, marker.ScreenPosition.y, 0f);
            rect.sizeDelta = new Vector2(size, size);
            marker.Image.color = UiPalette.WithAlpha(UiPalette.Amber, alpha);
            marker.Image.enabled = true;
        }

        private void UpdateFocusLabel()
        {
            // Подпись привязана к скобке предмета в фокусе. Фокус пропал — подпись гаснет вместе со скобкой.
            if (IsLyingOnGround(sensor.Focused))
            {
                Marker focusedMarker = FindMarker(sensor.Focused);
                if (focusedMarker != null)
                    labelMarker = focusedMarker;
            }

            float labelAlpha = 0f;
            if (labelMarker != null && labelMarker.Image.enabled)
                labelAlpha = labelMarker.FocusAmount * Mathf.SmoothStep(0f, 1f, labelMarker.Visibility);

            focusLabelGroup.alpha = labelAlpha;
            bool labelVisible = labelAlpha > 0.01f;
            focusLabel.gameObject.SetActive(labelVisible);
            if (!labelVisible)
                return;

            UpdateLabelText(labelMarker.Item);
            PlaceLabelAboveMarker(labelMarker);
        }

        private void UpdateLabelText(WorldItem item)
        {
            // Предмет уже подобран — оставляем последний текст, пока подпись гаснет.
            if (!IsLyingOnGround(item))
                return;

            // Пересобираем строку, только если сменился предмет или его количество, а не каждый кадр.
            if (item == labelItem && item.Amount == labelAmount)
                return;

            labelItem = item;
            labelAmount = item.Amount;
            focusLabelText.text = UiText.PickupHint(item.Item, item.Amount);
        }

        private void PlaceLabelAboveMarker(Marker marker)
        {
            // Ставим подпись в центр скобки и поднимаем на половину высоты скобки плюс отступ.
            RectTransform markerRect = marker.Image.rectTransform;
            focusLabel.position = markerRect.position;
            float halfMarkerHeight = markerRect.sizeDelta.y * 0.5f;
            focusLabel.anchoredPosition += new Vector2(0f, halfMarkerHeight) + focusLabelOffset;
        }

        private Marker FindMarker(WorldItem item)
        {
            foreach (Marker marker in markers)
            {
                if (marker.Item == item)
                    return marker;
            }
            return null;
        }

        // Предмет существует и ещё не подобран.
        private static bool IsLyingOnGround(WorldItem item)
        {
            return item != null && item.Amount > 0;
        }

        private Image TakeFreeImage()
        {
            Image image = freeImages.Pop();
            image.gameObject.SetActive(true);
            // Включится в DrawMarker, когда у скобки будет позиция.
            image.enabled = false;
            return image;
        }

        private void RemoveMarker(int index)
        {
            Marker marker = markers[index];
            if (labelMarker == marker)
                labelMarker = null;

            marker.Image.gameObject.SetActive(false);
            freeImages.Push(marker.Image);
            markers.RemoveAt(index);
        }
    }
}
