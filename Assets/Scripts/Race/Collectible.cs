using UnityEngine;
using VelocityRush.Cars;
using VelocityRush.Progression;

namespace VelocityRush.Race
{
    public enum CollectibleType { Coin, Nitro }

    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        [SerializeField] private CollectibleType type = CollectibleType.Coin;
        [SerializeField, Min(1)] private int coinValue = 5;
        [SerializeField, Min(.1f)] private float nitroSeconds = 1f;
        [SerializeField] private float spinDegreesPerSecond = 120f;
        [SerializeField] private float bobHeight = .15f;
        [SerializeField] private float bobSpeed = 2f;
        private Vector3 origin;
        private bool collected;

        private void Awake()
        {
            origin = transform.position;
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
            transform.position = origin + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            CarController car = other.GetComponentInParent<CarController>();
            if (car == null || !car.IsPlayer) return;
            collected = true;
            if (type == CollectibleType.Coin && ProgressionService.Instance != null)
                ProgressionService.Instance.AddCoins(coinValue);
            else if (type == CollectibleType.Nitro)
                car.AddNitro(nitroSeconds);
            Destroy(gameObject);
        }
    }
}
