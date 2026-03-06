using _Project.Scripts.Infrastructure.AddressablesExtension;
using _Project.Scripts.Infrastructure.Services.Config;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace _Project.Scripts.Application.Config
{
    [CreateAssetMenu(fileName = "ProjectConfig", menuName = "Project/Project Config")]
    [GameConfig("Project")]
    public sealed class ProjectConfig : ScriptableObject, IGameConfig
    {
        [SerializeField] private AssetReferenceScene startupScene;
        [SerializeField] private AssetReferenceScene gameplayScene;
        [SerializeField] private AssetReferenceScene loadingScene;
        [SerializeField] private string transitionSceneName;
        [SerializeField] private float virtualInputHideSeconds = 2f;
        [SerializeField] private float deathDespawnSeconds = 1.5f;

        public AssetReferenceScene StartupScene => startupScene;
        public AssetReferenceScene GameplayScene => gameplayScene;
        public AssetReferenceScene LoadingScene => loadingScene;
        public string TransitionSceneName => transitionSceneName;
        public float VirtualInputHideSeconds => virtualInputHideSeconds;
        public float DeathDespawnSeconds => deathDespawnSeconds;
    }
}
