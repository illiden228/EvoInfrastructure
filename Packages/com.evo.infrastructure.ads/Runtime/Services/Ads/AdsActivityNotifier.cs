using System;

namespace Evo.Infrastructure.Services.Ads
{
    public sealed class AdsActivityNotifier : IAdsActivityNotifier
    {
        public event Action AdStarted;
        public event Action AdFinished;

        public void NotifyAdStarted()
        {
            AdStarted?.Invoke();
        }

        public void NotifyAdFinished()
        {
            AdFinished?.Invoke();
        }
    }
}
