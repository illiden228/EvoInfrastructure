using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace _Project.Scripts.Infrastructure.AddressablesExtension
{
    [Serializable]
    public sealed class AssetReferenceAudio : AssetReferenceT<AudioClip>
    {
        public AssetReferenceAudio(string guid) : base(guid)
        {
        }
    }
}
