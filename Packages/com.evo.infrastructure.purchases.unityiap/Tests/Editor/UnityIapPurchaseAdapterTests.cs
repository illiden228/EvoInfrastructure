using System.Threading;
using Evo.Infrastructure.Services.Purchases;
using Evo.Infrastructure.Services.Purchases.UnityIap;
using NUnit.Framework;

namespace Evo.Infrastructure.Purchases.UnityIap.Tests
{
    public sealed class UnityIapPurchaseAdapterTests
    {
        [Test]
        public void Factory_CreatesUnavailableUninitializedAdapter()
        {
            var factory = new UnityIapPurchaseAdapterFactory();

            using var adapter = factory.Create();

            Assert.That(factory.AdapterId, Is.EqualTo("unity-iap"));
            Assert.That(adapter.AdapterId, Is.EqualTo(factory.AdapterId));
            Assert.That(adapter.IsInitialized, Is.False);
            Assert.That(adapter.IsAvailable, Is.False);
            Assert.That(adapter.Products, Is.Empty);
        }

        [Test]
        public void PurchaseBeforeInitialization_ReturnsNotInitialized()
        {
            using var adapter = new UnityIapPurchaseAdapter();

            var result = adapter.PurchaseAsync("offer", "store-product", CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(PurchaseStatus.NotInitialized));
        }

        [Test]
        public void ConfirmUnknownTransaction_ReturnsFalse()
        {
            using var adapter = new UnityIapPurchaseAdapter();

            var confirmed = adapter.ConfirmAsync(default, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(confirmed, Is.False);
        }
    }
}
