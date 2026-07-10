using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.ResourceLoader;
using NUnit.Framework;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Evo.Infrastructure.Services.ResourceLoader.Tests
{
    public sealed class AddressablesResourceLoaderServiceSceneTests
    {
        private const string SceneKeyParameterName = "EvoResourcesAddressableSceneKey";
        private const string SceneKeyEnvironmentName = "EVO_RESOURCES_ADDRESSABLE_SCENE_KEY";

        [Test]
        public async Task AddressableScene_CanUnloadNoOpAndLoadAgain()
        {
            var sceneKey = GetAddressableSceneKey();
            if (string.IsNullOrEmpty(sceneKey))
            {
                Assert.Ignore(
                    $"Set NUnit parameter '{SceneKeyParameterName}' or environment variable " +
                    $"'{SceneKeyEnvironmentName}' to run the Addressables scene unload lifecycle test.");
            }

            using var service = new AddressablesResourceLoaderService();
            await service.InitializeAsync();

            var firstLoad = service.LoadSceneHandle(sceneKey, LoadSceneMode.Additive);
            await AwaitHandleCompletionAsync(firstLoad, CancellationToken.None);
            Assert.That(firstLoad.Result.Scene.IsValid(), Is.True);
            Assert.That(firstLoad.Result.Scene.isLoaded, Is.True);

            await service.UnloadSceneAsync(sceneKey);
            await service.UnloadSceneAsync(sceneKey);

            var secondLoad = service.LoadSceneHandle(sceneKey, LoadSceneMode.Additive);
            await AwaitHandleCompletionAsync(secondLoad, CancellationToken.None);
            Assert.That(secondLoad.Result.Scene.IsValid(), Is.True);
            Assert.That(secondLoad.Result.Scene.isLoaded, Is.True);

            await service.UnloadSceneAsync(sceneKey);
        }

        private static string GetAddressableSceneKey()
        {
            var parameter = TestContext.Parameters.Get(SceneKeyParameterName);
            if (!string.IsNullOrWhiteSpace(parameter))
            {
                return parameter;
            }

            return Environment.GetEnvironmentVariable(SceneKeyEnvironmentName);
        }

        private static async Task AwaitHandleCompletionAsync(
            AsyncOperationHandle<SceneInstance> handle,
            CancellationToken cancellationToken)
        {
            while (!handle.IsDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                throw handle.OperationException ?? new InvalidOperationException("Addressables scene load failed.");
            }
        }
    }
}
