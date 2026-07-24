using UnityEngine;
using VelocityRush.Race;

namespace VelocityRush.Cars
{
    /// <summary>
    /// Deliberately inexpensive waypoint AI for 3-5 mobile opponents. It slows for corners,
    /// uses one sphere cast to overtake a slower car, and has a small reverse recovery path.
    /// It is not intended to be a full racing-line or network-authoritative AI.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class AICarController : MonoBehaviour
    {
        [Header("Pace")]
        [SerializeField, Range(.5f, 1.25f)] private float pace = .9f;
        [SerializeField, Range(3f, 25f)] private float waypointReachDistance = 10f;
        [SerializeField, Range(.1f, 1f)] private float steeringLookAhead = .45f;
        [SerializeField, Range(1f, 8f)] private float laneOffsetLimit = 3f;

        [Header("Overtake / recovery")]
        [SerializeField, Range(4f, 24f)] private float overtakeProbeDistance = 12f;
        [SerializeField, Range(.25f, 2f)] private float overtakeProbeRadius = .7f;
        [SerializeField] private LayerMask vehicleProbeMask = ~0;
        [SerializeField, Range(.2f, 3f)] private float stuckSecondsBeforeReverse = 1.1f;
        [SerializeField, Range(.2f, 2f)] private float reverseSeconds = .65f;

        private CarController car;
        private WaypointCircuit circuit;
        private int waypointIndex;
        private float currentLaneOffset;
        private float targetLaneOffset;
        private float stuckTimer;
        private float reverseTimer;
        private float overtakeCommitTimer;

        private void Awake() => car = GetComponent<CarController>();

        public void SetCircuit(WaypointCircuit value)
        {
            circuit = value;
            waypointIndex = 0;
            currentLaneOffset = targetLaneOffset = 0f;
        }

        private void FixedUpdate()
        {
            if (circuit == null || circuit.Count == 0 || car == null || car.Definition == null || car.IsWrecked) return;
            Transform waypoint = circuit.GetWaypoint(waypointIndex);
            Transform nextWaypoint = circuit.GetWaypoint(waypointIndex + 1);
            if (waypoint == null || nextWaypoint == null) return;

            UpdateOvertake(waypoint, nextWaypoint);
            currentLaneOffset = Mathf.MoveTowards(currentLaneOffset, targetLaneOffset, Time.fixedDeltaTime * 4f);
            Vector3 routeForward = (nextWaypoint.position - waypoint.position).normalized;
            if (routeForward.sqrMagnitude < .01f) routeForward = transform.forward;
            Vector3 routeRight = Vector3.Cross(Vector3.up, routeForward).normalized;
            Vector3 targetPosition = waypoint.position + routeRight * currentLaneOffset;
            Vector3 localTarget = transform.InverseTransformPoint(targetPosition);
            float steering = Mathf.Clamp((localTarget.x / Mathf.Max(1f, localTarget.magnitude)) / steeringLookAhead, -1f, 1f);
            float cornerAngle = Mathf.Abs(Mathf.Atan2(localTarget.x, Mathf.Max(.01f, localTarget.z)) * Mathf.Rad2Deg);
            float cornerSpeedFactor = Mathf.Lerp(.42f, 1f, 1f - Mathf.Clamp01(cornerAngle / 88f));
            float desiredSpeed = car.Definition.topSpeedKph * pace * cornerSpeedFactor;

            UpdateStuckRecovery(localTarget);
            if (reverseTimer > 0f)
            {
                reverseTimer -= Time.fixedDeltaTime;
                car.SetExternalInput(-steering, -.7f, 0f, false, false);
            }
            else
            {
                float throttle = car.CurrentSpeedKph < desiredSpeed ? 1f : 0f;
                float brake = car.CurrentSpeedKph > desiredSpeed + 7f ? 1f : 0f;
                bool nitro = cornerAngle < 7f && currentLaneOffset == 0f && car.CurrentSpeedKph > 70f;
                car.SetExternalInput(steering, throttle, brake, nitro, cornerAngle > 48f && car.CurrentSpeedKph > 55f);
            }

            if (Vector3.Distance(transform.position, waypoint.position) < waypointReachDistance)
                waypointIndex = (waypointIndex + 1) % circuit.Count;
        }

        private void UpdateOvertake(Transform waypoint, Transform nextWaypoint)
        {
            if (overtakeCommitTimer > 0f)
            {
                overtakeCommitTimer -= Time.fixedDeltaTime;
                return;
            }

            Vector3 origin = transform.position + Vector3.up * .65f + transform.forward * 1.2f;
            if (Physics.SphereCast(origin, overtakeProbeRadius, transform.forward, out RaycastHit hit,
                overtakeProbeDistance, vehicleProbeMask, QueryTriggerInteraction.Ignore))
            {
                CarController blocker = hit.collider.GetComponentInParent<CarController>();
                if (blocker != null && blocker != car && blocker.CurrentSpeedKph < car.CurrentSpeedKph + 9f)
                {
                    // Pick the side away from the blocker and keep it long enough to complete a pass.
                    float localX = transform.InverseTransformPoint(blocker.transform.position).x;
                    targetLaneOffset = (localX >= 0f ? -1f : 1f) * laneOffsetLimit;
                    overtakeCommitTimer = 1.25f;
                    return;
                }
            }
            targetLaneOffset = Mathf.MoveTowards(targetLaneOffset, 0f, Time.fixedDeltaTime * 2f);
        }

        private void UpdateStuckRecovery(Vector3 localTarget)
        {
            bool shouldBeMoving = localTarget.z > 2f && car.CurrentThrottle > .1f;
            if (shouldBeMoving && car.CurrentSpeedKph < 2f) stuckTimer += Time.fixedDeltaTime;
            else stuckTimer = 0f;
            if (stuckTimer >= stuckSecondsBeforeReverse)
            {
                stuckTimer = 0f;
                reverseTimer = reverseSeconds;
            }
        }
    }
}
