using System.Collections;
using UnityEngine;
using VelocityRush.Cars;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.UI;

namespace VelocityRush.Race
{
    /// <summary>Owns a circuit race's grid, countdown, checkpoint order, laps, and timer.</summary>
    public class RaceManager : MonoBehaviour
    {
        [Header("Track setup")]
        [SerializeField] private Transform playerStart;
        [SerializeField] private Transform[] opponentStarts;
        [SerializeField] private Checkpoint[] orderedCheckpoints;
        [SerializeField] private WaypointCircuit waypoints;
        [SerializeField, Min(1)] private int fallbackLaps = 3;
        [SerializeField] private float countdownSeconds = 3f;

        public WaypointCircuit Waypoints => waypoints;
        public float ElapsedSeconds { get; private set; }
        public bool RaceActive { get; private set; }
        public int CurrentLap { get; private set; }
        public int LapsToComplete { get; private set; }

        private int nextCheckpoint;
        private UIManager ui;

        private void Start()
        {
            ui = FindObjectOfType<UIManager>();
            RaceSession session = GameManager.Instance == null ? null : GameManager.Instance.CurrentSession;
            LapsToComplete = session == null || session.Laps <= 0 ? fallbackLaps : session.Laps;
            CurrentLap = 1;
            nextCheckpoint = orderedCheckpoints != null && orderedCheckpoints.Length > 1 ? 1 : 0;
            StartCoroutine(BeginRace());
        }

        public Transform GetPlayerStart() => playerStart == null ? transform : playerStart;

        public Transform GetOpponentStart(int index)
        {
            if (opponentStarts == null || opponentStarts.Length == 0) return null;
            return opponentStarts[index % opponentStarts.Length];
        }

        public void RegisterPlayerCheckpoint(int checkpointIndex)
        {
            if (!RaceActive || checkpointIndex != nextCheckpoint) return;
            if (orderedCheckpoints == null || orderedCheckpoints.Length == 0) return;

            if (checkpointIndex == 0)
            {
                CurrentLap++;
                if (CurrentLap > LapsToComplete)
                {
                    Finish(RaceResult.Won);
                    return;
                }
                nextCheckpoint = orderedCheckpoints.Length > 1 ? 1 : 0;
            }
            else
            {
                nextCheckpoint = (checkpointIndex + 1) % orderedCheckpoints.Length;
            }
            if (ui != null) ui.SetLap(CurrentLap, LapsToComplete);
        }

        public void Finish(RaceResult result)
        {
            if (!RaceActive) return;
            RaceActive = false;
            if (GameManager.Instance != null) GameManager.Instance.EndRace(result, ElapsedSeconds);
        }

        private IEnumerator BeginRace()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("Velocity Rush could not create its persistent GameManager services.");
                yield break;
            }

            GameManager.Instance.SpawnRaceCars(this);
            SetAllCarsEnabled(false);
            if (ui != null) ui.SetLap(CurrentLap, LapsToComplete);

            float remaining = countdownSeconds;
            while (remaining > 0f)
            {
                if (ui != null) ui.ShowCountdown(Mathf.CeilToInt(remaining).ToString());
                remaining -= Time.deltaTime;
                yield return null;
            }
            if (ui != null) ui.ShowCountdown("GO!");
            yield return new WaitForSeconds(.6f);
            if (ui != null) ui.ShowCountdown(string.Empty);

            RaceActive = true;
            SetAllCarsEnabled(true);
        }

        private void Update()
        {
            if (!RaceActive) return;
            ElapsedSeconds += Time.deltaTime;
            if (ui != null) ui.SetTimer(ElapsedSeconds);
        }

        private void SetAllCarsEnabled(bool enabled)
        {
            foreach (PlayerCarController car in FindObjectsOfType<PlayerCarController>())
                car.SetInputEnabled(enabled);
        }
    }
}
