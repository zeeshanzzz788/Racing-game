using UnityEngine;
using VelocityRush.Cars;
using VelocityRush.Core;

namespace VelocityRush.CameraSystem
{
    public enum RaceCameraMode { Chase, Cinematic }

    /// <summary>Responsive camera without Cinemachine, keeping mobile dependency and CPU cost low.</summary>
    public class RaceCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private RaceCameraMode mode = RaceCameraMode.Chase;
        [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 4.2f, -7.5f);
        [SerializeField] private Vector3 cinematicOffset = new Vector3(2.4f, 2.2f, -5f);
        [SerializeField] private float followSmoothTime = .16f;
        [SerializeField] private float rotationSharpness = 9f;
        [SerializeField] private float baseFov = 62f;
        [SerializeField] private float maxFov = 74f;

        private Vector3 velocity;
        private UnityEngine.Camera cachedCamera;
        private PlayerCarController car;

        private void Awake() => cachedCamera = GetComponent<UnityEngine.Camera>();

        private void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerSpawned += SetTarget;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerSpawned -= SetTarget;
        }

        private void LateUpdate()
        {
            if (target == null && GameManager.Instance != null && GameManager.Instance.PlayerCar != null)
                SetTarget(GameManager.Instance.PlayerCar);
            if (target == null) return;

            Vector3 offset = mode == RaceCameraMode.Chase ? chaseOffset : cinematicOffset;
            Vector3 desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, followSmoothTime);
            Vector3 lookPoint = target.position + target.forward * (mode == RaceCameraMode.Chase ? 9f : 5f) + Vector3.up * 1.1f;
            Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
            if (cachedCamera != null && car != null)
                cachedCamera.fieldOfView = Mathf.Lerp(baseFov, maxFov, Mathf.InverseLerp(0f, car.Definition.topSpeedKph, car.CurrentSpeedKph));
        }

        public void SetTarget(PlayerCarController controller)
        {
            car = controller;
            target = controller == null ? null : controller.transform;
        }

        public void ToggleMode()
        {
            mode = mode == RaceCameraMode.Chase ? RaceCameraMode.Cinematic : RaceCameraMode.Chase;
        }
    }
}
