using System;

namespace _Project.Scripts.Infrastructure.Services.Config
{
    public enum CatalogDropdownKind
    {
        PlatformId = 0,
        AnalyticsAdapterId = 1,
        AnalyticsEventKey = 2,
        AnalyticsParameterKey = 3,
        AdsAdapterId = 4
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class CatalogDropdownAttribute : Attribute
    {
        public CatalogDropdownKind Kind { get; }

        public CatalogDropdownAttribute(CatalogDropdownKind kind)
        {
            Kind = kind;
        }
    }
}
