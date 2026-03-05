using System;
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.Services.Config;
using _Project.Scripts.Infrastructure.Services.Debug;

namespace _Project.Scripts.Infrastructure.Services.Leaderboard
{
    public sealed class LeaderboardService : ILeaderboardService
    {
        private const string SOURCE = nameof(LeaderboardService);
        private readonly IReadOnlyList<ILeaderboardAdapter> _adapters;

        public LeaderboardService(
            IReadOnlyList<ILeaderboardAdapter> adapters,
            IConfigService configService = null)
        {
            _adapters = adapters ?? Array.Empty<ILeaderboardAdapter>();
        }

        public void Submit(in LeaderboardSubmitRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.LeaderboardKey))
            {
                EvoDebug.LogWarning("Leaderboard submit skipped: empty key.", SOURCE);
                return;
            }

            EvoDebug.Log(
                request.IsTimeSeconds
                    ? $"Leaderboard submit requested: key='{request.LeaderboardKey}', time={request.TimeSeconds:0.###}s."
                    : $"Leaderboard submit requested: key='{request.LeaderboardKey}', score={request.Score}.",
                SOURCE);

            if (_adapters.Count == 0)
            {
                EvoDebug.LogWarning($"Leaderboard submit skipped: no adapters for '{request.LeaderboardKey}'.", SOURCE);
                return;
            }

            var canonicalKey = request.LeaderboardKey;
            var resolvedIsTimeSeconds = request.IsTimeSeconds;
            var resolvedExtraData = request.ExtraData;
            var mergedParameters = request.Parameters;

            var handled = 0;
            for (var i = 0; i < _adapters.Count; i++)
            {
                var adapter = _adapters[i];
                if (adapter == null || !adapter.IsAvailable)
                {
                    continue;
                }

                if (!adapter.IsInitialized)
                {
                    EvoDebug.LogWarning($"Adapter '{adapter.AdapterId}' is not initialized.", SOURCE);
                    continue;
                }

                if (!adapter.Supports(resolvedIsTimeSeconds))
                {
                    continue;
                }

                try
                {
                    var mappedKey = canonicalKey;
                    var mappedParameters = mergedParameters;
                    var mappedRequest = BuildMappedRequest(
                        resolvedIsTimeSeconds,
                        mappedKey,
                        request,
                        mappedParameters,
                        resolvedExtraData);
                    adapter.Submit(mappedRequest);
                    EvoDebug.Log(
                        $"Adapter '{adapter.AdapterId}' handled leaderboard '{request.LeaderboardKey}' (mapped '{mappedKey}').",
                        SOURCE);
                    handled++;
                }
                catch (Exception ex)
                {
                    EvoDebug.LogError(
                        $"Adapter '{adapter.AdapterId}' failed to submit leaderboard '{request.LeaderboardKey}': {ex.Message}",
                        SOURCE);
                }
            }

            if (handled == 0)
            {
                EvoDebug.LogWarning($"No adapter handled leaderboard submit '{request.LeaderboardKey}'.", SOURCE);
            }
        }

        private static LeaderboardSubmitRequest BuildMappedRequest(
            bool isTimeSeconds,
            string leaderboardKey,
            in LeaderboardSubmitRequest source,
            IReadOnlyDictionary<string, object> parameters,
            string extraData)
        {
            if (isTimeSeconds)
            {
                return new LeaderboardSubmitRequest(leaderboardKey, source.TimeSeconds, parameters, extraData);
            }

            return new LeaderboardSubmitRequest(leaderboardKey, source.Score, parameters, extraData);
        }
    }
}
