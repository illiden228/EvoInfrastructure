using System.Collections.Generic;
using System.Linq;
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
        private static List<EvoBuildStepAsset> _cachedStepAssets;
        private static double _lastStepAssetsRefreshTime;

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
            DrawAndroidSettings();
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

        private void DrawAndroidSettings()
        {
            var profile = (PlatformBuildProfile)target;
            if (profile == null || profile.BuildTarget != BuildTarget.Android)
            {
                return;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("androidBuild"), includeChildren: true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("androidSigning"), includeChildren: true);
        }

        private static void DrawStepsHelp()
        {
            EditorGUILayout.HelpBox(
                "Steps are executed in phase/order. Step assets describe behavior; profile-specific values such as Android signing passwords live on the build profile.",
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
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                },
                onAddCallback = _ => AddEmptyElement(property)
            };

            if (isStepsList)
            {
                list.onAddDropdownCallback = (buttonRect, _) => ShowStepAddMenu(buttonRect, property);
            }

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

        private static void ShowStepAddMenu(Rect buttonRect, SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var propertyPath = property.propertyPath;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Empty"), false, () =>
            {
                AddStepToSerializedList(targetObject, propertyPath, null);
            });

            menu.AddSeparator(string.Empty);
            var steps = CollectBuildStepAssets();
            if (steps.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No EvoBuildStepAsset assets found"));
            }
            else
            {
                for (var i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    var label = step == null ? "<missing>" : $"{step.GetType().Name}/{step.name}";
                    menu.AddItem(new GUIContent(label), false, () =>
                    {
                        AddStepToSerializedList(targetObject, propertyPath, step);
                    });
                }
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Refresh Step List"), false, () =>
            {
                _cachedStepAssets = null;
                _lastStepAssetsRefreshTime = 0;
            });
            menu.DropDown(buttonRect);
        }

        private static void AddStepToSerializedList(Object targetObject, string propertyPath, EvoBuildStepAsset step)
        {
            if (targetObject == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                return;
            }

            var serialized = new SerializedObject(targetObject);
            serialized.Update();
            var property = serialized.FindProperty(propertyPath);
            if (property == null || !property.isArray)
            {
                return;
            }

            var index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            var element = property.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.ObjectReference)
            {
                element.objectReferenceValue = step;
            }

            serialized.ApplyModifiedProperties();
        }

        private static List<EvoBuildStepAsset> CollectBuildStepAssets()
        {
            if (_cachedStepAssets != null && EditorApplication.timeSinceStartup - _lastStepAssetsRefreshTime < 10)
            {
                return _cachedStepAssets;
            }

            var result = new List<EvoBuildStepAsset>();
            var seen = new HashSet<EvoBuildStepAsset>();
            var types = TypeCache.GetTypesDerivedFrom<EvoBuildStepAsset>()
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .OrderBy(type => type.Name);

            foreach (var type in types)
            {
                var guids = AssetDatabase.FindAssets($"t:{type.Name}");
                for (var i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var step = AssetDatabase.LoadAssetAtPath<EvoBuildStepAsset>(path);
                    if (step != null && seen.Add(step))
                    {
                        result.Add(step);
                    }
                }
            }

            result.Sort((left, right) =>
            {
                var typeCompare = string.CompareOrdinal(left.GetType().Name, right.GetType().Name);
                return typeCompare != 0 ? typeCompare : string.CompareOrdinal(left.name, right.name);
            });

            _cachedStepAssets = result;
            _lastStepAssetsRefreshTime = EditorApplication.timeSinceStartup;
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
