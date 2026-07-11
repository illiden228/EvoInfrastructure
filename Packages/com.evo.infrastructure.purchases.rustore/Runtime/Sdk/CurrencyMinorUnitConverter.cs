using System;
using System.Collections.Generic;

namespace Evo.Infrastructure.Services.Purchases.RuStore
{
    internal static class CurrencyMinorUnitConverter
    {
        private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "PYG", "RWF", "UGX", "UYI", "VND", "VUV", "XAF", "XOF", "XPF"
        };

        private static readonly HashSet<string> ThreeDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "BHD", "IQD", "JOD", "KWD", "LYD", "OMR", "TND"
        };

        public static decimal ToMajorUnits(long value, string currencyCode)
        {
            if (value <= 0)
            {
                return 0m;
            }

            if (ZeroDecimalCurrencies.Contains(currencyCode ?? string.Empty))
            {
                return value;
            }

            return ThreeDecimalCurrencies.Contains(currencyCode ?? string.Empty)
                ? value / 1000m
                : value / 100m;
        }
    }
}
