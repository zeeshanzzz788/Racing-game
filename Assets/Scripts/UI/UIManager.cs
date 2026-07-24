using UnityEngine;
using UnityEngine.UI;
using VelocityRush.Cars;
using VelocityRush.Core;

namespace VelocityRush.UI
{
    /// <summary>Scene UI bridge. Assign optional controls in the Inspector; omitted widgets are safe.</summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private Text speedText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text lapText;
        [SerializeField] private Text countdownText;
        [SerializeField] private Slider nitroSlider;
        [SerializeField] private GameObject pausePanel;

        [Header("Results")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultTimeText;

        private CarController player;

        private void Start()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void Update()
        {
            if (player == null && GameManager.Instance != null) player = GameManager.Instance.PlayerCar;
            if (player == null) return;
            SetSpeed(player.CurrentSpeedKph);
            if (nitroSlider != null) nitroSlider.value = player.NitroNormalized;
        }

        public void SetSpeed(float speedKph)
        {
            if (speedText != null) speedText.text = Mathf.RoundToInt(speedKph) + "\n<size=42>KM/H</size>";
        }

        public void SetTimer(float seconds)
        {
            if (timerText != null) timerText.text = FormatTime(seconds);
        }

        public void SetScore(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString("N0");
        }

        public void SetLap(int current, int total)
        {
            if (lapText != null) lapText.text = "LAP " + current + "/" + total;
        }

        public void ShowCountdown(string value)
        {
            if (countdownText != null) countdownText.text = value;
        }

        public void ShowRaceResult(RaceResult result, float elapsedSeconds)
        {
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultTitleText != null)
            {
                switch (result)
                {
                    case RaceResult.Won: resultTitleText.text = "FINISH!"; break;
                    case RaceResult.Crashed: resultTitleText.text = "WRECKED"; break;
                    case RaceResult.Quit: resultTitleText.text = "RUN ENDED"; break;
                    default: resultTitleText.text = "RACE OVER"; break;
                }
            }
            if (resultTimeText != null) resultTimeText.text = elapsedSeconds > 0f ? FormatTime(elapsedSeconds) : string.Empty;
        }

        public void TogglePause()
        {
            bool paused = Time.timeScale > 0f;
            Time.timeScale = paused ? 0f : 1f;
            if (pausePanel != null) pausePanel.SetActive(paused);
        }

        public void Resume() => SetPause(false);

        public void QuitToMenu()
        {
            SetPause(false);
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
        }

        private void SetPause(bool pause)
        {
            Time.timeScale = pause ? 0f : 1f;
            if (pausePanel != null) pausePanel.SetActive(pause);
        }

        public static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int wholeSeconds = Mathf.FloorToInt(seconds % 60f);
            int milliseconds = Mathf.FloorToInt((seconds * 100f) % 100f);
            return minutes.ToString("00") + ":" + wholeSeconds.ToString("00") + "." + milliseconds.ToString("00");
        }
    }
}
