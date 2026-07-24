using UnityEngine;
using VelocityRush.AudioSystem;
using VelocityRush.Input;
using VelocityRush.Polish;

namespace VelocityRush.Core
{
    /// <summary>
    /// Ensures any scene can be opened directly during development without losing persistent
    /// services. Menu scenes may also contain these components; duplicate instances self-remove.
    /// </summary>
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsurePersistentServices()
        {
            if (GameManager.Instance != null) return;
            GameObject root = new GameObject("VelocityRushServices");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<GameManager>();
            root.AddComponent<InputManager>();
            root.AddComponent<AudioManager>();
            root.AddComponent<MobileGraphicsController>();
            root.AddComponent<CinematicTimeController>();
        }
    }
}
