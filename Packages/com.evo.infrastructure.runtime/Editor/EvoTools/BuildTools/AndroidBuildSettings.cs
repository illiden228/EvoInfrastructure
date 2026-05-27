using System;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [Serializable]
    public sealed class AndroidBuildSettings
    {
        [SerializeField] private bool overrideBuildAppBundle;
        [SerializeField] private bool buildAppBundle;

        public bool OverrideBuildAppBundle => overrideBuildAppBundle;
        public bool BuildAppBundle => buildAppBundle;
    }
}
