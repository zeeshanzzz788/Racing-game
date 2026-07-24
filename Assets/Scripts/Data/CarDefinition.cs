using UnityEngine;

namespace VelocityRush.Data
{
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
        [Min(0)] public int unlockCost = 0;

        [Header("Physics")]
        [Min(60)] public float topSpeedKph = 170f;
        [Min(100)] public float motorTorque = 2200f;
        [Min(500)] public float brakeTorque = 4200f;
        [Range(10f, 50f)] public float steeringAngle = 30f;
        [Range(0.2f, 2f)] public float handling = 1f;
        [Range(0.5f, 2f)] public float grip = 1f;
        [Min(600)] public float mass = 1200f;

        [Header("Boost")]
        [Min(0f)] public float nitroCapacity = 4f;
        [Min(0f)] public float nitroForce = 1400f;

        // Normalized values intended for garage bars, not a second physics model.
        public float SpeedRating => Mathf.InverseLerp(100f, 320f, topSpeedKph);
        public float AccelerationRating => Mathf.InverseLerp(1200f, 5000f, motorTorque);
        public float HandlingRating => Mathf.InverseLerp(.3f, 1.8f, handling * grip);
    }
}
