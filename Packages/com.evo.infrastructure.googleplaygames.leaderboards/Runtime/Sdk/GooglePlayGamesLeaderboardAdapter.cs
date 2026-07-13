using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Debug;
using Evo.Infrastructure.Services.Leaderboard;
using GooglePlayGames;

namespace Evo.Infrastructure.GooglePlayGames.Leaderboards
{
    public sealed class GooglePlayGamesLeaderboardAdapter : ILeaderboardAdapter
    {
        private const string Source = nameof(GooglePlayGamesLeaderboardAdapter);
        private readonly IGooglePlayGamesSession _session;
        private readonly Dictionary<string, GooglePlayGamesLeaderboardEntry> _entries = new(StringComparer.Ordinal);
        private bool _sdkExceptionLogged;

        public GooglePlayGamesLeaderboardAdapter(IGooglePlayGamesSession session, GooglePlayGamesLeaderboardsOptions options)
        {
            _session = session;
            if (options?.entries == null) return;
            foreach (var entry in options.entries)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.logicalKey) && !string.IsNullOrWhiteSpace(entry.googleId))
                    _entries[entry.logicalKey] = entry;
        }

        public string AdapterId => "google-play-games";
        public int Priority => 100;
        public bool IsInitialized => _session.IsInitialized;
        public bool IsAvailable => _session.IsAuthenticated;
        public bool Supports(bool isTimeSeconds) => true;

        public void Submit(in LeaderboardSubmitRequest request)
        {
            if (!IsAvailable || !_entries.TryGetValue(request.LeaderboardKey, out var entry)) return;
            if (entry.timeInSeconds != request.IsTimeSeconds) return;
            if (request.IsTimeSeconds && (float.IsNaN(request.TimeSeconds) || float.IsInfinity(request.TimeSeconds))) return;
            try
            {
                var score = request.IsTimeSeconds
                    ? SafeTimeScore(request.TimeSeconds, entry.timeScoreMultiplier)
                    : request.Score;
                PlayGamesPlatform.Instance.ReportScore(score, entry.googleId, _ => { });
            }
            catch (Exception exception)
            {
                LogSdkExceptionOnce(exception, "submit score");
            }
        }

        private static long SafeTimeScore(float seconds, double multiplier)
        {
            if (seconds < 0 || double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier <= 0) return 0;
            var value = seconds * multiplier;
            return value >= long.MaxValue ? long.MaxValue : Convert.ToInt64(value);
        }

        private void LogSdkExceptionOnce(Exception exception, string operation)
        {
            if (_sdkExceptionLogged) return;
            _sdkExceptionLogged = true;
            EvoDebug.LogWarning($"Google Play Games failed to {operation}: {exception.Message}", Source);
        }
    }
}
