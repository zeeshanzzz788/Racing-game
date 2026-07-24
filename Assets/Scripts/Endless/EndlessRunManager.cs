using UnityEngine;
using VelocityRush.Cars;
using VelocityRush.Core;
using VelocityRush.TrackSystem;
using VelocityRush.UI;

namespace VelocityRush.Endless
{
    /// <summary>
    /// Scores an Endless session. TrackManager is the preferred pooled modular generator; the
    /// legacy straight EndlessTrackGenerator field remains only so older generated scenes keep working.
    /// </summary>
    public class EndlessRunManager : MonoBehaviour
    {
        [SerializeField] private Transform playerStart;
        [SerializeField] private TrackManager trackManager;
        [SerializeField] private EndlessTrackGenerator legacyTrackGenerator;
        [SerializeField, Min(1f)] private float pointsPerMeter = 1f;
        [SerializeField, Min(.001f)] private float legacyDifficultyGainPerSecond = .008f;
        [SerializeField] private float legacyMaxDifficultyMultiplier = 1.45f;

        public float RunTime { get; private set; }
        public int Score { get; private set; }
        public bool IsRunning { get; private set; }
        private CarController player;
        private UIManager ui;
        private Vector3 previousPlayerPosition;
        private float travelledDistance;

        private void Start()
        {
            ui = FindObjectOfType<UIManager>();
            if (GameManager.Instance == null)
            {
                Debug.LogError("Velocity Rush could not create its persistent GameManager services.");
                return;
            }
            player = GameManager.Instance.SpawnPlayerAt(playerStart == null ? transform : playerStart);
            if (player == null) return;
            if (trackManager != null) trackManager.SetPlayer(player);
            else if (legacyTrackGenerator != null) legacyTrackGenerator.SetPlayer(player);
            previousPlayerPosition = player.transform.position;
            IsRunning = true;
        }

        private void Update()
        {
            if (!IsRunning || player == null) return;
            RunTime += Time.deltaTime;
            travelledDistance += Vector3.Distance(player.transform.position, previousPlayerPosition);
            previousPlayerPosition = player.transform.position;

            if (trackManager == null)
            {
                float difficulty = Mathf.Min(legacyMaxDifficultyMultiplier, 1f + RunTime * legacyDifficultyGainPerSecond);
                player.SetDifficultyMultiplier(difficulty);
            }

            Score = Mathf.Max(0, Mathf.FloorToInt(travelledDistance * pointsPerMeter));
            if (ui != null)
            {
                ui.SetTimer(RunTime);
                ui.SetScore(Score);
            }
        }

        public void Crash()
        {
            if (!IsRunning) return;
            IsRunning = false;
            GameManager.Instance.EndRace(RaceResult.Crashed, RunTime);
        }

        public void QuitRun()
        {
            if (!IsRunning) return;
            IsRunning = false;
            GameManager.Instance.EndRace(RaceResult.Quit, RunTime);
        }
    }
}
