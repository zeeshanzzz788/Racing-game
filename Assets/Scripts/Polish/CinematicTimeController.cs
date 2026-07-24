using UnityEngine;

namespace VelocityRush.Polish
{
    /// <summary>Central, unscaled-time slow-motion controller. Keeping one owner prevents stacked
    /// timeScale bugs from jumps, near misses and crashes.</summary>
    public class CinematicTimeController : MonoBehaviour
    {
        public static CinematicTimeController Instance { get; private set; }
        private float originalFixedDeltaTime;
        private float restoreAtUnscaledTime;
        private float activeScale = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            originalFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void Update()
        {
            if (activeScale < 1f && Time.unscaledTime >= restoreAtUnscaledTime) Restore();
        }

        public void PlaySlowMotion(float scale, float realSeconds)
        {
            scale = Mathf.Clamp(scale, .05f, 1f);
            if (scale >= activeScale && activeScale < 1f)
            {
                restoreAtUnscaledTime = Mathf.Max(restoreAtUnscaledTime, Time.unscaledTime + realSeconds);
                return;
            }
            activeScale = scale;
            restoreAtUnscaledTime = Time.unscaledTime + Mathf.Max(.01f, realSeconds);
            Time.timeScale = activeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime * activeScale;
        }

        public void Restore()
        {
            activeScale = 1f;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
    }
}
