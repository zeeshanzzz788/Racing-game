using UnityEngine;
using VelocityRush.Cars;
using VelocityRush.Core;

namespace VelocityRush.Polish
{
    /// <summary>Post-follow micro shake and speed feedback; place on the race camera after
    /// RaceCameraController. It avoids Cinemachine and works with the existing camera rig.</summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    public class CameraPolishController : MonoBehaviour
    {
        [SerializeField, Range(0f, .35f)] private float maxSpeedShake = .045f;
        [SerializeField, Range(0f, .8f)] private float crashShake = .35f;
        [SerializeField, Range(.1f, 2f)] private float shakeDecay = .65f;
        private CarController player;
        private float shakeAmount;

        private void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerSpawned += SetPlayer;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerSpawned -= SetPlayer;
            if (player != null) player.Crashed -= OnCrash;
        }

        private void LateUpdate()
        {
            if (player == null && GameManager.Instance != null) SetPlayer(GameManager.Instance.PlayerCar);
            if (player == null) return;
            float speedShake = Mathf.InverseLerp(100f, player.Definition.topSpeedKph, player.CurrentSpeedKph) * maxSpeedShake;
            float amount = speedShake + shakeAmount;
            transform.position += Random.insideUnitSphere * amount;
            shakeAmount = Mathf.MoveTowards(shakeAmount, 0f, Time.unscaledDeltaTime / shakeDecay);
        }

        public void AddShake(float amount) => shakeAmount = Mathf.Max(shakeAmount, amount);

        private void SetPlayer(CarController controller)
        {
            if (player != null) player.Crashed -= OnCrash;
            player = controller;
            if (player != null) player.Crashed += OnCrash;
        }

        private void OnCrash(float impact) => AddShake(Mathf.Lerp(.08f, crashShake, Mathf.InverseLerp(8f, 35f, impact)));
    }
}
