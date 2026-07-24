using UnityEngine;
using VelocityRush.Cars;
using VelocityRush.Core;

namespace VelocityRush.Race
{
    /// <summary>Attach to cones, barriers, or traffic objects. Its collider may be trigger or solid.</summary>
    public class Hazard : MonoBehaviour
    {
        [SerializeField] private bool endsEndlessRun = true;
        [SerializeField] private float impactForce = 600f;
        [SerializeField, Min(0f)] private float damageAmount = 14f;

        private void OnCollisionEnter(Collision collision) => Hit(collision.collider);
        private void OnTriggerEnter(Collider other) => Hit(other);

        private void Hit(Collider other)
        {
            CarController car = other.GetComponentInParent<CarController>();
            if (car == null || !car.IsPlayer) return;
            Rigidbody body = car.GetComponent<Rigidbody>();
            if (body != null) body.AddForce(-car.transform.forward * impactForce, ForceMode.Impulse);
            car.ApplyDamage(damageAmount);
            VelocityRush.Endless.EndlessRunManager endless = FindObjectOfType<VelocityRush.Endless.EndlessRunManager>();
            if (endsEndlessRun && endless != null) endless.Crash();
        }
    }
}
