using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Analytics.Config;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Debug;
using Firebase;
using Firebase.Analytics;

namespace Evo.Infrastructure.Services.Analytics.Firebase
{
    public sealed class FirebaseAnalyticsAdapter : IAnalyticsAdapter
    {
        private const string DEFAULT_ID = "firebase";
        private const string SOURCE = "Firebase Analytics Adapter";
        private bool _isInitialized;
        private bool _isAvailable;
        private bool _warningLogged;

        public FirebaseAnalyticsAdapter(IConfigService configs)
        {
            FirebaseAnalyticsAdapterConfig config = null;
            if (configs != null &&
                configs.TryGet<AnalyticsAdapterCatalog>(out var catalog) &&
                catalog != null)
            {
                catalog.TryGet(out config);
            }

            AdapterId = config?.ResolveAdapterId(DEFAULT_ID) ?? DEFAULT_ID;
            InitializeAsync().Forget(ex => CompleteFailed($"Firebase initialization failed: {ex.Message}"));
        }

        public string AdapterId { get; }
        public bool IsInitialized => _isInitialized;
        public bool Supports(AnalyticsEventType eventType) => _isAvailable;

        public void Track(in AnalyticsDispatchEvent analyticsEvent)
        {
            if (!_isAvailable)
            {
                return;
            }

            var eventName = SanitizeName(analyticsEvent.EventKey, 40);
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            try
            {
                var values = Merge(analyticsEvent);
                var parameters = BuildParameters(values);
                if (parameters.Count == 0)
                {
                    FirebaseAnalytics.LogEvent(eventName);
                }
                else
                {
                    FirebaseAnalytics.LogEvent(eventName, parameters.ToArray());
                }
            }
            catch (Exception ex)
            {
                WarnOnce($"Firebase event failed: {ex.Message}");
            }
        }

        private async UniTask InitializeAsync()
        {
            try
            {
                var status = await FirebaseApp.CheckAndFixDependenciesAsync();
                await UniTask.SwitchToMainThread();
                if (status != DependencyStatus.Available)
                {
                    CompleteFailed($"Firebase dependencies unavailable: {status}.");
                    return;
                }

                FirebaseAnalytics.SetSessionTimeoutDuration(TimeSpan.FromMinutes(20));
                _isAvailable = true;
            }
            catch (Exception ex)
            {
                CompleteFailed($"Firebase initialization failed: {ex.Message}");
            }
            finally
            {
                _isInitialized = true;
            }
        }

        private static Dictionary<string, object> Merge(in AnalyticsDispatchEvent analyticsEvent)
        {
            var result = new Dictionary<string, object>();
            Add(result, analyticsEvent.Parameters);
            if (analyticsEvent.EventType == AnalyticsEventType.Purchase)
            {
                var purchase = analyticsEvent.PurchaseEventData;
                if (purchase.Revenue >= 0m && !string.IsNullOrWhiteSpace(purchase.Currency))
                {
                    result["value"] = (double)purchase.Revenue;
                    result["currency"] = purchase.Currency.Trim().ToUpperInvariant();
                }

                Add(result, "transaction_id", purchase.TransactionId);
                Add(result, "item_id", purchase.ItemId);
            }
            else if (analyticsEvent.EventType == AnalyticsEventType.Ad)
            {
                var ad = analyticsEvent.AdEventData;
                Add(result, "ad_platform", ad.Platform);
                Add(result, "ad_source", ad.NetworkName);
                Add(result, "ad_unit_name", ad.UnitId);
                Add(result, "ad_format", ad.Format);
                Add(result, "placement", ad.Placement);
                if (ad.Revenue >= 0d &&
                    !double.IsNaN(ad.Revenue) &&
                    !double.IsInfinity(ad.Revenue) &&
                    !string.IsNullOrWhiteSpace(ad.Currency))
                {
                    result["value"] = ad.Revenue;
                    result["currency"] = ad.Currency.Trim().ToUpperInvariant();
                }
            }

            return result;
        }

        private static List<Parameter> BuildParameters(Dictionary<string, object> values)
        {
            var result = new List<Parameter>(Math.Min(values.Count, 25));
            foreach (var pair in values)
            {
                if (result.Count == 25)
                {
                    break;
                }

                var key = SanitizeName(pair.Key, 40);
                if (string.IsNullOrEmpty(key) || pair.Value == null)
                {
                    continue;
                }

                switch (pair.Value)
                {
                    case bool boolean:
                        result.Add(new Parameter(key, boolean ? 1L : 0L));
                        break;
                    case byte number:
                        result.Add(new Parameter(key, (long)number));
                        break;
                    case short number:
                        result.Add(new Parameter(key, (long)number));
                        break;
                    case int number:
                        result.Add(new Parameter(key, (long)number));
                        break;
                    case long number:
                        result.Add(new Parameter(key, number));
                        break;
                    case float number when !float.IsNaN(number) && !float.IsInfinity(number):
                        result.Add(new Parameter(key, (double)number));
                        break;
                    case double number when !double.IsNaN(number) && !double.IsInfinity(number):
                        result.Add(new Parameter(key, number));
                        break;
                    case decimal number:
                        result.Add(new Parameter(key, (double)number));
                        break;
                    default:
                        var text = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
                        result.Add(new Parameter(key, Limit(text, 100)));
                        break;
                }
            }
            return result;
        }

        private static string SanitizeName(string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var source = value.Trim();
            var result = new StringBuilder(Math.Min(source.Length, max));
            for (var i = 0; i < source.Length && result.Length < max; i++)
            {
                var character = source[i];
                result.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
            }

            if (result.Length == 0)
            {
                return null;
            }

            if (!char.IsLetter(result[0]))
            {
                result.Insert(0, 'e');
            }

            var sanitized = result.Length > max ? result.ToString(0, max) : result.ToString();
            if (sanitized.StartsWith("firebase_", StringComparison.OrdinalIgnoreCase) ||
                sanitized.StartsWith("google_", StringComparison.OrdinalIgnoreCase) ||
                sanitized.StartsWith("ga_", StringComparison.OrdinalIgnoreCase))
            {
                sanitized = "e_" + sanitized.Substring(0, Math.Min(sanitized.Length, max - 2));
            }

            return sanitized;
        }

        private static void Add(
            Dictionary<string, object> target,
            IReadOnlyDictionary<string, object> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (var pair in source)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
                {
                    target[pair.Key] = pair.Value;
                }
            }
        }

        private static void Add(Dictionary<string, object> target, string key, object value)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (value != null && !string.IsNullOrWhiteSpace(text))
            {
                target[key] = value;
            }
        }

        private static string Limit(string value, int max)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : value.Substring(0, Math.Min(value.Length, max));
        }

        private void CompleteFailed(string message)
        {
            _isAvailable = false;
            _isInitialized = true;
            WarnOnce(message);
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
