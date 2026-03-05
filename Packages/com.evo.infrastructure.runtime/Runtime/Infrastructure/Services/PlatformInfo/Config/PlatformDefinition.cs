using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Services.PlatformInfo.Config
{
    [CreateAssetMenu(fileName = "PlatformDefinition", menuName = "Project/Platform/Platform Definition")]
    public sealed class PlatformDefinition : ScriptableObject
    {
        [SerializeField] private string platformId;
        [SerializeField] private string displayName;
        [SerializeField] private List<string> defines = new();

        public string PlatformId => platformId;
        public string DisplayName => displayName;
        public IReadOnlyList<string> Defines => defines;
    }
}
