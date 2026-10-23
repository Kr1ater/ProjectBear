using UnityEngine;

namespace DeadAir.Player
{
    // Движение героя относительно камеры: W — туда, куда смотрит камера.
    // Вес инвентаря на скорость не влияет.
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float moveSpeed = 3.5f;
        [Tooltip("Скорость поворота корпуса, градусов в секунду")]
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController controller;
        private float verticalSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector3 moveDirection = GetMoveDirection();

            if (moveDirection != Vector3.zero)
                TurnTowards(moveDirection);

            ApplyGravity();

            Vector3 velocity = moveDirection * moveSpeed;
            velocity.y = verticalSpeed;
            controller.Move(velocity * Time.deltaTime);
        }

        // Переводит нажатые WASD в направление в мире относительно камеры.
        private Vector3 GetMoveDirection()
        {
            Vector2 keys = input.Move;

            // «Вперёд» камеры без наклона вверх-вниз, чтобы герой не пытался идти в землю или в небо.
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();
            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            Vector3 direction = cameraForward * keys.y + cameraRight * keys.x;

            // По диагонали (W + D) вектор длиннее 1 — ограничиваем, чтобы герой не ходил быстрее.
            return Vector3.ClampMagnitude(direction, 1f);
        }

        // Плавно поворачивает героя лицом к направлению движения.
        private void TurnTowards(Vector3 direction)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float maxTurnThisFrame = turnSpeed * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxTurnThisFrame);
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded)
            {
                // На земле держим небольшую скорость вниз: так контроллер не «отлипает» от земли на неровностях.
                verticalSpeed = -2f;
            }
            else
            {
                // В воздухе скорость падения растёт каждый кадр.
                verticalSpeed += gravity * Time.deltaTime;
            }
        }
    }
}
