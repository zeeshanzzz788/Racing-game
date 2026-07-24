using UnityEngine;
using VelocityRush.Cars;

namespace VelocityRush.AudioSystem
{
    [RequireComponent(typeof(PlayerCarController))]
    public class CarAudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource engineSource;
        [SerializeField] private AudioSource tireSource;
        [SerializeField] private AudioClip engineLoop;
        [SerializeField] private AudioClip tireSkidLoop;
        [SerializeField, Range(.5f, 2f)] private float minEnginePitch = .7f;
        [SerializeField, Range(1f, 3f)] private float maxEnginePitch = 2.1f;

        private PlayerCarController car;

        private void Awake()
        {
            car = GetComponent<PlayerCarController>();
            ConfigureLoop(engineSource, engineLoop);
            ConfigureLoop(tireSource, tireSkidLoop);
        }

        private void Update()
        {
            if (car == null || car.Definition == null) return;
            float normalizedSpeed = Mathf.InverseLerp(0f, car.Definition.topSpeedKph, car.CurrentSpeedKph);
            if (engineSource != null)
            {
                engineSource.pitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, normalizedSpeed);
                engineSource.volume = Mathf.Lerp(.2f, 1f, normalizedSpeed);
            }
            if (tireSource != null)
            {
                float skid = Mathf.Abs(car.CurrentSteering) * normalizedSpeed;
                tireSource.volume = Mathf.SmoothStep(0f, .7f, skid);
            }
        }

        private static void ConfigureLoop(AudioSource source, AudioClip clip)
        {
            if (source == null || clip == null) return;
            source.clip = clip;
            source.loop = true;
            source.Play();
        }
    }
}
