using System;

namespace _Project.Scripts.Application.UI
{
    public interface IUiViewModel : IDisposable
    {
        void OnShow();
        void OnHide();
    }
}
