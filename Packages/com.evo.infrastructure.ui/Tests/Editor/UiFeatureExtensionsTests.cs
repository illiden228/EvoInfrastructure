using System;
using Evo.Infrastructure.DI;
using Evo.Infrastructure.Services.UI;
using NUnit.Framework;
using VContainer;

namespace Evo.Infrastructure.UI.Tests
{
    public sealed class UiFeatureExtensionsTests
    {
        [Test]
        public void UseUi_WithoutConfig_FailsBeforeRegistrations()
        {
            var builder = new ContainerBuilder();
            var features = new EvoFeatureRegistry(builder);

            Assert.Throws<ArgumentNullException>(() => features.UseUi(null));
        }
    }
}
