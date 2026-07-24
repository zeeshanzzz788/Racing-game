using UnityEngine;
using UnityEngine.EventSystems;

namespace VelocityRush.Input
{
    /// <summary>Attach to the steering-wheel UI image. Drag horizontally from its centre to steer.</summary>
    public class TouchSteeringWheel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField, Range(.1f, 2f)] private float sensitivity = .85f;
        [SerializeField] private RectTransform wheelVisual;
        [SerializeField, Range(10f, 180f)] private float maxVisualRotation = 100f;
        private Vector2 origin;

        public void OnPointerDown(PointerEventData eventData)
        {
            origin = eventData.position;
            UpdateSteering(eventData.position);
        }

        public void OnDrag(PointerEventData eventData) => UpdateSteering(eventData.position);

        public void OnPointerUp(PointerEventData eventData)
        {
            if (InputManager.Instance != null) InputManager.Instance.SetSteering(0f);
            if (wheelVisual != null) wheelVisual.localRotation = Quaternion.identity;
        }

        private void UpdateSteering(Vector2 position)
        {
            float width = Mathf.Max(1f, ((RectTransform)transform).rect.width);
            float value = Mathf.Clamp((position.x - origin.x) / (width * sensitivity), -1f, 1f);
            if (InputManager.Instance != null) InputManager.Instance.SetSteering(value);
            if (wheelVisual != null) wheelVisual.localRotation = Quaternion.Euler(0f, 0f, -value * maxVisualRotation);
        }
    }
}
