using System;

namespace Evo.Infrastructure.Services.Ads
{
    public interface IAdsActivityNotifier
    {
        event Action AdStarted;
        event Action AdFinished;
        void NotifyAdStarted();
        void NotifyAdFinished();
    }
}
