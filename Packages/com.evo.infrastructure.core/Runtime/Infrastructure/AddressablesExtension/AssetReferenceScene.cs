using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Scripts.Infrastructure.AddressablesExtension
{
    [Serializable]
#if UNITY_EDITOR
    public sealed class AssetReferenceScene : AssetReferenceT<SceneAsset>
#else
    public sealed class AssetReferenceScene : AssetReference
#endif
    {
        public AssetReferenceScene(string guid) : base(guid)
        {
        }
    }
}
