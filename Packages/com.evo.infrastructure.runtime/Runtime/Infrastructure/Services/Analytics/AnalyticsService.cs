using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using _Project.Scripts.Infrastructure.Services.Analytics.Config;
using _Project.Scripts.Infrastructure.Services.Analytics.Mapping;
using _Project.Scripts.Infrastructure.Services.Config;
using _Project.Scripts.Infrastructure.Services.Debug;
using _Project.Scripts.Infrastructure.Services.PlatformInfo;
using _Project.Scripts.Infrastructure.Services.PlatformInfo.Config;

namespace _Project.Scripts.Infrastructure.Services.Analytics
{
    public sealed class AnalyticsService : IAnalyticsService, IAnalyticsInitialization
    {
        private const string SOURCE = nameof(AnalyticsService);

        private readonly IReadOnlyList<IAnalyticsAdapter> _allAdapters;
        private readonly IReadOnlyList<IAnalyticsAdapter> _activeAdapters;
        private readonly AnalyticsConfig _analyticsConfig;
        private readonly AnalyticsEventMapper _eventMapper;
        private readonly AnalyticsRuntimePlatform _runtimePlatform;
        private readonly string _platformId;

        public AnalyticsService(
            IReadOnlyList<IAnalyticsAdapter> adapters,
            IConfigService configService = null,
            IPlatformInfoService platformInfoService = null)
        {
            _allAdapters = adapters ?? Array.Empty<IAnalyticsAdapter>();
            _analyticsConfig = ResolveAnalyticsConfig(configService);
            _eventMapper = ResolveEventMapper(configService);
            _runtimePlatform = AnalyticsRuntimePlatformResolver.Resolve(platformInfoService);
            _platformId = ResolvePlatformId(configService);
            var bindings = _analyticsConfig != null
                ? _analyticsConfig.ResolveBindingsForPlatform(_platformId)
                : null;
            _activeAdapters = BuildActiveAdapters(_allAdapters, bindings);
        }

        public void TrackCustom(string eventKey, IReadOnlyDictionary<string, object> parameters = null)
        {
            DispatchEvent(new AnalyticsDispatchEvent(eventKey, parameters));
        }

        public void TrackPurchase(PurchaseEventData purchaseEventData, IReadOnlyDictionary<string, object> parameters = null)
        {
            DispatchEvent(new AnalyticsDispatchEvent(purchaseEventData, parameters));
        }

        public void TrackAd(AdEventData adEventData, IReadOnlyDictionary<string, object> parameters = null)
        {
            DispatchEvent(new AnalyticsDispatchEvent(adEventData, parameters));
        }

        private void DispatchEvent(in AnalyticsDispatchEvent analyticsEvent)
        {
            if (_analyticsConfig != null && !_analyticsConfig.AnalyticsEnabled)
            {
                return;
            }

            EvoDebug.Log(
                $"Track {analyticsEvent.EventType} '{analyticsEvent.EventKey}' (params: {analyticsEvent.Parameters?.Count ?? 0}).",
                SOURCE);

            if (_activeAdapters.Count == 0)
            {
                LogSkip($"No analytics adapters registered for event '{analyticsEvent.EventKey}'.");
                return;
            }

            var handledCount = 0;
            for (var i = 0; i < _activeAdapters.Count; i++)
            {
                var adapter = _activeAdapters[i];
                if (adapter == null)
                {
                    continue;
                }

                if (!adapter.IsInitialized)
                {
                    LogSkip($"Adapter '{adapter.AdapterId}' is not initialized.");
                    continue;
                }

                if (!adapter.Supports(analyticsEvent.EventType))
                {
                    continue;
                }

                try
                {
                    var mappedEvent = MapEventForAdapter(analyticsEvent, adapter.AdapterId);
                    adapter.Track(mappedEvent);
                    EvoDebug.Log(
                        $"Adapter '{adapter.AdapterId}' handled '{analyticsEvent.EventKey}'.",
                        SOURCE);
                    handledCount++;
                }
                catch (Exception ex)
                {
                    EvoDebug.LogError($"Adapter '{adapter.AdapterId}' failed to send event '{analyticsEvent.EventKey}': {ex.Message}", SOURCE);
                }
            }

            if (handledCount == 0)
            {
                LogSkip($"No adapter handled analytics event '{analyticsEvent.EventKey}' ({analyticsEvent.EventType}).");
            }
        }

        private AnalyticsDispatchEvent MapEventForAdapter(in AnalyticsDispatchEvent analyticsEvent, string adapterId)
        {
            if (_eventMapper == null)
            {
                return analyticsEvent;
            }

            var mappedEventKey = _eventMapper.ResolveEventKey(analyticsEvent.EventKey, adapterId, _platformId);
            var mappedParameters = _eventMapper.MapParameters(
                analyticsEvent.EventKey,
                adapterId,
                _platformId,
                analyticsEvent.Parameters);

            switch (analyticsEvent.EventType)
            {
                case AnalyticsEventType.Custom:
                    return new AnalyticsDispatchEvent(mappedEventKey, mappedParameters);
                case AnalyticsEventType.Purchase:
                    var purchase = analyticsEvent.PurchaseEventData;
                    var mappedPurchase = new PurchaseEventData(
                        mappedEventKey,
                        purchase.Currency,
                        purchase.Revenue,
                        purchase.TransactionId,
                        purchase.ItemId,
                        purchase.PurchaseToken,
                        purchase.ReceiptJson);
                    return new AnalyticsDispatchEvent(mappedPurchase, mappedParameters);
                case AnalyticsEventType.Ad:
                    var ad = analyticsEvent.AdEventData;
                    var mappedAd = new AdEventData(
                        mappedEventKey,
                        ad.Platform,
                        ad.NetworkName,
                        ad.UnitId,
                        ad.Format,
                        ad.Placement,
                        ad.NetworkPlacement,
                        ad.Currency,
                        ad.Revenue,
                        ad.CountryCode);
                    return new AnalyticsDispatchEvent(mappedAd, mappedParameters);
                default:
                    return analyticsEvent;
            }
        }

        private IReadOnlyList<IAnalyticsAdapter> BuildActiveAdapters(
            IReadOnlyList<IAnalyticsAdapter> allAdapters,
            IReadOnlyList<AnalyticsConfig.AdapterBinding> bindings)
        {
            if (allAdapters == null || allAdapters.Count == 0)
            {
                return Array.Empty<IAnalyticsAdapter>();
            }

            var activeAdapters = new List<IAnalyticsAdapter>(allAdapters.Count);
            for (var i = 0; i < allAdapters.Count; i++)
            {
                var adapter = allAdapters[i];
                if (adapter == null)
                {
                    continue;
                }

                if (bindings != null && bindings.Count > 0 && !HasBinding(bindings, adapter.AdapterId))
                {
                    continue;
                }

                activeAdapters.Add(adapter);
            }

            if (_analyticsConfig != null)
            {
                _analyticsConfig.SortByPriority(activeAdapters, bindings);
            }
            return activeAdapters;
        }

        private void LogSkip(string message)
        {
            if (_analyticsConfig == null || _analyticsConfig.LogSkippedEvents)
            {
                EvoDebug.LogWarning(message, SOURCE);
            }
        }

        public UniTask WaitForInitializationAsync(CancellationToken cancellationToken)
        {
            return WaitForAdaptersReadyAsync(cancellationToken);
        }

        private static AnalyticsConfig ResolveAnalyticsConfig(IConfigService configService)
        {
            if (configService != null && configService.TryGet<AnalyticsConfig>(out var analyticsConfig))
            {
                return analyticsConfig;
            }

            return null;
        }

        private static AnalyticsEventMapper ResolveEventMapper(IConfigService configService)
        {
            if (configService != null && configService.TryGet<AnalyticsEventMappingConfig>(out var mappingConfig) && mappingConfig != null)
            {
                return new AnalyticsEventMapper(mappingConfig);
            }

            return null;
        }

        private static string ResolvePlatformId(IConfigService configService)
        {
            if (configService != null && configService.TryGet<PlatformCatalog>(out var platformCatalog) && platformCatalog != null)
            {
                return platformCatalog.CurrentPlatformId;
            }

            return null;
        }

        private async UniTask WaitForAdaptersReadyAsync(CancellationToken cancellationToken)
        {
            if (_activeAdapters.Count == 0)
            {
                return;
            }

            await UniTask.WaitWhile(
                () => !AreAllAdaptersInitialized(_activeAdapters),
                cancellationToken: cancellationToken);
        }

        private static bool AreAllAdaptersInitialized(IReadOnlyList<IAnalyticsAdapter> adapters)
        {
            for (var i = 0; i < adapters.Count; i++)
            {
                var adapter = adapters[i];
                if (adapter == null || !adapter.IsInitialized)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasBinding(
            IReadOnlyList<AnalyticsConfig.AdapterBinding> bindings,
            string adapterId)
        {
            if (bindings == null || bindings.Count == 0 || string.IsNullOrWhiteSpace(adapterId))
            {
                return false;
            }

            for (var i = 0; i < bindings.Count; i++)
            {
                if (string.Equals(bindings[i].AdapterId, adapterId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
