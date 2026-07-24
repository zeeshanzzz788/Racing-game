using UnityEngine;
using VelocityRush.Cars;

namespace VelocityRush.AudioSystem
{
    /// <summary>
    /// Lightweight loop-based vehicle audio. Feed it one engine loop, one tire loop and optionally
    /// a nitro loop; pitch/volume track CarController RPM and wheel slip without AudioSource churn.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class CarAudioController : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource engineSource;
        [SerializeField] private AudioSource tireSource;
        [SerializeField] private AudioSource nitroSource;
        [SerializeField] private AudioSource impactSource;

        [Header("Clips")]
        [SerializeField] private AudioClip engineLoop;
        [SerializeField] private AudioClip tireSkidLoop;
        [SerializeField] private AudioClip nitroLoop;
        [SerializeField] private AudioClip impactClip;

        [Header("Mix")]
        [SerializeField, Range(.5f, 2f)] private float idleEnginePitch = .72f;
        [SerializeField, Range(1f, 3f)] private float redlineEnginePitch = 2.05f;
        [SerializeField, Range(0f, 1f)] private float engineIdleVolume = .18f;
        [SerializeField, Range(0f, 1f)] private float engineMaxVolume = .9f;
        [SerializeField, Range(.05f, 1f)] private float skidSlipThreshold = .18f;

        private CarController car;

        private void Awake()
        {
            car = GetComponent<CarController>();
            ConfigureLoop(engineSource, engineLoop);
            ConfigureLoop(tireSource, tireSkidLoop);
            ConfigureLoop(nitroSource, nitroLoop);
            if (car != null) car.Crashed += PlayImpact;
        }

        private void OnDestroy()
        {
            if (car != null) car.Crashed -= PlayImpact;
        }

        private void Update()
        {
            if (car == null || car.Definition == null) return;
            float rpm01 = Mathf.InverseLerp(car.Definition.idleRpm, car.Definition.maxRpm, car.EngineRpm);
            if (engineSource != null)
            {
                engineSource.pitch = Mathf.Lerp(idleEnginePitch, redlineEnginePitch, rpm01);
                engineSource.volume = Mathf.Lerp(engineIdleVolume, engineMaxVolume, rpm01) * (1f - car.DamageNormalized * .18f);
            }

            if (tireSource != null)
            {
                float skid = Mathf.InverseLerp(skidSlipThreshold, .9f, car.NormalizedSlip);
                tireSource.volume = Mathf.SmoothStep(0f, .72f, car.IsDrifting ? Mathf.Max(skid, .4f) : skid);
                tireSource.pitch = Mathf.Lerp(.85f, 1.25f, Mathf.Clamp01(car.CurrentSpeedKph / 150f));
            }

            if (nitroSource != null)
            {
                nitroSource.volume = Mathf.MoveTowards(nitroSource.volume, car.IsNitroActive ? .8f : 0f, Time.deltaTime * 8f);
                nitroSource.pitch = car.IsNitroActive ? 1.12f : .9f;
            }
        }

        private void PlayImpact(float impact)
        {
            if (impactSource == null || impactClip == null) return;
            float volume = Mathf.InverseLerp(4f, 35f, impact);
            impactSource.PlayOneShot(impactClip, volume);
        }

        private static void ConfigureLoop(AudioSource source, AudioClip clip)
        {
            if (source == null || clip == null) return;
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.Play();
        }
    }
}
