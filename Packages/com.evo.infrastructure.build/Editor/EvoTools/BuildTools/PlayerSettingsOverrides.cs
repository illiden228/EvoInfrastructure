using System;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [Serializable]
    public sealed class PlayerSettingsOverrides
    {
        [SerializeField] private bool overrideProductName;
#if ODIN_INSPECTOR
        [ShowIf(nameof(overrideProductName))]
#endif
        [SerializeField] private string productName;
        [SerializeField] private bool overrideBundleVersion;
#if ODIN_INSPECTOR
        [ShowIf(nameof(overrideBundleVersion))]
#endif
        [SerializeField] private string bundleVersion;
        [SerializeField] private bool overrideApplicationIdentifier;
#if ODIN_INSPECTOR
        [ShowIf(nameof(overrideApplicationIdentifier))]
#endif
        [SerializeField] private string applicationIdentifier;
        [SerializeField] private bool overrideOrientation;
#if ODIN_INSPECTOR
        [ShowIf(nameof(overrideOrientation))]
#endif
        [SerializeField] private UIOrientation defaultOrientation = UIOrientation.LandscapeLeft;
#if ODIN_INSPECTOR
        [ShowIf(nameof(overrideOrientation))]
#endif
        [SerializeField] private bool autorotateToPortrait;
#if ODIN_INSPECTOR
        [ShowIf(nameof(overrideOrientation))]
#endif
        [SerializeField] private bool autorotateToPortraitUpsideDown;
#if ODIN_INSPECTOR
        [ShowIf(nameof(overrideOrientation))]
#endif
        [SerializeField] private bool autorotateToLandscapeLeft = true;
#if ODIN_INSPECTOR
        [ShowIf(nameof(overrideOrientation))]
#endif
        [SerializeField] private bool autorotateToLandscapeRight = true;

        public bool OverrideProductName => overrideProductName;
        public string ProductName => productName;
        public bool OverrideBundleVersion => overrideBundleVersion;
        public string BundleVersion => bundleVersion;
        public bool OverrideApplicationIdentifier => overrideApplicationIdentifier;
        public string ApplicationIdentifier => applicationIdentifier;
        public bool OverrideOrientation => overrideOrientation;
        public UIOrientation DefaultOrientation => defaultOrientation;
        public bool AutorotateToPortrait => autorotateToPortrait;
        public bool AutorotateToPortraitUpsideDown => autorotateToPortraitUpsideDown;
        public bool AutorotateToLandscapeLeft => autorotateToLandscapeLeft;
        public bool AutorotateToLandscapeRight => autorotateToLandscapeRight;

        internal void SetBundleVersion(string value)
        {
            bundleVersion = value ?? string.Empty;
        }
    }
}
