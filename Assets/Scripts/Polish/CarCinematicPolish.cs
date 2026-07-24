using UnityEngine;
using VelocityRush.Cars;

namespace VelocityRush.Polish
{
    /// <summary>Optional jump-landing and near-miss polish. Uses a fixed collider buffer and a
    /// cooldown, so it is safe to run on the player only.</summary>
    [RequireComponent(typeof(CarController))]
    public class CarCinematicPolish : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float minimumAirTimeForSlowMo = .55f;
        [SerializeField, Min(.5f)] private float minimumJumpHeight = 1.5f;
        [SerializeField, Range(.1f, .8f)] private float jumpSlowMoScale = .45f;
        [SerializeField, Range(.05f, 2f)] private float jumpSlowMoSeconds = .42f;
        [SerializeField] private LayerMask nearMissMask = ~0;
        [SerializeField, Range(1f, 8f)] private float nearMissRadius = 2.2f;
        [SerializeField, Range(10f, 120f)] private float minimumNearMissSpeedKph = 55f;
        [SerializeField, Range(.1f, .8f)] private float nearMissSlowMoScale = .62f;
        [SerializeField, Range(.05f, 1f)] private float nearMissSlowMoSeconds = .18f;
        [SerializeField, Min(.2f)] private float eventCooldown = 2f;

        private readonly Collider[] nearMissBuffer = new Collider[8];
        private CarController car;
        private bool wasGrounded;
        private float airStartTime;
        private float airStartHeight;
        private float peakHeight;
        private float nextNearMissCheck;
        private float nextCinematicTime;

        private void Awake() => car = GetComponent<CarController>();

        private void FixedUpdate()
        {
            if (car == null || !car.IsPlayer || car.IsWrecked) return;
            UpdateJumpState();
            if (Time.unscaledTime >= nextNearMissCheck)
            {
                nextNearMissCheck = Time.unscaledTime + .12f;
                CheckNearMiss();
            }
        }

        private void UpdateJumpState()
        {
            if (!car.IsGrounded && wasGrounded)
            {
                airStartTime = Time.unscaledTime;
                airStartHeight = transform.position.y;
                peakHeight = airStartHeight;
            }
            if (!car.IsGrounded) peakHeight = Mathf.Max(peakHeight, transform.position.y);
            if (car.IsGrounded && !wasGrounded)
            {
                float airTime = Time.unscaledTime - airStartTime;
                if (airTime >= minimumAirTimeForSlowMo && peakHeight - airStartHeight >= minimumJumpHeight)
                    TriggerSlowMo(jumpSlowMoScale, jumpSlowMoSeconds);
            }
            wasGrounded = car.IsGrounded;
        }

        private void CheckNearMiss()
        {
            if (car.CurrentSpeedKph < minimumNearMissSpeedKph || Time.unscaledTime < nextCinematicTime) return;
            int count = Physics.OverlapSphereNonAlloc(transform.position + transform.forward * 1.4f, nearMissRadius,
                nearMissBuffer, nearMissMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider candidate = nearMissBuffer[i];
                if (candidate == null) continue;
                CarController other = candidate.GetComponentInParent<CarController>();
                if (other != null && other != car && Vector3.Dot(transform.forward, other.transform.position - transform.position) < .15f)
                {
                    TriggerSlowMo(nearMissSlowMoScale, nearMissSlowMoSeconds);
                    break;
                }
            }
        }

        private void TriggerSlowMo(float scale, float seconds)
        {
            if (Time.unscaledTime < nextCinematicTime) return;
            nextCinematicTime = Time.unscaledTime + eventCooldown;
            if (CinematicTimeController.Instance != null) CinematicTimeController.Instance.PlaySlowMotion(scale, seconds);
        }
    }
}
