using System;
using System.Collections.Generic;
using System.Reflection;
using AdjustSdk;
using Evo.Infrastructure.Services.Analytics;
using Evo.Infrastructure.Services.Analytics.Adjust;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using Evo.Infrastructure.Services.Config;

namespace Evo.Infrastructure.Analytics.Adjust.Tests
{
    public sealed class AdjustAnalyticsAdapterTests
    {
        private readonly List<AdjustAnalyticsAdapterConfig> _configs = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var config in _configs)
                UnityEngine.Object.DestroyImmediate(config);
            _configs.Clear();
        }

        [Test]
        public void VContainer_UsesConfigServiceConstructor()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance<IConfigService>(new EmptyConfigService());
            builder.Register<AdjustAnalyticsAdapter>(Lifetime.Singleton);

            using var container = builder.Build();
            var adapter = container.Resolve<AdjustAnalyticsAdapter>();

            Assert.That(adapter, Is.Not.Null);
            Assert.That(adapter.IsInitialized, Is.True);
        }

        [Test]
        public void AndroidPurchaseWithToken_UsesPlayStoreVerification()
        {
            var sdk = new RecordingAdjustSdk();
            var adapter = CreateAdapter(sdk, AnalyticsRuntimePlatform.Android);

            adapter.Track(CreatePurchase("google-token"));

            Assert.That(sdk.PlayStoreVerifiedEvents, Has.Count.EqualTo(1));
            Assert.That(sdk.TrackedEvents, Is.Empty);
            AssertPurchaseFields(sdk.PlayStoreVerifiedEvents[0], "google-token");
        }

        [Test]
        public void AndroidPurchaseWithoutToken_TracksOneFallbackPerDispatchAndWarnsOnce()
        {
            var sdk = new RecordingAdjustSdk();
            var adapter = CreateAdapter(sdk, AnalyticsRuntimePlatform.Android);
            LogAssert.Expect(
                LogType.Warning,
                "[Adjust Analytics Adapter] Google Play purchase verification is unavailable because ProductId or " +
                "PurchaseToken is missing; tracking one unverified revenue event instead.");

            adapter.Track(CreatePurchase(null));
            adapter.Track(CreatePurchase(null));

            Assert.That(sdk.PlayStoreVerifiedEvents, Is.Empty);
            Assert.That(sdk.TrackedEvents, Has.Count.EqualTo(2));
            AssertPurchaseFields(sdk.TrackedEvents[0], null);
        }

        [Test]
        public void EditorProductionConfig_DoesNotInitializeOrSendByDefault()
        {
            var sdk = new RecordingAdjustSdk();
            var adapter = CreateAdapter(sdk, AnalyticsRuntimePlatform.Editor);

            adapter.Track(CreatePurchase("token"));

            Assert.That(adapter.IsInitialized, Is.True);
            Assert.That(adapter.Supports(AnalyticsEventType.Purchase), Is.False);
            Assert.That(sdk.InitializeCalls, Is.Zero);
            Assert.That(sdk.TotalTrackingCalls, Is.Zero);
        }

        [Test]
        public void EditorTracking_CanBeEnabledExplicitly()
        {
            var sdk = new RecordingAdjustSdk();
            var adapter = CreateAdapter(sdk, AnalyticsRuntimePlatform.Editor, allowEditorTracking: true);

            adapter.Track(CreatePurchase("token"));

            Assert.That(sdk.InitializeCalls, Is.EqualTo(1));
            Assert.That(sdk.TrackedEvents, Has.Count.EqualTo(1));
            Assert.That(sdk.TotalTrackingCalls, Is.EqualTo(1));
        }

        [Test]
        public void SandboxEnvironment_IsSelectedThroughConfig()
        {
            var sdk = new RecordingAdjustSdk();

            CreateAdapter(
                sdk,
                AnalyticsRuntimePlatform.Android,
                environment: EvoAdjustEnvironment.Sandbox);

            Assert.That(sdk.InitializedConfig, Is.Not.Null);
            Assert.That(sdk.InitializedConfig.Environment, Is.EqualTo(AdjustEnvironment.Sandbox));
        }

        [Test]
        public void IosPurchase_UsesAppStoreVerification()
        {
            var sdk = new RecordingAdjustSdk();
            var adapter = CreateAdapter(sdk, AnalyticsRuntimePlatform.IOS);

            adapter.Track(CreatePurchase(null));

            Assert.That(sdk.AppStoreVerifiedEvents, Has.Count.EqualTo(1));
            Assert.That(sdk.PlayStoreVerifiedEvents, Is.Empty);
            Assert.That(sdk.TrackedEvents, Is.Empty);
            AssertPurchaseFields(sdk.AppStoreVerifiedEvents[0], null);
        }

        private AdjustAnalyticsAdapter CreateAdapter(
            RecordingAdjustSdk sdk,
            AnalyticsRuntimePlatform platform,
            bool allowEditorTracking = false,
            EvoAdjustEnvironment environment = EvoAdjustEnvironment.Production)
        {
            var config = ScriptableObject.CreateInstance<AdjustAnalyticsAdapterConfig>();
            _configs.Add(config);
            SetField(config, "appKey", "adjust-app-token");
            SetField(config, "purchaseToken", "purchase-event-token");
            SetField(config, "environment", environment);
            SetField(config, "allowEditorTracking", allowEditorTracking);
            return new AdjustAnalyticsAdapter(config, sdk, platform);
        }

        private static AnalyticsDispatchEvent CreatePurchase(string purchaseToken) =>
            new(new PurchaseEventData(
                "purchase-event-token",
                "usd",
                3.49m,
                "transaction-id",
                "store.product.id",
                purchaseToken));

        private static void AssertPurchaseFields(AdjustEvent adjustEvent, string purchaseToken)
        {
            Assert.That(adjustEvent.Revenue, Is.EqualTo(3.49d).Within(0.0001d));
            Assert.That(adjustEvent.Currency, Is.EqualTo("USD"));
            Assert.That(adjustEvent.ProductId, Is.EqualTo("store.product.id"));
            Assert.That(adjustEvent.TransactionId, Is.EqualTo("transaction-id"));
            Assert.That(adjustEvent.PurchaseToken, Is.EqualTo(purchaseToken));
        }

        private static void SetField(object target, string fieldName, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                    continue;
                field.SetValue(target, value);
                return;
            }

            throw new MissingFieldException(target.GetType().FullName, fieldName);
        }

        private sealed class RecordingAdjustSdk : IAdjustSdkFacade
        {
            public int InitializeCalls { get; private set; }
            public AdjustConfig InitializedConfig { get; private set; }
            public List<AdjustEvent> TrackedEvents { get; } = new();
            public List<AdjustEvent> AppStoreVerifiedEvents { get; } = new();
            public List<AdjustEvent> PlayStoreVerifiedEvents { get; } = new();
            public int AdRevenueCalls { get; private set; }
            public int TotalTrackingCalls =>
                TrackedEvents.Count + AppStoreVerifiedEvents.Count + PlayStoreVerifiedEvents.Count + AdRevenueCalls;

            public void InitSdk(AdjustConfig config)
            {
                InitializeCalls++;
                InitializedConfig = config;
            }
            public void TrackEvent(AdjustEvent adjustEvent) => TrackedEvents.Add(adjustEvent);
            public void TrackAdRevenue(AdjustAdRevenue adRevenue) => AdRevenueCalls++;

            public void VerifyAndTrackAppStorePurchase(
                AdjustEvent adjustEvent,
                Action<AdjustPurchaseVerificationResult> callback) =>
                AppStoreVerifiedEvents.Add(adjustEvent);

            public void VerifyAndTrackPlayStorePurchase(
                AdjustEvent adjustEvent,
                Action<AdjustPurchaseVerificationResult> callback) =>
                PlayStoreVerifiedEvents.Add(adjustEvent);
        }

        private sealed class EmptyConfigService : IConfigService
        {
            public T Get<T>() where T : class => null;
            public bool TryGet<T>(out T config) where T : class
            {
                config = null;
                return false;
            }

            public object Get(Type type) => null;
            public bool TryGet(Type type, out object config)
            {
                config = null;
                return false;
            }
        }
    }
}
