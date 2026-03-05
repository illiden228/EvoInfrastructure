using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

namespace _Project.Scripts.Infrastructure.Services.Localization
{
    public interface ILocalizationService
    {
        event System.Action<Locale> LocaleChanged;
        Locale CurrentLocale { get; }
        UniTask InitializeAsync(CancellationToken cancellationToken = default);
        IReadOnlyList<Locale> GetAvailableLocales();
        void SetLocale(Locale locale);
        void SetLocale(string localeCode);
        string Get(string table, string key, string fallback = null);
    }
}
