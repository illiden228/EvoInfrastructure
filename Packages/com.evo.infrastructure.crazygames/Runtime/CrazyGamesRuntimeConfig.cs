using Evo.Infrastructure.Services.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.CrazyGames
{
    [GameConfig("CrazyGames")]
    [CreateAssetMenu(fileName = "CrazyGamesRuntimeConfig", menuName = "Project/CrazyGames/Runtime Config")]
    public sealed class CrazyGamesRuntimeConfig : ScriptableObject, IGameConfig
    {
        [SerializeField] private bool ads = true;
        [SerializeField] private bool leaderboard = true;
        [SerializeField] private bool platformInfo = true;
        [SerializeField] private bool platformLifecycle = true;
        [SerializeField] private bool cloudSave = true;
        [SerializeField] private bool playerAuth = true;
        [SerializeField] private string saveKey = "EVO_SAVE_FULL_CRAZY";

        public bool Ads => ads;
        public bool Leaderboard => leaderboard;
        public bool PlatformInfo => platformInfo;
        public bool PlatformLifecycle => platformLifecycle;
        public bool CloudSave => cloudSave;
        public bool PlayerAuth => playerAuth;
        public string SaveKey => string.IsNullOrWhiteSpace(saveKey) ? "EVO_SAVE_FULL_CRAZY" : saveKey;
    }
}
