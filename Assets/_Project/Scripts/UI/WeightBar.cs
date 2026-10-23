using DeadAir.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Шкала веса «12.6 / 15.0». При отказе по весу коротко вспыхивает янтарным.
    // На сцене их две: компактная в HUD и в шапке инвентаря.
    public sealed class WeightBar : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [Tooltip("Заполнение: ширина задаётся через anchorMax.x")]
        [SerializeField] private RectTransform fill;
        [SerializeField] private Graphic fillGraphic;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float flashDuration = 0.8f;

        // Когда началась последняя вспышка. Минус бесконечность — вспышек ещё не было.
        private float flashStartedAt = float.NegativeInfinity;

        private void OnEnable()
        {
            playerInventory.Inventory.OnChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            playerInventory.Inventory.OnChanged -= Refresh;
        }

        public void Flash()
        {
            flashStartedAt = Time.unscaledTime;
        }

        private void Refresh()
        {
            float current = playerInventory.Inventory.CurrentWeight;
            float max = playerInventory.Inventory.MaxWeight;

            // Правый край заполнения: 0 — пустая шкала, 1 — полная.
            float filledPart = Mathf.Clamp01(current / max);
            fill.anchorMax = new Vector2(filledPart, 1f);

            label.text = UiText.Weight(current) + " / " + UiText.Weight(max);
        }

        private void Update()
        {
            // В момент вспышки цвет янтарный и за flashDuration секунд плавно возвращается к обычному.
            Color color = UiPalette.Text;
            float timeSinceFlash = Time.unscaledTime - flashStartedAt;
            if (timeSinceFlash < flashDuration)
            {
                float progress = timeSinceFlash / flashDuration;
                color = Color.Lerp(UiPalette.Amber, UiPalette.Text, progress);
            }

            fillGraphic.color = color;
            label.color = color;
        }
    }
}
