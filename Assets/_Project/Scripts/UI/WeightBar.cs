using DeadAir.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadAir.UI
{
    // Шкала веса «12.6 / 15.0». При отказе по весу коротко вспыхивает янтарным.
    public sealed class WeightBar : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [Tooltip("Заполнение: ширина задаётся через anchorMax.x")]
        [SerializeField] private RectTransform fill;
        [SerializeField] private Graphic fillGraphic;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float flashDuration = 0.8f;

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
            var inventory = playerInventory.Inventory;
            fill.anchorMax = new Vector2(Mathf.Clamp01(inventory.CurrentWeight / inventory.MaxWeight), 1f);
            label.text = $"{UiText.Weight(inventory.CurrentWeight)} / {UiText.Weight(inventory.MaxWeight)}";
        }

        private void Update()
        {
            // Два коротких импульса, затухающих к обычному цвету.
            float t = (Time.unscaledTime - flashStartedAt) / flashDuration;
            float pulse = t < 1f ? Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * (1f - t) : 0f;
            fillGraphic.color = Color.Lerp(UiPalette.Text, UiPalette.Amber, Mathf.Clamp01(pulse * 2f));
            label.color = Color.Lerp(UiPalette.Text, UiPalette.Amber, Mathf.Clamp01(pulse * 2f));
        }
    }
}
