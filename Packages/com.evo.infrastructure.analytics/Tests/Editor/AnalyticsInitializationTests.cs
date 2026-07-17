using System;
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
        public void OptionalFeatureRegistration_RegistersAdapterWithoutTypeLookup()
        {
            var adapter = new TerminalUnavailableAdapter();
            var builder = new ContainerBuilder();
            var features = new EvoFeatureRegistry(builder);

            var key = "test_adapter_" + Guid.NewGuid().ToString("N");
            EvoOptionalFeatureRegistry.Register(
                key,
                registry => registry.Builder.RegisterInstance(adapter).As<IAnalyticsAdapter>());

            Assert.That(
                EvoOptionalFeatureRegistry.TryRegister(features, key),
                Is.True);

            using var container = builder.Build();
            Assert.That(container.Resolve<IAnalyticsAdapter>(), Is.SameAs(adapter));
        }

        [Test]
        public void OptionalFeatureRegistration_ReplayReplacesFactoryAfterDomainReload()
        {
            var adapter = new TerminalUnavailableAdapter();
            var builder = new ContainerBuilder();
            var features = new EvoFeatureRegistry(builder);
            var key = "test_adapter_replay_" + Guid.NewGuid().ToString("N");
            Action<EvoFeatureRegistry> register = registry =>
                registry.Builder.RegisterInstance(adapter).As<IAnalyticsAdapter>();

            EvoOptionalFeatureRegistry.Register(key, register);
            EvoOptionalFeatureRegistry.Register(key, register);

            Assert.That(EvoOptionalFeatureRegistry.TryRegister(features, key), Is.True);

            using var container = builder.Build();
            Assert.That(container.Resolve<IAnalyticsAdapter>(), Is.SameAs(adapter));
        }

        [Test]
        public void OptionalFeatureRegistration_AppliesMultipleFactoriesIndependently()
        {
            var calls = 0;
            var features = new EvoFeatureRegistry(new ContainerBuilder());
            var firstKey = "test_factory_a_" + Guid.NewGuid().ToString("N");
            var secondKey = "test_factory_b_" + Guid.NewGuid().ToString("N");
            EvoOptionalFeatureRegistry.Register(firstKey, _ => calls++);
            EvoOptionalFeatureRegistry.Register(secondKey, _ => calls++);

            Assert.That(EvoOptionalFeatureRegistry.TryRegister(features, firstKey), Is.True);
            Assert.That(EvoOptionalFeatureRegistry.TryRegister(features, secondKey), Is.True);
            Assert.That(calls, Is.EqualTo(2));
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

        [Test]
        public void Events_AreFannedOutOnceToEverySupportingAdapter()
        {
            var first = new RecordingAdapter("first");
            var second = new RecordingAdapter("second");
            var service = new AnalyticsService(new List<IAnalyticsAdapter> { first, second });

            service.TrackCustom("level_started");
            service.TrackPurchase(new PurchaseEventData(
                "purchase",
                "USD",
                2.99m,
                "transaction",
                "supporter_pack"));
            service.TrackAd(new AdEventData(
                "ad_revenue",
                "applovin",
                "network",
                "unit",
                "interstitial",
                "placement",
                "network_placement",
                "USD",
                0.01d,
                "US"));

            Assert.That(first.CustomCount, Is.EqualTo(1));
            Assert.That(second.CustomCount, Is.EqualTo(1));
            Assert.That(first.PurchaseCount, Is.EqualTo(1));
            Assert.That(second.PurchaseCount, Is.EqualTo(1));
            Assert.That(first.AdCount, Is.EqualTo(1));
            Assert.That(second.AdCount, Is.EqualTo(1));
        }

        private sealed class TerminalUnavailableAdapter : IAnalyticsAdapter
        {
            public string AdapterId => "unavailable";
            public bool IsInitialized => true;
            public int TrackCount { get; private set; }
            public bool Supports(AnalyticsEventType eventType) => false;
            public void Track(in AnalyticsDispatchEvent analyticsEvent) => TrackCount++;
        }

        private sealed class RecordingAdapter : IAnalyticsAdapter
        {
            public RecordingAdapter(string adapterId)
            {
                AdapterId = adapterId;
            }

            public string AdapterId { get; }
            public bool IsInitialized => true;
            public int CustomCount { get; private set; }
            public int PurchaseCount { get; private set; }
            public int AdCount { get; private set; }

            public bool Supports(AnalyticsEventType eventType) => true;

            public void Track(in AnalyticsDispatchEvent analyticsEvent)
            {
                switch (analyticsEvent.EventType)
                {
                    case AnalyticsEventType.Custom:
                        CustomCount++;
                        break;
                    case AnalyticsEventType.Purchase:
                        PurchaseCount++;
                        break;
                    case AnalyticsEventType.Ad:
                        AdCount++;
                        break;
                }
            }
        }
    }
}
