using UnityEngine;
using VelocityRush.Cars;

namespace VelocityRush.Race
{
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Must match the index in RaceManager's ordered checkpoints array; finish line is 0.")]
        [SerializeField, Min(0)] private int checkpointIndex;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
            gameObject.tag = "Checkpoint";
        }

        private void OnTriggerEnter(Collider other)
        {
            CarController car = other.GetComponentInParent<CarController>();
            if (car == null || !car.IsPlayer) return;
            RaceManager manager = FindObjectOfType<RaceManager>();
            if (manager != null) manager.RegisterPlayerCheckpoint(checkpointIndex);
        }
    }
}
