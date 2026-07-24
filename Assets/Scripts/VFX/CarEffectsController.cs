using UnityEngine;
using VelocityRush.Cars;

namespace VelocityRush.VFX
{
    /// <summary>Optional pooled particle hooks for dust, impact sparks and nitro exhaust.</summary>
    [RequireComponent(typeof(CarController))]
    public class CarEffectsController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem dust;
        [SerializeField] private ParticleSystem sparks;
        [SerializeField] private ParticleSystem nitro;
        [SerializeField, Min(4f)] private float sparkImpactThreshold = 8f;
        private CarController car;

        private void Awake() => car = GetComponent<CarController>();

        private void Update()
        {
            if (car == null || car.Definition == null) return;
            bool moving = car.CurrentSpeedKph > 15f;
            SetEmission(dust, car.IsGrounded && moving && car.IsDrifting);
            SetEmission(nitro, car.IsNitroActive);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > sparkImpactThreshold && sparks != null) sparks.Play(true);
        }

        private static void SetEmission(ParticleSystem system, bool enabled)
        {
            if (system == null) return;
            if (enabled && !system.isPlaying) system.Play(true);
            else if (!enabled && system.isPlaying) system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
