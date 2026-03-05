using _Project.Scripts.Infrastructure.AddressablesExtension;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

namespace _Project.Scripts.Infrastructure.Services.Audio
{
    [CreateAssetMenu(fileName = "AudioCueKey", menuName = "Project/Audio/Cue Key")]
    public sealed class AudioCueKey : ScriptableObject
    {
        [SerializeField] private string cueName = "NewAudioCue";
#if ODIN_INSPECTOR
        [Required]
#endif
        [SerializeField] private AssetReferenceAudio clip;

        public string CueName => cueName;
        public AssetReferenceAudio Clip => clip;
        public bool HasClip => clip != null && !string.IsNullOrEmpty(clip.AssetGUID);

#if UNITY_EDITOR
        private static readonly HashSet<int> PendingRenameIds = new();

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(cueName))
            {
                return;
            }

            var instanceId = GetInstanceID();
            if (!PendingRenameIds.Add(instanceId))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                PendingRenameIds.Remove(instanceId);
                if (this == null)
                {
                    return;
                }

                var path = AssetDatabase.GetAssetPath(this);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                var targetName = cueName.Trim();
                if (string.IsNullOrEmpty(targetName) || targetName == name)
                {
                    return;
                }

                AssetDatabase.RenameAsset(path, targetName);
                AssetDatabase.SaveAssets();
            };
        }

#if ODIN_INSPECTOR
        [GUIColor(1f, 0.45f, 0.45f)]
        [Button(ButtonSizes.Small, Name = "Delete", Icon = SdfIconType.Trash)]
#endif
        private void DeleteAsset()
        {
            var path = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var confirmed = EditorUtility.DisplayDialog(
                "Delete Audio Cue Key",
                $"Delete asset '{name}'?\nThis action cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
