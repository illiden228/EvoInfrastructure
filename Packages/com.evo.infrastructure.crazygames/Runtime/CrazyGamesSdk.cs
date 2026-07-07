using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Debug;
using UnityEngine;
#if CRAZY
using CrazyGames;
#endif

namespace Evo.Infrastructure.Services.CrazyGames
{
    internal static class CrazyGamesSdk
    {
        private const string SOURCE = nameof(CrazyGamesSdk);
        private const int INIT_TIMEOUT_MS = 1000;
        private const int INIT_RETRIES = 5;
        private const int RETRY_DELAY_MS = 250;

        public static bool IsInitialized
        {
            get
            {
#if CRAZY
                return CrazySDK.IsInitialized;
#else
                return false;
#endif
            }
        }

        public static bool IsSupportedRuntime
        {
            get
            {
#if CRAZY
                return Application.isEditor || Application.platform == RuntimePlatform.WebGLPlayer;
#else
                return false;
#endif
            }
        }

        public static bool TryEnsureReady()
        {
#if CRAZY
            if (!IsSupportedRuntime)
            {
                return false;
            }

            if (CrazySDK.IsInitialized)
            {
                return true;
            }

            try
            {
                CrazySDK.Init(() => { });
            }
            catch (System.Exception ex)
            {
                EvoDebug.LogWarning($"CrazySDK init failed: {ex.Message}", SOURCE);
                return false;
            }

            return CrazySDK.IsInitialized;
#else
            return false;
#endif
        }

        public static async UniTask<bool> AwaitReadyWithRetriesAsync(CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < INIT_RETRIES; attempt++)
            {
                if (await AwaitReadyAsync(cancellationToken))
                {
                    return true;
                }

                if (attempt < INIT_RETRIES - 1)
                {
                    await UniTask.Delay(RETRY_DELAY_MS, cancellationToken: cancellationToken);
                }
            }

            EvoDebug.LogWarning("CrazySDK init failed after retries.", SOURCE);
            return false;
        }

        private static async UniTask<bool> AwaitReadyAsync(CancellationToken cancellationToken)
        {
#if CRAZY
            if (!IsSupportedRuntime)
            {
                return false;
            }

            if (CrazySDK.IsInitialized)
            {
                return true;
            }

            var initSignal = new UniTaskCompletionSource<bool>();
            try
            {
                CrazySDK.Init(() => initSignal.TrySetResult(true));
            }
            catch (System.Exception ex)
            {
                EvoDebug.LogWarning($"CrazySDK init threw exception: {ex.Message}", SOURCE);
                return false;
            }

            var (hasResultLeft, _) = await UniTask.WhenAny(
                initSignal.Task,
                UniTask.Delay(INIT_TIMEOUT_MS, cancellationToken: cancellationToken));
            return hasResultLeft && CrazySDK.IsInitialized;
#else
            await UniTask.CompletedTask;
            return false;
#endif
        }
    }
}
