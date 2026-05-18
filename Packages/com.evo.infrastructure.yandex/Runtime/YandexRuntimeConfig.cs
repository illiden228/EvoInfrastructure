using Evo.Infrastructure.Services.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Yandex
{
    [GameConfig("Yandex")]
    [CreateAssetMenu(fileName = "YandexRuntimeConfig", menuName = "Project/Yandex/Runtime Config")]
    public sealed class YandexRuntimeConfig : ScriptableObject, IGameConfig
    {
        [SerializeField] private bool ads = true;
        [SerializeField] private bool analytics = true;
        [SerializeField] private bool leaderboard = true;
        [SerializeField] private bool platformInfo = true;
        [SerializeField] private bool cloudSave = true;
        [SerializeField] private bool playerAuth = true;

        public bool Ads => ads;
        public bool Analytics => analytics;
        public bool Leaderboard => leaderboard;
        public bool PlatformInfo => platformInfo;
        public bool CloudSave => cloudSave;
        public bool PlayerAuth => playerAuth;
    }
}
