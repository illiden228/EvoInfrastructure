using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.DI;
using NUnit.Framework;
using VContainer;

namespace Evo.Infrastructure.Services.Analytics.Tests
{
    public sealed class AnalyticsInitializationTests
    {
        [Test]
        public void UseAnalytics_ResolvesWithoutOptionalDependencies()
        {
            var builder = new ContainerBuilder();
            new EvoFeatureRegistry(builder).UseAnalytics();

            using var container = builder.Build();

            Assert.That(container.Resolve<IAnalyticsService>(), Is.Not.Null);
        }

        [Test]
        public async Task TerminalUnavailableAdapter_DoesNotBlockInitialization()
        {
            var adapter = new TerminalUnavailableAdapter();
            var service = new AnalyticsService(new List<IAnalyticsAdapter> { adapter });
            using var timeout = new CancellationTokenSource(1000);

            await service.WaitForInitializationAsync(timeout.Token).AsTask();

            Assert.That(timeout.IsCancellationRequested, Is.False);
        }

        [Test]
        public void UnavailableAdapter_DoesNotHandleEvents()
        {
            var adapter = new TerminalUnavailableAdapter();
            var service = new AnalyticsService(new List<IAnalyticsAdapter> { adapter });

            service.TrackCustom("valid_event");

            Assert.That(adapter.TrackCount, Is.Zero);
        }

        private sealed class TerminalUnavailableAdapter : IAnalyticsAdapter
        {
            public string AdapterId => "unavailable";
            public bool IsInitialized => true;
            public int TrackCount { get; private set; }
            public bool Supports(AnalyticsEventType eventType) => false;
            public void Track(in AnalyticsDispatchEvent analyticsEvent) => TrackCount++;
        }
    }
}
