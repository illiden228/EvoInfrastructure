using Evo.Infrastructure.Services.PlatformInfo.Config;

namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public sealed class EvoBuildCiRequest
    {
        public string BuildTag { get; set; }
        public string OutputPathOverride { get; set; }
        public string ProfileGuid { get; set; }
        public string ProfileId { get; set; }
        public string GlobalConfigGuid { get; set; }
        public string PlatformCatalogGuid { get; set; }
        public string CiConfigGuid { get; set; }
        public EvoBuildCiConfig CiConfig { get; set; }
        public BuildGlobalConfig GlobalConfig { get; set; }
        public PlatformCatalog PlatformCatalog { get; set; }
        public PlatformBuildProfile Profile { get; set; }
        public EvoBuildCiTag ParsedTag { get; set; }
    }
}
