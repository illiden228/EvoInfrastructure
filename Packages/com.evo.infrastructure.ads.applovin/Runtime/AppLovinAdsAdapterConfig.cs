using Evo.Infrastructure.Services.Ads.Config;
using Evo.Infrastructure.Services.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Ads.AppLovin
{
    [CreateAssetMenu(fileName = "AppLovinAdsAdapterConfig", menuName = "Project/Ads/Adapters/AppLovin Config")]
    [GameConfig("Ads")]
    public sealed class AppLovinAdsAdapterConfig : AdsAdapterConfigBase, IGameConfig
    {
    }
}
