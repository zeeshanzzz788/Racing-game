using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Progression;

namespace VelocityRush.UI
{
    /// <summary>Use one level button per campaign asset and call PlayLevel with its level number.</summary>
    public class LevelSelectController : MonoBehaviour
    {
        public bool IsUnlocked(CampaignLevelDefinition level)
        {
            if (level == null) return false;
            int stars = 0;
            if (GameManager.Instance != null)
                foreach (CampaignLevelDefinition candidate in GameManager.Instance.CampaignLevels)
                    if (candidate != null && candidate.levelNumber < level.levelNumber && ProgressionService.Instance != null)
                        stars += ProgressionService.Instance.GetStars(candidate.levelNumber);
            return stars >= level.starsRequiredToUnlock;
        }

        public void PlayLevel(int levelNumber)
        {
            CampaignLevelDefinition level = GameManager.Instance.GetCampaignLevel(levelNumber);
            if (IsUnlocked(level)) GameManager.Instance.StartCampaign(level);
        }

        public void Back() => GameManager.Instance.ReturnToMainMenu();
    }
}
