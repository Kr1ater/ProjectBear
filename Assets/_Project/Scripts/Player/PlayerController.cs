using UnityEngine;

namespace DeadAir.Player
{
    // Простое движение от третьего лица относительно камеры. Вес инвентаря на скорость не влияет.
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
            Vector2 moveInput = input.Move;
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = Vector3.ClampMagnitude(forward * moveInput.y + right * moveInput.x, 1f);

            if (direction.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            // Небольшая постоянная скорость вниз держит контроллер прижатым к земле.
            verticalSpeed = controller.isGrounded ? -2f : verticalSpeed + gravity * Time.deltaTime;

            Vector3 velocity = direction * moveSpeed + Vector3.up * verticalSpeed;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
