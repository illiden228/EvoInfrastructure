using Evo.Infrastructure.DI;
using NUnit.Framework;
using VContainer;

namespace Evo.Infrastructure.Services.Ads.Tests
{
    public sealed class AdsFeatureExtensionsTests
    {
        [Test]
        public void UseAds_ResolvesWithoutOptionalDependencies()
        {
            var builder = new ContainerBuilder();
            new EvoFeatureRegistry(builder).UseAds();

            using var container = builder.Build();

            Assert.That(container.Resolve<IAdsService>(), Is.Not.Null);
            Assert.That(container.Resolve<RewardedAdsCooldownService>(), Is.Not.Null);
            Assert.That(container.Resolve<InterstitialAdsCooldownService>(), Is.Not.Null);
        }
    }
}
