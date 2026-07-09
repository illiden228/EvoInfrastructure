using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Save;
using VContainer;

namespace Evo.Infrastructure.Services.Save
{
    public static class SaveFeatureExtensions
    {
        public static EvoFeatureRegistry UseSave(
            this EvoFeatureRegistry features,
            SaveStorageOptions options = null)
        {
            var builder = features.Builder;
            builder.RegisterInstance(options ?? new SaveStorageOptions());
            builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
            builder.Register<ISaveBackend, FileSaveBackend>(Lifetime.Singleton);
            builder.Register<ISaveBackend, PrefsSaveBackend>(Lifetime.Singleton);
            return features;
        }
    }
}
