using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Localization;
using Evo.Infrastructure.Services.ResourceLoader;
using Evo.Infrastructure.Services.ResourceProvider;
using Evo.Infrastructure.Services.SceneLoader;

namespace Evo.Infrastructure.Runtime.Loading
{
    public sealed class ServiceWarmupStep : ILoadingStep
    {
        public string Message => "Initializing services";
        public float Weight => 1f;
        public int Order => 0;

        private readonly IResourceLoaderService _resourceLoader;
        private readonly IAnalyticsInitialization _analyticsInitialization;
        private readonly ILocalizationService _localizationService;

        public ServiceWarmupStep(
            IResourceLoaderService resourceLoader,
            ISceneLoaderService sceneLoader,
            IResourceProviderService resourceProvider,
            IAnalyticsInitialization analyticsInitialization = null,
            ILocalizationService localizationService = null)
        {
            _resourceLoader = resourceLoader;
            _analyticsInitialization = analyticsInitialization;
            _localizationService = localizationService;
        }

        public async UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
        {
            var tasks = new List<UniTask>(3);
            if (_resourceLoader != null)
            {
                tasks.Add(_resourceLoader.InitializeAsync(cancellationToken));
            }

            if (_analyticsInitialization != null)
            {
                tasks.Add(_analyticsInitialization.WaitForInitializationAsync(cancellationToken));
            }

            if (_localizationService != null)
            {
                tasks.Add(_localizationService.InitializeAsync(cancellationToken));
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }

            progress?.Report(1f);
        }
    }
}
