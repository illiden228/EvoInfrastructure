namespace Evo.Infrastructure.Editor.EvoTools.Build
{
    public sealed class EvoBuildExecutorOptions
    {
        public bool BuildAndRun { get; set; }
        public bool Interactive { get; set; } = true;
        public bool RevealOutput { get; set; } = true;
        public string OutputPathOverride { get; set; }
        public EvoBuildCiRequest CiRequest { get; set; }
    }
}
