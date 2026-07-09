using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Focus;
using VContainer;

namespace Evo.Infrastructure.Services.Focus
{
    public static class FocusFeatureExtensions
    {
        public static EvoFeatureRegistry UseFocus(this EvoFeatureRegistry features)
        {
            features.Builder.Register<IFocusService, FocusService>(Lifetime.Singleton);
            return features;
        }
    }
}
