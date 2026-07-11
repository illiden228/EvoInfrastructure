using System;
using System.Collections.Generic;
using System.Linq;

namespace Evo.Infrastructure.Services.Achievements
{
    public sealed class AchievementService : IAchievementService
    {
        private readonly IReadOnlyList<IAchievementAdapter> _adapters;

        public AchievementService(IEnumerable<IAchievementAdapter> adapters)
        {
            _adapters = (adapters ?? Array.Empty<IAchievementAdapter>())
                .OrderByDescending(adapter => adapter.Priority)
                .ToArray();
        }

        public bool Unlock(string key) => Execute(key, adapter => adapter.Unlock(key));
        public bool Reveal(string key) => Execute(key, adapter => adapter.Reveal(key));
        public bool Increment(string key, int steps) => steps > 0 && Execute(key, adapter => adapter.Increment(key, steps));
        public bool SetProgress(string key, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return false;
            return Execute(key, adapter => adapter.SetProgress(key, Math.Max(0d, Math.Min(100d, value))));
        }

        public bool ShowPlatformUi()
        {
            var adapter = _adapters.FirstOrDefault(IsUsable);
            if (adapter == null) return false;
            adapter.ShowPlatformUi();
            return true;
        }

        private bool Execute(string key, Action<IAchievementAdapter> action)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            var adapter = _adapters.FirstOrDefault(candidate => IsUsable(candidate) && candidate.Supports(key));
            if (adapter == null) return false;
            action(adapter);
            return true;
        }

        private static bool IsUsable(IAchievementAdapter adapter) =>
            adapter != null && adapter.IsInitialized && adapter.IsAvailable;
    }
}
