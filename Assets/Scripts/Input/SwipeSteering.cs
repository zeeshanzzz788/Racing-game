using UnityEngine;
using UnityEngine.EventSystems;

namespace VelocityRush.Input
{
    /// <summary>
    /// Optional alternative to TouchSteeringWheel. Place on a transparent UI panel that covers a
    /// safe steering zone (not the pedals); horizontal drag controls steering until release.
    /// </summary>
    public class SwipeSteering : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField, Min(40f)] private float pixelsForFullSteer = 220f;
        [SerializeField, Range(.2f, 2f)] private float sensitivity = 1f;
        private Vector2 dragOrigin;

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragOrigin = eventData.position;
            SetSteering(eventData.position);
        }

        public void OnDrag(PointerEventData eventData) => SetSteering(eventData.position);

        public void OnEndDrag(PointerEventData eventData)
        {
            if (InputManager.Instance != null) InputManager.Instance.SetSteering(0f);
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null) InputManager.Instance.SetSteering(0f);
        }

        private void SetSteering(Vector2 screenPosition)
        {
            float value = (screenPosition.x - dragOrigin.x) / pixelsForFullSteer * sensitivity;
            if (InputManager.Instance != null) InputManager.Instance.SetSteering(Mathf.Clamp(value, -1f, 1f));
        }
    }
}
