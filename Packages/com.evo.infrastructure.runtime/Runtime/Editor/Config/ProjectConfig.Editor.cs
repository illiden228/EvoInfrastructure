#if UNITY_EDITOR && ODIN_INSPECTOR
using System.Collections.Generic;
using _Project.Scripts.Infrastructure.AddressablesExtension;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace _Project.Scripts.Application.Config
{
    public sealed partial class ProjectConfig
    {
        [Title("Startup Scene")]
        [ShowInInspector]
        [ValueDropdown(nameof(GetAddressableScenes))]
        [LabelText("Startup Scene (Addressables)")]
        private SceneAsset StartupSceneAsset
        {
            get => GetSceneAsset(startupScene);
            set => SetSceneAsset(value);
        }

        [ShowInInspector]
        [ValueDropdown(nameof(GetAddressableScenes))]
        [LabelText("Gameplay Scene (Addressables)")]
        private SceneAsset GameplaySceneAsset
        {
            get => GetSceneAsset(gameplayScene);
            set => SetSceneAsset(value, isGameplay: true);
        }

        [ShowInInspector]
        [ValueDropdown(nameof(GetAddressableScenes))]
        [LabelText("Loading Scene (Addressables)")]
        private SceneAsset LoadingSceneAsset
        {
            get => GetSceneAsset(loadingScene);
            set => SetSceneAsset(value, isLoading: true);
        }

        private static IEnumerable<ValueDropdownItem<SceneAsset>> GetAddressableScenes()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                yield break;
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
                    if (string.IsNullOrEmpty(path) || !path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    if (asset != null)
                    {
                        yield return new ValueDropdownItem<SceneAsset>(asset.name, asset);
                    }
                }
            }
        }

        private static SceneAsset GetSceneAsset(AssetReferenceScene reference)
        {
            if (reference == null || string.IsNullOrEmpty(reference.AssetGUID))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        private void SetSceneAsset(SceneAsset asset, bool isGameplay = false, bool isLoading = false)
        {
            if (asset == null)
            {
                if (isGameplay)
                {
                    gameplayScene = null;
                }
                else if (isLoading)
                {
                    loadingScene = null;
                }
                else
                {
                    startupScene = null;
                }
                return;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (isGameplay)
            {
                gameplayScene = new AssetReferenceScene(guid);
            }
            else if (isLoading)
            {
                loadingScene = new AssetReferenceScene(guid);
            }
            else
            {
                startupScene = new AssetReferenceScene(guid);
            }
        }
    }
}
#endif
