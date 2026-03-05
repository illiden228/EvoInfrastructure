using _Project.Scripts.Application.UI;

namespace _Project.Scripts.Infrastructure.Services.UI
{
    public sealed class UiOpenOptions
    {
        public UiLayer? LayerOverride;
        public UiOpenMode? OpenModeOverride;
        public bool? KeepAliveOverride;
        public bool KeepHistory;
    }
}
