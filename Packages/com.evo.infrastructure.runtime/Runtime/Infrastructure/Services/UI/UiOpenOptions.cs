using Evo.Infrastructure.Runtime.UI;

namespace Evo.Infrastructure.Services.UI
{
    public sealed class UiOpenOptions
    {
        public UiLayer? LayerOverride;
        public UiOpenMode? OpenModeOverride;
        public bool? KeepAliveOverride;
        public bool KeepHistory;
    }
}
