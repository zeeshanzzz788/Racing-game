using UnityEngine;
using UnityEngine.EventSystems;

namespace VelocityRush.Input
{
    public enum MobileInputAction { Accelerate, Brake, Nitro, Handbrake }

    /// <summary>Pointer-safe hold button for the throttle, brake, nitro, or handbrake UI.</summary>
    public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private MobileInputAction action;

        public void OnPointerDown(PointerEventData eventData) => SetAction(true);
        public void OnPointerUp(PointerEventData eventData) => SetAction(false);
        public void OnPointerExit(PointerEventData eventData) => SetAction(false);

        private void OnDisable() => SetAction(false);

        private void SetAction(bool pressed)
        {
            if (InputManager.Instance == null) return;
            switch (action)
            {
                case MobileInputAction.Accelerate: InputManager.Instance.SetAccelerate(pressed); break;
                case MobileInputAction.Brake: InputManager.Instance.SetBrake(pressed); break;
                case MobileInputAction.Nitro: InputManager.Instance.SetNitro(pressed); break;
                case MobileInputAction.Handbrake: InputManager.Instance.SetHandbrake(pressed); break;
            }
        }
    }
}
