using VelocityRush.Data;

namespace VelocityRush.Core
{
    /// <summary>Small serializable-at-runtime record of the next race request.</summary>
    public class RaceSession
    {
        public GameMode Mode { get; private set; }
        public string TrackId { get; private set; }
        public int CampaignLevel { get; private set; }
        public int Laps { get; private set; }
        public int OpponentCount { get; private set; }

        public RaceSession(GameMode mode, string trackId, int laps, int opponentCount, int campaignLevel = 0)
        {
            Mode = mode;
            TrackId = trackId;
            Laps = laps;
            OpponentCount = opponentCount;
            CampaignLevel = campaignLevel;
        }
    }
}
