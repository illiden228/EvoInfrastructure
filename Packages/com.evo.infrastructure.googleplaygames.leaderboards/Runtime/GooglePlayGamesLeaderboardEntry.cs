using System;

namespace Evo.Infrastructure.GooglePlayGames.Leaderboards
{
    [Serializable]
    public sealed class GooglePlayGamesLeaderboardEntry
    {
        public string logicalKey;
        public string googleId;
        public bool timeInSeconds;
        public double timeScoreMultiplier = 1000d;
    }
}
