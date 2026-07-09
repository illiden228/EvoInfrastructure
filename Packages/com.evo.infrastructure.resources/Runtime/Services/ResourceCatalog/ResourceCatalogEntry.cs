using System;

namespace Evo.Infrastructure.Services.ResourceCatalog
{
    [Serializable]
    public struct ResourceCatalogEntry
    {
        public string Key;
        public ResourceType Type;
        public SpriteEntryType SpriteType;
        public string AssetKey;
        public string AtlasKey;
        public string SpriteName;
    }
}
