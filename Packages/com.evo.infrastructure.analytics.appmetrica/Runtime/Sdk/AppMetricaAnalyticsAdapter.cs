using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Analytics.Config;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Debug;
using Io.AppMetrica;
using AppMetricaApi = Io.AppMetrica.AppMetrica;

namespace Evo.Infrastructure.Services.Analytics.AppMetrica
{
    public sealed class AppMetricaAnalyticsAdapter : IAnalyticsAdapter
    {
        private const string DEFAULT_ID = "appmetrica";
        private const string SOURCE = "AppMetrica Analytics Adapter";
        private const int ACTIVATION_TIMEOUT_MS = 10000;
        private readonly AppMetricaAnalyticsAdapterConfig _config;
        private UniTaskCompletionSource _activationCompletion;
        private bool _isInitialized;
        private bool _isAvailable;
        private bool _warningLogged;

        public AppMetricaAnalyticsAdapter(IConfigService configs)
        {
            if (configs != null &&
                configs.TryGet<AnalyticsAdapterCatalog>(out var catalog) &&
                catalog != null)
            {
                catalog.TryGet(out _config);
            }

            AdapterId = _config?.ResolveAdapterId(DEFAULT_ID) ?? DEFAULT_ID;
            InitializeAsync().Forget(ex => CompleteFailed($"AppMetrica activation failed: {ex.Message}"));
        }

        public string AdapterId { get; }
        public bool IsInitialized => _isInitialized;
        public bool Supports(AnalyticsEventType eventType) => _isAvailable;

        public void Track(in AnalyticsDispatchEvent analyticsEvent)
        {
            if (!_isAvailable || string.IsNullOrWhiteSpace(analyticsEvent.EventKey))
            {
                return;
            }

            try
            {
                if (!AppMetricaApi.IsActivated())
                {
                    _isAvailable = false;
                    WarnOnce("AppMetrica is no longer activated; adapter is disabled.");
                    return;
                }

                switch (analyticsEvent.EventType)
                {
                    case AnalyticsEventType.Purchase:
                        TrackPurchase(analyticsEvent);
                        break;
                    case AnalyticsEventType.Ad:
                        TrackAd(analyticsEvent);
                        break;
                    default:
                        TrackCustom(analyticsEvent.EventKey, analyticsEvent.Parameters);
                        break;
                }
            }
            catch (Exception ex)
            {
                WarnOnce($"AppMetrica tracking failed: {ex.Message}");
            }
        }

        private async UniTask InitializeAsync()
        {
            try
            {
                if (_config == null)
                {
                    CompleteFailed("AppMetrica config is missing; adapter is disabled.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_config.AppKey))
                {
                    CompleteFailed("AppMetrica app key is missing; adapter is disabled.");
                    return;
                }

                if (AppMetricaApi.IsActivated())
                {
                    CompleteActivated();
                    return;
                }

                _activationCompletion = new UniTaskCompletionSource();
                AppMetricaApi.OnActivation += OnActivated;
                var sdkConfig = new AppMetricaConfig(_config.AppKey.Trim())
                {
                    DataSendingEnabled = true,
                    Logs = _config.EnableSdkLogs
                };
                AppMetricaApi.Activate(sdkConfig);
                if (TryCompleteActivated())
                {
                    return;
                }

                using var activationCancellation = new CancellationTokenSource();
                var completed = await UniTask.WhenAny(
                    _activationCompletion.Task,
                    UniTask.Delay(ACTIVATION_TIMEOUT_MS, cancellationToken: activationCancellation.Token));
                activationCancellation.Cancel();
                if (completed != 0)
                {
                    if (!TryCompleteActivated())
                    {
                        CompleteFailed($"AppMetrica activation timed out after {ACTIVATION_TIMEOUT_MS} ms; adapter is disabled.");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!TryCompleteActivated())
                {
                    CompleteFailed($"AppMetrica activation failed: {ex.Message}");
                }
            }
        }

        private void OnActivated(AppMetricaConfig _)
        {
            CompleteActivated();
        }

        private void CompleteActivated()
        {
            if (_isInitialized)
            {
                return;
            }

            _isAvailable = true;
            _isInitialized = true;
            _activationCompletion?.TrySetResult();
            UnsubscribeFromActivation();
        }

        private bool TryCompleteActivated()
        {
            try
            {
                if (!AppMetricaApi.IsActivated())
                {
                    return false;
                }

                CompleteActivated();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CompleteFailed(string message)
        {
            if (_isInitialized)
            {
                return;
            }

            _isAvailable = false;
            _isInitialized = true;
            _activationCompletion?.TrySetResult();
            UnsubscribeFromActivation();
            WarnOnce(message);
        }

        private void UnsubscribeFromActivation()
        {
            AppMetricaApi.OnActivation -= OnActivated;
        }

        private void TrackPurchase(in AnalyticsDispatchEvent analyticsEvent)
        {
            var purchase = analyticsEvent.PurchaseEventData;
            if (purchase.Revenue < 0m || string.IsNullOrWhiteSpace(purchase.Currency))
            {
                TrackCustom(analyticsEvent.EventKey, analyticsEvent.Parameters);
                return;
            }

            if (!TryToMicros(purchase.Revenue, out var micros))
            {
                WarnOnce("AppMetrica purchase revenue is outside the supported range.");
                return;
            }

            var revenue = new Revenue(micros, purchase.Currency.Trim().ToUpperInvariant())
            {
                ProductID = Limit(purchase.ItemId),
                Quantity = 1,
                Payload = BuildJson(analyticsEvent.Parameters)
            };
            if (!string.IsNullOrWhiteSpace(purchase.ReceiptJson) || !string.IsNullOrWhiteSpace(purchase.TransactionId))
            {
                revenue.ReceiptValue = new Revenue.Receipt
                {
                    Data = purchase.ReceiptJson,
                    TransactionID = purchase.TransactionId
                };
            }

            AppMetricaApi.ReportRevenue(revenue);
            Flush();
        }

        private void TrackAd(in AnalyticsDispatchEvent analyticsEvent)
        {
            var ad = analyticsEvent.AdEventData;
            if (ad.Revenue <= 0d || double.IsNaN(ad.Revenue) || double.IsInfinity(ad.Revenue) || string.IsNullOrWhiteSpace(ad.Currency))
            {
                TrackCustom(analyticsEvent.EventKey, analyticsEvent.Parameters);
                return;
            }

            var type = MapAdType(ad.Format);
            if (!type.HasValue)
            {
                WarnOnce($"Unsupported AppMetrica ad type '{ad.Format}'.");
                return;
            }

            var revenue = new AdRevenue(ad.Revenue, ad.Currency.Trim().ToUpperInvariant())
            {
                AdNetwork = Limit(ad.NetworkName),
                AdPlacementId = Limit(ad.NetworkPlacement),
                AdPlacementName = Limit(ad.Placement),
                AdUnitId = Limit(ad.UnitId),
                AdType = type,
                Payload = ToStrings(analyticsEvent.Parameters)
            };
            AppMetricaApi.ReportAdRevenue(revenue);
            Flush();
        }

        private void TrackCustom(string key, IReadOnlyDictionary<string, object> parameters)
        {
            var payload = BuildJson(parameters);
            if (payload == null)
            {
                AppMetricaApi.ReportEvent(key.Trim());
            }
            else
            {
                AppMetricaApi.ReportEvent(key.Trim(), payload);
            }

            if (_config.LogReportedEvents)
            {
                EvoDebug.Log($"Reported event '{key}'.", SOURCE);
            }

            Flush();
        }

        private void Flush()
        {
            if (!_config.FlushEventsImmediately)
            {
                return;
            }

            try
            {
                AppMetricaApi.SendEventsBuffer();
            }
            catch (Exception ex)
            {
                WarnOnce($"AppMetrica flush failed: {ex.Message}");
            }
        }

        private static bool TryToMicros(decimal value, out long result)
        {
            result = 0;
            if (value < 0m || value > long.MaxValue / 1_000_000m)
            {
                return false;
            }

            result = (long)Math.Round(value * 1_000_000m, MidpointRounding.AwayFromZero);
            return true;
        }

        private static AdType? MapAdType(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return null;
            }

            switch (format.Trim().Replace("_", "").ToLowerInvariant())
            {
                case "banner": return AdType.Banner;
                case "interstitial": return AdType.Interstitial;
                case "rewarded": return AdType.Rewarded;
                case "appopen": return AdType.AppOpen;
                case "native": return AdType.Native;
                case "mrec": return AdType.Mrec;
                default: return null;
            }
        }

        private static string BuildJson(IReadOnlyDictionary<string, object> values)
        {
            if (values == null || values.Count == 0)
            {
                return null;
            }

            var builder = new StringBuilder("{");
            var first = true;
            foreach (var p in values)
            {
                if (string.IsNullOrWhiteSpace(p.Key) || p.Value == null)
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                AppendQuoted(builder, Limit(p.Key, 128));
                builder.Append(':');
                var value = Convert.ToString(p.Value, CultureInfo.InvariantCulture);
                AppendQuoted(builder, Limit(value, 2048));
            }

            return first ? null : builder.Append('}').ToString();
        }

        private static IDictionary<string, string> ToStrings(IReadOnlyDictionary<string, object> values)
        {
            if (values == null)
            {
                return null;
            }

            var result = new Dictionary<string, string>();
            foreach (var pair in values)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                {
                    continue;
                }

                var value = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
                result[Limit(pair.Key, 128)] = Limit(value, 2048);
            }

            return result.Count == 0 ? null : result;
        }

        private static void AppendQuoted(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (var character in value ?? string.Empty)
            {
                if (character == '"' || character == '\\')
                {
                    builder.Append('\\');
                }

                if (character == '\n')
                {
                    builder.Append("\\n");
                }
                else if (character != '\r')
                {
                    builder.Append(character);
                }
            }
            builder.Append('"');
        }

        private static string Limit(string value, int max = 1024)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : value.Substring(0, Math.Min(value.Length, max));
        }

        private void WarnOnce(string message)
        {
            if (_warningLogged)
            {
                return;
            }

            _warningLogged = true;
            EvoDebug.LogWarning(message, SOURCE);
        }
    }
}
