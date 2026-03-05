using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Infrastructure.Services.Save
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int schemaVersion = 1;
        public long updatedAtUnixMs;
        public ProfileSaveData profile = new();
    }

    [Serializable]
    public sealed class ProfileSaveData
    {
        public string nickname;
        public int wins;
        public int losses;
        public int gamesCompleted;
        public int kills;
        public int balance;
        public long lastRewardedAdUnixMs;
        public string equippedSkinId = string.Empty;
        public List<string> ownedSkinIds = new();
        public List<SkinAdProgressSaveData> skinAdProgress = new();
        public bool interstitialWarm;
        public string lastSessionDate = string.Empty;
        public string lastAppVersion = string.Empty;
        public string lastOsVersion = string.Empty;
        public float masterVolume = 1f;
        public float backgroundVolume = 1f;
        public float effectsVolume = 1f;
        public float uiEffectsVolume = 1f;
        public float sensitivity = 1f;
        public string localeCode = string.Empty;
    }

    [Serializable]
    public sealed class SkinAdProgressSaveData
    {
        public string skinId = string.Empty;
        public int views;
        public long lastViewUnixMs;
    }

    public interface IPlayerAuthService
    {
        bool IsAuthorized { get; }
        string PlayerName { get; }
        UniTask InitializeAsync(CancellationToken cancellationToken = default);
        UniTask RequestAuthorizationAsync(CancellationToken cancellationToken = default);
    }

    public interface ISaveBackend
    {
        string BackendId { get; }
        int Priority { get; }
        bool IsAvailable { get; }
        UniTask<SaveEnvelope> LoadAsync(CancellationToken cancellationToken = default);
        UniTask<bool> SaveAsync(SaveEnvelope envelope, CancellationToken cancellationToken = default);
    }

    public interface ISaveService
    {
        UniTask<SaveEnvelope> LoadLatestValidAsync(CancellationToken cancellationToken = default);
        UniTask SaveAsync(SaveEnvelope envelope, CancellationToken cancellationToken = default);
    }
}
