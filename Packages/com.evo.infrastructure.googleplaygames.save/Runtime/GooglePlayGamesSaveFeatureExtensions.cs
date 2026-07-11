using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Save;
using VContainer;
using System.Runtime.CompilerServices;

namespace Evo.Infrastructure.GooglePlayGames.Save
{
    public static class GooglePlayGamesSaveFeatureExtensions
    {
        private const string BackendTypeName = "Evo.Infrastructure.GooglePlayGames.Save.GooglePlayGamesSaveBackend, Evo.Infrastructure.GooglePlayGames.Save.Sdk";
        private static readonly ConditionalWeakTable<EvoFeatureRegistry, object> Registrations = new();

        public static EvoFeatureRegistry UseGooglePlayGamesSave(this EvoFeatureRegistry features, GooglePlayGamesSaveOptions options = null)
        {
            if (Registrations.TryGetValue(features, out _)) return features;
            Registrations.Add(features, new object());
            features.UseGooglePlayGames();
            features.Builder.RegisterInstance(options ?? new GooglePlayGamesSaveOptions());
            var backendType = Type.GetType(BackendTypeName, false);
            if (backendType != null) features.Builder.Register(backendType, Lifetime.Singleton).As<ISaveBackend>();
            return features;
        }
    }
}
