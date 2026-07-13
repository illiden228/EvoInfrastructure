using System;
using System.Collections.Generic;
using Evo.Infrastructure.Services.PlatformInfo.Config;

namespace Evo.Infrastructure.Services.Purchases
{
    internal static class PurchasePlatformIdUtility
    {
        public static string Normalize(string platformId) => platformId?.Trim() ?? string.Empty;

        public static bool Matches(IReadOnlyList<string> platformIds, string platformId)
        {
            platformId = Normalize(platformId);
            if (platformId.Length == 0 || platformIds == null)
            {
                return false;
            }

            for (var index = 0; index < platformIds.Count; index++)
            {
                if (string.Equals(Normalize(platformIds[index]), platformId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static int CountDistinct(IReadOnlyList<string> platformIds)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (platformIds == null)
            {
                return 0;
            }

            for (var index = 0; index < platformIds.Count; index++)
            {
                var platformId = Normalize(platformIds[index]);
                if (platformId.Length > 0)
                {
                    ids.Add(platformId);
                }
            }

            return ids.Count;
        }

        public static bool TryGetCurrentPlatformId(PlatformCatalog catalog, out string platformId)
        {
            platformId = Normalize(catalog?.CurrentPlatformId);
            if (platformId.Length == 0 || catalog?.Entries == null)
            {
                return false;
            }

            for (var index = 0; index < catalog.Entries.Count; index++)
            {
                var entry = catalog.Entries[index];
                if (entry != null && string.Equals(
                        Normalize(entry.PlatformId),
                        platformId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
