using UnityEngine;

namespace VelocityRush.Polish
{
    /// <summary>Dependency-free CanvasGroup fade/scale animator. It is a safe fallback when
    /// DOTween/LeanTween is not imported; UI buttons may call Show, Hide or Toggle directly.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasPanelAnimator : MonoBehaviour
    {
        [SerializeField, Min(.01f)] private float duration = .18f;
        [SerializeField] private Vector3 hiddenScale = new Vector3(.94f, .94f, 1f);
        [SerializeField] private bool visibleOnStart;
        private CanvasGroup group;
        private RectTransform rect;
        private bool targetVisible;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rect = GetComponent<RectTransform>();
            SetImmediate(visibleOnStart);
        }

        private void Update()
        {
            float target = targetVisible ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / duration);
            if (rect != null) rect.localScale = Vector3.Lerp(hiddenScale, Vector3.one, group.alpha);
            group.blocksRaycasts = group.alpha > .98f;
            group.interactable = group.blocksRaycasts;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            targetVisible = true;
        }

        public void Hide() => targetVisible = false;
        public void Toggle() { if (targetVisible) Hide(); else Show(); }

        public void SetImmediate(bool visible)
        {
            targetVisible = visible;
            group.alpha = visible ? 1f : 0f;
            if (rect != null) rect.localScale = visible ? Vector3.one : hiddenScale;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }
    }
}
