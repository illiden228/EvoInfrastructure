using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Evo.Infrastructure.Services.Save
{
    public sealed class PrefsSaveBackend : ISaveBackend
    {
        private readonly string _saveKey;

        public PrefsSaveBackend(SaveStorageOptions options = null)
        {
            _saveKey = !string.IsNullOrWhiteSpace(options?.playerPrefsKey)
                ? options.playerPrefsKey
                : SaveStorageDefaults.PlayerPrefsKey;
        }

        public string BackendId => "prefs";
        public int Priority => 10;
        public bool IsAvailable => true;

        public async UniTask<SaveEnvelope> LoadAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            if (!PlayerPrefs.HasKey(_saveKey))
            {
                return null;
            }

            var json = PlayerPrefs.GetString(_saveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<SaveEnvelope>(json);
        }

        public async UniTask<bool> SaveAsync(SaveEnvelope envelope, System.Threading.CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            var json = JsonUtility.ToJson(envelope);
            PlayerPrefs.SetString(_saveKey, json);
            PlayerPrefs.Save();
            return true;
        }
    }
}
