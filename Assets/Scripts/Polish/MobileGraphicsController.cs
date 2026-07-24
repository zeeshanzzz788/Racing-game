using UnityEngine;

namespace VelocityRush.Polish
{
    /// <summary>
    /// Conservative dynamic render-scale controller for a 60 FPS target. It changes at most once
    /// per sample window, uses hysteresis, and never replaces device profiling/quality tiers.
    /// </summary>
    public class MobileGraphicsController : MonoBehaviour
    {
        [SerializeField, Range(30, 120)] private int targetFrameRate = 60;
        [SerializeField, Range(.6f, 1f)] private float minimumRenderScale = .78f;
        [SerializeField, Range(.6f, 1.2f)] private float maximumRenderScale = 1f;
        [SerializeField, Range(.02f, .2f)] private float scaleStep = .05f;
        [SerializeField, Min(.5f)] private float sampleSeconds = 2f;
        [SerializeField] private bool adjustOnlyOnMobile = true;

        public float CurrentRenderScale { get; private set; } = 1f;
        public float AverageFrameMs { get; private set; }
        private float elapsed;
        private int frameCount;
        private float frameTimeTotal;

        private void Awake()
        {
            if (adjustOnlyOnMobile && !Application.isMobilePlatform) { enabled = false; return; }
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
            CurrentRenderScale = Mathf.Clamp(maximumRenderScale, minimumRenderScale, maximumRenderScale);
            ApplyScale();
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            frameTimeTotal += Time.unscaledDeltaTime;
            frameCount++;
            if (elapsed < sampleSeconds) return;

            AverageFrameMs = frameCount == 0 ? 0f : frameTimeTotal / frameCount * 1000f;
            float targetMs = 1000f / targetFrameRate;
            // Hysteresis avoids visual scale pumping around the performance target.
            if (AverageFrameMs > targetMs * 1.12f && CurrentRenderScale > minimumRenderScale)
            {
                CurrentRenderScale = Mathf.Max(minimumRenderScale, CurrentRenderScale - scaleStep);
                ApplyScale();
            }
            else if (AverageFrameMs < targetMs * .82f && CurrentRenderScale < maximumRenderScale)
            {
                CurrentRenderScale = Mathf.Min(maximumRenderScale, CurrentRenderScale + scaleStep);
                ApplyScale();
            }
            elapsed = 0f;
            frameCount = 0;
            frameTimeTotal = 0f;
        }

        public void SetRenderScale(float value)
        {
            CurrentRenderScale = Mathf.Clamp(value, minimumRenderScale, maximumRenderScale);
            ApplyScale();
        }

        private void ApplyScale()
        {
            ScalableBufferManager.ResizeBuffers(CurrentRenderScale, CurrentRenderScale);
        }
    }
}
