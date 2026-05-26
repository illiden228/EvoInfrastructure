using System.Collections.Generic;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using UnityEditor;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CustomEditor(typeof(PlatformBuildProfile))]
    internal sealed class PlatformBuildProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            DrawPlatformIdSelector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            var profile = (PlatformBuildProfile)target;
            EditorGUILayout.LabelField("Resolved Build Target Group", profile.BuildTargetGroup.ToString());

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Dry Run"))
                {
                    var report = EvoBuildPlanner.CreateDryRun(FindFirstBuildGlobalConfig(), profile);
                    ShowReport(report);
                }

                if (GUILayout.Button("Apply This Profile"))
                {
                    Apply(profile);
                }
            }
        }

        private void DrawPlatformIdSelector()
        {
            var platformIdProperty = serializedObject.FindProperty("platformId");
            if (platformIdProperty == null)
            {
                return;
            }

            var ids = CollectPlatformIds();
            if (ids.Count == 0)
            {
                return;
            }

            var currentIndex = Mathf.Max(0, ids.IndexOf(platformIdProperty.stringValue));
            var nextIndex = EditorGUILayout.Popup("Platform Id From Catalog", currentIndex, ids.ToArray());
            if (nextIndex >= 0 && nextIndex < ids.Count)
            {
                platformIdProperty.stringValue = ids[nextIndex];
            }
        }

        private static List<string> CollectPlatformIds()
        {
            var result = new List<string>();
            var guids = AssetDatabase.FindAssets("t:PlatformCatalog");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<PlatformCatalog>(path);
                var entries = catalog?.Entries;
                if (entries == null)
                {
                    continue;
                }

                for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    var entry = entries[entryIndex];
                    var id = entry?.PlatformId;
                    if (!string.IsNullOrWhiteSpace(id) && !result.Contains(id))
                    {
                        result.Add(id);
                    }
                }
            }

            result.Sort(string.CompareOrdinal);
            return result;
        }

        private static void Apply(PlatformBuildProfile profile)
        {
            var catalog = FindFirstPlatformCatalog();
            var globalConfig = FindFirstBuildGlobalConfig();
            var report = EvoBuildPlanner.CreateDryRun(globalConfig, profile);
            if (report.HasErrors)
            {
                ShowReport(report);
                return;
            }

            if (report.RequiresDefineRemovalConfirmation && !EditorUtility.DisplayDialog(
                    "Confirm Define Removal",
                    "The selected profile will remove these scripting define symbols:\n\n" + string.Join("\n", report.WillBeRemovedDefines),
                    "Apply",
                    "Cancel"))
            {
                return;
            }

            var result = EvoBuildApplier.ApplyPlatform(globalConfig, profile, catalog, switchBuildTarget: true);
            var message = result.Success ? string.Join("\n", result.Messages) : string.Join("\n", result.Errors);
            EditorUtility.DisplayDialog("Evo Build", string.IsNullOrWhiteSpace(message) ? "Done." : message, "OK");
        }

        private static PlatformCatalog FindFirstPlatformCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:PlatformCatalog");
            if (guids.Length == 0)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<PlatformCatalog>(path);
        }

        private static BuildGlobalConfig FindFirstBuildGlobalConfig()
        {
            var guids = AssetDatabase.FindAssets("t:BuildGlobalConfig");
            if (guids.Length == 0)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<BuildGlobalConfig>(path);
        }

        private static void ShowReport(EvoBuildDryRunReport report)
        {
            var message = report.HasErrors
                ? string.Join("\n", report.Errors)
                : "No errors.\n\nWill add defines:\n" + string.Join("\n", report.WillBeAddedDefines) +
                  "\n\nWill remove defines:\n" + string.Join("\n", report.WillBeRemovedDefines);
            EditorUtility.DisplayDialog("Evo Build Dry Run", message, "OK");
        }
    }
}
