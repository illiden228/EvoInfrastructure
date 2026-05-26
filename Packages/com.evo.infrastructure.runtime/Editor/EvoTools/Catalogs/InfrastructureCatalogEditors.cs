using Evo.Infrastructure.Services.Ads.Config;
using Evo.Infrastructure.Services.Analytics.Config;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using UnityEditor;

namespace Evo.Infrastructure.Editor.EvoTools.Catalogs
{
    [CustomEditor(typeof(AdsAdapterCatalog))]
    internal sealed class AdsAdapterCatalogEditor : CatalogConfigEditorBase
    {
    }

    [CustomEditor(typeof(AnalyticsAdapterCatalog))]
    internal sealed class AnalyticsAdapterCatalogEditor : CatalogConfigEditorBase
    {
    }

    [CustomEditor(typeof(PlatformCatalog))]
    internal sealed class PlatformCatalogEditor : CatalogConfigEditorBase
    {
    }
}
