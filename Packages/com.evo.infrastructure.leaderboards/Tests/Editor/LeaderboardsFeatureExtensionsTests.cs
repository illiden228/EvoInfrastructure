using Evo.Infrastructure.DI;
using NUnit.Framework;
using VContainer;

namespace Evo.Infrastructure.Services.Leaderboard.Tests
{
    public sealed class LeaderboardsFeatureExtensionsTests
    {
        [Test]
        public void UseLeaderboards_ResolvesWithoutOptionalConfig()
        {
            var builder = new ContainerBuilder();
            new EvoFeatureRegistry(builder).UseLeaderboards();

            using var container = builder.Build();

            Assert.That(container.Resolve<ILeaderboardService>(), Is.Not.Null);
        }
    }
}
