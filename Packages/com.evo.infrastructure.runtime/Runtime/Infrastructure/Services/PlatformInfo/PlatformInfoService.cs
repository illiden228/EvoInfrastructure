using System;
using System.Collections.Generic;

namespace _Project.Scripts.Infrastructure.Services.PlatformInfo
{
    public sealed class PlatformInfoService : IPlatformInfoService
    {
        private readonly IReadOnlyList<IPlatformInfoProvider> _providers;
        private PlatformInfoSnapshot _current;

        public PlatformInfoSnapshot Current => _current;
        public bool IsWeb => _current.IsWeb;
        public bool IsMobileWeb => _current.IsMobileWeb;
        public bool IsDesktopWeb => _current.IsDesktopWeb;

        public PlatformInfoService(IReadOnlyList<IPlatformInfoProvider> providers)
        {
            _providers = providers ?? Array.Empty<IPlatformInfoProvider>();
            Refresh();
        }

        public void Refresh()
        {
            var selected = default(PlatformInfoSnapshot);
            var selectedPriority = int.MinValue;
            var hasValue = false;

            for (var i = 0; i < _providers.Count; i++)
            {
                var provider = _providers[i];
                if (provider == null || !provider.TryGet(out var candidate))
                {
                    continue;
                }

                if (hasValue && provider.Priority < selectedPriority)
                {
                    continue;
                }

                selected = candidate;
                selectedPriority = provider.Priority;
                hasValue = true;
            }

            _current = hasValue ? selected : default;
        }
    }
}
