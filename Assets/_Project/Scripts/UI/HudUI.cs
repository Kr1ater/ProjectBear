using DeadAir.InventorySystem;
using DeadAir.Player;
using UnityEngine;

namespace DeadAir.UI
{
    // Связывает сенсор лута с HUD: итог подбора — в ленту, отказ по весу — вспышка шкалы.
    public sealed class HudUI : MonoBehaviour
    {
        [SerializeField] private LootSensor sensor;
        [SerializeField] private MessageFeed messages;
        [SerializeField] private WeightBar weightBar;

        private void OnEnable() => sensor.PickupAttempted += OnPickupAttempted;

        private void OnDisable() => sensor.PickupAttempted -= OnPickupAttempted;

        private void OnPickupAttempted(AddResult result)
        {
            bool refused = result.Status != AddStatus.Ok;
            messages.Show(UiText.PickupResult(result), attention: refused);
            if (refused)
                weightBar.Flash();
        }
    }
}
