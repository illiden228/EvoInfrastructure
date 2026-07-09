using System;
using Evo.Infrastructure.Services.UI;
using R3;

namespace Evo.Infrastructure.Runtime.UI.ViewLogics
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
