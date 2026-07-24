using UnityEngine;
using VelocityRush.AudioSystem;
using VelocityRush.Input;

namespace VelocityRush.UI
{
    public class SettingsController : MonoBehaviour
    {
        public void SetMusicVolume(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
        }

        public void SetSfxVolume(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(value);
        }

        public void SetSteeringMode(int mode)
        {
            if (InputManager.Instance != null) InputManager.Instance.SetSteeringMode(mode);
        }

        public void SetQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1), true);
        }
    }
}
