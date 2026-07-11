using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Purchases;
using NUnit.Framework;
using UnityEngine;

namespace Evo.Infrastructure.Purchases.Tests
{
    public sealed class PurchaseCatalogResolverTests
    {
        [TestCase(RuntimePlatform.Android, PurchasePlatformMask.Android)]
        [TestCase(RuntimePlatform.IPhonePlayer, PurchasePlatformMask.IOS)]
        [TestCase(RuntimePlatform.WebGLPlayer, PurchasePlatformMask.WebGL)]
        public void ToMask_ReturnsExpectedPlatform(RuntimePlatform platform, PurchasePlatformMask expected)
        {
            Assert.That(PurchaseCatalogResolver.ToMask(platform), Is.EqualTo(expected));
        }

        [Test]
        public void Resolve_WithMissingCatalog_ReturnsEmptyList()
        {
            Assert.That(PurchaseCatalogResolver.Resolve(null, "test", RuntimePlatform.Android), Is.Empty);
        }

        [Test]
        public void Validator_RejectsDuplicateLogicalIds()
        {
            var catalog = ScriptableObject.CreateInstance<PurchaseCatalogConfig>();
            JsonUtility.FromJsonOverwrite(
                "{\"offers\":[" +
                "{\"id\":\"coins\",\"defaultStoreProductId\":\"coins.a\"}," +
                "{\"id\":\"COINS\",\"defaultStoreProductId\":\"coins.b\"}]}",
                catalog);
            var issues = PurchaseCatalogValidator.Validate(catalog);
            Assert.That(issues.Any(issue => issue.Severity == PurchaseCatalogIssueSeverity.Error &&
                                            issue.Message.Contains("duplicated")), Is.True);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Resolve_PrefersExactPlatformRegardlessOfSerializedOrder()
        {
            var catalog = ScriptableObject.CreateInstance<PurchaseCatalogConfig>();
            JsonUtility.FromJsonOverwrite(
                "{\"offers\":[{\"id\":\"offer\",\"enabled\":true,\"overrides\":[" +
                "{\"adapterId\":\"mock\",\"platforms\":6,\"storeProductId\":\"broad\"}," +
                "{\"adapterId\":\"mock\",\"platforms\":2,\"storeProductId\":\"exact\"}]}]}",
                catalog);

            var offers = PurchaseCatalogResolver.Resolve(
                catalog,
                "mock",
                RuntimePlatform.Android);

            Assert.That(offers.Single().StoreProductId, Is.EqualTo("exact"));
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public async Task InitializeAsync_WithoutAdapter_CompletesUnavailable()
        {
            using var service = new PurchaseService(
                Array.Empty<IPurchaseAdapterFactory>(),
                Array.Empty<IPurchaseFulfillmentHandler>(),
                new EmptyConfigService(),
                new PurchaseServiceOptions());
            await service.InitializeAsync().AsTask();
            Assert.That(service.IsInitialized, Is.True);
            Assert.That(service.IsAvailable, Is.False);
        }

        [Test]
        public async Task PurchaseAsync_FulfillsBeforeStoreConfirmation()
        {
            var catalog = ScriptableObject.CreateInstance<PurchaseCatalogConfig>();
            var routing = ScriptableObject.CreateInstance<PurchaseRoutingConfig>();
            JsonUtility.FromJsonOverwrite(
                "{\"offers\":[{\"id\":\"starter\",\"enabled\":true," +
                "\"fulfillmentKey\":\"starter\"," +
                "\"defaultStoreProductId\":\"store.starter\"}]}",
                catalog);
            JsonUtility.FromJsonOverwrite(
                "{\"adapters\":[{\"adapterId\":\"mock\",\"enabled\":true,\"platforms\":-1,\"priority\":100}]}",
                routing);
            var adapter = new MockAdapter();
            var handler = new MockHandler(adapter);
            using var service = new PurchaseService(
                new[] { new MockFactory(adapter) }, new[] { handler },
                new ConfigService(catalog, routing),
                new PurchaseServiceOptions { AutoRestorePendingPurchases = false });

            await service.InitializeAsync().AsTask();
            var result = await service.PurchaseAsync("starter").AsTask();

            Assert.That(result.Success, Is.True);
            Assert.That(handler.WasCalled, Is.True);
            Assert.That(adapter.ConfirmedAfterFulfillment, Is.True);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(routing);
        }

    }
}
