using UnityEngine;

namespace Evo.Infrastructure.Services.PlatformInfo.Config
{
    [CreateAssetMenu(fileName = "PlatformDefinition", menuName = "Project/Platform/Platform Definition")]
    public sealed class PlatformDefinition : ScriptableObject
    {
        [SerializeField] private string platformId;
        [SerializeField] private string displayName;

        public string PlatformId => platformId;
        public string DisplayName => displayName;
    }
}
