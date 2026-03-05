using System.Collections.Generic;

namespace _Project.Scripts.Infrastructure.Services.Leaderboard
{
    public readonly struct LeaderboardSubmitRequest
    {
        public readonly string LeaderboardKey;
        public readonly bool IsTimeSeconds;
        public readonly int Score;
        public readonly float TimeSeconds;
        public readonly string ExtraData;
        public readonly IReadOnlyDictionary<string, object> Parameters;

        public LeaderboardSubmitRequest(
            string leaderboardKey,
            int score,
            IReadOnlyDictionary<string, object> parameters = null,
            string extraData = null)
        {
            LeaderboardKey = leaderboardKey;
            IsTimeSeconds = false;
            Score = score;
            TimeSeconds = 0f;
            ExtraData = extraData;
            Parameters = parameters;
        }

        public LeaderboardSubmitRequest(
            string leaderboardKey,
            float timeSeconds,
            IReadOnlyDictionary<string, object> parameters = null,
            string extraData = null)
        {
            LeaderboardKey = leaderboardKey;
            IsTimeSeconds = true;
            Score = 0;
            TimeSeconds = timeSeconds;
            ExtraData = extraData;
            Parameters = parameters;
        }
    }
}
