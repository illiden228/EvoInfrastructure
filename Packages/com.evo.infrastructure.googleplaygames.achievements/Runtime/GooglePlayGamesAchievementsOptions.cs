using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.GooglePlayGames.Achievements
{
    [Serializable]
    public sealed class GooglePlayGamesAchievementsOptions
    {
        public List<GooglePlayGamesAchievementEntry> entries = new();
    }
}
