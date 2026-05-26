using System;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [Serializable]
    public sealed class PlayerSettingsOverrides
    {
        [SerializeField] private bool overrideProductName;
        [SerializeField] private string productName;
        [SerializeField] private bool overrideBundleVersion;
        [SerializeField] private string bundleVersion;
        [SerializeField] private bool overrideApplicationIdentifier;
        [SerializeField] private string applicationIdentifier;
        [SerializeField] private bool overrideOrientation;
        [SerializeField] private UIOrientation defaultOrientation = UIOrientation.LandscapeLeft;
        [SerializeField] private bool autorotateToPortrait;
        [SerializeField] private bool autorotateToPortraitUpsideDown;
        [SerializeField] private bool autorotateToLandscapeLeft = true;
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
    }
}
