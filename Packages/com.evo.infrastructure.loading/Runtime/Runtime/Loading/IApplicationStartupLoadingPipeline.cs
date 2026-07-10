using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Evo.Infrastructure.Runtime.Loading
{
    public interface IApplicationStartupLoadingPipeline
    {
        UniTask LoadStartupAsync(
            IReadOnlyList<ILoadingStep> bootstrapSteps,
            AssetReference startupScene,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            CancellationToken cancellationToken = default);
    }
}
