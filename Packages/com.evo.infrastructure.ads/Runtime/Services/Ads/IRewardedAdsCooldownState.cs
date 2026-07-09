namespace Evo.Infrastructure.Services.Ads
{
    public interface IRewardedAdsCooldownState
    {
        long LastRewardedAdUnixMs { get; set; }
    }
}
