using System;
using System.Threading;
using Evo.Infrastructure.Services.Debug;
using Evo.Infrastructure.Services.ResourceLoader;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Evo.Infrastructure.Runtime.Loading
{
    public sealed class SkinsPreloadStep : ILoadingStep
    {
        private const string SKINS_LABEL = "Skins";

        private readonly IResourceLoaderService _resourceLoader;

        public SkinsPreloadStep(IResourceLoaderService resourceLoader)
        {
            _resourceLoader = resourceLoader;
        }

        public string Message => "Preloading skins";
        public float Weight => 1f;
        public int Order => 5;

        public async UniTask Execute(IProgress<float> progress, CancellationToken cancellationToken)
        {
            progress?.Report(0f);

            if (_resourceLoader == null)
            {
                progress?.Report(1f);
                return;
            }

            await _resourceLoader.InitializeAsync(cancellationToken);

            var handle = Addressables.DownloadDependenciesAsync(SKINS_LABEL, true);
            try
            {
                await handle.ToUniTask(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Skins preload skipped for key '{SKINS_LABEL}'. {ex.Message}",
                    nameof(SkinsPreloadStep));
                throw;
            }

            progress?.Report(1f);
        }
    }
}
