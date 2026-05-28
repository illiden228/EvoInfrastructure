using System;
using Evo.Infrastructure.Runtime.UI;

namespace Evo.Infrastructure.Services.UI
{
    public sealed class UiOpenOptions
    {
        public UiLayer? LayerOverride;
        public UiOpenMode? OpenModeOverride;
        public bool? KeepAliveOverride;
        public bool KeepHistory;
        public object Context;
        public Type ContextType;
        internal IUiContextPayload ContextPayload;
    }

    internal interface IUiContextPayload
    {
        Type ContextType { get; }
        void Apply(IUiViewModel viewModel);
    }

    internal sealed class UiContextPayload<TContext> : IUiContextPayload
    {
        private readonly TContext _context;

        public UiContextPayload(TContext context)
        {
            _context = context;
        }

        public Type ContextType => typeof(TContext);

        public void Apply(IUiViewModel viewModel)
        {
            if (viewModel is IUiContextReceiver<TContext> receiver)
            {
                receiver.SetContext(_context);
            }
        }
    }
}
