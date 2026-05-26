using UnityEditor;
using UnityEngine;
using Evo.Infrastructure.Services.PlatformInfo.Config;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public sealed class EvoBuildWindow : EditorWindow
    {
        private const string MenuPath = "EvoTools/Build/Open Window";

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

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Build Config", EditorStyles.boldLabel);
            _globalConfig = (BuildGlobalConfig)EditorGUILayout.ObjectField("Global Config", _globalConfig, typeof(BuildGlobalConfig), false);
            DrawProfileSelector();
            _platformCatalog = (PlatformCatalog)EditorGUILayout.ObjectField("Platform Catalog", _platformCatalog, typeof(PlatformCatalog), false);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Create Default Build Scaffold", GUILayout.Height(24f)))
            {
                CreateDefaultScaffold();
            }

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
            _profile = (PlatformBuildProfile)EditorGUILayout.ObjectField("Profile", _profile, typeof(PlatformBuildProfile), false);
            var profiles = _globalConfig?.Profiles;
            if (profiles == null || profiles.Count == 0)
            {
                return;
            }

            var names = new string[profiles.Count];
            var currentIndex = -1;
            for (var i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                names[i] = profile == null ? "<missing>" : profile.DisplayName;
                if (profile == _profile)
                {
                    currentIndex = i;
                }
            }

            if (currentIndex >= 0)
            {
                _selectedProfileIndex = currentIndex;
            }
            else if (_profile == null && profiles.Count > 0)
            {
                _selectedProfileIndex = Mathf.Clamp(_selectedProfileIndex, 0, profiles.Count - 1);
                _profile = profiles[_selectedProfileIndex];
            }
            else if (_selectedProfileIndex < 0 || _selectedProfileIndex >= profiles.Count)
            {
                _selectedProfileIndex = 0;
            }

            var nextIndex = EditorGUILayout.Popup("Profile From Config", _selectedProfileIndex, names);
            if (nextIndex != _selectedProfileIndex)
            {
                _selectedProfileIndex = nextIndex;
                _profile = profiles[_selectedProfileIndex];
                _report = null;
                _applyResult = null;
            }
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

            _report = null;
            _applyResult = null;
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

            var outputPath = EvoBuildExecutor.ResolveOutputPath(_globalConfig, _profile);
            var title = buildAndRun ? "Build And Run" : "Build";
            if (!EditorUtility.DisplayDialog("Evo Build", $"{title} profile '{_profile.DisplayName}'?\n\nOutput:\n{outputPath}", title, "Cancel"))
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
