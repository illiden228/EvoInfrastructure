using System;
using System.Collections.Generic;
using System.Threading;
using _Project.Scripts.Infrastructure.Services.Debug;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace _Project.Scripts.Infrastructure.Services.Localization
{
    public sealed class LocalizationService : ILocalizationService
    {
        private bool _initialized;
        private bool _subscribed;
        private bool _initInProgress;
        private AsyncOperationHandle _initHandle;
        private readonly HashSet<string> _loadedTables = new(StringComparer.Ordinal);
        private readonly HashSet<string> _loadingTables = new(StringComparer.Ordinal);
        private readonly Dictionary<string, StringTable> _tableCache = new(StringComparer.Ordinal);

        public event Action<Locale> LocaleChanged;

        public Locale CurrentLocale => LocalizationSettings.SelectedLocale;

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);
            await EnsureTableLoadedAsync(LocalizationKeys.UI_TABLE, cancellationToken);
        }

        public IReadOnlyList<Locale> GetAvailableLocales()
        {
            EnsureInitialized();
            if (!_initialized)
            {
                return Array.Empty<Locale>();
            }
            return LocalizationSettings.AvailableLocales.Locales;
        }

        public void SetLocale(Locale locale)
        {
            EnsureInitialized();
            if (!_initialized)
            {
                return;
            }
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }
        }

        public void SetLocale(string localeCode)
        {
            EnsureInitialized();
            if (!_initialized)
            {
                return;
            }
            if (string.IsNullOrEmpty(localeCode))
            {
                EvoDebug.LogWarning("SetLocale called with empty locale code.", nameof(LocalizationService));
                return;
            }

            var locales = LocalizationSettings.AvailableLocales.Locales;
            for (var i = 0; i < locales.Count; i++)
            {
                var locale = locales[i];
                if (locale == null)
                {
                    continue;
                }

                if (string.Equals(locale.Identifier.Code, localeCode, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(locale.LocaleName, localeCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    LocalizationSettings.SelectedLocale = locale;
                    return;
                }
            }

            EvoDebug.LogWarning($"Could not find locale '{localeCode}'.", nameof(LocalizationService));
        }

        public string Get(string table, string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(key))
            {
                return fallback ?? string.Empty;
            }

            EnsureInitialized();
            if (!_initialized)
            {
                return fallback ?? string.Empty;
            }

            if (!IsTableLoaded(table))
            {
                EnsureTableLoadedAsync(table, CancellationToken.None).Forget();
                return fallback ?? string.Empty;
            }

            string value = null;
            try
            {
                if (_tableCache.TryGetValue(GetTableCacheKey(table), out var stringTable) && stringTable != null)
                {
                    var entry = stringTable.GetEntry(key);
                    value = entry != null ? entry.LocalizedValue : null;
                }
            }
            catch (Exception ex)
            {
                EvoDebug.LogError($"Failed to resolve '{table}:{key}'. {ex.Message}", nameof(LocalizationService));
                value = null;
            }

            return string.IsNullOrEmpty(value) ? (fallback ?? string.Empty) : value;
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            var handle = LocalizationSettings.InitializationOperation;
            if (handle.IsValid() && !handle.IsDone)
            {
                HookInitialize(handle);
                return;
            }

            _initialized = true;
            Subscribe();
        }

        private async UniTask EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_initialized)
            {
                return;
            }

            var handle = LocalizationSettings.InitializationOperation;
            if (handle.IsValid() && !handle.IsDone)
            {
                HookInitialize(handle);
                await handle.ToUniTask(cancellationToken: cancellationToken);
            }

            _initialized = true;
            Subscribe();
        }

        private void OnLocaleChanged(Locale locale)
        {
            _loadedTables.Clear();
            _loadingTables.Clear();
            _tableCache.Clear();
            EnsureTableLoadedAsync(LocalizationKeys.UI_TABLE, CancellationToken.None).Forget();
            LocaleChanged?.Invoke(locale);
        }

        private void HookInitialize(AsyncOperationHandle handle)
        {
            if (_initInProgress && _initHandle.Equals(handle))
            {
                return;
            }

            _initInProgress = true;
            _initHandle = handle;
            handle.Completed += _ =>
            {
                _initInProgress = false;
                _initialized = true;
                Subscribe();
            };
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            _subscribed = true;
        }

        private bool IsTableLoaded(string table)
        {
            if (string.IsNullOrEmpty(table))
            {
                return false;
            }

            return _loadedTables.Contains(GetTableCacheKey(table));
        }

        private async UniTask EnsureTableLoadedAsync(string table, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(table) || !_initialized)
            {
                return;
            }

            var cacheKey = GetTableCacheKey(table);
            if (_loadedTables.Contains(cacheKey) || _loadingTables.Contains(cacheKey))
            {
                return;
            }

            _loadingTables.Add(cacheKey);
            try
            {
                var handle = LocalizationSettings.StringDatabase.GetTableAsync(table);
                await handle.ToUniTask(cancellationToken: cancellationToken);
                var stringTable = handle.Result as StringTable;
                if (stringTable != null)
                {
                    _loadedTables.Add(cacheKey);
                    _tableCache[cacheKey] = stringTable;
                    LocaleChanged?.Invoke(CurrentLocale);
                }
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning($"Failed to preload localization table '{table}'. {ex.Message}", nameof(LocalizationService));
            }
            finally
            {
                _loadingTables.Remove(cacheKey);
            }
        }

        private string GetTableCacheKey(string table)
        {
            var localeCode = CurrentLocale?.Identifier.Code ?? "none";
            return $"{localeCode}::{table}";
        }
    }
}
