using UnityEngine;

namespace VelocityRush.Input
{
    public enum SteeringMode { Wheel, Tilt }

    /// <summary>
    /// Input abstraction for both physical controller/keyboard testing and mobile UI controls.
    /// Keep project Player > Active Input Handling set to Both if using the legacy editor axes.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Mobile")]
        [SerializeField] private SteeringMode steeringMode = SteeringMode.Wheel;
        [SerializeField, Range(.25f, 3f)] private float tiltSensitivity = 1.25f;
        [SerializeField, Range(.01f, .5f)] private float steeringReturnSpeed = .14f;
        [SerializeField] private bool useKeyboardInEditor = true;

        public float Steering { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public bool NitroHeld { get; private set; }
        public bool HandbrakeHeld { get; private set; }
        public bool IsUsingTilt => steeringMode == SteeringMode.Tilt;

        private float wheelSteering;
        private bool acceleratePressed;
        private bool brakePressed;
        private bool nitroPressed;
        private bool handbrakePressed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (steeringMode == SteeringMode.Tilt && Application.isMobilePlatform)
                Steering = Mathf.Clamp(UnityEngine.Input.acceleration.x * tiltSensitivity, -1f, 1f);
            else
                Steering = Mathf.MoveTowards(Steering, wheelSteering, Time.unscaledDeltaTime / steeringReturnSpeed);

            Throttle = acceleratePressed ? 1f : 0f;
            Brake = brakePressed ? 1f : 0f;
            NitroHeld = nitroPressed;
            HandbrakeHeld = handbrakePressed;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (useKeyboardInEditor)
            {
                Steering = Mathf.Clamp(wheelSteering + UnityEngine.Input.GetAxisRaw("Horizontal"), -1f, 1f);
                Throttle = Mathf.Max(Throttle, Mathf.Clamp01(UnityEngine.Input.GetAxisRaw("Vertical")));
                Brake = Mathf.Max(Brake, Mathf.Clamp01(-UnityEngine.Input.GetAxisRaw("Vertical")));
                NitroHeld |= UnityEngine.Input.GetKey(KeyCode.LeftShift);
                HandbrakeHeld |= UnityEngine.Input.GetKey(KeyCode.Space);
            }
#endif
        }

        public void SetSteering(float normalizedSteering)
        {
            if (steeringMode != SteeringMode.Wheel) return;
            wheelSteering = Mathf.Clamp(normalizedSteering, -1f, 1f);
        }

        public void SetSteeringMode(int mode)
        {
            steeringMode = (SteeringMode)Mathf.Clamp(mode, 0, 1);
            wheelSteering = 0f;
        }

        public void SetAccelerate(bool pressed) => acceleratePressed = pressed;
        public void SetBrake(bool pressed) => brakePressed = pressed;
        public void SetNitro(bool pressed) => nitroPressed = pressed;
        public void SetHandbrake(bool pressed) => handbrakePressed = pressed;

        public void ClearMobileInput()
        {
            wheelSteering = 0f;
            acceleratePressed = false;
            brakePressed = false;
            nitroPressed = false;
            handbrakePressed = false;
            NitroHeld = false;
            HandbrakeHeld = false;
        }
    }
}
