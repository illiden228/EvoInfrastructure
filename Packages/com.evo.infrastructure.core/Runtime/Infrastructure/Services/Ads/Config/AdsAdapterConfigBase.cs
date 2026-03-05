using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.Ads.Config
{
    public abstract class AdsAdapterConfigBase : ScriptableObject
    {
        [SerializeField] private string adapterId;
        [SerializeField] private string appKey;
        [SerializeField] private string interstitialPlacementId;
        [SerializeField] private string rewardedPlacementId;
        [SerializeField] private string bannerPlacementId;

        public string AdapterId => adapterId;
        public string AppKey => appKey;
        public string InterstitialPlacementId => interstitialPlacementId;
        public string RewardedPlacementId => rewardedPlacementId;
        public string BannerPlacementId => bannerPlacementId;
    }
}
