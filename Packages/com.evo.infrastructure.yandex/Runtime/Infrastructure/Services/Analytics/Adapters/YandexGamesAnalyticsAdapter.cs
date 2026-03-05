using System;
using System.Collections.Generic;
#if YandexGamesPlatform_yg
using YG;
#endif
using _Project.Scripts.Infrastructure.Services.Analytics.Config;
using _Project.Scripts.Infrastructure.Services.Config;
using _Project.Scripts.Infrastructure.Services.PlatformInfo;
using _Project.Scripts.Infrastructure.Services.PlatformInfo.Config;

namespace _Project.Scripts.Infrastructure.Services.Analytics.Adapters
{
    public sealed class YandexGamesAnalyticsAdapter : IAnalyticsAdapter
    {
        private const string DEFAULT_ADAPTER_ID = "yandex";

        private readonly bool _useDefaultPurchaseEvent;
        private readonly bool _useDefaultAdEvent;
        private readonly IReadOnlyList<string> _purchaseDefaultPlatforms;
        private readonly IReadOnlyList<string> _adDefaultPlatforms;
        private readonly string _platformId;
        private readonly string _adapterId;

        public YandexGamesAnalyticsAdapter(
            IConfigService configService = null,
            IPlatformInfoService platformInfoService = null)
        {
            if (TryGetConfig(configService, out var config))
            {
                _useDefaultPurchaseEvent = config.UseDefaultPurchaseEvent;
                _useDefaultAdEvent = config.UseDefaultAdEvent;
                _purchaseDefaultPlatforms = config.PurchaseDefaultPlatforms;
                _adDefaultPlatforms = config.AdDefaultPlatforms;
                _adapterId = config.ResolveAdapterId(DEFAULT_ADAPTER_ID);
            }
            else
            {
                _adapterId = DEFAULT_ADAPTER_ID;
            }

            _platformId = ResolvePlatformId(configService);
        }

        public string AdapterId => _adapterId;
        public bool IsInitialized
        {
            get
            {
#if YandexGamesPlatform_yg
                return YG2.isSDKEnabled;
#else
                return false;
#endif
            }
        }

        public bool Supports(AnalyticsEventType eventType)
        {
            return true;
        }

        public void Track(in AnalyticsDispatchEvent analyticsEvent)
        {
            if (!IsInitialized)
            {
                return;
            }

            if (ShouldUseDefaultEvent(analyticsEvent.EventType))
            {
                return;
            }

            var payload = BuildPayload(analyticsEvent);
            if (payload != null && payload.Count > 0)
            {
#if YandexGamesPlatform_yg
                YG2.MetricaSend(analyticsEvent.EventKey, payload);
#endif
                return;
            }

#if YandexGamesPlatform_yg
            YG2.MetricaSend(analyticsEvent.EventKey);
#endif
        }

        private static Dictionary<string, object> BuildPayload(in AnalyticsDispatchEvent analyticsEvent)
        {
            var payload = CopyParameters(analyticsEvent.Parameters);

            switch (analyticsEvent.EventType)
            {
                case AnalyticsEventType.Purchase:
                    AddPurchasePayload(payload, analyticsEvent.PurchaseEventData);
                    break;
                case AnalyticsEventType.Ad:
                    AddAdPayload(payload, analyticsEvent.AdEventData);
                    break;
            }

            return payload;
        }

        private bool ShouldUseDefaultEvent(AnalyticsEventType eventType)
        {
            switch (eventType)
            {
                case AnalyticsEventType.Purchase:
                    if (!_useDefaultPurchaseEvent)
                    {
                        return false;
                    }

                    return IsPlatformEnabled(_purchaseDefaultPlatforms);
                case AnalyticsEventType.Ad:
                    if (!_useDefaultAdEvent)
                    {
                        return false;
                    }

                    return IsPlatformEnabled(_adDefaultPlatforms);
                default:
                    return false;
            }
        }

        private bool IsPlatformEnabled(IReadOnlyList<string> platforms)
        {
            if (platforms == null || platforms.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < platforms.Count; i++)
            {
                if (string.Equals(platforms[i], _platformId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ResolvePlatformId(IConfigService configService)
        {
            if (configService != null && configService.TryGet<PlatformCatalog>(out var platformCatalog) && platformCatalog != null)
            {
                return platformCatalog.CurrentPlatformId;
            }

            return null;
        }

        private static bool TryGetConfig(IConfigService configService, out YandexGamesAnalyticsAdapterConfig config)
        {
            if (configService != null &&
                configService.TryGet<AnalyticsAdapterCatalog>(out var catalog) &&
                catalog != null &&
                catalog.TryGet(out config))
            {
                return true;
            }

            config = null;
            return false;
        }

        private static Dictionary<string, object> CopyParameters(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            var copy = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in parameters)
            {
                copy[pair.Key] = pair.Value;
            }

            return copy;
        }

        private static void AddPurchasePayload(Dictionary<string, object> payload, PurchaseEventData purchase)
        {
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_CURRENCY, purchase.Currency);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_REVENUE, purchase.Revenue);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_TRANSACTION_ID, purchase.TransactionId);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_ITEM_ID, purchase.ItemId);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_PURCHASE_TOKEN, purchase.PurchaseToken);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_RECEIPT, purchase.ReceiptJson);
        }

        private static void AddAdPayload(Dictionary<string, object> payload, AdEventData ad)
        {
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_AD_PLATFORM, ad.Platform);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_NETWORK, ad.NetworkName);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_UNIT_ID, ad.UnitId);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_FORMAT, ad.Format);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_PLACEMENT, ad.Placement);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_NETWORK_PLACEMENT, ad.NetworkPlacement);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_CURRENCY, ad.Currency);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_REVENUE, ad.Revenue);
            AddIfNotEmpty(payload, AnalyticsEventKeys.PARAM_COUNTRY_CODE, ad.CountryCode);
        }

        private static void AddIfNotEmpty(Dictionary<string, object> payload, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            payload[key] = value;
        }

        private static void AddIfNotEmpty(Dictionary<string, object> payload, string key, decimal value)
        {
            if (value <= 0)
            {
                return;
            }

            payload[key] = value;
        }

        private static void AddIfNotEmpty(Dictionary<string, object> payload, string key, double value)
        {
            if (value <= 0)
            {
                return;
            }

            payload[key] = value;
        }
    }
}
