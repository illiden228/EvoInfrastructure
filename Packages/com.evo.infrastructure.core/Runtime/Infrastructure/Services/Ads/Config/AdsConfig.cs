using _Project.Scripts.Infrastructure.Services.Config;
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.Ads.Config
{
    [CreateAssetMenu(fileName = "AdsConfig", menuName = "Project/Ads/Ads Config")]
    [GameConfig("Ads")]
    public sealed class AdsConfig : ScriptableObject, IGameConfig
    {
        [Header("Frequency")]
        [SerializeField] private int interstitialEveryNGames = 2;

        [Header("Timeouts (ms)")]
        [SerializeField] private int defaultShowTimeoutMs = 8000;
        [SerializeField] private int interstitialShowTimeoutMs = 8000;
        [SerializeField] private int rewardedShowTimeoutMs = 12000;

        [Header("Fallback")]
        [SerializeField] private int maxFallbackAttempts = 1;

        [Header("Rewarded Cooldown (sec)")]
        [SerializeField] private int rewardedCooldownSeconds = 0;

        public int InterstitialEveryNGames => interstitialEveryNGames > 0 ? interstitialEveryNGames : 1;
        public int DefaultShowTimeoutMs => defaultShowTimeoutMs > 0 ? defaultShowTimeoutMs : 8000;
        public int MaxFallbackAttempts => maxFallbackAttempts < 0 ? 0 : maxFallbackAttempts;
        public int RewardedCooldownSeconds => rewardedCooldownSeconds < 0 ? 0 : rewardedCooldownSeconds;

        public int GetShowTimeoutMs(AdType adType)
        {
            return adType switch
            {
                AdType.Interstitial when interstitialShowTimeoutMs > 0 => interstitialShowTimeoutMs,
                AdType.Rewarded when rewardedShowTimeoutMs > 0 => rewardedShowTimeoutMs,
                _ => DefaultShowTimeoutMs
            };
        }
    }
}
