using System;
using System.Collections.Generic;
using UnityEngine;
using VelocityRush.Cars;
using VelocityRush.Race;

namespace VelocityRush.TrackSystem
{
    public enum TrackManagerMode
    {
        FixedCircuit,
        Endless
    }

    /// <summary>
    /// Builds designer-authored modular circuits or a pooled endless road. Endless pieces are
    /// connected from entry/exit anchors, reused after the player passes them, and populated with
    /// escalating pickups, power-ups and hazards. It is intentionally self-contained so track
    /// geometry, spawn slots, and difficulty are data on TrackPiece prefabs rather than hard-coded.
    /// </summary>
    public class TrackManager : MonoBehaviour
    {
        [Header("Mode")]
        [SerializeField] private TrackManagerMode mode = TrackManagerMode.Endless;
        [SerializeField] private CarController player;
        [SerializeField] private Transform spawnOrigin;
        [SerializeField] private int randomSeed = 4189;

        [Header("Fixed circuit / AI")]
        [Tooltip("Scene instances in driving order. Their AI markers are copied into Circuit Waypoints.")]
        [SerializeField] private TrackPiece[] fixedTrackPieces;
        [SerializeField] private WaypointCircuit circuitWaypoints;

        [Header("Endless pieces")]
        [SerializeField] private TrackPiece[] modularPiecePrefabs;
        [SerializeField, Range(4, 16)] private int piecesAhead = 7;
        [SerializeField, Min(15f)] private float recycleDistancePastExit = 35f;
        [SerializeField] private bool preventConsecutiveSpecialPieces = true;

        [Header("Endless content")]
        [SerializeField] private GameObject[] collectiblePrefabs;
        [SerializeField] private GameObject[] powerUpPrefabs;
        [SerializeField] private GameObject[] obstaclePrefabs;
        [SerializeField, Range(0, 8)] private int baseCollectiblesPerPiece = 2;
        [SerializeField, Range(0, 8)] private int maximumCollectiblesPerPiece = 5;
        [SerializeField, Range(0f, 1f)] private float baseObstacleChance = .12f;
        [SerializeField, Range(0f, 1f)] private float maximumObstacleChance = .65f;
        [SerializeField, Range(0f, 1f)] private float powerUpChance = .12f;

        [Header("Difficulty ramp")]
        [SerializeField, Min(10f)] private float secondsToMaximumDifficulty = 180f;
        [SerializeField, Range(1f, 1.6f)] private float maximumSpeedMultiplier = 1.35f;

        public TrackManagerMode Mode => mode;
        public WaypointCircuit CircuitWaypoints => circuitWaypoints;
        public float Difficulty01 { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public float DistanceTravelled { get; private set; }
        public int ActivePieceCount => activePieces.Count;
        public event Action<TrackPiece> PieceSpawned;
        public event Action<TrackPiece> PieceRecycled;

        private sealed class ActivePiece
        {
            public TrackPiece instance;
            public TrackPiece sourcePrefab;
            public readonly List<PooledTrackObject> spawnedObjects = new List<PooledTrackObject>(12);

            public void Reset(TrackPiece newInstance, TrackPiece source)
            {
                instance = newInstance;
                sourcePrefab = source;
                spawnedObjects.Clear();
            }
        }

        private readonly Queue<ActivePiece> activePieces = new Queue<ActivePiece>();
        private readonly Stack<ActivePiece> activePieceRecordPool = new Stack<ActivePiece>();
        private readonly Dictionary<TrackPiece, Stack<TrackPiece>> piecePools = new Dictionary<TrackPiece, Stack<TrackPiece>>();
        private readonly Dictionary<GameObject, Stack<GameObject>> objectPools = new Dictionary<GameObject, Stack<GameObject>>();
        private readonly List<Transform> fixedWaypointBuffer = new List<Transform>(96);
        private System.Random random;
        private Transform nextConnector;
        private Transform poolRoot;
        private TrackPiece lastGeneratedPiece;
        private float runSeconds;

        private void Awake()
        {
            if (player == null && GameManagerSafePlayerExists()) player = VelocityRush.Core.GameManager.Instance.PlayerCar;
            if (spawnOrigin == null) spawnOrigin = transform;
            random = new System.Random(randomSeed);
            GameObject root = new GameObject("TrackPool");
            root.transform.SetParent(transform, false);
            poolRoot = root.transform;
        }

        private void Start()
        {
            if (mode == TrackManagerMode.FixedCircuit)
            {
                RefreshFixedCircuitWaypoints();
                return;
            }

            nextConnector = spawnOrigin;
            for (int i = 0; i < piecesAhead; i++) SpawnNextPiece();
        }

        private void Update()
        {
            if (player == null && GameManagerSafePlayerExists()) player = VelocityRush.Core.GameManager.Instance.PlayerCar;
            if (player == null || mode != TrackManagerMode.Endless) return;

            UpdateDifficulty();
            RecyclePassedPieces();
            while (activePieces.Count < piecesAhead) SpawnNextPiece();
        }

        public void SetPlayer(CarController controller)
        {
            player = controller;
        }

        /// <summary>Call after re-ordering fixed scene pieces; RaceManager can consume CircuitWaypoints.</summary>
        public void RefreshFixedCircuitWaypoints()
        {
            if (mode != TrackManagerMode.FixedCircuit) return;
            if (circuitWaypoints == null) circuitWaypoints = GetComponent<WaypointCircuit>();
            if (circuitWaypoints == null) return;

            fixedWaypointBuffer.Clear();
            if (fixedTrackPieces != null)
            {
                for (int pieceIndex = 0; pieceIndex < fixedTrackPieces.Length; pieceIndex++)
                {
                    TrackPiece piece = fixedTrackPieces[pieceIndex];
                    if (piece == null || piece.AiWaypoints == null) continue;
                    Transform[] markers = piece.AiWaypoints;
                    for (int markerIndex = 0; markerIndex < markers.Length; markerIndex++)
                        if (markers[markerIndex] != null) fixedWaypointBuffer.Add(markers[markerIndex]);
                }
            }
            if (fixedWaypointBuffer.Count >= 2) circuitWaypoints.SetWaypoints(fixedWaypointBuffer.ToArray());
        }

        /// <summary>Called by PooledTrackObject and is public only to support pickup scripts.</summary>
        public void ReleasePooledObject(PooledTrackObject item)
        {
            if (item == null || item.IsInPool || item.SourcePrefab == null) return;
            GameObject source = item.SourcePrefab;
            if (!objectPools.TryGetValue(source, out Stack<GameObject> pool))
            {
                pool = new Stack<GameObject>();
                objectPools.Add(source, pool);
            }
            item.MarkPooled();
            GameObject instance = item.gameObject;
            instance.SetActive(false);
            instance.transform.SetParent(poolRoot, false);
            pool.Push(instance);
        }

        private void UpdateDifficulty()
        {
            runSeconds += Time.deltaTime;
            Difficulty01 = Mathf.Clamp01(runSeconds / secondsToMaximumDifficulty);
            SpeedMultiplier = Mathf.Lerp(1f, maximumSpeedMultiplier, Difficulty01);
            player.SetDifficultyMultiplier(SpeedMultiplier);
        }

        private void SpawnNextPiece()
        {
            TrackPiece prefab = SelectNextPiece();
            if (prefab == null || nextConnector == null) return;
            TrackPiece instance = GetPooledPiece(prefab);
            instance.transform.SetParent(transform, false);
            instance.gameObject.SetActive(false);
            AlignEntryToConnector(instance, nextConnector);
            instance.PrepareForSpawn();

            ActivePiece active = activePieceRecordPool.Count > 0 ? activePieceRecordPool.Pop() : new ActivePiece();
            active.Reset(instance, prefab);
            SpawnPieceContent(active);
            activePieces.Enqueue(active);
            nextConnector = instance.ExitAnchor;
            lastGeneratedPiece = prefab;
            PieceSpawned?.Invoke(instance);
        }

        private void RecyclePassedPieces()
        {
            while (activePieces.Count > 0)
            {
                ActivePiece active = activePieces.Peek();
                TrackPiece piece = active.instance;
                if (piece == null)
                {
                    activePieces.Dequeue();
                    activePieceRecordPool.Push(active);
                    continue;
                }

                Vector3 fromExit = player.transform.position - piece.ExitAnchor.position;
                if (Vector3.Dot(fromExit, piece.ExitAnchor.forward) < recycleDistancePastExit) break;

                activePieces.Dequeue();
                DistanceTravelled += piece.NominalLength;
                ReleasePieceContent(active);
                ReturnPieceToPool(piece, active.sourcePrefab);
                PieceRecycled?.Invoke(piece);
                activePieceRecordPool.Push(active);
            }
        }

        private TrackPiece SelectNextPiece()
        {
            if (modularPiecePrefabs == null || modularPiecePrefabs.Length == 0) return null;
            TrackPiece selected = WeightedSelection(preventConsecutiveSpecialPieces && lastGeneratedPiece != null && lastGeneratedPiece.IsSpecialPiece);
            return selected ?? WeightedSelection(false) ?? modularPiecePrefabs[0];
        }

        private TrackPiece WeightedSelection(bool excludeSpecial)
        {
            float totalWeight = 0f;
            for (int i = 0; i < modularPiecePrefabs.Length; i++)
            {
                TrackPiece candidate = modularPiecePrefabs[i];
                if (candidate == null || candidate.MinimumDifficulty > Difficulty01) continue;
                if (excludeSpecial && candidate.IsSpecialPiece) continue;
                totalWeight += candidate.SelectionWeight;
            }
            if (totalWeight <= 0f) return null;

            float roll = (float)(random.NextDouble() * totalWeight);
            for (int i = 0; i < modularPiecePrefabs.Length; i++)
            {
                TrackPiece candidate = modularPiecePrefabs[i];
                if (candidate == null || candidate.MinimumDifficulty > Difficulty01) continue;
                if (excludeSpecial && candidate.IsSpecialPiece) continue;
                roll -= candidate.SelectionWeight;
                if (roll <= 0f) return candidate;
            }
            return null;
        }

        private TrackPiece GetPooledPiece(TrackPiece prefab)
        {
            if (piecePools.TryGetValue(prefab, out Stack<TrackPiece> pool) && pool.Count > 0)
                return pool.Pop();
            TrackPiece instance = Instantiate(prefab);
            instance.name = prefab.name + " (Pooled)";
            return instance;
        }

        private void ReturnPieceToPool(TrackPiece instance, TrackPiece source)
        {
            if (instance == null || source == null) return;
            if (!piecePools.TryGetValue(source, out Stack<TrackPiece> pool))
            {
                pool = new Stack<TrackPiece>();
                piecePools.Add(source, pool);
            }
            instance.PrepareForRecycle();
            instance.transform.SetParent(poolRoot, false);
            pool.Push(instance);
        }

        private static void AlignEntryToConnector(TrackPiece piece, Transform connector)
        {
            Transform entry = piece.EntryAnchor;
            Vector3 entryLocalPosition = piece.transform.InverseTransformPoint(entry.position);
            Quaternion entryLocalRotation = Quaternion.Inverse(piece.transform.rotation) * entry.rotation;
            piece.transform.rotation = connector.rotation * Quaternion.Inverse(entryLocalRotation);
            piece.transform.position = connector.position - piece.transform.rotation * entryLocalPosition;
        }

        private void SpawnPieceContent(ActivePiece active)
        {
            TrackPiece piece = active.instance;
            int collectibleCount = Mathf.RoundToInt(Mathf.Lerp(baseCollectiblesPerPiece, maximumCollectiblesPerPiece, Difficulty01));
            SpawnAtSlots(active, piece.CollectibleSlots, collectiblePrefabs, collectibleCount, 1f);
            SpawnAtSlots(active, piece.PowerUpSlots, powerUpPrefabs, 1, powerUpChance);
            float obstacleChance = Mathf.Lerp(baseObstacleChance, maximumObstacleChance, Difficulty01);
            SpawnAtSlots(active, piece.ObstacleSlots, obstaclePrefabs, piece.ObstacleSlots == null ? 0 : piece.ObstacleSlots.Length, obstacleChance);
        }

        private void SpawnAtSlots(ActivePiece active, Transform[] slots, GameObject[] prefabs, int maximumCount, float chance)
        {
            if (slots == null || slots.Length == 0 || prefabs == null || prefabs.Length == 0 || maximumCount <= 0) return;
            int count = Mathf.Min(maximumCount, slots.Length);
            for (int i = 0; i < count; i++)
            {
                if ((float)random.NextDouble() > chance) continue;
                Transform slot = slots[(i + random.Next(slots.Length)) % slots.Length];
                GameObject prefab = prefabs[random.Next(prefabs.Length)];
                if (slot == null || prefab == null) continue;
                PooledTrackObject spawned = SpawnPooledObject(prefab, slot, active.instance.transform);
                if (spawned != null) active.spawnedObjects.Add(spawned);
            }
        }

        private PooledTrackObject SpawnPooledObject(GameObject prefab, Transform slot, Transform parent)
        {
            GameObject instance;
            if (objectPools.TryGetValue(prefab, out Stack<GameObject> pool) && pool.Count > 0)
                instance = pool.Pop();
            else
            {
                instance = Instantiate(prefab);
                instance.name = prefab.name + " (Pooled)";
            }

            instance.SetActive(false);
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(slot.position, slot.rotation);
            PooledTrackObject pooled = instance.GetComponent<PooledTrackObject>();
            if (pooled == null) pooled = instance.AddComponent<PooledTrackObject>();
            pooled.SpawnedBy(this, prefab);
            instance.SetActive(true);
            return pooled;
        }

        private static bool GameManagerSafePlayerExists()
        {
            return VelocityRush.Core.GameManager.Instance != null && VelocityRush.Core.GameManager.Instance.PlayerCar != null;
        }

        private void ReleasePieceContent(ActivePiece active)
        {
            for (int i = 0; i < active.spawnedObjects.Count; i++)
            {
                PooledTrackObject item = active.spawnedObjects[i];
                if (item != null && !item.IsInPool) item.ReturnToPool();
            }
            active.spawnedObjects.Clear();
        }

        private void OnDestroy()
        {
            while (activePieces.Count > 0)
            {
                ActivePiece active = activePieces.Dequeue();
                ReleasePieceContent(active);
            }
        }
    }
}
