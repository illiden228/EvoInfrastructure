using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.GooglePlayGames.Leaderboards
{
    [Serializable]
    public sealed class GooglePlayGamesLeaderboardsOptions
    {
        public List<GooglePlayGamesLeaderboardEntry> entries = new();
    }
}
