using System;

namespace Evo.Infrastructure.Runtime.UI
{
    public interface IUiViewModel : IDisposable
    {
        void OnShow();
        void OnHide();
    }
}
