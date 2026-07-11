using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Purchases.Yandex;
using NUnit.Framework;

namespace Evo.Infrastructure.Services.Purchases.Tests
{
    public sealed class YandexPurchaseAdapterTests
    {
#if !Payments_yg
        [Test]
        public void MissingPaymentsModule_CompletesInitializationAsUnavailable()
        {
            var factory = new YandexPurchaseAdapterFactory(new YandexPurchasesOptions
            {
                CatalogTimeout = TimeSpan.Zero
            });
            using var adapter = factory.Create();

            adapter.InitializeAsync(Array.Empty<PurchaseAdapterProductDefinition>(), CancellationToken.None)
                .AsTask().GetAwaiter().GetResult();

            Assert.That(adapter.IsInitialized, Is.True);
            Assert.That(adapter.IsAvailable, Is.False);
            Assert.That(adapter.Products, Is.Empty);
        }

        [Test]
        public void MissingPaymentsModule_DoesNotHandlePurchases()
        {
            var factory = new YandexPurchaseAdapterFactory(new YandexPurchasesOptions
            {
                CatalogTimeout = TimeSpan.Zero
            });
            using var adapter = factory.Create();
            adapter.InitializeAsync(Array.Empty<PurchaseAdapterProductDefinition>(), CancellationToken.None)
                .AsTask().GetAwaiter().GetResult();

            var result = adapter.PurchaseAsync("offer", "store-id", CancellationToken.None)
                .AsTask().GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(PurchaseStatus.Unavailable));
        }
#endif
    }
}
