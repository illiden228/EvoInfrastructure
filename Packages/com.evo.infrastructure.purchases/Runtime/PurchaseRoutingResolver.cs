using System;
using System.Collections.Generic;
using System.Linq;
using Evo.Infrastructure.Services.PlatformInfo.Config;

namespace Evo.Infrastructure.Services.Purchases
{
    public static class PurchaseRoutingResolver
    {
        public static IPurchaseAdapterFactory SelectFactory(
            PurchaseRoutingConfig routing,
            IEnumerable<IPurchaseAdapterFactory> factories,
            PlatformCatalog platformCatalog,
            bool isEditor)
        {
            if (!PurchasePlatformIdUtility.TryGetCurrentPlatformId(platformCatalog, out var platformId))
            {
                return null;
            }

            var candidates = (routing?.Adapters ?? Array.Empty<PurchaseAdapterBinding>())
                .Where(binding => binding != null &&
                                  binding.Enabled &&
                                  binding.EditorMock == isEditor &&
                                  PurchasePlatformIdUtility.Matches(binding.PlatformIds, platformId))
                .Join(
                    factories ?? Array.Empty<IPurchaseAdapterFactory>(),
                    binding => binding.AdapterId,
                    factory => factory.AdapterId,
                    (binding, factory) => (binding.Priority, Factory: factory),
                    StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(candidate => candidate.Priority)
                .ToArray();
            if (candidates.Length == 0 ||
                candidates.Length > 1 && candidates[0].Priority == candidates[1].Priority)
            {
                return null;
            }

            return candidates[0].Factory;
        }
    }
}
