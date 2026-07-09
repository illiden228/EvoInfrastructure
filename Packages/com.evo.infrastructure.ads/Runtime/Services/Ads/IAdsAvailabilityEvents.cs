using System;

namespace Evo.Infrastructure.Services.Ads
{
    public interface IAdsAvailabilityEvents
    {
        event Action<AdType, string> AvailabilityChanged;
    }
}
