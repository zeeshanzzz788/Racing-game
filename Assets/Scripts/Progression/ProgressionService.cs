using System;
using System.Collections.Generic;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Progression
{
    /// <summary>
    /// Central PlayerPrefs-backed progression store. Replace this class with a cloud-save adapter
    /// rather than scattering PlayerPrefs calls through gameplay/UI code.
    /// </summary>
    public class ProgressionService : MonoBehaviour
    {
        public const string CoinsKey = "vr.coins";
        private const string SelectedCarKey = "vr.selected_car";
        private const string CarUnlockedPrefix = "vr.car.unlocked.";
        private const string BestTimePrefix = "vr.best_time.";
        private const string StarsPrefix = "vr.level.stars.";
        private const string UpgradePrefix = "vr.car.upgrade.";

        public static ProgressionService Instance { get; private set; }
        public event Action<int> CoinsChanged;
        public int Coins => PlayerPrefs.GetInt(CoinsKey, 0);
        public string SelectedCarId => PlayerPrefs.GetString(SelectedCarKey, string.Empty);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SeedDefaultCars(IEnumerable<CarDefinition> cars)
        {
            foreach (CarDefinition car in cars)
            {
                if (car != null && car.unlockedByDefault && !PlayerPrefs.HasKey(CarUnlockedPrefix + car.id))
                    PlayerPrefs.SetInt(CarUnlockedPrefix + car.id, 1);
            }
            PlayerPrefs.Save();
        }

        public bool IsCarUnlocked(CarDefinition car)
        {
            return car != null && (car.unlockedByDefault || PlayerPrefs.GetInt(CarUnlockedPrefix + car.id, 0) == 1);
        }

        public bool TryUnlockCar(CarDefinition car)
        {
            if (car == null || IsCarUnlocked(car)) return false;
            if (Coins < car.unlockCost) return false;
            SetCoins(Coins - car.unlockCost);
            PlayerPrefs.SetInt(CarUnlockedPrefix + car.id, 1);
            PlayerPrefs.Save();
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            SetCoins(Coins + amount);
        }

        public void SetSelectedCar(CarDefinition car)
        {
            if (car == null || !IsCarUnlocked(car)) return;
            PlayerPrefs.SetString(SelectedCarKey, car.id);
            PlayerPrefs.Save();
        }

        public int GetUpgradeLevel(CarDefinition car, CarUpgradeType type)
        {
            if (car == null) return 0;
            return Mathf.Clamp(PlayerPrefs.GetInt(UpgradeKey(car, type), 0), 0, car.maxUpgradeLevel);
        }

        public int GetUpgradeCost(CarDefinition car, CarUpgradeType type)
        {
            if (car == null) return 0;
            int nextLevel = GetUpgradeLevel(car, type) + 1;
            if (nextLevel > car.maxUpgradeLevel) return -1;
            int baseCost = type == CarUpgradeType.Engine ? car.engineUpgradeBaseCost :
                type == CarUpgradeType.Handling ? car.handlingUpgradeBaseCost : car.nitroUpgradeBaseCost;
            return Mathf.Max(0, baseCost * nextLevel);
        }

        public bool TryPurchaseUpgrade(CarDefinition car, CarUpgradeType type)
        {
            if (car == null || !IsCarUnlocked(car)) return false;
            int cost = GetUpgradeCost(car, type);
            if (cost < 0 || Coins < cost) return false;
            SetCoins(Coins - cost);
            PlayerPrefs.SetInt(UpgradeKey(car, type), GetUpgradeLevel(car, type) + 1);
            PlayerPrefs.Save();
            return true;
        }

        public float GetUpgradeMultiplier(CarDefinition car, CarUpgradeType type)
        {
            if (car == null) return 1f;
            int level = GetUpgradeLevel(car, type);
            float bonus = type == CarUpgradeType.Engine ? car.engineBonusPerLevel :
                type == CarUpgradeType.Handling ? car.handlingBonusPerLevel : car.nitroBonusPerLevel;
            return 1f + level * bonus;
        }

        public float GetBestTime(string trackId)
        {
            return PlayerPrefs.GetFloat(BestTimePrefix + trackId, 0f);
        }

        /// <returns>True only when this is a new record.</returns>
        public bool SetBestTimeIfFaster(string trackId, float timeSeconds)
        {
            float oldTime = GetBestTime(trackId);
            if (timeSeconds <= 0f || (oldTime > 0f && timeSeconds >= oldTime)) return false;
            PlayerPrefs.SetFloat(BestTimePrefix + trackId, timeSeconds);
            PlayerPrefs.Save();
            return true;
        }

        public int GetStars(int level) => PlayerPrefs.GetInt(StarsPrefix + level, 0);

        public void SetStarsIfHigher(int level, int stars)
        {
            if (stars <= GetStars(level)) return;
            PlayerPrefs.SetInt(StarsPrefix + level, Mathf.Clamp(stars, 0, 3));
            PlayerPrefs.Save();
        }

        public void ResetAllProgressForDevelopment()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            CoinsChanged?.Invoke(0);
        }

        private static string UpgradeKey(CarDefinition car, CarUpgradeType type)
        {
            return UpgradePrefix + car.id + "." + type;
        }

        private void SetCoins(int value)
        {
            int clamped = Mathf.Max(0, value);
            PlayerPrefs.SetInt(CoinsKey, clamped);
            PlayerPrefs.Save();
            CoinsChanged?.Invoke(clamped);
        }
    }
}
