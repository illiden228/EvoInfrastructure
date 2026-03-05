namespace _Project.Scripts.Infrastructure.Services.Ads
{
    public interface IAdsAdapterFactory
    {
        string AdapterId { get; }
        IAdsAdapter Create();
    }
}
