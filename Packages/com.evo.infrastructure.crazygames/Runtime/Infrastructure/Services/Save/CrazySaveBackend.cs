using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.CrazyGames;
using Evo.Infrastructure.Services.Debug;
using UnityEngine;
#if CRAZY
using CrazyGames;
#endif

namespace Evo.Infrastructure.Services.Save
{
    public sealed class CrazySaveBackend : ISaveBackend
    {
        private const string SOURCE = nameof(CrazySaveBackend);
        private const string DEFAULT_SAVE_KEY = "EVO_SAVE_FULL_CRAZY";
        private readonly string _saveKey;

        public string BackendId => "crazy";
        public int Priority => 90;

        public bool IsAvailable
        {
            get
            {
#if CRAZY
                return CrazyGamesSdk.IsSupportedRuntime;
#else
                return false;
#endif
            }
        }

        public CrazySaveBackend(IConfigService configService = null)
        {
            _saveKey = ResolveSaveKey(configService);
        }

        public async UniTask<SaveEnvelope> LoadAsync(CancellationToken cancellationToken = default)
        {
#if CRAZY
            if (!await CrazyGamesSdk.AwaitReadyWithRetriesAsync(cancellationToken))
            {
                return null;
            }

            try
            {
                var json = CrazySDK.Data.GetString(_saveKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonUtility.FromJson<SaveEnvelope>(json);
            }
            catch (System.Exception ex)
            {
                EvoDebug.LogWarning($"LoadAsync failed: {ex.Message}", SOURCE);
                return null;
            }
#else
            await UniTask.CompletedTask;
            return null;
#endif
        }

        public async UniTask<bool> SaveAsync(SaveEnvelope envelope, CancellationToken cancellationToken = default)
        {
#if CRAZY
            if (envelope == null || !await CrazyGamesSdk.AwaitReadyWithRetriesAsync(cancellationToken))
            {
                return false;
            }

            try
            {
                var json = JsonUtility.ToJson(envelope);
                CrazySDK.Data.SetString(_saveKey, json);
                CrazySDK.User.SyncUnityGameData();
                return true;
            }
            catch (System.Exception ex)
            {
                EvoDebug.LogWarning($"SaveAsync failed: {ex.Message}", SOURCE);
                return false;
            }
#else
            await UniTask.CompletedTask;
            return false;
#endif
        }

        private static string ResolveSaveKey(IConfigService configService)
        {
            if (configService != null &&
                configService.TryGet<CrazyGamesRuntimeConfig>(out var config) &&
                config != null &&
                !string.IsNullOrWhiteSpace(config.SaveKey))
            {
                return config.SaveKey;
            }

            return DEFAULT_SAVE_KEY;
        }
    }
}
