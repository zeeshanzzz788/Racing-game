using System;
using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Input;
using VelocityRush.Progression;

namespace VelocityRush.Cars
{
    /// <summary>
    /// A WheelCollider-based controller tuned for a convincing-but-forgiving mobile racer.
    ///
    /// Attach this to the car root (scale 1,1,1), place four WheelColliders below it, and either
    /// fill Wheels in the Inspector or let it discover them. Front wheels are determined from their
    /// local Z position. Feed touch/gyro input through InputManager, or AI input through
    /// SetExternalInput. No per-frame allocations, raycasts, or Find calls occur while driving.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Serializable]
        public class WheelSetup
        {
            public WheelCollider collider;
            [Tooltip("Mesh transform moved from WheelCollider.GetWorldPose. Defaults to child named Visual.")]
            public Transform visual;
            [Tooltip("Optional local dust/skid system. The controller only plays/stops it.")]
            public ParticleSystem dust;
            [Tooltip("Optional local skid trail. Emission is enabled from wheel slip.")]
            public TrailRenderer skidTrail;
            [Tooltip("Assigned automatically from the preset when Auto Assign Wheel Roles is enabled.")]
            public bool steer;
            public bool drive;
            public bool handbrake;
        }

        [Header("Setup")]
        [SerializeField] private Rigidbody body;
        [SerializeField] private WheelSetup[] wheels;
        [SerializeField] private Transform centerOfMass;
        [SerializeField] private Transform visualBody;
        [SerializeField] private Renderer[] damageRenderers;
        [SerializeField] private bool autoAssignWheelRoles = true;
        [SerializeField] private bool initializeFromAssignedPresetOnAwake;
        [SerializeField] private CarDefinition assignedPreset;

        [Header("Arcade Stability")]
        [Tooltip("Force applied at speed to keep the chassis planted. Tune with the preset top speed.")]
        [SerializeField, Range(0f, 3f)] private float downforce = .65f;
        [SerializeField, Range(0f, 20000f)] private float antiRollForce = 7000f;
        [SerializeField, Range(0f, 20f)] private float normalLateralDamping = 5.5f;
        [SerializeField, Range(0f, 20f)] private float driftLateralDamping = 1.2f;
        [SerializeField, Range(0f, 10f)] private float uprightStabilizer = 2.4f;
        [SerializeField, Range(5f, 100f)] private float driftMinimumKph = 28f;
        [SerializeField, Range(.1f, 2f)] private float reverseTorqueMultiplier = .55f;
        [SerializeField] private bool tractionControl = true;
        [SerializeField] private bool abs = true;

        [Header("Damage")]
        [SerializeField, Min(1f)] private float maxDamage = 100f;
        [SerializeField, Min(1f)] private float damageImpactThreshold = 5f;
        [SerializeField, Min(1f)] private float wreckImpactThreshold = 34f;
        [SerializeField, Range(.1f, 10f)] private float damagePerImpactUnit = 2.2f;
        [SerializeField, Range(0f, .5f)] private float visualDamageDarkening = .18f;
        [SerializeField] private Color damageTint = new Color(.12f, .08f, .06f);
        [SerializeField] private ParticleSystem damageSmoke;

        public CarDefinition Definition { get; private set; }
        public bool IsPlayer { get; private set; }
        public bool InputEnabled { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsDrifting { get; private set; }
        public bool IsNitroActive { get; private set; }
        public bool IsWrecked { get; private set; }
        public float CurrentSpeedKph { get; private set; }
        public float ForwardSpeedKph { get; private set; }
        public float CurrentThrottle { get; private set; }
        public float CurrentSteering { get; private set; }
        public float EngineRpm { get; private set; }
        public float NormalizedSlip { get; private set; }
        public float Damage { get; private set; }
        public float DamageNormalized => maxDamage <= 0f ? 0f : Damage / maxDamage;
        public float PerformanceMultiplier => Definition == null ? 1f : Mathf.Clamp01(1f - DamageNormalized * Definition.maxPerformanceLossAtFullDamage);
        public float NitroNormalized => Definition == null || Definition.nitroCapacity <= 0f ? 0f : nitroRemaining / (Definition.nitroCapacity * nitroUpgradeMultiplier);
        public float NitroCooldownRemaining => nitroCooldownRemaining;
        public Rigidbody Body => body;

        public event Action<float> Crashed;
        public event Action<float> DamageChanged;
        public event Action<bool> NitroStateChanged;

        private WheelFrictionCurve[] baseForwardFriction;
        private WheelFrictionCurve[] baseSidewaysFriction;
        private Renderer[] cachedDamageRenderers;
        private Color[] baseRendererColors;
        private MaterialPropertyBlock propertyBlock;
        private WheelSetup frontLeft;
        private WheelSetup frontRight;
        private WheelSetup rearLeft;
        private WheelSetup rearRight;
        private float nitroRemaining;
        private float nitroCooldownRemaining;
        private float externalSteering;
        private float externalThrottle;
        private float externalBrake;
        private bool externalNitro;
        private bool externalHandbrake;
        private float lastDamageTime;
        private float difficultyMultiplier = 1f;
        private float engineUpgradeMultiplier = 1f;
        private float handlingUpgradeMultiplier = 1f;
        private float nitroUpgradeMultiplier = 1f;
        private int drivenWheelCount;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int DamageAmountId = Shader.PropertyToID("_DamageAmount");

        protected virtual void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 12f;
            if (centerOfMass != null) body.centerOfMass = transform.InverseTransformPoint(centerOfMass.position);

            DiscoverWheelsIfRequired();
            CacheDamageRenderers();
            if (initializeFromAssignedPresetOnAwake && assignedPreset != null)
                Initialize(assignedPreset, CompareTag("Player"));
        }

        /// <summary>Called by GameManager after a selected car prefab is spawned.</summary>
        public void Initialize(CarDefinition definition, bool isPlayer)
        {
            Definition = definition;
            assignedPreset = definition;
            IsPlayer = isPlayer;
            InputEnabled = true;
            IsWrecked = false;
            Damage = 0f;
            nitroRemaining = definition == null ? 0f : definition.nitroCapacity;
            nitroCooldownRemaining = 0f;
            difficultyMultiplier = 1f;
            gameObject.tag = isPlayer ? "Player" : "AI";

            if (definition == null) return;
            body.mass = definition.mass;
            if (isPlayer && ProgressionService.Instance != null)
            {
                engineUpgradeMultiplier = ProgressionService.Instance.GetUpgradeMultiplier(definition, CarUpgradeType.Engine);
                handlingUpgradeMultiplier = ProgressionService.Instance.GetUpgradeMultiplier(definition, CarUpgradeType.Handling);
                nitroUpgradeMultiplier = ProgressionService.Instance.GetUpgradeMultiplier(definition, CarUpgradeType.Nitro);
            }
            else
            {
                engineUpgradeMultiplier = handlingUpgradeMultiplier = nitroUpgradeMultiplier = 1f;
            }
            nitroRemaining = definition.nitroCapacity * nitroUpgradeMultiplier;
            ConfigureWheelsFromPreset();
            ApplyPaintAndDamage();
            if (damageSmoke != null) damageSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>Used by simple AI and tests. Throttle accepts -1 (reverse) to +1 (drive).</summary>
        public void SetExternalInput(float steering, float throttle, float brake, bool nitro, bool handbrake)
        {
            externalSteering = Mathf.Clamp(steering, -1f, 1f);
            externalThrottle = Mathf.Clamp(throttle, -1f, 1f);
            externalBrake = Mathf.Clamp01(brake);
            externalNitro = nitro;
            externalHandbrake = handbrake;
        }

        public void SetInputEnabled(bool enabled)
        {
            InputEnabled = enabled;
            if (!enabled) SetExternalInput(0f, 0f, 1f, false, false);
        }

        /// <summary>Adds seconds of nitro. Pickups may call this safely even while cooldown is active.</summary>
        public void AddNitro(float seconds)
        {
            if (Definition == null || seconds <= 0f) return;
            nitroRemaining = Mathf.Clamp(nitroRemaining + seconds, 0f, Definition.nitroCapacity * nitroUpgradeMultiplier);
        }

        /// <summary>Endless mode uses this to increase pace without changing the ScriptableObject asset.</summary>
        public void SetDifficultyMultiplier(float value)
        {
            difficultyMultiplier = Mathf.Clamp(value, 1f, 1.6f);
        }

        public void Repair(float amount)
        {
            if (amount <= 0f || Damage <= 0f) return;
            Damage = Mathf.Max(0f, Damage - amount);
            IsWrecked = false;
            ApplyPaintAndDamage();
            DamageChanged?.Invoke(DamageNormalized);
        }

        public void RepairFully() => Repair(maxDamage);

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || IsWrecked) return;
            Damage = Mathf.Clamp(Damage + amount, 0f, maxDamage);
            ApplyPaintAndDamage();
            DamageChanged?.Invoke(DamageNormalized);
            if (DamageNormalized >= 1f) Wreck(maxDamage);
        }

        private void FixedUpdate()
        {
            if (Definition == null || wheels == null || wheels.Length == 0 || IsWrecked) return;

            ReadInput(out float steering, out float throttle, out float brake, out bool wantsNitro, out bool handbrake);
            CurrentSteering = steering;
            CurrentThrottle = throttle;

            Vector3 localVelocity = transform.InverseTransformDirection(body.velocity);
            CurrentSpeedKph = body.velocity.magnitude * 3.6f;
            ForwardSpeedKph = localVelocity.z * 3.6f;
            UpdateGroundAndSlip(localVelocity, handbrake);
            ApplySteering(steering);
            ApplyNitro(wantsNitro, throttle);
            ApplyDriveAndBrakes(throttle, brake, handbrake, localVelocity.z);
            ApplyFriction(handbrake);
            ApplyAntiRoll();
            ApplyArcadeStability(localVelocity);
            UpdateEngineRpm();
        }

        private void LateUpdate()
        {
            if (wheels == null) return;
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel == null || wheel.collider == null || wheel.visual == null) continue;
                wheel.collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
                wheel.visual.SetPositionAndRotation(position, rotation);
            }
        }

        private void ReadInput(out float steering, out float throttle, out float brake, out bool nitro, out bool handbrake)
        {
            steering = 0f;
            throttle = 0f;
            brake = 0f;
            nitro = false;
            handbrake = false;

            if (!InputEnabled)
            {
                brake = 1f;
                return;
            }

            if (IsPlayer && InputManager.Instance != null)
            {
                steering = InputManager.Instance.Steering;
                throttle = InputManager.Instance.Throttle;
                brake = InputManager.Instance.Brake;
                nitro = InputManager.Instance.NitroHeld;
                handbrake = InputManager.Instance.HandbrakeHeld;

                // A single brake pedal is intuitive on touch: it brakes while moving forward, then
                // reverses only after the car has almost stopped and throttle is not held.
                float forwardMps = Vector3.Dot(body.velocity, transform.forward);
                if (brake > .01f && throttle < .01f && forwardMps < 1.1f)
                {
                    throttle = -brake;
                    brake = 0f;
                }
            }
            else
            {
                steering = externalSteering;
                throttle = externalThrottle;
                brake = externalBrake;
                nitro = externalNitro;
                handbrake = externalHandbrake;
            }
        }

        private void ApplySteering(float input)
        {
            float speedFactor = Mathf.Lerp(1f, .32f,
                Mathf.InverseLerp(20f, Definition.topSpeedKph * difficultyMultiplier, Mathf.Abs(ForwardSpeedKph)));
            float steeringAngle = input * Definition.steeringAngle * Definition.handling * handlingUpgradeMultiplier * speedFactor * PerformanceMultiplier;
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel != null && wheel.collider != null && wheel.steer)
                    wheel.collider.steerAngle = steeringAngle;
            }
        }

        private void ApplyDriveAndBrakes(float throttle, float brake, bool handbrake, float forwardMps)
        {
            bool changingDirection = (throttle > .05f && forwardMps < -1f) || (throttle < -.05f && forwardMps > 1f);
            float directionSpeedKph = throttle >= 0f ? forwardMps * 3.6f : -forwardMps * 3.6f;
            float speedLimit = throttle >= 0f ? Definition.topSpeedKph * difficultyMultiplier : Mathf.Max(10f, Definition.maxReverseKph);
            float motor = 0f;

            if (!changingDirection && Mathf.Abs(throttle) > .01f && directionSpeedKph < speedLimit)
            {
                float speed01 = Mathf.Clamp01(Mathf.Abs(ForwardSpeedKph) / Mathf.Max(1f, Definition.topSpeedKph));
                float curve = Definition.torqueBySpeed == null ? 1f : Mathf.Max(0f, Definition.torqueBySpeed.Evaluate(speed01));
                float reverseMultiplier = throttle < 0f ? reverseTorqueMultiplier : 1f;
                motor = throttle * Definition.motorTorque * engineUpgradeMultiplier * curve * PerformanceMultiplier * difficultyMultiplier * reverseMultiplier;
            }

            if (changingDirection) brake = Mathf.Max(brake, Mathf.Abs(throttle));
            float torquePerWheel = drivenWheelCount > 0 ? motor / drivenWheelCount : 0f;
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel == null || wheel.collider == null) continue;

                float tractionFactor = GetTractionControlFactor(wheel);
                wheel.collider.motorTorque = wheel.drive ? torquePerWheel * tractionFactor : 0f;
                float wheelBrake = CalculateBrakeTorque(wheel, brake);
                if (handbrake && wheel.handbrake) wheelBrake = Mathf.Max(wheelBrake, Definition.brakeTorque * .82f);
                wheel.collider.brakeTorque = wheelBrake;
            }
        }

        private float CalculateBrakeTorque(WheelSetup wheel, float brakeInput)
        {
            if (brakeInput <= .01f) return 0f;
            bool front = IsFrontWheel(wheel);
            float frontBias = Mathf.Clamp(Definition.frontBrakeBias, .45f, .85f);
            float axleBias = front ? frontBias : 1f - frontBias;
            float result = Definition.brakeTorque * brakeInput * axleBias * 2f;
            if (abs && wheel.collider.GetGroundHit(out WheelHit hit) && Mathf.Abs(hit.forwardSlip) > .45f)
                result *= .42f;
            return result;
        }

        private float GetTractionControlFactor(WheelSetup wheel)
        {
            if (!tractionControl || !wheel.drive || !wheel.collider.GetGroundHit(out WheelHit hit)) return 1f;
            return Mathf.Abs(hit.forwardSlip) > .42f ? .55f : 1f;
        }

        private void ApplyNitro(bool wantsNitro, float throttle)
        {
            bool wasActive = IsNitroActive;
            IsNitroActive = false;
            if (nitroCooldownRemaining > 0f)
                nitroCooldownRemaining = Mathf.Max(0f, nitroCooldownRemaining - Time.fixedDeltaTime);

            bool canBoost = wantsNitro && throttle > .15f && IsGrounded && CurrentSpeedKph > 12f &&
                            nitroRemaining > 0f && nitroCooldownRemaining <= 0f;
            // Releasing boost has a short lockout so tapping the button cannot bypass recharge pacing.
            if (wasActive && !canBoost && nitroRemaining > 0f && nitroCooldownRemaining <= 0f)
                nitroCooldownRemaining = Definition.nitroCooldown * .3f;

            if (canBoost)
            {
                IsNitroActive = true;
                nitroRemaining = Mathf.Max(0f, nitroRemaining - Definition.nitroDrainPerSecond * Time.fixedDeltaTime);
                body.AddForce(transform.forward * Definition.nitroForce * nitroUpgradeMultiplier * PerformanceMultiplier, ForceMode.Force);
                if (nitroRemaining <= 0f) nitroCooldownRemaining = Definition.nitroCooldown;
            }
            else if (nitroCooldownRemaining <= 0f && Definition.nitroRechargePerSecond > 0f)
            {
                nitroRemaining = Mathf.Min(Definition.nitroCapacity * nitroUpgradeMultiplier, nitroRemaining + Definition.nitroRechargePerSecond * nitroUpgradeMultiplier * Time.fixedDeltaTime);
            }

            if (wasActive != IsNitroActive) NitroStateChanged?.Invoke(IsNitroActive);
        }

        private void ApplyFriction(bool handbrake)
        {
            float grip = Definition.grip * handlingUpgradeMultiplier * Mathf.Lerp(1f, .82f, DamageNormalized);
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel == null || wheel.collider == null) continue;
                WheelFrictionCurve forward = baseForwardFriction[i];
                WheelFrictionCurve sideways = baseSidewaysFriction[i];
                bool rearDriftWheel = IsDrifting && !IsFrontWheel(wheel);
                forward.stiffness = grip * (rearDriftWheel ? Definition.driftForwardGrip : 1f);
                sideways.stiffness = grip * (rearDriftWheel ? Definition.driftRearGrip : 1f);
                wheel.collider.forwardFriction = forward;
                wheel.collider.sidewaysFriction = sideways;

                bool emit = IsDrifting && wheel.collider.isGrounded && !IsFrontWheel(wheel);
                if (wheel.skidTrail != null) wheel.skidTrail.emitting = emit;
                if (wheel.dust != null)
                {
                    if (emit && !wheel.dust.isPlaying) wheel.dust.Play(true);
                    else if (!emit && wheel.dust.isPlaying) wheel.dust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void UpdateGroundAndSlip(Vector3 localVelocity, bool handbrake)
        {
            int groundedCount = 0;
            float totalSlip = 0f;
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel == null || wheel.collider == null) continue;
                if (wheel.collider.GetGroundHit(out WheelHit hit))
                {
                    groundedCount++;
                    totalSlip += Mathf.Abs(hit.sidewaysSlip);
                }
            }
            IsGrounded = groundedCount > 0;
            NormalizedSlip = groundedCount > 0 ? totalSlip / groundedCount : 0f;
            bool speedAllowsDrift = Mathf.Abs(ForwardSpeedKph) > driftMinimumKph;
            bool steeringDrift = Mathf.Abs(CurrentSteering) > .35f && Mathf.Abs(localVelocity.x) > 1.2f;
            IsDrifting = IsGrounded && speedAllowsDrift && (handbrake || steeringDrift || NormalizedSlip > .32f);
        }

        private void ApplyArcadeStability(Vector3 localVelocity)
        {
            if (!IsGrounded) return;
            float lateralDamping = IsDrifting ? driftLateralDamping : normalLateralDamping;
            body.AddForce(-transform.right * localVelocity.x * lateralDamping * body.mass, ForceMode.Force);
            body.AddForce(-transform.up * localVelocity.z * localVelocity.z * downforce, ForceMode.Force);

            Vector3 rollAxis = Vector3.Cross(transform.up, Vector3.up);
            body.AddTorque(rollAxis * uprightStabilizer * body.mass, ForceMode.Force);
        }

        private void ApplyAntiRoll()
        {
            ApplyAntiRollPair(frontLeft, frontRight);
            ApplyAntiRollPair(rearLeft, rearRight);
        }

        private void ApplyAntiRollPair(WheelSetup left, WheelSetup right)
        {
            if (left == null || right == null || left.collider == null || right.collider == null) return;
            float leftTravel = GetSuspensionTravel(left.collider, out bool leftGrounded);
            float rightTravel = GetSuspensionTravel(right.collider, out bool rightGrounded);
            float force = (leftTravel - rightTravel) * antiRollForce;
            if (leftGrounded) body.AddForceAtPosition(left.collider.transform.up * -force, left.collider.transform.position, ForceMode.Force);
            if (rightGrounded) body.AddForceAtPosition(right.collider.transform.up * force, right.collider.transform.position, ForceMode.Force);
        }

        private static float GetSuspensionTravel(WheelCollider wheel, out bool grounded)
        {
            grounded = wheel.GetGroundHit(out WheelHit hit);
            if (!grounded) return 1f;
            float distance = Mathf.Max(.001f, wheel.suspensionDistance);
            return (-wheel.transform.InverseTransformPoint(hit.point).y - wheel.radius) / distance;
        }

        private void UpdateEngineRpm()
        {
            float wheelRpm = 0f;
            int rpmWheelCount = 0;
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel != null && wheel.collider != null && wheel.drive)
                {
                    wheelRpm += Mathf.Abs(wheel.collider.rpm);
                    rpmWheelCount++;
                }
            }
            float averageWheelRpm = rpmWheelCount == 0 ? 0f : wheelRpm / rpmWheelCount;
            float speedRpm01 = Mathf.Clamp01(Mathf.Abs(ForwardSpeedKph) / Mathf.Max(1f, Definition.topSpeedKph));
            float wheelRpm01 = Mathf.Clamp01(averageWheelRpm / 1150f);
            float targetRpm = Mathf.Lerp(Definition.idleRpm, Definition.maxRpm, Mathf.Max(speedRpm01, wheelRpm01));
            if (IsNitroActive) targetRpm = Mathf.Min(Definition.maxRpm, targetRpm * 1.06f);
            EngineRpm = Mathf.MoveTowards(EngineRpm <= 0f ? Definition.idleRpm : EngineRpm, targetRpm, 7500f * Time.fixedDeltaTime);
        }

        private void ConfigureWheelsFromPreset()
        {
            DiscoverWheelsIfRequired();
            baseForwardFriction = new WheelFrictionCurve[wheels.Length];
            baseSidewaysFriction = new WheelFrictionCurve[wheels.Length];
            drivenWheelCount = 0;
            frontLeft = frontRight = rearLeft = rearRight = null;

            for (int i = 0; i < wheels.Length; i++)
            {
                WheelSetup wheel = wheels[i];
                if (wheel == null || wheel.collider == null) continue;
                bool front = IsFrontWheel(wheel);
                bool left = transform.InverseTransformPoint(wheel.collider.transform.position).x < 0f;
                if (autoAssignWheelRoles)
                {
                    wheel.steer = front;
                    wheel.handbrake = !front;
                    wheel.drive = Definition.driveLayout == DriveLayout.AllWheelDrive ||
                                  (Definition.driveLayout == DriveLayout.FrontWheelDrive && front) ||
                                  (Definition.driveLayout == DriveLayout.RearWheelDrive && !front);
                }
                if (wheel.drive) drivenWheelCount++;
                if (front && left) frontLeft = wheel;
                else if (front) frontRight = wheel;
                else if (left) rearLeft = wheel;
                else rearRight = wheel;

                wheel.collider.suspensionDistance = Mathf.Clamp(Definition.suspensionTravel, .05f, .45f);
                JointSpring spring = wheel.collider.suspensionSpring;
                spring.spring = Mathf.Max(1000f, Definition.suspensionSpring);
                spring.damper = Mathf.Max(100f, Definition.suspensionDamper);
                spring.targetPosition = .5f;
                wheel.collider.suspensionSpring = spring;

                WheelFrictionCurve forward = wheel.collider.forwardFriction;
                WheelFrictionCurve sideways = wheel.collider.sidewaysFriction;
                forward.stiffness = Definition.grip * handlingUpgradeMultiplier;
                sideways.stiffness = Definition.grip * handlingUpgradeMultiplier;
                wheel.collider.forwardFriction = forward;
                wheel.collider.sidewaysFriction = sideways;
                baseForwardFriction[i] = forward;
                baseSidewaysFriction[i] = sideways;
            }
        }

        private void DiscoverWheelsIfRequired()
        {
            if (wheels != null && wheels.Length > 0) return;
            WheelCollider[] found = GetComponentsInChildren<WheelCollider>(true);
            wheels = new WheelSetup[found.Length];
            for (int i = 0; i < found.Length; i++)
            {
                wheels[i] = new WheelSetup
                {
                    collider = found[i],
                    visual = found[i].transform.Find("Visual")
                };
            }
        }

        private bool IsFrontWheel(WheelSetup wheel)
        {
            return transform.InverseTransformPoint(wheel.collider.transform.position).z > 0f;
        }

        private void CacheDamageRenderers()
        {
            if (damageRenderers == null || damageRenderers.Length == 0)
            {
                Transform source = visualBody == null ? transform : visualBody;
                damageRenderers = source.GetComponentsInChildren<Renderer>(true);
            }
            cachedDamageRenderers = damageRenderers;
            baseRendererColors = new Color[cachedDamageRenderers.Length];
            for (int i = 0; i < cachedDamageRenderers.Length; i++)
            {
                Renderer renderer = cachedDamageRenderers[i];
                if (renderer == null || renderer.sharedMaterial == null) continue;
                Material material = renderer.sharedMaterial;
                baseRendererColors[i] = material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) :
                    material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
            }
            propertyBlock = new MaterialPropertyBlock();
        }

        private void ApplyPaintAndDamage()
        {
            if (cachedDamageRenderers == null) CacheDamageRenderers();
            Color paint = Definition == null ? Color.white : Definition.bodyColor;
            float darken = DamageNormalized * visualDamageDarkening;
            for (int i = 0; i < cachedDamageRenderers.Length; i++)
            {
                Renderer renderer = cachedDamageRenderers[i];
                if (renderer == null) continue;
                Color baseColor = Definition == null ? baseRendererColors[i] : paint;
                Color damagedColor = Color.Lerp(baseColor, damageTint, darken);
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, damagedColor);
                propertyBlock.SetColor(ColorId, damagedColor);
                propertyBlock.SetFloat(DamageAmountId, DamageNormalized);
                renderer.SetPropertyBlock(propertyBlock);
            }
            if (damageSmoke != null)
            {
                bool shouldSmoke = DamageNormalized >= .55f && !IsWrecked;
                if (shouldSmoke && !damageSmoke.isPlaying) damageSmoke.Play(true);
                else if (!shouldSmoke && damageSmoke.isPlaying) damageSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Definition == null || IsWrecked) return;
            float impact = collision.relativeVelocity.magnitude;
            if (impact < damageImpactThreshold || Time.time - lastDamageTime < .12f) return;
            lastDamageTime = Time.time;
            float amount = (impact - damageImpactThreshold) * damagePerImpactUnit * Definition.collisionDamageMultiplier;
            ApplyDamage(amount);
            if (impact >= wreckImpactThreshold && !IsWrecked) Wreck(impact);
        }

        private void Wreck(float impact)
        {
            if (IsWrecked) return;
            IsWrecked = true;
            InputEnabled = false;
            IsNitroActive = false;
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] == null || wheels[i].collider == null) continue;
                wheels[i].collider.motorTorque = 0f;
                wheels[i].collider.brakeTorque = Definition == null ? 0f : Definition.brakeTorque;
            }
            ApplyPaintAndDamage();
            Crashed?.Invoke(impact);
            if (!IsPlayer) return;

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
