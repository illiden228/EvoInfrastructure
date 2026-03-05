namespace _Project.Scripts.Infrastructure.Services.ResourceCatalog
{
    public interface IResourceCatalog
    {
        bool TryGetEntry(string key, out ResourceCatalogEntry entry);
        bool TryGetEntry(string key, ResourceType type, out ResourceCatalogEntry entry);
    }
}
