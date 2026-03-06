#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace _Project.Scripts.Application.Config
{
    [CustomEditor(typeof(ProjectConfig))]
    public sealed class ProjectConfigEditor : UnityEditor.Editor
    {
        private const string StartupScenePropertyName = "startupScene";
        private const string GameplayScenePropertyName = "gameplayScene";
        private const string LoadingScenePropertyName = "loadingScene";
        private const string AssetGuidPropertyName = "m_AssetGUID";

        private SerializedProperty _startupScene;
        private SerializedProperty _gameplayScene;
        private SerializedProperty _loadingScene;

        private void OnEnable()
        {
            _startupScene = serializedObject.FindProperty(StartupScenePropertyName);
            _gameplayScene = serializedObject.FindProperty(GameplayScenePropertyName);
            _loadingScene = serializedObject.FindProperty(LoadingScenePropertyName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawAddressableSceneField("Startup Scene (Addressables)", _startupScene);
            DrawAddressableSceneField("Gameplay Scene (Addressables)", _gameplayScene);
            DrawAddressableSceneField("Loading Scene (Addressables)", _loadingScene);

            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                StartupScenePropertyName,
                GameplayScenePropertyName,
                LoadingScenePropertyName);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawAddressableSceneField(string label, SerializedProperty assetReferenceProperty)
        {
            if (assetReferenceProperty == null)
            {
                return;
            }

            var guidProperty = assetReferenceProperty.FindPropertyRelative(AssetGuidPropertyName);
            if (guidProperty == null)
            {
                EditorGUILayout.PropertyField(assetReferenceProperty, new GUIContent(label));
                return;
            }

            var currentScene = LoadSceneAssetByGuid(guidProperty.stringValue);
            var selectedScene = (SceneAsset)EditorGUILayout.ObjectField(label, currentScene, typeof(SceneAsset), false);
            if (selectedScene == currentScene)
            {
                return;
            }

            if (selectedScene == null)
            {
                guidProperty.stringValue = string.Empty;
                return;
            }

            var selectedPath = AssetDatabase.GetAssetPath(selectedScene);
            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            if (!IsAddressableScenePath(selectedPath))
            {
                EditorGUILayout.HelpBox("Selected scene is not in Addressables groups.", MessageType.Warning);
                return;
            }

            guidProperty.stringValue = AssetDatabase.AssetPathToGUID(selectedPath);
        }

        private static SceneAsset LoadSceneAssetByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        private static bool IsAddressableScenePath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var addressablePaths = GetAddressableScenePaths();
            return addressablePaths.Contains(path);
        }

        private static HashSet<string> GetAddressableScenePaths()
        {
            var result = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return result;
            }

            foreach (var group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (var entry in group.entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    var path = entry.AssetPath;
                    if (!string.IsNullOrEmpty(path) && path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(path);
                    }
                }
            }

            return result;
        }
    }
}
#endif
