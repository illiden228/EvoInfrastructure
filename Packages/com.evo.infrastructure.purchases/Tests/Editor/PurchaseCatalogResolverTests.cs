using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Purchases;
using Evo.Infrastructure.Services.Purchases.Editor;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Evo.Infrastructure.Purchases.Tests
{
    public sealed class PurchaseCatalogResolverTests
    {
        [Test]
        public void Resolve_WithMissingCatalog_ReturnsEmptyList()
        {
            Assert.That(PurchaseCatalogResolver.Resolve(null, "test", "google_play"), Is.Empty);
        }

        [Test]
        public void AdapterDropdown_UsesRoutingConfigAdapterIdsAndPreservesCustomValue()
        {
            const string assetPath = "Assets/PurchaseRoutingConfig.DropdownTest.asset";
            var routing = CreateRouting(
                "{\"adapters\":[" +
                "{\"adapterId\":\"unity-iap\"}," +
                "{\"adapterId\":\"rustore\"}," +
                "{\"adapterId\":\"UNITY-IAP\"}]}");
            AssetDatabase.CreateAsset(routing, assetPath);

            try
            {
                var adapterIds = PurchaseAdapterDropdownDrawer.BuildAdapterIds("custom-store");

                Assert.That(adapterIds, Is.EqualTo(new[]
                {
                    string.Empty,
                    "custom-store",
                    "rustore",
                    "unity-iap"
                }));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void Validator_RejectsDuplicateLogicalIds()
        {
            var catalog = ScriptableObject.CreateInstance<PurchaseCatalogConfig>();
            JsonUtility.FromJsonOverwrite(
                "{\"products\":[" +
                "{\"id\":\"coins\",\"defaultStoreProductId\":\"coins.a\"}," +
                "{\"id\":\"COINS\",\"defaultStoreProductId\":\"coins.b\"}]}",
                catalog);
            var issues = PurchaseCatalogValidator.Validate(catalog);
            Assert.That(issues.Any(issue => issue.Severity == PurchaseCatalogIssueSeverity.Error &&
                                            issue.Message.Contains("duplicated")), Is.True);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Resolve_UsesDifferentSkusForTwoAndroidStores()
        {
            var catalog = ScriptableObject.CreateInstance<PurchaseCatalogConfig>();
            JsonUtility.FromJsonOverwrite(
                "{\"products\":[{\"id\":\"product\",\"enabled\":true,\"overrides\":[" +
                "{\"adapterId\":\"unity-iap\",\"platformIds\":[\"google_play\"]," +
                "\"storeProductId\":\"google.coins\"}," +
                "{\"adapterId\":\"unity-iap\",\"platformIds\":[\"rustore\"]," +
                "\"storeProductId\":\"rustore.coins\"}]}]}",
                catalog);

            var googleProducts = PurchaseCatalogResolver.Resolve(catalog, "unity-iap", "google_play");
            var ruStoreProducts = PurchaseCatalogResolver.Resolve(catalog, "unity-iap", "rustore");

            Assert.That(googleProducts.Single().StoreProductId, Is.EqualTo("google.coins"));
            Assert.That(ruStoreProducts.Single().StoreProductId, Is.EqualTo("rustore.coins"));
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Routing_SelectsGooglePlayAdapterForGooglePlay()
        {
            var routing = CreateRouting(
                "{\"adapters\":[" +
                "{\"adapterId\":\"unity-iap\",\"enabled\":true," +
                "\"platformIds\":[\"app_store\",\"google_play\"],\"priority\":100}," +
                "{\"adapterId\":\"rustore\",\"enabled\":true," +
                "\"platformIds\":[\"rustore\"],\"priority\":100}]}");
            var platformCatalog = CreatePlatformCatalog("google_play", "google_play", "app_store", "rustore");
            var googleFactory = new MockFactory(new MockAdapter(), "unity-iap");
            var ruStoreFactory = new MockFactory(new MockAdapter(), "rustore");

            var selected = PurchaseRoutingResolver.SelectFactory(
                routing,
                new[] { googleFactory, ruStoreFactory },
                platformCatalog,
                false);

            Assert.That(selected, Is.SameAs(googleFactory));
            DestroyPlatformCatalog(platformCatalog);
            Object.DestroyImmediate(routing);
        }

        [Test]
        public void Routing_EditorWithoutMockSelectsNoAdapter()
        {
            var routing = CreateRouting(
                "{\"adapters\":[{" +
                "\"adapterId\":\"unity-iap\",\"enabled\":true," +
                "\"platformIds\":[\"google_play\"],\"priority\":100}]}");
            var platformCatalog = CreatePlatformCatalog("google_play", "google_play");
            var realFactory = new MockFactory(new MockAdapter(), "unity-iap");

            var selected = PurchaseRoutingResolver.SelectFactory(
                routing,
                new[] { realFactory },
                platformCatalog,
                true);

            Assert.That(selected, Is.Null);
            DestroyPlatformCatalog(platformCatalog);
            Object.DestroyImmediate(routing);
        }

        [Test]
        public void Routing_EditorMockHasPriorityInEditor()
        {
            var routing = CreateRouting(
                "{\"adapters\":[" +
                "{\"adapterId\":\"google-play\",\"enabled\":true," +
                "\"platformIds\":[\"google_play\"],\"priority\":1000}," +
                "{\"adapterId\":\"mock\",\"enabled\":true," +
                "\"platformIds\":[\"google_play\"],\"editorMock\":true,\"priority\":1}]}");
            var platformCatalog = CreatePlatformCatalog("google_play", "google_play");
            var realFactory = new MockFactory(new MockAdapter(), "google-play");
            var mockFactory = new MockFactory(new MockAdapter());

            var selected = PurchaseRoutingResolver.SelectFactory(
                routing,
                new[] { realFactory, mockFactory },
                platformCatalog,
                true);

            Assert.That(selected, Is.SameAs(mockFactory));
            DestroyPlatformCatalog(platformCatalog);
            Object.DestroyImmediate(routing);
        }

        [Test]
        public void Routing_UnknownCurrentPlatformSelectsNoAdapter()
        {
            var routing = CreateRouting(
                "{\"adapters\":[{" +
                "\"adapterId\":\"google-play\",\"enabled\":true," +
                "\"platformIds\":[\"google_play\"],\"priority\":100}]}");
            var platformCatalog = CreatePlatformCatalog("unknown_store", "google_play");
            var factory = new MockFactory(new MockAdapter(), "google-play");

            var selected = PurchaseRoutingResolver.SelectFactory(
                routing,
                new[] { factory },
                platformCatalog,
                false);

            Assert.That(selected, Is.Null);
            var issues = PurchaseRoutingValidator.Validate(routing, platformCatalog);
            Assert.That(issues.Any(issue => issue.Message.Contains("unknown")), Is.True);
            DestroyPlatformCatalog(platformCatalog);
            Object.DestroyImmediate(routing);
        }

        [Test]
        public void Validators_RejectEmptyAndUnknownPlatformIds()
        {
            var routing = CreateRouting(
                "{\"adapters\":[{" +
                "\"adapterId\":\"mock\",\"enabled\":true," +
                "\"platformIds\":[\"\",\"not_in_catalog\",\"GOOGLE_PLAY\",\"google_play\"]," +
                "\"editorMock\":true}]}");
            var catalog = ScriptableObject.CreateInstance<PurchaseCatalogConfig>();
            JsonUtility.FromJsonOverwrite(
                "{\"products\":[{\"id\":\"empty\",\"overrides\":[{" +
                "\"adapterId\":\"mock\",\"platformIds\":[\"\"]}]}," +
                "{\"id\":\"unknown\",\"overrides\":[{" +
                "\"adapterId\":\"mock\",\"platformIds\":[\"not_in_catalog\"]}]}]}",
                catalog);
            var platformCatalog = CreatePlatformCatalog("google_play", "google_play");

            var routingIssues = PurchaseRoutingValidator.Validate(routing, platformCatalog);
            var catalogIssues = PurchaseCatalogValidator.Validate(catalog, platformCatalog);

            Assert.That(routingIssues.Any(issue => issue.Message.Contains("empty platform ID")), Is.True);
            Assert.That(routingIssues.Any(issue => issue.Message.Contains("unknown platform ID")), Is.True);
            Assert.That(routingIssues.Any(issue => issue.Message.Contains("repeats platform ID")), Is.True);
            Assert.That(catalogIssues.Any(issue => issue.Message.Contains("empty platform ID")), Is.True);
            Assert.That(catalogIssues.Any(issue => issue.Message.Contains("unknown platform ID")), Is.True);
            DestroyPlatformCatalog(platformCatalog);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(routing);
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
                "{\"products\":[{\"id\":\"starter\",\"enabled\":true," +
                "\"fulfillmentKey\":\"starter\"," +
                "\"defaultStoreProductId\":\"store.starter\"}]}",
                catalog);
            JsonUtility.FromJsonOverwrite(
                "{\"adapters\":[{\"adapterId\":\"mock\",\"enabled\":true," +
                "\"platformIds\":[\"google_play\"],\"editorMock\":true,\"priority\":100}]}",
                routing);
            var platformCatalog = CreatePlatformCatalog("google_play", "google_play");
            var adapter = new MockAdapter();
            var handler = new MockHandler(adapter);
            using var service = new PurchaseService(
                new[] { new MockFactory(adapter) }, new[] { handler },
                new ConfigService(catalog, routing, platformCatalog),
                new PurchaseServiceOptions { AutoRestorePendingPurchases = false });

            await service.InitializeAsync().AsTask();
            var result = await service.PurchaseAsync("starter").AsTask();

            Assert.That(result.Success, Is.True);
            Assert.That(handler.WasCalled, Is.True);
            Assert.That(adapter.ConfirmedAfterFulfillment, Is.True);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(routing);
            DestroyPlatformCatalog(platformCatalog);
        }

        private static PurchaseRoutingConfig CreateRouting(string json)
        {
            var routing = ScriptableObject.CreateInstance<PurchaseRoutingConfig>();
            JsonUtility.FromJsonOverwrite(json, routing);
            return routing;
        }

        private static PlatformCatalog CreatePlatformCatalog(string currentPlatformId, params string[] platformIds)
        {
            var catalog = ScriptableObject.CreateInstance<PlatformCatalog>();
            JsonUtility.FromJsonOverwrite(
                $"{{\"currentPlatformId\":\"{currentPlatformId}\"}}",
                catalog);
            var definitions = platformIds.Select(platformId =>
            {
                var definition = ScriptableObject.CreateInstance<PlatformDefinition>();
                JsonUtility.FromJsonOverwrite($"{{\"platformId\":\"{platformId}\"}}", definition);
                return definition;
            }).ToList();
            typeof(PlatformCatalog)
                .GetField("platforms", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(catalog, definitions);
            return catalog;
        }

        private static void DestroyPlatformCatalog(PlatformCatalog catalog)
        {
            foreach (var definition in catalog.Entries)
            {
                Object.DestroyImmediate(definition);
            }

            Object.DestroyImmediate(catalog);
        }
    }
}
