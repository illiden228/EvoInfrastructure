using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Evo.Infrastructure.Runtime.Loading
{
    public interface ISceneLoadingPipeline
    {
        IReadOnlyList<ILoadingStep> CreateSteps(
            AssetReference sceneReference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100);

        UniTask LoadSceneAsync(
            AssetReference sceneReference,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100,
            CancellationToken cancellationToken = default);
    }
}
