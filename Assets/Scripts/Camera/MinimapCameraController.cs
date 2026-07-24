using UnityEngine;
using VelocityRush.Cars;
using VelocityRush.Core;

namespace VelocityRush.CameraSystem
{
    /// <summary>Top-down minimap camera. Assign a 256/512 RenderTexture and keep its culling mask lean.</summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class MinimapCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float height = 72f;
        [SerializeField] private bool rotateWithPlayer;
        private CarController car;

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
            transform.position = target.position + Vector3.up * height;
            transform.rotation = rotateWithPlayer
                ? Quaternion.Euler(90f, target.eulerAngles.y, 0f)
                : Quaternion.Euler(90f, 0f, 0f);
        }

        public void SetTarget(CarController controller)
        {
            car = controller;
            target = controller == null ? null : controller.transform;
        }
    }
}
