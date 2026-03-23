using System;
using _Project.Scripts.Infrastructure.Services.UI;
using R3;

namespace _Project.Scripts.Application.UI.ViewLogics
{
    public abstract class ViewLogicBase : IDisposable
    {
        protected IUiService UiService { get; }
        protected CompositeDisposable Disposables { get; } = new();

        protected ViewLogicBase(IUiService uiService)
        {
            UiService = uiService;
        }

        public void Dispose()
        {
            OnDispose();
            Disposables.Dispose();
        }

        protected virtual void OnDispose()
        {
        }
    }
}
