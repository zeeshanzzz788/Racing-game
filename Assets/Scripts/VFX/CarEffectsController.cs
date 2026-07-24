using UnityEngine;
using VelocityRush.Cars;

namespace VelocityRush.VFX
{
    [RequireComponent(typeof(PlayerCarController))]
    public class CarEffectsController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem dust;
        [SerializeField] private ParticleSystem sparks;
        [SerializeField] private ParticleSystem nitro;
        private PlayerCarController car;

        private void Awake() => car = GetComponent<PlayerCarController>();

        private void Update()
        {
            if (car == null || car.Definition == null) return;
            bool moving = car.CurrentSpeedKph > 15f;
            SetEmission(dust, car.IsGrounded && moving && Mathf.Abs(car.CurrentSteering) > .25f);
            SetEmission(nitro, moving && car.NitroNormalized > 0f && car.CurrentThrottle > .8f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > 8f && sparks != null) sparks.Play(true);
        }

        private static void SetEmission(ParticleSystem system, bool enabled)
        {
            if (system == null) return;
            if (enabled && !system.isPlaying) system.Play(true);
            else if (!enabled && system.isPlaying) system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
