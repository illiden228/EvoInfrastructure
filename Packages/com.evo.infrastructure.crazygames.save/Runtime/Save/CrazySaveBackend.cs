using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.CrazyGames;
using Evo.Infrastructure.Services.Debug;
using UnityEngine;

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
                return CrazyGamesSdk.IsSupportedRuntime;
            }
        }

        public CrazySaveBackend(IConfigService configService = null)
        {
            _saveKey = ResolveSaveKey(configService);
        }

        public async UniTask<SaveEnvelope> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (!await CrazyGamesSdk.AwaitReadyWithRetriesAsync(cancellationToken))
            {
                return null;
            }

            try
            {
                if (!CrazyGamesSdk.TryGetDataString(_saveKey, out var json))
                {
                    return null;
                }

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
        }

        public async UniTask<bool> SaveAsync(SaveEnvelope envelope, CancellationToken cancellationToken = default)
        {
            if (envelope == null || !await CrazyGamesSdk.AwaitReadyWithRetriesAsync(cancellationToken))
            {
                return false;
            }

            try
            {
                var json = JsonUtility.ToJson(envelope);
                return CrazyGamesSdk.TrySetDataString(_saveKey, json);
            }
            catch (System.Exception ex)
            {
                EvoDebug.LogWarning($"SaveAsync failed: {ex.Message}", SOURCE);
                return false;
            }
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
