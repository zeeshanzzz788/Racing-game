using UnityEngine;

namespace VelocityRush.Data
{
    [CreateAssetMenu(fileName = "Level_", menuName = "Velocity Rush/Campaign Level")]
    public class CampaignLevelDefinition : ScriptableObject
    {
        [Range(1, 99)] public int levelNumber = 1;
        public TrackDefinition track;
        [Min(1)] public int laps = 1;
        [Min(1)] public int aiOpponents = 3;
        [Min(1f)] public float targetTimeSeconds = 90f;
        [Min(0)] public int coinReward = 100;
        [Range(0, 30)] public int starsRequiredToUnlock;
    }
}
