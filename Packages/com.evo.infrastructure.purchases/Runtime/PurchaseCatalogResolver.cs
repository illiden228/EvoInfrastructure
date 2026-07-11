using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    public static class PurchaseCatalogResolver
    {
        public static IReadOnlyList<PurchaseProduct> Resolve(
            PurchaseCatalogConfig catalog,
            string adapterId,
            RuntimePlatform platform)
        {
            if (catalog?.Products == null)
            {
                return Array.Empty<PurchaseProduct>();
            }

            var mask = ToMask(platform);
            var result = new List<PurchaseProduct>(catalog.Products.Count);
            foreach (var definition in catalog.Products)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                {
                    continue;
                }

                var target = definition.Overrides?
                    .Where(item => item != null && Matches(item, adapterId, mask))
                    .OrderByDescending(item => Specificity(item, adapterId, mask))
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

        public static PurchasePlatformMask CurrentPlatform => ToMask(Application.platform);

        public static PurchasePlatformMask ToMask(RuntimePlatform platform) => platform switch
        {
            RuntimePlatform.Android => PurchasePlatformMask.Android,
            RuntimePlatform.IPhonePlayer => PurchasePlatformMask.IOS,
            RuntimePlatform.WebGLPlayer => PurchasePlatformMask.WebGL,
            RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor =>
                Application.isEditor ? PurchasePlatformMask.Editor : PurchasePlatformMask.Windows,
            RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor =>
                Application.isEditor ? PurchasePlatformMask.Editor : PurchasePlatformMask.MacOS,
            RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor =>
                Application.isEditor ? PurchasePlatformMask.Editor : PurchasePlatformMask.Linux,
            _ => Application.isEditor ? PurchasePlatformMask.Editor : PurchasePlatformMask.None
        };

        private static bool Matches(PurchaseTargetOverride item, string adapterId, PurchasePlatformMask platform)
        {
            var adapterMatches = string.IsNullOrWhiteSpace(item.AdapterId) ||
                                 string.Equals(item.AdapterId, adapterId, StringComparison.OrdinalIgnoreCase);
            return adapterMatches && (item.Platforms & platform) != 0;
        }

        private static int Specificity(
            PurchaseTargetOverride item,
            string adapterId,
            PurchasePlatformMask platform)
        {
            var adapterScore = string.Equals(
                item.AdapterId,
                adapterId,
                StringComparison.OrdinalIgnoreCase)
                ? 100
                : 0;
            var platformScore = item.Platforms == platform
                ? 50
                : 32 - CountBits((int)item.Platforms);
            return adapterScore + platformScore;
        }

        private static int CountBits(int value)
        {
            var bits = unchecked((uint)value);
            var count = 0;
            while (bits != 0)
            {
                count += (int)(bits & 1u);
                bits >>= 1;
            }

            return count;
        }
    }
}
