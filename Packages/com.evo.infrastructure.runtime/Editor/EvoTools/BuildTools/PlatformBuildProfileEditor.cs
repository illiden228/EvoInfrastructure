using System.Collections.Generic;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    [CustomEditor(typeof(PlatformBuildProfile))]
    internal sealed class PlatformBuildProfileEditor : UnityEditor.Editor
    {
        private ReorderableList _definesList;
        private ReorderableList _stepsList;

        private void OnEnable()
        {
            _definesList = CreateList("defines", "Build Defines");
            _stepsList = CreateList("steps", "Build Steps");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawMainFields();
            DrawPlatformIdSelector();
            _definesList?.DoLayoutList();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playerSettings"), includeChildren: true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("outputPathTemplate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildOptions"));
            DrawStepsHelp();
            _stepsList?.DoLayoutList();
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

        private void DrawMainFields()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("profileId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("platformId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showInGeneratedMenu"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buildTarget"));
        }

        private static void DrawStepsHelp()
        {
            EditorGUILayout.HelpBox(
                "Steps are executed in phase/order. Version bump steps should use PrepareBuild. Android signing password step should use BeforeBuild.",
                MessageType.Info);
        }

        private ReorderableList CreateList(string propertyName, string label)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return null;
            }

            var isStepsList = propertyName == "steps";
            var list = new ReorderableList(serializedObject, property, draggable: true, displayHeader: true, displayAddButton: true, displayRemoveButton: true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, label),
                elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 4f,
                drawElementCallback = (rect, index, _, _) =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    rect.y += 2f;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    if (isStepsList)
                    {
                        DrawStepPopup(rect, element);
                    }
                    else
                    {
                        EditorGUI.PropertyField(rect, element, GUIContent.none);
                    }
                },
                onAddCallback = _ => AddEmptyElement(property)
            };

            return list;
        }

        private static void AddEmptyElement(SerializedProperty property)
        {
            var index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            var element = property.GetArrayElementAtIndex(index);
            switch (element.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    element.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.String:
                    element.stringValue = string.Empty;
                    break;
            }
        }

        private static void DrawStepPopup(Rect rect, SerializedProperty element)
        {
            var steps = CollectBuildStepAssets();
            var names = new string[steps.Count + 1];
            names[0] = "<None>";
            var selectedIndex = 0;
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                names[i + 1] = step == null ? "<missing>" : $"{step.name} ({step.GetType().Name})";
                if (step == element.objectReferenceValue)
                {
                    selectedIndex = i + 1;
                }
            }

            var nextIndex = EditorGUI.Popup(rect, selectedIndex, names);
            element.objectReferenceValue = nextIndex <= 0 ? null : steps[nextIndex - 1];
        }

        private static List<EvoBuildStepAsset> CollectBuildStepAssets()
        {
            var result = new List<EvoBuildStepAsset>();
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var step = AssetDatabase.LoadAssetAtPath<EvoBuildStepAsset>(path);
                if (step != null && !result.Contains(step))
                {
                    result.Add(step);
                }
            }

            result.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return result;
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
