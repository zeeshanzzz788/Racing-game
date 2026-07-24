using UnityEngine;

namespace VelocityRush.AudioSystem
{
    /// <summary>Persistent music/effects volume facade. Wire an AudioMixer in production.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        [SerializeField] private AudioSource musicSource;
        private const string MusicKey = "vr.audio.music";
        private const string SfxKey = "vr.audio.sfx";
        public float MusicVolume => PlayerPrefs.GetFloat(MusicKey, .8f);
        public float SfxVolume => PlayerPrefs.GetFloat(SfxKey, .9f);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetMusicVolume(MusicVolume);
        }

        public void SetMusicVolume(float value)
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, value);
            if (musicSource != null) musicSource.volume = value;
        }

        public void SetSfxVolume(float value)
        {
            PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        }
    }
}
