using System;
using System.Collections.Generic;
using System.Linq;
namespace _Project.Scripts.Infrastructure.Services.Ads
{
    internal sealed class AdsAdapterSelector
    {
        private readonly List<IAdsAdapterFactory> _factories;
        private readonly Dictionary<string, IAdsAdapter> _created;
        private readonly List<string> _configuredOrder;

        public AdsAdapterSelector(IReadOnlyList<IAdsAdapterFactory> factories, IReadOnlyList<string> configuredOrder)
        {
            _factories = factories?
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.AdapterId))
                .ToList() ?? new List<IAdsAdapterFactory>();
            _created = new Dictionary<string, IAdsAdapter>(StringComparer.OrdinalIgnoreCase);
            _configuredOrder = configuredOrder != null
                ? new List<string>(configuredOrder)
                : new List<string>();
        }

        public bool TryGetReady(
            AdType adType,
            string placementId,
            HashSet<IAdsAdapter> excluded,
            out IAdsAdapter adapter)
        {
            var ordered = EnumerateOrderedAdapters();
            for (var i = 0; i < ordered.Count; i++)
            {
                var candidate = ordered[i];
                if (excluded != null && excluded.Contains(candidate))
                {
                    continue;
                }

                if (candidate.IsReady(adType, placementId))
                {
                    adapter = candidate;
                    return true;
                }
            }

            adapter = null;
            return false;
        }

        public IReadOnlyList<IAdsAdapter> EnumerateOrderedAdapters()
        {
            if (_configuredOrder.Count == 0)
            {
                return CreateAllFactoriesInOrder(_factories);
            }

            var result = new List<IAdsAdapter>(_factories.Count);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < _configuredOrder.Count; i++)
            {
                var adapterId = _configuredOrder[i];
                TryCreate(adapterId, result, used);
            }

            AppendRemainingFactories(result, used);

            return result;
        }

        private IReadOnlyList<IAdsAdapter> CreateAllFactoriesInOrder(IReadOnlyList<IAdsAdapterFactory> factories)
        {
            var result = new List<IAdsAdapter>(factories.Count);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < factories.Count; i++)
            {
                var id = factories[i].AdapterId;
                TryCreate(id, result, used);
            }

            return result;
        }

        private void AppendRemainingFactories(List<IAdsAdapter> result, HashSet<string> used)
        {
            for (var i = 0; i < _factories.Count; i++)
            {
                var id = _factories[i].AdapterId;
                if (used.Contains(id))
                {
                    continue;
                }

                TryCreate(id, result, used);
            }
        }

        private void TryCreate(string adapterId, List<IAdsAdapter> result, HashSet<string> used)
        {
            if (string.IsNullOrWhiteSpace(adapterId))
            {
                return;
            }

            if (used.Contains(adapterId))
            {
                return;
            }

            if (_created.TryGetValue(adapterId, out var existing))
            {
                result.Add(existing);
                used.Add(adapterId);
                return;
            }

            for (var i = 0; i < _factories.Count; i++)
            {
                var factory = _factories[i];
                if (!string.Equals(factory.AdapterId, adapterId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var created = factory.Create();
                if (created != null)
                {
                    _created[adapterId] = created;
                    result.Add(created);
                    used.Add(adapterId);
                }
                return;
            }
        }
    }
}
