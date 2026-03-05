namespace _Project.Scripts.Infrastructure.Services.Analytics
{
    public interface IAnalyticsAdapter
    {
        string AdapterId { get; }
        bool IsInitialized { get; }
        bool Supports(AnalyticsEventType eventType);
        void Track(in AnalyticsDispatchEvent analyticsEvent);
    }
}
