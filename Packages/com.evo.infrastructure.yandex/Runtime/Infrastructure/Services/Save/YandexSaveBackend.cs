using System.Threading;
using Cysharp.Threading.Tasks;
using _Project.Scripts.Infrastructure.Services.Debug;
using _Project.Scripts.Infrastructure.Services.PlatformInfo;
#if YandexGamesPlatform_yg
using YG;
#endif
#if YandexGamesPlatform_yg && Storage_yg
using YG.Insides;
#endif

namespace _Project.Scripts.Infrastructure.Services.Save
{
    public sealed class YandexSaveBackend : ISaveBackend
    {
        private const string SOURCE = nameof(YandexSaveBackend);
        private const int PLUGIN_READY_TIMEOUT_MS = 10000;
        private readonly IPlatformInfoService _platformInfoService;

        public string BackendId => "yandex";
        public int Priority => 100;
        public bool IsAvailable => _platformInfoService != null && _platformInfoService.IsWeb;

        public YandexSaveBackend(IPlatformInfoService platformInfoService)
        {
            _platformInfoService = platformInfoService;
        }

        public async UniTask<SaveEnvelope> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (!await AwaitPluginReadyAsync(cancellationToken))
            {
                return null;
            }

#if YandexGamesPlatform_yg && Storage_yg
            await AwaitStorageDataAsync(cancellationToken);
            var envelope = YG2.saves != null ? YG2.saves.blindShotSave : null;
            if (envelope == null || envelope.profile == null || envelope.schemaVersion <= 0)
            {
                return null;
            }

            return envelope;
#else
            return null;
#endif
        }

        public async UniTask<bool> SaveAsync(SaveEnvelope envelope, CancellationToken cancellationToken = default)
        {
            if (!await AwaitPluginReadyAsync(cancellationToken))
            {
                return false;
            }

#if YandexGamesPlatform_yg && Storage_yg
            if (YG2.saves == null)
            {
                YG2.saves = new SavesYG();
            }

            YG2.saves.blindShotSave = envelope;
            YG2.SaveProgress();
            return true;
#else
            return false;
#endif
        }

        private static bool IsPluginReady()
        {
#if YandexGamesPlatform_yg
            return YG2.isSDKEnabled;
#else
            return true;
#endif
        }

        private static async UniTask<bool> AwaitPluginReadyAsync(CancellationToken cancellationToken)
        {
            if (IsPluginReady())
            {
                return true;
            }

            var readyTask = UniTask.WaitUntil(IsPluginReady, cancellationToken: cancellationToken);
            var timeoutTask = UniTask.Delay(PLUGIN_READY_TIMEOUT_MS, cancellationToken: cancellationToken);
            var completed = await UniTask.WhenAny(readyTask, timeoutTask);
            if (completed == 0)
            {
                return true;
            }

            EvoDebug.LogWarning($"PluginYG2 is not ready after {PLUGIN_READY_TIMEOUT_MS} ms.", SOURCE);
            return false;
        }

        private static async UniTask AwaitStorageDataAsync(CancellationToken cancellationToken)
        {
#if YandexGamesPlatform_yg && Storage_yg
            var tcs = new UniTaskCompletionSource();

            void OnDataLoaded()
            {
                tcs.TrySetResult();
            }

            YG2.onGetSDKData += OnDataLoaded;
            try
            {
                YGInsides.LoadProgress();
                var completed = await UniTask.WhenAny(
                    tcs.Task,
                    UniTask.Delay(PLUGIN_READY_TIMEOUT_MS, cancellationToken: cancellationToken));
                if (completed != 0)
                {
                    EvoDebug.LogWarning($"Cloud data is not loaded after {PLUGIN_READY_TIMEOUT_MS} ms.", SOURCE);
                }
            }
            finally
            {
                YG2.onGetSDKData -= OnDataLoaded;
            }
#endif
        }
    }

    public sealed class YandexPlayerAuthService : IPlayerAuthService
    {
        public bool IsAuthorized { get; private set; }
        public string PlayerName { get; private set; } = string.Empty;

        public UniTask InitializeAsync(System.Threading.CancellationToken cancellationToken = default)
        {
#if YandexGamesPlatform_yg && Authorization_yg
            if (!YG2.isSDKEnabled)
            {
                IsAuthorized = false;
                PlayerName = string.Empty;
                return UniTask.CompletedTask;
            }

            YG2.GetAuth();
            IsAuthorized = YG2.player.auth;
#else
            IsAuthorized = false;
#endif
            PlayerName = string.Empty;
            return UniTask.CompletedTask;
        }

        public UniTask RequestAuthorizationAsync(System.Threading.CancellationToken cancellationToken = default)
        {
#if YandexGamesPlatform_yg && Authorization_yg
            YG2.OpenAuthDialog();
            YG2.GetAuth();
            IsAuthorized = YG2.player.auth;
#else
            IsAuthorized = false;
#endif
            return UniTask.CompletedTask;
        }
    }
}
