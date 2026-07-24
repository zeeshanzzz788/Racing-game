using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;

namespace VelocityRush.UI
{
    /// <summary>Wire the MainMenu button OnClick events to these public methods.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject modesPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TrackDefinition defaultTrack;

        private void Start()
        {
            if (modesPanel != null) modesPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void Play() => PlayQuickRace();
        public void PlayQuickRace() => GameManager.Instance.StartQuickRace(defaultTrack);
        public void PlayEndless() => GameManager.Instance.StartEndless(defaultTrack);
        public void PlayTimeTrial() => GameManager.Instance.StartTimeTrial(defaultTrack);
        public void OpenGarage() => GameManager.Instance.ShowGarage();
        public void OpenCampaign() => GameManager.Instance.ShowLevelSelect();
        public void Quit() => GameManager.Instance.QuitGame();

        public void ToggleModes()
        {
            if (modesPanel != null) modesPanel.SetActive(!modesPanel.activeSelf);
        }

        public void ToggleSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
}
