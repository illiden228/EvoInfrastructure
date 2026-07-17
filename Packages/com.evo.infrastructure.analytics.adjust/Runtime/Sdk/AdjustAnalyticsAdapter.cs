using System;
using System.Collections.Generic;
using System.Globalization;
using AdjustSdk;
using Evo.Infrastructure.Services.Analytics.Config;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Debug;
using VContainer;

namespace Evo.Infrastructure.Services.Analytics.Adjust
{
    public sealed class AdjustAnalyticsAdapter : IAnalyticsAdapter
    {
        private const string DEFAULT_ID = "adjust";
        private const string APPLOVIN_SOURCE = "applovin_max_sdk";
        private const string SOURCE = "Adjust Analytics Adapter";
        private readonly AdjustAnalyticsAdapterConfig _config;
        private readonly IAdjustSdkFacade _sdk;
        private readonly AnalyticsRuntimePlatform _runtimePlatform;
        private bool _isInitialized;
        private bool _isAvailable;
        private bool _sdkWarningLogged;
        private bool _purchaseVerificationWarningLogged;

        [Inject]
        public AdjustAnalyticsAdapter(IConfigService configs) : this(
            ResolveConfig(configs),
            new AdjustSdkFacade(),
            AnalyticsRuntimePlatformResolver.Resolve())
        {
        }

        internal AdjustAnalyticsAdapter(
            AdjustAnalyticsAdapterConfig config,
            IAdjustSdkFacade sdk,
            AnalyticsRuntimePlatform runtimePlatform)
        {
            _config = config;
            _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
            _runtimePlatform = runtimePlatform;
            AdapterId = _config?.ResolveAdapterId(DEFAULT_ID) ?? DEFAULT_ID;
            Initialize();
        }

        public string AdapterId { get; }
        public bool IsInitialized => _isInitialized;
        public bool Supports(AnalyticsEventType eventType) => _isAvailable &&
            (eventType == AnalyticsEventType.Purchase || eventType == AnalyticsEventType.Ad);

        public void Track(in AnalyticsDispatchEvent analyticsEvent)
        {
            if (!_isAvailable || string.IsNullOrWhiteSpace(analyticsEvent.EventKey))
            {
                return;
            }

            try
            {
                if (analyticsEvent.EventType == AnalyticsEventType.Purchase)
                {
                    TrackPurchase(analyticsEvent);
                }
                else if (analyticsEvent.EventType == AnalyticsEventType.Ad)
                {
                    TrackAdRevenue(analyticsEvent);
                }
            }
            catch (Exception ex)
            {
                WarnSdkOnce($"Adjust tracking failed: {ex.Message}");
            }
        }

        private void Initialize()
        {
            try
            {
                if (_config == null)
                {
                    WarnSdkOnce("Adjust config is missing; adapter is disabled.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_config.AppKey))
                {
                    WarnSdkOnce("Adjust app token is missing; adapter is disabled.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_config.PurchaseToken))
                {
                    WarnSdkOnce("Adjust purchase token is missing; adapter is disabled.");
                    return;
                }

                if (_runtimePlatform == AnalyticsRuntimePlatform.Editor && !_config.AllowEditorTracking)
                {
                    return;
                }

                var sdkConfig = new AdjustConfig(
                    _config.AppKey,
                    (AdjustEnvironment)(int)_config.Environment,
                    _config.LogLevel == EvoAdjustLogLevel.Suppress)
                {
                    LogLevel = (AdjustLogLevel)(int)_config.LogLevel,
                    IsSendingInBackgroundEnabled = false,
                    IsDeferredDeeplinkOpeningEnabled = true,
                    IsAdServicesEnabled = true,
                    IsIdfaReadingEnabled = true,
                    IsSkanAttributionEnabled = true
                };
                _sdk.InitSdk(sdkConfig);
                _isAvailable = true;
            }
            catch (Exception ex)
            {
                WarnSdkOnce($"Adjust initialization failed: {ex.Message}");
            }
            finally
            {
                _isInitialized = true;
            }
        }

        private void TrackPurchase(in AnalyticsDispatchEvent analyticsEvent)
        {
            if (_config == null || string.IsNullOrWhiteSpace(_config.PurchaseToken))
            {
                WarnSdkOnce("Adjust purchase token is missing; purchases are ignored.");
                return;
            }

            var purchase = analyticsEvent.PurchaseEventData;
            if (purchase.Revenue <= 0m || string.IsNullOrWhiteSpace(purchase.Currency))
            {
                return;
            }

            var sdkEvent = new AdjustEvent(_config.PurchaseToken);
            sdkEvent.SetRevenue(
                decimal.ToDouble(purchase.Revenue),
                purchase.Currency.Trim().ToUpperInvariant());
            sdkEvent.ProductId = Limit(purchase.ItemId);
            sdkEvent.TransactionId = Limit(purchase.TransactionId);
            if (!string.IsNullOrWhiteSpace(purchase.PurchaseToken))
            {
                sdkEvent.PurchaseToken = purchase.PurchaseToken;
            }

            AddParameters(sdkEvent.AddCallbackParameter, analyticsEvent.Parameters);
            if (_runtimePlatform == AnalyticsRuntimePlatform.IOS)
            {
                _sdk.VerifyAndTrackAppStorePurchase(sdkEvent, LogVerification);
                return;
            }

            if (_runtimePlatform == AnalyticsRuntimePlatform.Android)
            {
                if (!string.IsNullOrWhiteSpace(sdkEvent.ProductId) &&
                    !string.IsNullOrWhiteSpace(sdkEvent.PurchaseToken))
                {
                    _sdk.VerifyAndTrackPlayStorePurchase(sdkEvent, LogVerification);
                    return;
                }

                WarnPurchaseVerificationOnce(
                    "Google Play purchase verification is unavailable because ProductId or PurchaseToken is missing; " +
                    "tracking one unverified revenue event instead.");
            }

            _sdk.TrackEvent(sdkEvent);
        }

        private void TrackAdRevenue(in AnalyticsDispatchEvent analyticsEvent)
        {
            var ad = analyticsEvent.AdEventData;
            if (ad.Revenue <= 0d ||
                double.IsNaN(ad.Revenue) ||
                double.IsInfinity(ad.Revenue) ||
                string.IsNullOrWhiteSpace(ad.Currency))
            {
                return;
            }

            var platform = string.IsNullOrWhiteSpace(ad.Platform) ? "unknown" : ad.Platform.Trim();
            var source = string.Equals(platform, "applovin", StringComparison.OrdinalIgnoreCase)
                ? APPLOVIN_SOURCE
                : platform;
            var revenue = new AdjustAdRevenue(source);
            revenue.SetRevenue(ad.Revenue, ad.Currency.Trim().ToUpperInvariant());
            revenue.AdRevenueNetwork = Limit(ad.NetworkName);
            revenue.AdRevenueUnit = Limit(ad.UnitId);
            revenue.AdRevenuePlacement = Limit(ad.Placement);
            AddParameters(revenue.AddCallbackParameter, analyticsEvent.Parameters);
            _sdk.TrackAdRevenue(revenue);
        }

        private static AdjustAnalyticsAdapterConfig ResolveConfig(IConfigService configs)
        {
            if (configs != null &&
                configs.TryGet<AnalyticsAdapterCatalog>(out var catalog) &&
                catalog != null &&
                catalog.TryGet(out AdjustAnalyticsAdapterConfig config))
            {
                return config;
            }

            return null;
        }

        private static void AddParameters(Action<string, string> add, IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                return;
            }

            foreach (var pair in parameters)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
                {
                    var value = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
                    add(Limit(pair.Key, 128), Limit(value, 1024));
                }
            }
        }

        private static string Limit(string value, int max = 1024)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : value.Substring(0, Math.Min(value.Length, max));
        }

        private static void LogVerification(AdjustPurchaseVerificationResult result)
        {
            if (result == null)
            {
                return;
            }

            EvoDebug.Log(
                $"Purchase verification: {result.VerificationStatus}, {result.Code}, {result.Message}",
                SOURCE);
        }

        private void WarnSdkOnce(string message)
        {
            if (_sdkWarningLogged)
            {
                return;
            }

            _sdkWarningLogged = true;
            EvoDebug.LogWarning(message, SOURCE);
        }

        private void WarnPurchaseVerificationOnce(string message)
        {
            if (_purchaseVerificationWarningLogged)
            {
                return;
            }

            _purchaseVerificationWarningLogged = true;
#if FULL_LOG
            EvoDebug.LogWarning(message, SOURCE);
#else
            UnityEngine.Debug.LogWarning($"[{SOURCE}] {message}");
#endif
        }
    }
}
