using UnityEngine;

namespace VelocityRush.Polish
{
    /// <summary>Low-cost time-of-day and optional rain controller: one sun, ambient/fog changes,
    /// baked reflection probes, and one capped rain system. It updates at a coarse interval.</summary>
    public class TimeOfDayWeatherController : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private Material skyboxMaterial;
        [SerializeField, Range(0f, 24f)] private float timeOfDay = 13f;
        [SerializeField, Min(1f)] private float dayLengthSeconds = 420f;
        [SerializeField] private bool animateTime;
        [SerializeField] private Gradient sunColor = new Gradient();
        [SerializeField] private AnimationCurve sunIntensity = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private Gradient ambientColor = new Gradient();
        [SerializeField] private Color clearFogColor = new Color(.55f, .67f, .8f);
        [SerializeField] private Color rainFogColor = new Color(.32f, .38f, .46f);
        [SerializeField, Range(0f, .03f)] private float clearFogDensity = .002f;
        [SerializeField, Range(0f, .03f)] private float rainFogDensity = .008f;
        [SerializeField] private ParticleSystem rain;
        [SerializeField] private AudioSource rainAudio;
        [SerializeField, Range(.1f, 2f)] private float updateInterval = .2f;

        public bool IsRaining { get; private set; }
        private float nextUpdate;

        private void Start()
        {
            if (sun == null) sun = RenderSettings.sun;
            if (skyboxMaterial != null) RenderSettings.skybox = skyboxMaterial;
            ApplyEnvironment();
        }

        private void Update()
        {
            if (animateTime) timeOfDay = Mathf.Repeat(timeOfDay + 24f / dayLengthSeconds * Time.deltaTime, 24f);
            if (Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + updateInterval;
            ApplyEnvironment();
        }

        public void SetRain(bool enabled)
        {
            IsRaining = enabled;
            if (rain != null)
            {
                if (enabled && !rain.isPlaying) rain.Play(true);
                else if (!enabled && rain.isPlaying) rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (rainAudio != null)
            {
                if (enabled && !rainAudio.isPlaying) rainAudio.Play();
                rainAudio.volume = enabled ? .45f : 0f;
            }
            ApplyEnvironment();
        }

        public void SetTimeOfDay(float value)
        {
            timeOfDay = Mathf.Repeat(value, 24f);
            ApplyEnvironment();
        }

        private void ApplyEnvironment()
        {
            float day01 = timeOfDay / 24f;
            float sunlight = Mathf.Clamp01(Mathf.Sin((day01 - .25f) * Mathf.PI * 2f) * .5f + .5f);
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(-8f, 188f, day01), -35f, 0f);
                sun.color = sunColor.Evaluate(day01);
                sun.intensity = sunIntensity.Evaluate(sunlight) * (IsRaining ? .65f : 1f);
            }
            RenderSettings.ambientLight = ambientColor.Evaluate(day01) * (IsRaining ? .65f : 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = IsRaining ? rainFogColor : clearFogColor;
            RenderSettings.fogDensity = IsRaining ? rainFogDensity : clearFogDensity;
            if (skyboxMaterial != null && skyboxMaterial.HasProperty("_Exposure"))
                skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(.35f, 1.2f, sunlight));
        }
    }
}
