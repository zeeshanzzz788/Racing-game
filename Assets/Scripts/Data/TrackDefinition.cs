using UnityEngine;

namespace VelocityRush.Data
{
    [CreateAssetMenu(fileName = "Track_", menuName = "Velocity Rush/Track Definition")]
    public class TrackDefinition : ScriptableObject
    {
        [Tooltip("Stable save key. Do not change after release.")]
        public string id = "desert_circuit";
        public string displayName = "Desert Circuit";
        [Tooltip("Scene name, without the .unity extension.")]
        public string sceneName = "DesertCircuit";
        public Sprite preview;
        [TextArea] public string description;
        [Min(1)] public int defaultLaps = 3;
        [Range(1, 5)] public int recommendedDifficulty = 1;
        public bool supportsEndless;
    }
}
