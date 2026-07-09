using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Audio;
using VContainer;

namespace Evo.Infrastructure.Services.Audio
{
    public static class AudioFeatureExtensions
    {
        public static EvoFeatureRegistry UseAudio(this EvoFeatureRegistry features)
        {
            features.Builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
            return features;
        }
    }
}
