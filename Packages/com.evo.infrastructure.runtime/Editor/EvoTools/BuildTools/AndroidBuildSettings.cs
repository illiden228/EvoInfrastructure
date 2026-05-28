using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [Serializable]
    public sealed class AndroidBuildSettings
    {
        [SerializeField] private bool overrideBuildAppBundle;
#if ODIN_INSPECTOR
        [ShowIf(nameof(overrideBuildAppBundle))]
#endif
        [SerializeField] private bool buildAppBundle;

        public bool OverrideBuildAppBundle => overrideBuildAppBundle;
        public bool BuildAppBundle => buildAppBundle;
    }
}
