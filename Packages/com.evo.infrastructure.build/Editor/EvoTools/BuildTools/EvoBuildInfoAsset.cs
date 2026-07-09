using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CreateAssetMenu(fileName = "EvoBuildInfo", menuName = "EvoTools/Build/Build Info")]
    public sealed class EvoBuildInfoAsset : ScriptableObject
    {
        [SerializeField] private string appVersion;
        [SerializeField] private string platformId;
        [SerializeField] private string profileId;
        [SerializeField] private string buildMode;
        [SerializeField] private bool developmentBuild;
        [SerializeField] private int androidVersionCode;
        [SerializeField] private string iosBuildNumber;
        [SerializeField] private long builtAtUnixMs;
        [SerializeField] private string outputPath;

        public string AppVersion => appVersion;
        public string PlatformId => platformId;
        public string ProfileId => profileId;
        public string BuildMode => buildMode;
        public bool DevelopmentBuild => developmentBuild;
        public int AndroidVersionCode => androidVersionCode;
        public string IosBuildNumber => iosBuildNumber;
        public long BuiltAtUnixMs => builtAtUnixMs;
        public string OutputPath => outputPath;

        public void Capture(PlatformBuildProfile profile, string valueOutputPath)
        {
            appVersion = PlayerSettings.bundleVersion;
            platformId = profile?.PlatformId ?? string.Empty;
            profileId = profile?.ProfileId ?? string.Empty;
            buildMode = profile?.BuildMode.ToString() ?? string.Empty;
            developmentBuild = profile != null && (profile.BuildOptions & BuildOptions.Development) != 0;
            androidVersionCode = PlayerSettings.Android.bundleVersionCode;
            iosBuildNumber = PlayerSettings.iOS.buildNumber;
            builtAtUnixMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            outputPath = valueOutputPath ?? string.Empty;
            EditorUtility.SetDirty(this);
        }
    }
}
