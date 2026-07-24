using UnityEngine;
using VelocityRush.Cars;

namespace VelocityRush.TrackSystem
{
    public enum TrackPowerUpType
    {
        Nitro,
        Repair
    }

    /// <summary>Reusable Endless/track power-up. Coins remain handled by Race.Collectible.</summary>
    [RequireComponent(typeof(Collider))]
    public class PowerUpPickup : MonoBehaviour
    {
        [SerializeField] private TrackPowerUpType type = TrackPowerUpType.Repair;
        [SerializeField, Min(.1f)] private float nitroSeconds = 1.5f;
        [SerializeField, Min(1f)] private float repairAmount = 18f;
        [SerializeField] private float spinDegreesPerSecond = 130f;
        [SerializeField] private float bobHeight = .18f;
        [SerializeField] private float bobFrequency = 2.2f;

        private Vector3 origin;
        private bool collected;

        private void Awake()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnEnable()
        {
            collected = false;
            origin = transform.position;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
            transform.position = origin + Vector3.up * (Mathf.Sin(Time.time * bobFrequency) * bobHeight);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            CarController car = other.GetComponentInParent<CarController>();
            if (car == null || !car.IsPlayer) return;
            collected = true;
            if (type == TrackPowerUpType.Nitro) car.AddNitro(nitroSeconds);
            else car.Repair(repairAmount);

            PooledTrackObject pooled = GetComponent<PooledTrackObject>();
            if (pooled != null) pooled.ReturnToPool();
            else Destroy(gameObject);
        }
    }
}
