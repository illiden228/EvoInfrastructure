using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Evo.Infrastructure.AddressablesExtension
{
    [Serializable]
    public sealed class AssetReferenceMaterial : AssetReferenceT<Material>
    {
        public AssetReferenceMaterial(string guid) : base(guid)
        {
        }
    }
}
