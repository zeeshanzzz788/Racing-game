using UnityEngine;

namespace VelocityRush.Data
{
    public enum DriveLayout
    {
        FrontWheelDrive,
        RearWheelDrive,
        AllWheelDrive
    }

    public enum CarUpgradeType
    {
        Engine,
        Handling,
        Nitro
    }

    /// <summary>
    /// A single source of truth for a car's mobile-friendly arcade physics preset. Keep the id
    /// stable after release because ProgressionService uses it as a save key.
    /// </summary>
    [CreateAssetMenu(fileName = "Car_", menuName = "Velocity Rush/Car Definition")]
    public class CarDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable save key. Do not change after release.")]
        public string id = "street_rookie";
        public string displayName = "Street Rookie";
        [TextArea] public string description;
        public Sprite garageIcon;
        public GameObject prefab;
        public Color bodyColor = Color.red;

        [Header("Unlock")]
        public bool unlockedByDefault;
        [Min(0)] public int unlockCost;

        [Header("Powertrain")]
        public DriveLayout driveLayout = DriveLayout.AllWheelDrive;
        [Min(60f)] public float topSpeedKph = 170f;
        [Min(10f)] public float maxReverseKph = 36f;
        [Min(100f)] public float motorTorque = 2200f;
        [Min(500f)] public float brakeTorque = 4200f;
        [Range(.45f, .85f)] public float frontBrakeBias = .64f;
        [Tooltip("X is normalized speed from 0 to 1; Y is available torque multiplier.")]
        public AnimationCurve torqueBySpeed = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(.35f, .92f), new Keyframe(.8f, .55f), new Keyframe(1f, 0f));
        [Min(400f)] public float idleRpm = 850f;
        [Min(1000f)] public float maxRpm = 7200f;

        [Header("Handling")]
        [Range(10f, 50f)] public float steeringAngle = 30f;
        [Range(.2f, 2f)] public float handling = 1f;
        [Range(.5f, 2f)] public float grip = 1f;
        [Min(600f)] public float mass = 1200f;
        [Range(.05f, .45f)] public float suspensionTravel = .18f;
        [Min(1000f)] public float suspensionSpring = 36000f;
        [Min(100f)] public float suspensionDamper = 4500f;
        [Range(.25f, 1f)] public float driftRearGrip = .58f;
        [Range(.1f, 1f)] public float driftForwardGrip = .72f;

        [Header("Boost")]
        [Tooltip("Nitro is measured in seconds of boost.")]
        [Min(0f)] public float nitroCapacity = 4f;
        [Min(0f)] public float nitroForce = 1400f;
        [Min(.05f)] public float nitroDrainPerSecond = 1f;
        [Min(0f)] public float nitroCooldown = 1.25f;
        [Min(0f)] public float nitroRechargePerSecond = .35f;

        [Header("Damage")]
        [Tooltip("How much engine/grip is lost at 100% damage. Keep this below 0.5 for fun arcade racing.")]
        [Range(0f, .8f)] public float maxPerformanceLossAtFullDamage = .35f;
        [Range(.1f, 3f)] public float collisionDamageMultiplier = 1f;

        [Header("Garage Upgrades")]
        [Range(0, 10)] public int maxUpgradeLevel = 5;
        [Min(0)] public int engineUpgradeBaseCost = 180;
        [Min(0)] public int handlingUpgradeBaseCost = 160;
        [Min(0)] public int nitroUpgradeBaseCost = 200;
        [Range(0f, .2f)] public float engineBonusPerLevel = .045f;
        [Range(0f, .2f)] public float handlingBonusPerLevel = .035f;
        [Range(0f, .2f)] public float nitroBonusPerLevel = .06f;

        // Normalized values intended for garage bars, not a second physics model.
        public float SpeedRating => Mathf.InverseLerp(100f, 320f, topSpeedKph);
        public float AccelerationRating => Mathf.InverseLerp(1200f, 5000f, motorTorque);
        public float HandlingRating => Mathf.InverseLerp(.3f, 1.8f, handling * grip);
    }
}
