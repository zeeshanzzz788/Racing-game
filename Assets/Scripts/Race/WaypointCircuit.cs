using UnityEngine;

namespace VelocityRush.Race
{
    /// <summary>Ordered transform list shared by AI and optional minimap route rendering.</summary>
    public class WaypointCircuit : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        public int Count => waypoints == null ? 0 : waypoints.Length;

        public Transform GetWaypoint(int index)
        {
            if (Count == 0) return null;
            return waypoints[(index % Count + Count) % Count];
        }

        private void OnDrawGizmosSelected()
        {
            if (Count < 2) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < Count; i++)
            {
                Transform from = GetWaypoint(i);
                Transform to = GetWaypoint(i + 1);
                if (from != null && to != null) Gizmos.DrawLine(from.position, to.position);
            }
        }
    }
}
