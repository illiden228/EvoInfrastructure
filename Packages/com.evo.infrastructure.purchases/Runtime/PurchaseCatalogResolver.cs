using System;
using System.Collections.Generic;
using System.Linq;

namespace Evo.Infrastructure.Services.Purchases
{
    public static class PurchaseCatalogResolver
    {
        public static IReadOnlyList<PurchaseProduct> Resolve(
            PurchaseCatalogConfig catalog,
            string adapterId,
            string platformId)
        {
            platformId = PurchasePlatformIdUtility.Normalize(platformId);
            if (catalog?.Products == null || platformId.Length == 0)
            {
                return Array.Empty<PurchaseProduct>();
            }

            var result = new List<PurchaseProduct>(catalog.Products.Count);
            foreach (var definition in catalog.Products)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                {
                    continue;
                }

                var target = definition.Overrides?
                    .Where(item => item != null && Matches(item, adapterId, platformId))
                    .OrderByDescending(item => Specificity(item, adapterId))
                    .ThenByDescending(item => item.Priority)
                    .FirstOrDefault();
                var enabled = target?.OverrideEnabled == true ? target.Enabled : definition.Enabled;
                var type = target?.OverrideProductType == true ? target.ProductType : definition.ProductType;
                var grants = target?.OverrideGrants == true ? target.Grants : definition.Grants;
                var storeId = !string.IsNullOrWhiteSpace(target?.StoreProductId)
                    ? target.StoreProductId
                    : definition.DefaultStoreProductId;
                result.Add(new PurchaseProduct(
                    definition.Id,
                    storeId,
                    definition.FulfillmentKey,
                    type,
                    enabled,
                    grants));
            }

            return result;
        }

        private static bool Matches(PurchaseTargetOverride item, string adapterId, string platformId)
        {
            var adapterMatches = string.IsNullOrWhiteSpace(item.AdapterId) ||
                                 string.Equals(item.AdapterId, adapterId, StringComparison.OrdinalIgnoreCase);
            return adapterMatches && PurchasePlatformIdUtility.Matches(item.PlatformIds, platformId);
        }

        private static int Specificity(PurchaseTargetOverride item, string adapterId)
        {
            var adapterScore = string.Equals(
                item.AdapterId,
                adapterId,
                StringComparison.OrdinalIgnoreCase)
                ? 100
                : 0;
            var platformScore = 50 - PurchasePlatformIdUtility.CountDistinct(item.PlatformIds);
            return adapterScore + platformScore;
        }
    }
}
