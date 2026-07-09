using System;

namespace Evo.Infrastructure.Runtime.UI
{
    public interface IUiLifecycleView<in TViewModel> : IUiBindable<TViewModel>, IUiUnbindable, IDisposable
    {
    }
}
