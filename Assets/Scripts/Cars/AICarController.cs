using UnityEngine;
using VelocityRush.Race;

namespace VelocityRush.Cars
{
    /// <summary>Lightweight waypoint follower for 3-5 mobile-friendly opponents.</summary>
    [RequireComponent(typeof(PlayerCarController))]
    public class AICarController : MonoBehaviour
    {
        [SerializeField, Range(.5f, 1.25f)] private float pace = .9f;
        [SerializeField, Range(3f, 25f)] private float waypointReachDistance = 10f;
        [SerializeField, Range(.1f, 1f)] private float steeringLookAhead = .45f;

        private PlayerCarController car;
        private WaypointCircuit circuit;
        private int waypointIndex;

        private void Awake() => car = GetComponent<PlayerCarController>();

        public void SetCircuit(WaypointCircuit value)
        {
            circuit = value;
            waypointIndex = 0;
        }

        private void FixedUpdate()
        {
            if (circuit == null || circuit.Count == 0 || car == null || car.Definition == null) return;
            Transform target = circuit.GetWaypoint(waypointIndex);
            if (target == null) return;

            Vector3 localTarget = transform.InverseTransformPoint(target.position);
            float steering = Mathf.Clamp((localTarget.x / Mathf.Max(1f, localTarget.magnitude)) / steeringLookAhead, -1f, 1f);
            float cornerAngle = Mathf.Abs(Mathf.Atan2(localTarget.x, Mathf.Max(.01f, localTarget.z)) * Mathf.Rad2Deg);
            float desiredSpeed = car.Definition.topSpeedKph * pace * Mathf.Lerp(.42f, 1f, 1f - cornerAngle / 90f);
            float throttle = car.CurrentSpeedKph < desiredSpeed ? 1f : 0f;
            float brake = car.CurrentSpeedKph > desiredSpeed + 8f ? 1f : 0f;
            bool nitro = cornerAngle < 8f && car.CurrentSpeedKph > 70f;
            car.SetExternalInput(steering, throttle, brake, nitro, cornerAngle > 45f);

            if (Vector3.Distance(transform.position, target.position) < waypointReachDistance)
                waypointIndex = (waypointIndex + 1) % circuit.Count;
        }
    }
}
