using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadAir.Player
{
    public sealed class InputReader : MonoBehaviour
    {
        private const float MousePixelsToAxis = 0.1f;

        [Header("Клавиши")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode toggleInventoryKey = KeyCode.Tab;
        [SerializeField] private KeyCode salvageKey = KeyCode.X;
        [SerializeField] private KeyCode dropKey = KeyCode.G;
        [Tooltip("Та же клавиша, что и разборка: смена плеча работает в игре, разборка — при открытом инвентаре")]
        [SerializeField] private KeyCode swapShoulderKey = KeyCode.X;

        public event Action InteractPressed;
        public event Action ToggleInventoryPressed;
        public event Action SalvagePressed;
        public event Action DropPressed;
        public event Action SwapShoulderPressed;

        // false, пока открыт инвентарь: движение, камера и подбор не работают, курсор свободен.
        public bool GameplayEnabled { get; private set; }
        
        public Vector2 Move
        {
            get
            {
                if (!GameplayEnabled)
                    return Vector2.zero;
                
                float sideways = Input.GetAxisRaw("Horizontal");
                float forward = Input.GetAxisRaw("Vertical");
                return new Vector2(sideways, forward);
            }
        }
        
        public Vector2 Look
        {
            get
            {
                if (!GameplayEnabled)
                    return Vector2.zero;

                return Mouse.current.delta.ReadValue() * MousePixelsToAxis;
            }
        }

        // Игрок сам освободил курсор клавишей Escape. Тогда не возвращаем его, пока игрок не кликнет в окне игры.
        private bool cursorReleasedByPlayer;

        private void OnEnable()
        {
            SetGameplayEnabled(true);
        }

        private void Update()
        {
            KeepCursorInGame();
            
            if (Input.GetKeyDown(toggleInventoryKey))
                ToggleInventoryPressed?.Invoke();

            if (Input.GetKeyDown(salvageKey))
                SalvagePressed?.Invoke();

            if (Input.GetKeyDown(dropKey))
                DropPressed?.Invoke();
            
            if (GameplayEnabled && Input.GetKeyDown(interactKey))
                InteractPressed?.Invoke();

            if (GameplayEnabled && Input.GetKeyDown(swapShoulderKey))
                SwapShoulderPressed?.Invoke();
        }
        
        private void KeepCursorInGame()
        {
            if (!GameplayEnabled)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                cursorReleasedByPlayer = true;
            if (Input.GetMouseButtonDown(0))
                cursorReleasedByPlayer = false;

            if (cursorReleasedByPlayer)
                return;

            bool cursorFreed = Cursor.lockState != CursorLockMode.Locked || Cursor.visible;
            if (cursorFreed)
                LockCursor();
        }

        public void SetGameplayEnabled(bool enabled)
        {
            GameplayEnabled = enabled;

            if (enabled)
            {
                cursorReleasedByPlayer = false;
                LockCursor();
            }
            else
            {
                // Инвентарь: курсор виден, им можно кликать по интерфейсу.
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
