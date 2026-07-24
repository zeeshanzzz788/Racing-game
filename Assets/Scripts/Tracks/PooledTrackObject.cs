using UnityEngine;

namespace VelocityRush.TrackSystem
{
    /// <summary>
    /// Runtime-only component added by TrackManager to spawned pickups/obstacles. Gameplay pickup
    /// scripts call ReturnToPool instead of Destroy so Endless mode remains allocation-friendly.
    /// </summary>
    public class PooledTrackObject : MonoBehaviour
    {
        public bool IsInPool { get; private set; }
        internal GameObject SourcePrefab { get; private set; }
        private TrackManager owner;

        internal void SpawnedBy(TrackManager manager, GameObject source)
        {
            owner = manager;
            SourcePrefab = source;
            IsInPool = false;
        }

        public void ReturnToPool()
        {
            if (IsInPool) return;
            if (owner != null) owner.ReleasePooledObject(this);
            else
            {
                IsInPool = true;
                gameObject.SetActive(false);
            }
        }

        internal void MarkPooled()
        {
            IsInPool = true;
        }
    }
}
