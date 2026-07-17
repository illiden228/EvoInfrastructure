using System;
using System.Collections.Generic;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Runtime.UI;
using Evo.Infrastructure.Services.UI;
using VContainer;

namespace Evo.Infrastructure.Services.UI
{
    public static class UiFeatureExtensions
    {
        public static EvoFeatureRegistry UseUi(
            this EvoFeatureRegistry features,
            UiSystemConfig uiSystemConfig = null,
            Action<UiBindingRegistry> configureBindings = null)
        {
            if (uiSystemConfig == null)
            {
                throw new ArgumentNullException(nameof(uiSystemConfig));
            }

            var builder = features.Builder;
            var bindings = new UiBindingRegistry();
            configureBindings?.Invoke(bindings);
            builder.RegisterInstance(bindings);
            builder.RegisterInstance(uiSystemConfig);
            RegisterUiViewModels(builder, bindings);

            builder.Register<IUiService, UiService>(Lifetime.Singleton);
            return features;
        }

        private static void RegisterUiViewModels(IContainerBuilder builder, UiBindingRegistry bindings)
        {
            if (builder == null || bindings == null)
            {
                return;
            }

            var registered = new HashSet<Type>();
            foreach (var binding in bindings.Bindings)
            {
                var viewModelType = binding.ViewModelType;
                if (!binding.RegisterViewModel || viewModelType == null || registered.Contains(viewModelType))
                {
                    continue;
                }

                binding.RegisterViewModelAction(builder);
                registered.Add(viewModelType);
            }
        }
    }
}
