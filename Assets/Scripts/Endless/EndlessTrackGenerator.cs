using System;
using System.Collections.Generic;
using UnityEngine;
using VelocityRush.Cars;

namespace VelocityRush.Endless
{
    /// <summary>
    /// Pooled-in-spirit endless runner generator. Segment prefabs must be authored along local +Z
    /// and have matching segmentLength. Replace Destroy/Instantiate with a pool before shipping.
    /// </summary>
    public class EndlessTrackGenerator : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform spawnOrigin;
        [SerializeField] private GameObject[] segmentPrefabs;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private GameObject nitroPrefab;
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField, Min(20f)] private float segmentLength = 100f;
        [SerializeField, Range(3, 12)] private int segmentsAhead = 6;
        [SerializeField, Range(1, 4)] private int maxCoinsPerSegment = 3;
        [SerializeField, Range(0f, 1f)] private float obstacleChance = .35f;

        private readonly Queue<GameObject> activeSegments = new Queue<GameObject>();
        private float nextZ;
        public event Action<GameObject> SegmentCreated;

        private void Start()
        {
            if (spawnOrigin == null) spawnOrigin = transform;
            nextZ = spawnOrigin.position.z;
            for (int i = 0; i < segmentsAhead; i++) CreateSegment();
        }

        public void SetPlayer(CarController car)
        {
            player = car == null ? null : car.transform;
        }

        private void Update()
        {
            if (player == null || segmentPrefabs == null || segmentPrefabs.Length == 0) return;
            while (player.position.z + segmentLength * (segmentsAhead - 1) > nextZ) CreateSegment();
            while (activeSegments.Count > segmentsAhead + 1)
            {
                GameObject oldSegment = activeSegments.Peek();
                if (player.position.z - oldSegment.transform.position.z < segmentLength * 1.5f) break;
                activeSegments.Dequeue();
                Destroy(oldSegment);
            }
        }

        private void CreateSegment()
        {
            if (segmentPrefabs == null || segmentPrefabs.Length == 0) return;
            GameObject prefab = segmentPrefabs[UnityEngine.Random.Range(0, segmentPrefabs.Length)];
            if (prefab == null) return;
            Vector3 position = new Vector3(spawnOrigin.position.x, spawnOrigin.position.y, nextZ);
            GameObject segment = Instantiate(prefab, position, Quaternion.identity, transform);
            segment.name = "EndlessSegment_" + activeSegments.Count;
            activeSegments.Enqueue(segment);
            Populate(segment.transform);
            nextZ += segmentLength;
            SegmentCreated?.Invoke(segment);
        }

        private void Populate(Transform segment)
        {
            int coinCount = UnityEngine.Random.Range(1, maxCoinsPerSegment + 1);
            for (int i = 0; i < coinCount; i++)
            {
                GameObject prefab = (i == coinCount - 1 && nitroPrefab != null && UnityEngine.Random.value > .7f) ? nitroPrefab : coinPrefab;
                if (prefab == null) continue;
                float lane = UnityEngine.Random.Range(-3.5f, 3.5f);
                float z = UnityEngine.Random.Range(18f, segmentLength - 12f);
                Instantiate(prefab, segment.position + new Vector3(lane, 1f, z), Quaternion.identity, segment);
            }
            if (obstaclePrefab != null && UnityEngine.Random.value < obstacleChance)
            {
                float lane = UnityEngine.Random.Range(-3f, 3f);
                float z = UnityEngine.Random.Range(35f, segmentLength - 18f);
                Instantiate(obstaclePrefab, segment.position + new Vector3(lane, .5f, z), Quaternion.identity, segment);
            }
        }
    }
}
