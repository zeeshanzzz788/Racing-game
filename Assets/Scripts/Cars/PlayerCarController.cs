using System;
using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Input;

namespace VelocityRush.Cars
{
    /// <summary>
    /// WheelCollider-driven four-wheel car controller tuned for responsive mobile arcade racing.
    /// Use a 1 Unity unit = 1 meter vehicle hierarchy; wheel colliders must be children of this root.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerCarController : MonoBehaviour
    {
        [Header("Setup (auto-discovered if left empty)")]
        [SerializeField] private Rigidbody body;
        [SerializeField] private WheelCollider[] wheelColliders;
        [SerializeField] private Transform centerOfMass;
        [SerializeField] private Transform visualBody;

        [Header("Driving")]
        [SerializeField, Range(0f, 2f)] private float downforce = .55f;
        [SerializeField, Range(.1f, 2f)] private float reverseTorqueMultiplier = .55f;
        [SerializeField] private float crashImpactThreshold = 16f;
        [SerializeField] private bool allWheelDrive = true;

        public CarDefinition Definition { get; private set; }
        public bool IsPlayer { get; private set; }
        public bool InputEnabled { get; private set; }
        public float CurrentSpeedKph { get; private set; }
        public float NitroNormalized => Definition == null || Definition.nitroCapacity <= 0f ? 0f : nitroRemaining / Definition.nitroCapacity;
        public bool IsGrounded { get; private set; }
        public float CurrentThrottle { get; private set; }
        public float CurrentSteering { get; private set; }
        public event Action<float> Crashed;

        private float nitroRemaining;
        private float aiSteering;
        private float aiThrottle;
        private float aiBrake;
        private bool aiNitro;
        private bool aiHandbrake;
        private bool crashConsumed;
        private float difficultyMultiplier = 1f;

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (wheelColliders == null || wheelColliders.Length == 0)
                wheelColliders = GetComponentsInChildren<WheelCollider>(true);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (centerOfMass != null) body.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);
        }

        public void Initialize(CarDefinition definition, bool isPlayer)
        {
            Definition = definition;
            IsPlayer = isPlayer;
            InputEnabled = true;
            crashConsumed = false;
            body.mass = definition.mass;
            nitroRemaining = definition.nitroCapacity;
            gameObject.tag = isPlayer ? "Player" : "AI";
            ApplyBodyColor(definition.bodyColor);
            ConfigureWheelGrip();
        }

        public void SetInputEnabled(bool enabled)
        {
            InputEnabled = enabled;
            if (!enabled) SetExternalInput(0f, 0f, 1f, false, false);
        }

        public void SetExternalInput(float steering, float throttle, float brake, bool nitro, bool handbrake)
        {
            aiSteering = Mathf.Clamp(steering, -1f, 1f);
            aiThrottle = Mathf.Clamp01(throttle);
            aiBrake = Mathf.Clamp01(brake);
            aiNitro = nitro;
            aiHandbrake = handbrake;
        }

        public void AddNitro(float seconds)
        {
            if (Definition == null) return;
            nitroRemaining = Mathf.Clamp(nitroRemaining + seconds, 0f, Definition.nitroCapacity);
        }

        /// <summary>Used by Endless mode; 1 is normal and values above 1 raise pace gradually.</summary>
        public void SetDifficultyMultiplier(float value)
        {
            difficultyMultiplier = Mathf.Clamp(value, 1f, 1.6f);
        }

        private void FixedUpdate()
        {
            if (Definition == null || wheelColliders.Length == 0) return;

            ReadInput(out float steering, out float throttle, out float brake, out bool nitro, out bool handbrake);
            CurrentSteering = steering;
            CurrentThrottle = throttle;
            CurrentSpeedKph = body.velocity.magnitude * 3.6f;
            IsGrounded = AnyWheelGrounded();

            ApplySteering(steering);
            ApplyDrive(throttle, brake, nitro, handbrake);
            body.AddForce(-transform.up * body.velocity.magnitude * downforce, ForceMode.Force);
            KeepCarStable();
            UpdateWheelVisuals();
        }

        private void ReadInput(out float steering, out float throttle, out float brake, out bool nitro, out bool handbrake)
        {
            steering = throttle = brake = 0f;
            nitro = handbrake = false;
            if (!InputEnabled) return;

            if (IsPlayer && InputManager.Instance != null)
            {
                steering = InputManager.Instance.Steering;
                throttle = InputManager.Instance.Throttle;
                brake = InputManager.Instance.Brake;
                nitro = InputManager.Instance.NitroHeld;
                handbrake = InputManager.Instance.HandbrakeHeld;
            }
            else if (!IsPlayer)
            {
                steering = aiSteering;
                throttle = aiThrottle;
                brake = aiBrake;
                nitro = aiNitro;
                handbrake = aiHandbrake;
            }
        }

        private void ApplySteering(float input)
        {
            float speedFactor = Mathf.Lerp(1f, .35f, Mathf.InverseLerp(30f, Definition.topSpeedKph * difficultyMultiplier, CurrentSpeedKph));
            float steer = input * Definition.steeringAngle * Definition.handling * speedFactor;
            foreach (WheelCollider wheel in wheelColliders)
                if (IsFrontWheel(wheel)) wheel.steerAngle = steer;
        }

        private void ApplyDrive(float throttle, float brake, bool wantsNitro, bool handbrake)
        {
            bool atForwardLimit = CurrentSpeedKph >= Definition.topSpeedKph * difficultyMultiplier;
            float motor = atForwardLimit ? 0f : throttle * Definition.motorTorque * difficultyMultiplier;
            bool useNitro = wantsNitro && nitroRemaining > 0f && throttle > .1f && CurrentSpeedKph > 20f;
            if (useNitro)
            {
                nitroRemaining = Mathf.Max(0f, nitroRemaining - Time.fixedDeltaTime);
                body.AddForce(transform.forward * Definition.nitroForce, ForceMode.Force);
            }

            foreach (WheelCollider wheel in wheelColliders)
            {
                bool driven = allWheelDrive || !IsFrontWheel(wheel);
                wheel.motorTorque = driven ? motor : 0f;
                wheel.brakeTorque = brake > .01f ? Definition.brakeTorque * brake : 0f;

                if (handbrake && !IsFrontWheel(wheel))
                    wheel.brakeTorque = Definition.brakeTorque * .8f;

                WheelFrictionCurve sideways = wheel.sidewaysFriction;
                sideways.stiffness = Definition.grip * (handbrake && !IsFrontWheel(wheel) ? .55f : 1f);
                wheel.sidewaysFriction = sideways;
            }

            // Let a stopped car reverse only after the brake is released; a separate reverse control
            // can call SetExternalInput with a negative throttle when the project needs it.
            if (throttle < -.01f && CurrentSpeedKph < 12f)
            {
                foreach (WheelCollider wheel in wheelColliders)
                    if (allWheelDrive || !IsFrontWheel(wheel)) wheel.motorTorque = throttle * Definition.motorTorque * reverseTorqueMultiplier;
            }
        }

        private void KeepCarStable()
        {
            // Mild lateral velocity damping removes the unstable "ice skating" feeling while retaining drifts.
            Vector3 localVelocity = transform.InverseTransformDirection(body.velocity);
            localVelocity.x *= Mathf.Lerp(.96f, .995f, Definition.handling);
            body.velocity = transform.TransformDirection(localVelocity);
        }

        private void ConfigureWheelGrip()
        {
            foreach (WheelCollider wheel in wheelColliders)
            {
                WheelFrictionCurve forward = wheel.forwardFriction;
                forward.stiffness = Definition.grip;
                wheel.forwardFriction = forward;
                WheelFrictionCurve sideways = wheel.sidewaysFriction;
                sideways.stiffness = Definition.grip;
                wheel.sidewaysFriction = sideways;
            }
        }

        private bool AnyWheelGrounded()
        {
            foreach (WheelCollider wheel in wheelColliders)
                if (wheel.isGrounded) return true;
            return false;
        }

        private bool IsFrontWheel(WheelCollider wheel) => transform.InverseTransformPoint(wheel.transform.position).z > 0f;

        private void UpdateWheelVisuals()
        {
            foreach (WheelCollider wheel in wheelColliders)
            {
                Transform visual = wheel.transform.Find("Visual");
                if (visual == null) continue;
                wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
                visual.SetPositionAndRotation(position, rotation);
            }
        }

        private void ApplyBodyColor(Color color)
        {
            Transform target = visualBody == null ? transform : visualBody;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterial == null) continue;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color); // URP/Lit
                block.SetColor("_Color", color);     // Built-in / simple shaders
                renderer.SetPropertyBlock(block);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            float impact = collision.relativeVelocity.magnitude;
            if (!IsPlayer || crashConsumed || impact < crashImpactThreshold) return;
            crashConsumed = true;
            Crashed?.Invoke(impact);
            VelocityRush.Endless.EndlessRunManager endless = FindObjectOfType<VelocityRush.Endless.EndlessRunManager>();
            if (endless != null) endless.Crash();
            else
            {
                VelocityRush.Race.RaceManager race = FindObjectOfType<VelocityRush.Race.RaceManager>();
                if (race != null) race.Finish(RaceResult.Crashed);
                else if (GameManager.Instance != null) GameManager.Instance.EndRace(RaceResult.Crashed, 0f);
            }
        }
    }
}
