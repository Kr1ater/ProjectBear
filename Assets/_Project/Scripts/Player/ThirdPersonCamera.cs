using UnityEngine;

namespace DeadAir.Player
{
    // Камера за плечом: вращается мышью вокруг цели и не проходит сквозь препятствия.
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField] private Transform target;
        [Tooltip("Сдвиг точки обзора от цели: камера за правым плечом, герой не закрывает центр экрана")]
        [SerializeField] private Vector3 shoulderOffset = new(0.9f, 0.35f, 0f);
        [SerializeField] private float distance = 3.4f;
        [Tooltip("Градусов на пиксель движения мыши")]
        [SerializeField] private float sensitivity = 0.1f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 55f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private float collisionRadius = 0.2f;

        private float yaw;
        private float pitch = 15f;

        private void Start()
        {
            yaw = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            Vector2 look = input.Look;
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Quaternion.Euler(0f, yaw, 0f) * shoulderOffset;
            Vector3 back = rotation * Vector3.back;

            float allowed = distance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out var hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
                allowed = hit.distance;

            transform.SetPositionAndRotation(pivot + back * allowed, rotation);
        }
    }
}
