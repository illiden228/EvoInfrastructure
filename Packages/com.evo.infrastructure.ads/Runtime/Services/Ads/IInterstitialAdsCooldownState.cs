namespace Evo.Infrastructure.Services.Ads
{
    public interface IInterstitialAdsCooldownState
    {
        long InterstitialCooldownUntilUnixMs { get; set; }
        bool InterstitialWarm { get; set; }
    }
}
