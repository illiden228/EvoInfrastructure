using System;

namespace _Project.Scripts.Infrastructure.Services.ResourceCatalog
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

    public enum ResourceType
    {
        Sprite = 0,
        GameObject = 1,
        AudioClip = 2
    }

    public enum SpriteEntryType
    {
        Direct = 0,
        Atlas = 1
    }
}
