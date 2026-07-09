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
            UiSystemConfig uiSystemConfig = null)
        {
            var builder = features.Builder;
            if (uiSystemConfig != null)
            {
                builder.RegisterInstance(uiSystemConfig);
                RegisterUiViewModels(builder, uiSystemConfig);
            }

            builder.Register<IUiService, UiService>(Lifetime.Singleton);
            return features;
        }

        private static void RegisterUiViewModels(IContainerBuilder builder, UiSystemConfig config)
        {
            if (builder == null || config == null || config.Views == null)
            {
                return;
            }

            var registered = new HashSet<Type>();
            for (var i = 0; i < config.Views.Count; i++)
            {
                var entry = config.Views[i];
                var viewModelType = entry?.GetViewModelType();
                if (viewModelType == null || registered.Contains(viewModelType))
                {
                    continue;
                }

                builder.Register(viewModelType, Lifetime.Transient);
                registered.Add(viewModelType);
            }
        }
    }
}
