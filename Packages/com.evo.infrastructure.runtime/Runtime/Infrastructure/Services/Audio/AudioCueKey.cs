using Evo.Infrastructure.AddressablesExtension;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

namespace Evo.Infrastructure.Services.Audio
{
    [CreateAssetMenu(fileName = "AudioCueKey", menuName = "Project/Audio/Cue Key")]
    public sealed class AudioCueKey : ScriptableObject
    {
        [SerializeField] private string cueName = "NewAudioCue";
#if ODIN_INSPECTOR
        [Required]
#endif
        [SerializeField] private AssetReferenceAudio clip;

        public string Id => CueName;
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

#endif
    }
}
