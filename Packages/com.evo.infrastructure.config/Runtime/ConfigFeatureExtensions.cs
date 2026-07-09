using System;
using System.Collections.Generic;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.Config;
using VContainer;

namespace Evo.Infrastructure.Services.Config
{
    public static class ConfigFeatureExtensions
    {
        public static EvoFeatureRegistry UseConfig(
            this EvoFeatureRegistry features,
            IReadOnlyList<ScriptableConfigCatalog> configCatalogs = null)
        {
            var builder = features.Builder;
            builder.RegisterInstance(configCatalogs ?? Array.Empty<ScriptableConfigCatalog>());
            builder.Register<IConfigProvider, ScriptableObjectConfigProvider>(Lifetime.Singleton);
            builder.Register<IConfigService, ConfigService>(Lifetime.Singleton);
            return features;
        }
    }
}
