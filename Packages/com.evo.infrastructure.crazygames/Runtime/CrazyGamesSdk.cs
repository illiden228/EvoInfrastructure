using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Debug;
using UnityEngine;
#if CRAZY
using CrazyGames;
#endif

namespace Evo.Infrastructure.Services.CrazyGames
{
    public static class CrazyGamesSdk
    {
        private const string SOURCE = nameof(CrazyGamesSdk);
        private const int INIT_TIMEOUT_MS = 1000;
        private const int INIT_RETRIES = 5;
        private const int RETRY_DELAY_MS = 250;
        private static bool _unavailableLogged;

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

        public static bool IsUserAccountAvailable
        {
            get
            {
#if CRAZY
                return TryEnsureReady() && CrazySDK.User.IsUserAccountAvailable;
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
                LogUnavailableOnce("CrazySDK is available only in Editor/WebGL runtime.");
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
            LogUnavailableOnce("CRAZY define is disabled. CrazyGames services are inactive.");
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

        public static UniTask<CrazyGamesAdResult> ShowInterstitialAdAsync(CancellationToken cancellationToken)
        {
            return ShowAdAsync(false, cancellationToken);
        }

        public static UniTask<CrazyGamesAdResult> ShowRewardedAdAsync(CancellationToken cancellationToken)
        {
            return ShowAdAsync(true, cancellationToken);
        }

        public static void NotifyGameplayStart()
        {
#if CRAZY
            if (TryEnsureReady())
            {
                CrazySDK.Game.GameplayStart();
            }
#endif
        }

        public static void NotifyGameplayStop()
        {
#if CRAZY
            if (TryEnsureReady())
            {
                CrazySDK.Game.GameplayStop();
            }
#endif
        }

        public static bool TryGetDataString(string key, out string value)
        {
            value = string.Empty;
#if CRAZY
            if (!TryEnsureReady())
            {
                return false;
            }

            try
            {
                value = CrazySDK.Data.GetString(key, string.Empty);
                return true;
            }
            catch (System.Exception ex)
            {
                EvoDebug.LogWarning($"Get data string failed: {ex.Message}", SOURCE);
                return false;
            }
#else
            return false;
#endif
        }

        public static bool TrySetDataString(string key, string value)
        {
#if CRAZY
            if (!TryEnsureReady())
            {
                return false;
            }

            try
            {
                CrazySDK.Data.SetString(key, value ?? string.Empty);
                CrazySDK.User.SyncUnityGameData();
                return true;
            }
            catch (System.Exception ex)
            {
                EvoDebug.LogWarning($"Set data string failed: {ex.Message}", SOURCE);
                return false;
            }
#else
            return false;
#endif
        }

        public static UniTask<CrazyGamesUserInfo?> GetUserAsync(CancellationToken cancellationToken)
        {
#if CRAZY
            if (!TryEnsureReady())
            {
                return UniTask.FromResult<CrazyGamesUserInfo?>(null);
            }

            var tcs = new UniTaskCompletionSource<CrazyGamesUserInfo?>();
            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() => tcs.TrySetResult(null));
            }

            CrazySDK.User.GetUser(user =>
            {
                registration.Dispose();
                tcs.TrySetResult(user == null
                    ? null
                    : new CrazyGamesUserInfo(user.username, user.profilePictureUrl));
            });

            return tcs.Task;
#else
            return UniTask.FromResult<CrazyGamesUserInfo?>(null);
#endif
        }

        public static UniTask<CrazyGamesUserInfo?> ShowAuthPromptAsync(CancellationToken cancellationToken)
        {
#if CRAZY
            if (!TryEnsureReady())
            {
                return UniTask.FromResult<CrazyGamesUserInfo?>(null);
            }

            var tcs = new UniTaskCompletionSource<CrazyGamesUserInfo?>();
            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() => tcs.TrySetResult(null));
            }

            CrazySDK.User.ShowAuthPrompt((error, user) =>
            {
                registration.Dispose();
                if (error != null)
                {
                    EvoDebug.LogWarning($"Auth prompt error: {error.code} / {error.message}", SOURCE);
                    tcs.TrySetResult(null);
                    return;
                }

                tcs.TrySetResult(user == null
                    ? null
                    : new CrazyGamesUserInfo(user.username, user.profilePictureUrl));
            });

            return tcs.Task;
#else
            return UniTask.FromResult<CrazyGamesUserInfo?>(null);
#endif
        }

        public static bool TryGetDeviceType(out string deviceType)
        {
            deviceType = null;
#if CRAZY
            if (!CrazySDK.IsInitialized)
            {
                return false;
            }

            try
            {
                deviceType = CrazySDK.User.SystemInfo?.device?.type;
                return !string.IsNullOrWhiteSpace(deviceType);
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        private static async UniTask<CrazyGamesAdResult> ShowAdAsync(bool rewarded, CancellationToken cancellationToken)
        {
#if CRAZY
            if (!TryEnsureReady())
            {
                return new CrazyGamesAdResult(false, "CrazySDK is not initialized.");
            }

            var completion = new UniTaskCompletionSource<CrazyGamesAdResult>();
            var completed = false;
            CancellationTokenRegistration cancellationRegistration = default;

            void Finish(bool shown, string error = null)
            {
                if (completed)
                {
                    return;
                }

                completed = true;
                cancellationRegistration.Dispose();
                completion.TrySetResult(new CrazyGamesAdResult(shown, error));
            }

            try
            {
                CrazySDK.Ad.RequestAd(
                    rewarded ? CrazyAdType.Rewarded : CrazyAdType.Midgame,
                    adStarted: () => { },
                    adError: sdkError =>
                    {
                        var error = sdkError != null ? $"{sdkError.code}: {sdkError.message}" : "Unknown ad error.";
                        Finish(false, error);
                    },
                    adFinished: () => Finish(true));
            }
            catch (System.Exception ex)
            {
                Finish(false, ex.Message);
            }

            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.Register(() => Finish(false, "Canceled by token."));
            }

            return await completion.Task;
#else
            await UniTask.CompletedTask;
            return new CrazyGamesAdResult(false, "CRAZY define is disabled.");
#endif
        }

        private static async UniTask<bool> AwaitReadyAsync(CancellationToken cancellationToken)
        {
#if CRAZY
            if (!IsSupportedRuntime)
            {
                LogUnavailableOnce("CrazySDK is available only in Editor/WebGL runtime.");
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
            LogUnavailableOnce("CRAZY define is disabled. CrazyGames services are inactive.");
            await UniTask.CompletedTask;
            return false;
#endif
        }

        private static void LogUnavailableOnce(string message)
        {
            if (_unavailableLogged)
            {
                return;
            }

            _unavailableLogged = true;
            EvoDebug.LogWarning(message, SOURCE);
        }
    }
}
