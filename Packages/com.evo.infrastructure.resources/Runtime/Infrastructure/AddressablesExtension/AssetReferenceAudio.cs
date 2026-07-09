using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Evo.Infrastructure.AddressablesExtension
{
    [Serializable]
    public sealed class AssetReferenceAudio : AssetReferenceT<AudioClip>
    {
        public AssetReferenceAudio(string guid) : base(guid)
        {
        }
    }
}
