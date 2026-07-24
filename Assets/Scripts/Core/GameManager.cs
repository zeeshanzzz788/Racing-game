using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelocityRush.Cars;
using VelocityRush.Data;
using VelocityRush.Progression;
using VelocityRush.Race;
using VelocityRush.UI;

namespace VelocityRush.Core
{
    /// <summary>
    /// Persistent application coordinator. Menu code asks this class to start a session; track
    /// scenes ask it to spawn the selected car. It deliberately owns no scene-only references.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Optional inspector catalogs; Resources/Data is used when blank")]
        [SerializeField] private CarDefinition[] carCatalog;
        [SerializeField] private TrackDefinition[] trackCatalog;
        [SerializeField] private CampaignLevelDefinition[] campaignLevels;
        [SerializeField] private string defaultTrackId = "desert_circuit";
        [SerializeField] private string endlessSceneName = "EndlessRun";
        [SerializeField, Range(30, 120)] private int mobileTargetFrameRate = 60;

        public IReadOnlyList<CarDefinition> Cars => carCatalog;
        public IReadOnlyList<TrackDefinition> Tracks => trackCatalog;
        public IReadOnlyList<CampaignLevelDefinition> CampaignLevels => campaignLevels;
        public RaceSession CurrentSession { get; private set; }
        public CarController PlayerCar { get; private set; }
        public CarDefinition SelectedCar { get; private set; }
        public bool RaceFinished { get; private set; }

        public event Action<CarController> PlayerSpawned;
        public event Action<RaceResult, float> RaceEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (Application.isMobilePlatform) Application.targetFrameRate = mobileTargetFrameRate;
            LoadCatalogsIfNeeded();

            ProgressionService progression = GetComponent<ProgressionService>();
            if (progression == null) progression = gameObject.AddComponent<ProgressionService>();
            progression.SeedDefaultCars(carCatalog);
            SelectedCar = FindCar(progression.SelectedCarId) ?? FirstUnlockedCar();
        }

        public void SelectCar(CarDefinition car)
        {
            if (car == null || !ProgressionService.Instance.IsCarUnlocked(car)) return;
            SelectedCar = car;
            ProgressionService.Instance.SetSelectedCar(car);
        }

        public void StartEndless(TrackDefinition track = null)
        {
            TrackDefinition chosen = track ?? FindTrack(defaultTrackId) ?? FirstTrack();
            if (chosen == null) return;
            CurrentSession = new RaceSession(GameMode.Endless, chosen.id, 0, 0);
            RaceFinished = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(endlessSceneName);
        }

        public void StartQuickRace(TrackDefinition track, int opponentCount = 4, int laps = 3)
        {
            TrackDefinition chosen = track ?? FirstTrack();
            if (chosen == null) { Debug.LogError("Velocity Rush has no TrackDefinition assets. Run the prototype bootstrap or create a track."); return; }
            StartSession(new RaceSession(GameMode.QuickRace, chosen.id, Mathf.Max(1, laps), Mathf.Clamp(opponentCount, 1, 5)), chosen);
        }

        public void StartTimeTrial(TrackDefinition track, int laps = 1)
        {
            TrackDefinition chosen = track ?? FirstTrack();
            if (chosen == null) { Debug.LogError("Velocity Rush has no TrackDefinition assets. Run the prototype bootstrap or create a track."); return; }
            StartSession(new RaceSession(GameMode.TimeTrial, chosen.id, Mathf.Max(1, laps), 0), chosen);
        }

        public void StartCampaign(CampaignLevelDefinition level)
        {
            if (level == null || level.track == null) return;
            StartSession(new RaceSession(GameMode.Campaign, level.track.id, level.laps, level.aiOpponents, level.levelNumber), level.track);
        }

        public void ShowGarage() => LoadMenuScene(SceneNames.Garage);
        public void ShowLevelSelect() => LoadMenuScene(SceneNames.LevelSelect);
        public void ReturnToMainMenu() => LoadMenuScene(SceneNames.MainMenu);

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SpawnRaceCars(RaceManager raceManager)
        {
            RaceFinished = false;
            PlayerCar = null;
            if (raceManager == null) return;

            CarDefinition car = SelectedCar ?? FirstUnlockedCar();
            if (car == null || car.prefab == null)
            {
                Debug.LogError("Velocity Rush needs a selected CarDefinition with a prefab before a race can start.");
                return;
            }

            SpawnPlayerAt(raceManager.GetPlayerStart());

            if (CurrentSession != null && CurrentSession.Mode != GameMode.TimeTrial && CurrentSession.Mode != GameMode.Endless)
                SpawnOpponents(raceManager, CurrentSession.OpponentCount);
        }

        public CarController SpawnPlayerAt(Transform start)
        {
            CarDefinition car = SelectedCar ?? FirstUnlockedCar();
            if (car == null || car.prefab == null || start == null)
            {
                Debug.LogError("Velocity Rush needs a selected CarDefinition with a prefab and a valid spawn point.");
                return null;
            }

            GameObject instance = Instantiate(car.prefab, start.position, start.rotation);
            instance.name = "Player_" + car.displayName;
            PlayerCar = instance.GetComponent<CarController>();
            if (PlayerCar == null) PlayerCar = instance.AddComponent<CarController>();
            PlayerCar.Initialize(car, true);
            PlayerSpawned?.Invoke(PlayerCar);
            return PlayerCar;
        }

        public void EndRace(RaceResult result, float elapsedSeconds)
        {
            if (RaceFinished) return;
            RaceFinished = true;
            if (PlayerCar != null) PlayerCar.SetInputEnabled(false);

            if (CurrentSession != null)
            {
                if (CurrentSession.Mode == GameMode.TimeTrial && result == RaceResult.Won)
                    ProgressionService.Instance.SetBestTimeIfFaster(CurrentSession.TrackId, elapsedSeconds);

                if (CurrentSession.Mode == GameMode.Campaign && result == RaceResult.Won)
                {
                    CampaignLevelDefinition level = GetCampaignLevel(CurrentSession.CampaignLevel);
                    if (level != null)
                    {
                        ProgressionService.Instance.AddCoins(level.coinReward);
                        int stars = elapsedSeconds <= level.targetTimeSeconds ? 3 : elapsedSeconds <= level.targetTimeSeconds * 1.25f ? 2 : 1;
                        ProgressionService.Instance.SetStarsIfHigher(level.levelNumber, stars);
                    }
                }
            }

            UIManager ui = FindObjectOfType<UIManager>();
            if (ui != null) ui.ShowRaceResult(result, elapsedSeconds);
            RaceEnded?.Invoke(result, elapsedSeconds);
        }

        public CarDefinition FindCar(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (CarDefinition car in carCatalog)
                if (car != null && car.id == id) return car;
            return null;
        }

        public TrackDefinition FindTrack(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (TrackDefinition track in trackCatalog)
                if (track != null && track.id == id) return track;
            return null;
        }

        public CampaignLevelDefinition GetCampaignLevel(int number)
        {
            foreach (CampaignLevelDefinition level in campaignLevels)
                if (level != null && level.levelNumber == number) return level;
            return null;
        }

        private void SpawnOpponents(RaceManager raceManager, int count)
        {
            List<CarDefinition> candidates = new List<CarDefinition>();
            foreach (CarDefinition car in carCatalog)
                if (car != null && car.prefab != null) candidates.Add(car);

            for (int i = 0; i < count; i++)
            {
                Transform start = raceManager.GetOpponentStart(i);
                if (start == null || candidates.Count == 0) break;
                CarDefinition definition = candidates[(i + 1) % candidates.Count];
                GameObject instance = Instantiate(definition.prefab, start.position, start.rotation);
                instance.name = "AI_" + (i + 1) + "_" + definition.displayName;
                CarController controller = instance.GetComponent<CarController>();
                if (controller == null) controller = instance.AddComponent<CarController>();
                controller.Initialize(definition, false);
                AICarController ai = instance.GetComponent<AICarController>();
                if (ai == null) ai = instance.AddComponent<AICarController>();
                ai.SetCircuit(raceManager.Waypoints);
            }
        }

        private void StartSession(RaceSession session, TrackDefinition track)
        {
            if (track == null || string.IsNullOrEmpty(track.sceneName))
            {
                Debug.LogError("Velocity Rush could not start: a valid TrackDefinition is required.");
                return;
            }
            CurrentSession = session;
            RaceFinished = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(track.sceneName);
        }

        private void LoadMenuScene(string sceneName)
        {
            Time.timeScale = 1f;
            RaceFinished = false;
            SceneManager.LoadScene(sceneName);
        }

        private void LoadCatalogsIfNeeded()
        {
            if (carCatalog == null || carCatalog.Length == 0)
                carCatalog = Resources.LoadAll<CarDefinition>("Data/Cars");
            if (trackCatalog == null || trackCatalog.Length == 0)
                trackCatalog = Resources.LoadAll<TrackDefinition>("Data/Tracks");
            if (campaignLevels == null || campaignLevels.Length == 0)
                campaignLevels = Resources.LoadAll<CampaignLevelDefinition>("Data/Campaign");
        }

        private CarDefinition FirstUnlockedCar()
        {
            foreach (CarDefinition car in carCatalog)
                if (car != null && ProgressionService.Instance.IsCarUnlocked(car)) return car;
            return carCatalog != null && carCatalog.Length > 0 ? carCatalog[0] : null;
        }

        private TrackDefinition FirstTrack()
        {
            return trackCatalog != null && trackCatalog.Length > 0 ? trackCatalog[0] : null;
        }
    }
}
