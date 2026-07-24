using UnityEngine;
using VelocityRush.Cars;
using VelocityRush.Core;
using VelocityRush.UI;

namespace VelocityRush.Endless
{
    /// <summary>Scores an Endless session and ramps vehicle pace at a controlled mobile-friendly rate.</summary>
    public class EndlessRunManager : MonoBehaviour
    {
        [SerializeField] private Transform playerStart;
        [SerializeField] private EndlessTrackGenerator trackGenerator;
        [SerializeField, Min(1f)] private float pointsPerMeter = 1f;
        [SerializeField, Min(.001f)] private float difficultyGainPerSecond = .008f;
        [SerializeField] private float maxDifficultyMultiplier = 1.45f;

        public float RunTime { get; private set; }
        public int Score { get; private set; }
        public bool IsRunning { get; private set; }
        private PlayerCarController player;
        private UIManager ui;

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
            if (trackGenerator != null) trackGenerator.SetPlayer(player);
            IsRunning = true;
        }

        private void Update()
        {
            if (!IsRunning || player == null) return;
            RunTime += Time.deltaTime;
            float difficulty = Mathf.Min(maxDifficultyMultiplier, 1f + RunTime * difficultyGainPerSecond);
            player.SetDifficultyMultiplier(difficulty);
            Score = Mathf.Max(0, Mathf.FloorToInt(player.transform.position.z * pointsPerMeter));
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
