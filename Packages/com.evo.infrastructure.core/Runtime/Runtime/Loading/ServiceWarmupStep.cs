using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using _Project.Scripts.Infrastructure.Services.Analytics;
using _Project.Scripts.Infrastructure.Services.Localization;
using _Project.Scripts.Infrastructure.Services.ResourceLoader;
using _Project.Scripts.Infrastructure.Services.ResourceProvider;
using _Project.Scripts.Infrastructure.Services.SceneLoader;

namespace _Project.Scripts.Application.Loading
{
    public sealed class ServiceWarmupStep : ILoadingStep
    {
        public string Message => "Initializing services";
        public float Weight => 1f;
        public int Order => 0;

        private readonly IResourceLoaderService _resourceLoader;
        private readonly ILocalizationService _localizationService;

        public ServiceWarmupStep(
            IResourceLoaderService resourceLoader,
            ISceneLoaderService sceneLoader,
            IResourceProviderService resourceProvider,
            IAnalyticsInitialization analyticsInitialization = null,
            ILocalizationService localizationService = null)
        {
            _resourceLoader = resourceLoader;
            _localizationService = localizationService;
            _ = analyticsInitialization;
        }

        public async UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
        {
            var tasks = new List<UniTask>(2);
            if (_resourceLoader != null)
            {
                tasks.Add(_resourceLoader.InitializeAsync(cancellationToken));
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
