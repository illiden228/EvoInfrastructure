using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.Debug;
using Evo.Infrastructure.Services.Achievements;
using GooglePlayGames;

namespace Evo.Infrastructure.GooglePlayGames.Achievements
{
    public sealed class GooglePlayGamesAchievementAdapter : IAchievementAdapter
    {
        private const string Source = nameof(GooglePlayGamesAchievementAdapter);
        private readonly IGooglePlayGamesSession _session;
        private readonly Dictionary<string, string> _ids = new(StringComparer.Ordinal);
        private bool _sdkExceptionLogged;

        public GooglePlayGamesAchievementAdapter(IGooglePlayGamesSession session, GooglePlayGamesAchievementsOptions options)
        {
            _session = session;
            if (options?.entries == null) return;
            foreach (var entry in options.entries)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.logicalKey) && !string.IsNullOrWhiteSpace(entry.googleId))
                    _ids[entry.logicalKey] = entry.googleId;
        }

        public string AdapterId => "google-play-games";
        public int Priority => 100;
        public bool IsInitialized => _session.IsInitialized;
        public bool IsAvailable => _session.IsAuthenticated;
        public bool Supports(string key) => !string.IsNullOrWhiteSpace(key) && _ids.ContainsKey(key);
        public void Unlock(string key) => Invoke(key, id => PlayGamesPlatform.Instance.UnlockAchievement(id, _ => { }));
        public void Reveal(string key) => Invoke(key, id => PlayGamesPlatform.Instance.RevealAchievement(id, _ => { }));
        public void Increment(string key, int steps) => Invoke(key, id => PlayGamesPlatform.Instance.IncrementAchievement(id, steps, _ => { }));
        public void SetProgress(string key, double progress)
        {
            if (double.IsNaN(progress) || double.IsInfinity(progress)) return;
            Invoke(key, id => PlayGamesPlatform.Instance.ReportProgress(id, progress, _ => { }));
        }

        public void ShowPlatformUi()
        {
            if (!IsAvailable) return;
            try { PlayGamesPlatform.Instance.ShowAchievementsUI(); }
            catch (Exception exception) { LogSdkExceptionOnce(exception, "show achievements UI"); }
        }

        private void Invoke(string key, Action<string> action)
        {
            if (!IsAvailable || !_ids.TryGetValue(key, out var id)) return;
            try { action(id); }
            catch (Exception exception) { LogSdkExceptionOnce(exception, "update achievement"); }
        }

        private void LogSdkExceptionOnce(Exception exception, string operation)
        {
            if (_sdkExceptionLogged) return;
            _sdkExceptionLogged = true;
            EvoDebug.LogWarning($"Google Play Games failed to {operation}: {exception.Message}", Source);
        }
    }
}
