using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadAir.Player
{
    // Единственное место, которое знает про Input System. Остальные читают значения и события отсюда.
    public sealed class InputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference move;
        [SerializeField] private InputActionReference look;
        [SerializeField] private InputActionReference interact;
        [SerializeField] private InputActionReference toggleInventory;
        [SerializeField] private InputActionReference salvage;
        [SerializeField] private InputActionReference drop;

        public event Action InteractPressed;
        public event Action ToggleInventoryPressed;
        public event Action SalvagePressed;
        public event Action DropPressed;

        // Выключается, пока открыт инвентарь: движение, камера и подбор не работают, курсор свободен.
        public bool GameplayEnabled { get; private set; }

        public Vector2 Move => GameplayEnabled ? move.action.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => GameplayEnabled ? look.action.ReadValue<Vector2>() : Vector2.zero;

        private void OnEnable()
        {
            move.action.Enable();
            look.action.Enable();
            interact.action.Enable();
            toggleInventory.action.Enable();
            salvage.action.Enable();
            drop.action.Enable();

            interact.action.performed += OnInteract;
            toggleInventory.action.performed += OnToggleInventory;
            salvage.action.performed += OnSalvage;
            drop.action.performed += OnDrop;

            SetGameplayEnabled(true);
        }

        private void OnDisable()
        {
            interact.action.performed -= OnInteract;
            toggleInventory.action.performed -= OnToggleInventory;
            salvage.action.performed -= OnSalvage;
            drop.action.performed -= OnDrop;
        }

        public void SetGameplayEnabled(bool enabled)
        {
            GameplayEnabled = enabled;
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
        }

        private void OnInteract(InputAction.CallbackContext _)
        {
            if (GameplayEnabled)
                InteractPressed?.Invoke();
        }

        private void OnToggleInventory(InputAction.CallbackContext _) => ToggleInventoryPressed?.Invoke();

        private void OnSalvage(InputAction.CallbackContext _) => SalvagePressed?.Invoke();

        private void OnDrop(InputAction.CallbackContext _) => DropPressed?.Invoke();
    }
}
