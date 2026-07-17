using System;
using System.Collections.Generic;
using Evo.Infrastructure.Runtime.UI.Views;
using VContainer;

namespace Evo.Infrastructure.Runtime.UI
{
    public sealed class UiBinding
    {
        internal UiBinding(
            string id,
            Type viewType,
            Type viewModelType,
            bool registerViewModel,
            Action<IContainerBuilder> registerViewModelAction)
        {
            Id = id;
            ViewType = viewType;
            ViewModelType = viewModelType;
            RegisterViewModel = registerViewModel;
            RegisterViewModelAction = registerViewModelAction;
        }

        public string Id { get; }
        public Type ViewType { get; }
        public Type ViewModelType { get; }
        public bool RegisterViewModel { get; }
        internal Action<IContainerBuilder> RegisterViewModelAction { get; }
    }

    public sealed class UiBindingRegistry
    {
        private readonly Dictionary<string, UiBinding> _bindings = new(StringComparer.Ordinal);

        public IEnumerable<UiBinding> Bindings => _bindings.Values;

        public UiBindingRegistry Add<TView, TViewModel>(string bindingId, bool registerViewModel = true)
            where TView : UiViewBase
            where TViewModel : class, IUiViewModel
        {
            if (string.IsNullOrWhiteSpace(bindingId))
            {
                throw new ArgumentException("UI binding id must not be empty.", nameof(bindingId));
            }

            if (_bindings.ContainsKey(bindingId))
            {
                throw new InvalidOperationException($"UI binding '{bindingId}' is already registered.");
            }

            _bindings.Add(
                bindingId,
                new UiBinding(
                    bindingId,
                    typeof(TView),
                    typeof(TViewModel),
                    registerViewModel,
                    builder => builder.Register<TViewModel>(Lifetime.Transient)));
            return this;
        }

        public bool TryGet(string bindingId, out UiBinding binding)
        {
            return _bindings.TryGetValue(bindingId ?? string.Empty, out binding);
        }
    }
}
