using UnityEngine;
using UnityEngine.UI;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Progression;

namespace VelocityRush.UI
{
    public class GarageController : MonoBehaviour
    {
        [SerializeField] private Image carPreview;
        [SerializeField] private Text carName;
        [SerializeField] private Text description;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text actionText;
        [SerializeField] private Slider speedBar;
        [SerializeField] private Slider accelerationBar;
        [SerializeField] private Slider handlingBar;
        private int index;

        private void OnEnable()
        {
            if (ProgressionService.Instance != null) ProgressionService.Instance.CoinsChanged += OnCoinsChanged;
            CarDefinition selected = GameManager.Instance == null ? null : GameManager.Instance.SelectedCar;
            if (selected != null)
            {
                for (int i = 0; i < GameManager.Instance.Cars.Count; i++)
                    if (GameManager.Instance.Cars[i] == selected) index = i;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (ProgressionService.Instance != null) ProgressionService.Instance.CoinsChanged -= OnCoinsChanged;
        }

        public void Next() { index++; Refresh(); }
        public void Previous() { index--; Refresh(); }

        public void SelectOrUnlock()
        {
            CarDefinition car = CurrentCar();
            if (car == null || ProgressionService.Instance == null) return;
            if (!ProgressionService.Instance.IsCarUnlocked(car))
                ProgressionService.Instance.TryUnlockCar(car);
            if (ProgressionService.Instance.IsCarUnlocked(car)) GameManager.Instance.SelectCar(car);
            Refresh();
        }

        /// <summary>Wire an upgrade button with 0=Engine, 1=Handling, 2=Nitro.</summary>
        public void PurchaseUpgrade(int upgradeType)
        {
            CarDefinition car = CurrentCar();
            if (car == null || ProgressionService.Instance == null) return;
            ProgressionService.Instance.TryPurchaseUpgrade(car, (CarUpgradeType)Mathf.Clamp(upgradeType, 0, 2));
            Refresh();
        }

        public void Back() => GameManager.Instance.ReturnToMainMenu();

        private CarDefinition CurrentCar()
        {
            if (GameManager.Instance == null || GameManager.Instance.Cars.Count == 0) return null;
            int count = GameManager.Instance.Cars.Count;
            index = (index % count + count) % count;
            return GameManager.Instance.Cars[index];
        }

        private void Refresh()
        {
            CarDefinition car = CurrentCar();
            if (car == null) return;
            bool unlocked = ProgressionService.Instance != null && ProgressionService.Instance.IsCarUnlocked(car);
            if (carPreview != null) carPreview.sprite = car.garageIcon;
            if (carName != null) carName.text = car.displayName;
            if (description != null) description.text = car.description;
            if (speedBar != null) speedBar.value = car.SpeedRating;
            if (accelerationBar != null) accelerationBar.value = car.AccelerationRating;
            if (handlingBar != null) handlingBar.value = car.HandlingRating;
            if (coinsText != null && ProgressionService.Instance != null) coinsText.text = ProgressionService.Instance.Coins.ToString();
            if (actionText != null)
                actionText.text = unlocked ? (GameManager.Instance.SelectedCar == car ? "SELECTED" : "SELECT") : "UNLOCK " + car.unlockCost;
        }

        private void OnCoinsChanged(int unused) => Refresh();
    }
}
