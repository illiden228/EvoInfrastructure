using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Evo.Infrastructure.Services.PlatformInfo.Config;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public sealed class EvoBuildWindow : EditorWindow
    {
        private const string MenuPath = "EvoTools/Build/Open Window";
        private const string GlobalConfigGuidKey = "EvoTools.Build.LastGlobalConfigGuid";
        private const string ProfileGuidKey = "EvoTools.Build.LastProfileGuid";
        private const string PlatformCatalogGuidKey = "EvoTools.Build.LastPlatformCatalogGuid";

        private BuildGlobalConfig _globalConfig;
        private PlatformBuildProfile _profile;
        private PlatformCatalog _platformCatalog;
        private EvoBuildDryRunReport _report;
        private EvoBuildApplyResult _applyResult;
        private EvoBuildScaffoldResult _scaffoldResult;
        private int _selectedProfileIndex = -1;
        private Vector2 _scroll;

        [MenuItem(MenuPath, false, 90)]
        public static void Open()
        {
            var window = GetWindow<EvoBuildWindow>();
            window.titleContent = new GUIContent("Evo Build");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadLastSelection();
            AutoAssignMissingAssets();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Build Config", EditorStyles.boldLabel);
            var nextGlobalConfig = (BuildGlobalConfig)EditorGUILayout.ObjectField("Global Config", _globalConfig, typeof(BuildGlobalConfig), false);
            if (nextGlobalConfig != _globalConfig)
            {
                _globalConfig = nextGlobalConfig;
                _profile = null;
                _selectedProfileIndex = -1;
                AutoAssignMissingProfile();
                SaveLastSelection();
                ClearResults();
            }

            DrawProfileSelector();
            var nextPlatformCatalog = (PlatformCatalog)EditorGUILayout.ObjectField("Platform Catalog", _platformCatalog, typeof(PlatformCatalog), false);
            if (nextPlatformCatalog != _platformCatalog)
            {
                _platformCatalog = nextPlatformCatalog;
                SaveLastSelection();
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Create Default Build Scaffold", GUILayout.Height(24f)))
            {
                CreateDefaultScaffold();
            }

            DrawVersionControls();

            using (new EditorGUI.DisabledScope(_profile == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Dry Run", GUILayout.Height(26f)))
                {
                    _report = EvoBuildPlanner.CreateDryRun(_globalConfig, _profile);
                    _applyResult = null;
                }

                if (GUILayout.Button("Apply Platform", GUILayout.Height(26f)))
                {
                    ApplyPlatform();
                }

                if (GUILayout.Button("Build", GUILayout.Height(26f)))
                {
                    Build(buildAndRun: false);
                }

                if (GUILayout.Button("Build And Run", GUILayout.Height(26f)))
                {
                    Build(buildAndRun: true);
                }
                EditorGUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(_globalConfig == null))
            {
                if (GUILayout.Button("Generate Menu", GUILayout.Height(26f)))
                {
                    GenerateMenu();
                }
            }

            EditorGUILayout.Space(8f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawReport();
            DrawApplyResult();
            DrawScaffoldResult();
            EditorGUILayout.EndScrollView();
        }

        private void DrawProfileSelector()
        {
            var profiles = CollectSelectableProfiles();
            if (profiles.Count > 0)
            {
                var names = new string[profiles.Count];
                var currentIndex = -1;
                for (var i = 0; i < profiles.Count; i++)
                {
                    var profile = profiles[i];
                    names[i] = BuildProfileLabel(profile);
                    if (profile == _profile)
                    {
                        currentIndex = i;
                    }
                }

                if (currentIndex >= 0)
                {
                    _selectedProfileIndex = currentIndex;
                }
                else if (_profile == null)
                {
                    _selectedProfileIndex = Mathf.Clamp(_selectedProfileIndex, 0, profiles.Count - 1);
                    _profile = profiles[_selectedProfileIndex];
                    SaveLastSelection();
                }
                else if (_selectedProfileIndex < 0 || _selectedProfileIndex >= profiles.Count)
                {
                    _selectedProfileIndex = 0;
                }

                var nextIndex = EditorGUILayout.Popup(_globalConfig == null ? "Profile" : "Profile From Config", _selectedProfileIndex, names);
                if (nextIndex != _selectedProfileIndex)
                {
                    _selectedProfileIndex = nextIndex;
                    _profile = profiles[_selectedProfileIndex];
                    SaveLastSelection();
                    ClearResults();
                }
            }

            if (_globalConfig == null || profiles.Count == 0)
            {
                var nextProfile = (PlatformBuildProfile)EditorGUILayout.ObjectField("Profile Asset", _profile, typeof(PlatformBuildProfile), false);
                if (nextProfile != _profile)
                {
                    _profile = nextProfile;
                    SaveLastSelection();
                    ClearResults();
                }
            }

            using (new EditorGUI.DisabledScope(_profile == null))
            {
                if (GUILayout.Button("Ping Selected Profile", GUILayout.Height(22f)))
                {
                    EditorGUIUtility.PingObject(_profile);
                    Selection.activeObject = _profile;
                }
            }
        }

        private void DrawVersionControls()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Version", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Bundle Version", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Current", PlayerSettings.bundleVersion);
                DrawBundleVersionControls();

                if (_profile != null && _profile.PlayerSettings != null && !_profile.PlayerSettings.OverrideBundleVersion)
                {
                    EditorGUILayout.HelpBox(
                        "Selected profile does not override bundleVersion. Manual bump changes PlayerSettings only.",
                        MessageType.Info);
                }

                if (_profile != null && _profile.BuildTarget == BuildTarget.Android)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Android Version Code", EditorStyles.miniBoldLabel);
                    DrawNumberControl(
                        "Version Code",
                        PlayerSettings.Android.bundleVersionCode.ToString(),
                        () => ChangeAndroidVersionCode(-1),
                        () => ChangeAndroidVersionCode(1),
                        enabled: true);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("+ Patch + Code", GUILayout.Width(132f), GUILayout.Height(24f)))
                        {
                            BumpPatchAndAndroidVersionCode();
                        }
                    }
                }
            }
        }

        private void DrawBundleVersionControls()
        {
            ParseBundleVersion(PlayerSettings.bundleVersion, out var major, out var minor, out var patch);

            using (new EditorGUI.DisabledScope(_profile == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawNumberControl(
                        "Major",
                        major.ToString(),
                        () => ChangeBundleVersion(EvoVersionBumpMode.Major, -1),
                        () => ChangeBundleVersion(EvoVersionBumpMode.Major, 1),
                        enabled: true);
                    DrawVersionSeparator();
                    DrawNumberControl(
                        "Minor",
                        minor.ToString(),
                        () => ChangeBundleVersion(EvoVersionBumpMode.Minor, -1),
                        () => ChangeBundleVersion(EvoVersionBumpMode.Minor, 1),
                        enabled: true);
                    DrawVersionSeparator();
                    DrawNumberControl(
                        "Patch",
                        patch.ToString(),
                        () => ChangeBundleVersion(EvoVersionBumpMode.Patch, -1),
                        () => ChangeBundleVersion(EvoVersionBumpMode.Patch, 1),
                        enabled: true);
                    GUILayout.FlexibleSpace();
                }
            }
        }

        private static void DrawNumberControl(string label, string value, System.Action decrement, System.Action increment, bool enabled)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(96f)))
            {
                EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(96f));
                using (new EditorGUI.DisabledScope(!enabled))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("-", GUILayout.Width(26f), GUILayout.Height(24f)))
                        {
                            decrement?.Invoke();
                        }

                        var valueStyle = new GUIStyle(EditorStyles.helpBox)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontStyle = FontStyle.Bold
                        };
                        GUILayout.Label(value, valueStyle, GUILayout.Width(38f), GUILayout.Height(24f));

                        if (GUILayout.Button("+", GUILayout.Width(26f), GUILayout.Height(24f)))
                        {
                            increment?.Invoke();
                        }
                    }
                }
            }
        }

        private static void DrawVersionSeparator()
        {
            EditorGUILayout.LabelField(".", EditorStyles.boldLabel, GUILayout.Width(10f), GUILayout.Height(42f));
        }

        private static void ParseBundleVersion(string version, out int major, out int minor, out int patch)
        {
            var parts = (version ?? "0.0.0").Split('.');
            major = ParseVersionPart(parts, 0);
            minor = ParseVersionPart(parts, 1);
            patch = ParseVersionPart(parts, 2);
        }

        private static int ParseVersionPart(string[] parts, int index)
        {
            if (parts == null || index < 0 || index >= parts.Length)
            {
                return 0;
            }

            return int.TryParse(parts[index], out var value) ? Mathf.Max(0, value) : 0;
        }

        private List<PlatformBuildProfile> CollectSelectableProfiles()
        {
            var result = new List<PlatformBuildProfile>();
            var configProfiles = _globalConfig?.Profiles;
            if (configProfiles != null)
            {
                for (var i = 0; i < configProfiles.Count; i++)
                {
                    AddProfile(result, configProfiles[i]);
                }
            }

            if (result.Count > 0)
            {
                return result;
            }

            var guids = AssetDatabase.FindAssets("t:PlatformBuildProfile");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AddProfile(result, AssetDatabase.LoadAssetAtPath<PlatformBuildProfile>(path));
            }

            result.Sort((left, right) => string.CompareOrdinal(BuildProfileLabel(left), BuildProfileLabel(right)));
            return result;
        }

        private static void AddProfile(List<PlatformBuildProfile> profiles, PlatformBuildProfile profile)
        {
            if (profile != null && !profiles.Contains(profile))
            {
                profiles.Add(profile);
            }
        }

        private static string BuildProfileLabel(PlatformBuildProfile profile)
        {
            if (profile == null)
            {
                return "<missing>";
            }

            return $"{profile.DisplayName} | {profile.PlatformId} | {profile.BuildTarget}";
        }

        private void CreateDefaultScaffold()
        {
            _scaffoldResult = EvoBuildScaffold.EnsureDefaultAssets();
            if (_scaffoldResult.GlobalConfig != null)
            {
                _globalConfig = _scaffoldResult.GlobalConfig;
                if (_globalConfig.Profiles.Count > 0)
                {
                    _selectedProfileIndex = 0;
                    _profile = _globalConfig.Profiles[0];
                }
            }

            SaveLastSelection();
            ClearResults();
        }

        private void ApplyPlatform()
        {
            _report = EvoBuildPlanner.CreateDryRun(_globalConfig, _profile);
            _applyResult = null;
            if (_report.HasErrors)
            {
                return;
            }

            if (_report.RequiresDefineRemovalConfirmation && !ConfirmDefineRemoval(_report))
            {
                return;
            }

            _applyResult = EvoBuildApplier.ApplyPlatform(
                _globalConfig,
                _profile,
                _platformCatalog,
                switchBuildTarget: true);

            _report = EvoBuildPlanner.CreateDryRun(_globalConfig, _profile);
        }

        private void Build(bool buildAndRun)
        {
            _report = EvoBuildPlanner.CreateDryRun(_globalConfig, _profile);
            _applyResult = null;
            if (_report.HasErrors)
            {
                return;
            }

            if (_report.RequiresDefineRemovalConfirmation && !ConfirmDefineRemoval(_report))
            {
                return;
            }

            _applyResult = EvoBuildExecutor.Build(_globalConfig, _profile, _platformCatalog, buildAndRun);
            _report = EvoBuildPlanner.CreateDryRun(_globalConfig, _profile);
        }

        private void GenerateMenu()
        {
            if (_globalConfig == null)
            {
                EditorUtility.DisplayDialog("Evo Build", "Global Config is required to generate build menu.", "OK");
                return;
            }

            var path = EvoBuildMenuGenerator.Generate(_globalConfig, _platformCatalog);
            EditorUtility.DisplayDialog("Evo Build", $"Generated build menu:\n{path}", "OK");
        }

        private void ChangeBundleVersion(EvoVersionBumpMode mode, int direction)
        {
            var current = PlayerSettings.bundleVersion;
            var next = IncrementBundleVersionStep.ChangeVersion(current, mode, direction);
            PlayerSettings.bundleVersion = next;
            if (_profile != null && _profile.SyncBundleVersionOverride(next))
            {
                EditorUtility.SetDirty(_profile);
            }

            AssetDatabase.SaveAssets();
            _report = EvoBuildPlanner.CreateDryRun(_globalConfig, _profile);
            _applyResult = new EvoBuildApplyResult();
            _applyResult.AddMessage($"Bundle version: {current} -> {next}");
        }

        private void ChangeAndroidVersionCode(int delta)
        {
            var current = PlayerSettings.Android.bundleVersionCode;
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, current + delta);
            AssetDatabase.SaveAssets();
            _report = EvoBuildPlanner.CreateDryRun(_globalConfig, _profile);
            _applyResult = new EvoBuildApplyResult();
            _applyResult.AddMessage($"Android versionCode: {current} -> {PlayerSettings.Android.bundleVersionCode}");
        }

        private void BumpPatchAndAndroidVersionCode()
        {
            var currentVersion = PlayerSettings.bundleVersion;
            var currentCode = PlayerSettings.Android.bundleVersionCode;
            var nextVersion = IncrementBundleVersionStep.ChangeVersion(currentVersion, EvoVersionBumpMode.Patch, 1);
            PlayerSettings.bundleVersion = nextVersion;
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, currentCode + 1);
            if (_profile != null && _profile.SyncBundleVersionOverride(nextVersion))
            {
                EditorUtility.SetDirty(_profile);
            }

            AssetDatabase.SaveAssets();
            _report = EvoBuildPlanner.CreateDryRun(_globalConfig, _profile);
            _applyResult = new EvoBuildApplyResult();
            _applyResult.AddMessage($"Bundle version: {currentVersion} -> {nextVersion}");
            _applyResult.AddMessage($"Android versionCode: {currentCode} -> {PlayerSettings.Android.bundleVersionCode}");
        }

        private void LoadLastSelection()
        {
            _globalConfig = LoadAssetFromEditorPrefs<BuildGlobalConfig>(GlobalConfigGuidKey);
            _profile = LoadAssetFromEditorPrefs<PlatformBuildProfile>(ProfileGuidKey);
            _platformCatalog = LoadAssetFromEditorPrefs<PlatformCatalog>(PlatformCatalogGuidKey);
        }

        private void SaveLastSelection()
        {
            SaveAssetToEditorPrefs(GlobalConfigGuidKey, _globalConfig);
            SaveAssetToEditorPrefs(ProfileGuidKey, _profile);
            SaveAssetToEditorPrefs(PlatformCatalogGuidKey, _platformCatalog);
        }

        private void AutoAssignMissingAssets()
        {
            _globalConfig ??= FindFirstAsset<BuildGlobalConfig>();
            _platformCatalog ??= FindFirstAsset<PlatformCatalog>();
            AutoAssignMissingProfile();
            SaveLastSelection();
        }

        private void AutoAssignMissingProfile()
        {
            var profiles = _globalConfig?.Profiles;
            if (_profile != null || profiles == null || profiles.Count == 0)
            {
                return;
            }

            for (var i = 0; i < profiles.Count; i++)
            {
                if (profiles[i] == null)
                {
                    continue;
                }

                _selectedProfileIndex = i;
                _profile = profiles[i];
                return;
            }
        }

        private void ClearResults()
        {
            _report = null;
            _applyResult = null;
        }

        private static T LoadAssetFromEditorPrefs<T>(string key) where T : UnityEngine.Object
        {
            var guid = EditorPrefs.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void SaveAssetToEditorPrefs(string key, UnityEngine.Object asset)
        {
            if (asset == null)
            {
                EditorPrefs.DeleteKey(key);
                return;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            var guid = string.IsNullOrWhiteSpace(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                EditorPrefs.DeleteKey(key);
                return;
            }

            EditorPrefs.SetString(key, guid);
        }

        private static T FindFirstAsset<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static bool ConfirmDefineRemoval(EvoBuildDryRunReport report)
        {
            var message = "The selected profile will remove these scripting define symbols:\n\n" +
                          string.Join("\n", report.WillBeRemovedDefines) +
                          "\n\nContinue?";
            return EditorUtility.DisplayDialog("Confirm Define Removal", message, "Apply", "Cancel");
        }

        private void DrawReport()
        {
            if (_report == null)
            {
                EditorGUILayout.HelpBox("Select a profile and run Dry Run.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Dry Run", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Profile", _report.ProfileId);
            EditorGUILayout.LabelField("Platform", _report.PlatformId);
            EditorGUILayout.LabelField("Build Target", $"{_report.BuildTargetGroup}/{_report.BuildTarget}");
            EditorGUILayout.LabelField("Define Cleanup", _report.DefineCleanupPolicy.ToString());
            if (_profile != null)
            {
                EditorGUILayout.LabelField("Output", EvoBuildExecutor.ResolveOutputPath(_globalConfig, _profile));
            }

            DrawMessages("Errors", _report.Errors, MessageType.Error);
            DrawMessages("Warnings", _report.Warnings, MessageType.Warning);

            DrawStringList("Will Add Defines", _report.WillBeAddedDefines);
            DrawStringList("Will Remove Defines", _report.WillBeRemovedDefines);
            if (_report.RequiresDefineRemovalConfirmation)
            {
                EditorGUILayout.HelpBox("Apply/Build must ask for confirmation before removing these defines.", MessageType.Warning);
            }

            DrawPlayerSettingsChanges();
            DrawStringList("Target Defines", _report.TargetDefines);
        }

        private void DrawApplyResult()
        {
            if (_applyResult == null)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Apply Result", EditorStyles.boldLabel);
            var messageType = _applyResult.Success ? MessageType.Info : MessageType.Error;
            var title = _applyResult.Success ? "Apply completed." : "Apply failed.";
            EditorGUILayout.HelpBox(title, messageType);

            DrawMessages("Apply Errors", _applyResult.Errors, MessageType.Error);
            DrawMessages("Apply Messages", _applyResult.Messages, MessageType.Info);
        }

        private void DrawScaffoldResult()
        {
            if (_scaffoldResult == null)
            {
                return;
            }

            DrawMessages("Scaffold", _scaffoldResult.Messages, MessageType.Info);
        }

        private static void DrawMessages(string title, System.Collections.Generic.IReadOnlyList<string> messages, MessageType type)
        {
            if (messages == null || messages.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            for (var i = 0; i < messages.Count; i++)
            {
                EditorGUILayout.HelpBox(messages[i], type);
            }
        }

        private static void DrawStringList(string title, System.Collections.Generic.IReadOnlyList<string> values)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"{title} ({values?.Count ?? 0})", EditorStyles.boldLabel);
            if (values == null || values.Count == 0)
            {
                EditorGUILayout.LabelField("None");
                return;
            }

            for (var i = 0; i < values.Count; i++)
            {
                EditorGUILayout.LabelField(values[i]);
            }
        }

        private void DrawPlayerSettingsChanges()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"PlayerSettings Changes ({_report.PlayerSettingsChanges.Count})", EditorStyles.boldLabel);
            if (_report.PlayerSettingsChanges.Count == 0)
            {
                EditorGUILayout.LabelField("None");
                return;
            }

            for (var i = 0; i < _report.PlayerSettingsChanges.Count; i++)
            {
                var change = _report.PlayerSettingsChanges[i];
                EditorGUILayout.LabelField(change.FieldName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Current", change.CurrentValue);
                EditorGUILayout.LabelField("Target", change.TargetValue);
                EditorGUILayout.LabelField("Source", change.Source);
                EditorGUILayout.Space(2f);
            }
        }
    }
}
