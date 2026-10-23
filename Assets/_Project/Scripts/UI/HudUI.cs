using DeadAir.InventorySystem;
using DeadAir.Player;
using UnityEngine;

namespace DeadAir.UI
{
    // Связывает сенсор лута с HUD: итог каждого подбора — в ленту сообщений,
    // отказ по весу — ещё и вспышка шкалы веса.
    public sealed class HudUI : MonoBehaviour
    {
        [SerializeField] private LootSensor sensor;
        [SerializeField] private MessageFeed messages;
        [SerializeField] private WeightBar weightBar;

        private void OnEnable()
        {
            sensor.PickupAttempted += OnPickupAttempted;
        }

        private void OnDisable()
        {
            sensor.PickupAttempted -= OnPickupAttempted;
        }

        private void OnPickupAttempted(AddResult result)
        {
            bool notEverythingFit = result.Status != AddStatus.Ok;

            // Частичный подбор и перегруз выделяем янтарным.
            messages.Show(UiText.PickupResult(result), notEverythingFit);

            if (notEverythingFit)
                weightBar.Flash();
        }
    }
}
