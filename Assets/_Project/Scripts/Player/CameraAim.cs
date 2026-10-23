using Unity.Cinemachine;
using UnityEngine;

namespace DeadAir.Player
{
    [DefaultExecutionOrder(100)]
    public sealed class CameraAim : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;
        [SerializeField] private float sensitivity = 1f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 60f;
        [Tooltip("За сколько секунд камера переезжает на другое плечо")]
        [SerializeField] private float shoulderSwapDuration = 0.25f;
        
        private float yaw;
        private float pitch = 10f;
        
        private float targetSide = 1f;

        private void OnEnable()
        {
            input.SwapShoulderPressed += SwapShoulder;
        }

        private void OnDisable()
        {
            input.SwapShoulderPressed -= SwapShoulder;
        }

        private void Start()
        {
            yaw = transform.eulerAngles.y;
            thirdPersonFollow.CameraSide = targetSide;
        }

        private void Update()
        {
            Vector2 mouse = input.Look;
            yaw += mouse.x * sensitivity;
            pitch -= mouse.y * sensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            
            float step = Time.deltaTime / shoulderSwapDuration;
            thirdPersonFollow.CameraSide = Mathf.MoveTowards(thirdPersonFollow.CameraSide, targetSide, step);
        }

        private void SwapShoulder()
        {
            if (targetSide > 0.5f)
                targetSide = 0f;
            else
                targetSide = 1f;
        }
    }
}
