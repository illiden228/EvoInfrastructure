using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Evo.Infrastructure.Services.Purchases;
using Evo.Infrastructure.Services.Purchases.UnityIap;
using NUnit.Framework;
using UnityEngine.Purchasing;

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

            var result = adapter.PurchaseAsync("product", "store-product", CancellationToken.None)
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

        [Test]
        public void GooglePlayOrder_MapsPurchaseTokenAndAllAvailableFields()
        {
            const string token = "google-purchase-token";
            const string orderId = "GPA.1234-5678-9012-34567";
            var order = CreateOrder(
                token,
                CreateGoogleReceipt(token, orderId),
                google: true,
                price: 4.99m,
                currency: "usd");

            var transaction = UnityIapOrderMapper.ToTransaction(
                order,
                false,
                new Dictionary<string, string> { ["coins_100"] = "coins" });

            Assert.That(transaction.TransactionId, Is.EqualTo(token));
            Assert.That(transaction.PurchaseToken, Is.EqualTo(token));
            Assert.That(transaction.OrderId, Is.EqualTo(orderId));
            Assert.That(transaction.ProductId, Is.EqualTo("coins"));
            Assert.That(transaction.StoreProductId, Is.EqualTo("coins_100"));
            Assert.That(transaction.Receipt, Is.EqualTo(order.Info.Receipt));
            Assert.That(transaction.Price, Is.EqualTo(4.99m));
            Assert.That(transaction.CurrencyCode, Is.EqualTo("usd"));
            Assert.That(transaction.IsRestored, Is.False);
        }

        [Test]
        public void ReceiptWithoutGoogleFields_DoesNotThrow()
        {
            var order = CreateOrder("token-from-order", CreateUnifiedReceipt("{}"), google: true);

            Assert.DoesNotThrow(() => UnityIapOrderMapper.ToTransaction(order, false, null));
            var transaction = UnityIapOrderMapper.ToTransaction(order, false, null);

            Assert.That(transaction.PurchaseToken, Is.EqualTo("token-from-order"));
            Assert.That(transaction.OrderId, Is.Null);
        }

        [Test]
        public void MalformedReceipt_DoesNotThrow()
        {
            var order = CreateOrder("token-from-order", "{ definitely-not-json", google: true);

            Assert.DoesNotThrow(() => UnityIapOrderMapper.ToTransaction(order, false, null));
            var transaction = UnityIapOrderMapper.ToTransaction(order, false, null);

            Assert.That(transaction.TransactionId, Is.EqualTo("token-from-order"));
            Assert.That(transaction.PurchaseToken, Is.EqualTo("token-from-order"));
            Assert.That(transaction.OrderId, Is.Null);
        }

        [Test]
        public void RestoredConfirmedTransaction_PreservesStoreFields()
        {
            const string token = "restored-purchase-token";
            const string orderId = "GPA.restored";
            var pending = CreateOrder(token, CreateGoogleReceipt(token, orderId), google: true);
            var restored = new ConfirmedOrder(pending.CartOrdered, pending.Info);

            var transaction = UnityIapOrderMapper.ToTransaction(
                restored,
                true,
                new Dictionary<string, string> { ["coins_100"] = "coins" },
                useEntitlementFallback: true);

            Assert.That(transaction.TransactionId, Is.EqualTo(token));
            Assert.That(transaction.PurchaseToken, Is.EqualTo(token));
            Assert.That(transaction.OrderId, Is.EqualTo(orderId));
            Assert.That(transaction.ProductId, Is.EqualTo("coins"));
            Assert.That(transaction.StoreProductId, Is.EqualTo("coins_100"));
            Assert.That(transaction.Receipt, Is.Not.Empty);
            Assert.That(transaction.IsRestored, Is.True);
        }

        private static PendingOrder CreateOrder(
            string transactionId,
            string receipt,
            bool google,
            decimal price = 1.99m,
            string currency = "USD")
        {
            var definition = new ProductDefinition("coins_100", "coins_100", ProductType.NonConsumable);
            var metadata = new ProductMetadata("$1.99", "Coins", "Coins", currency, price);
            var product = (Product)Activator.CreateInstance(
                typeof(Product),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { definition, metadata },
                null);
            var info = new TestOrderInfo(receipt, transactionId, google);
            return new PendingOrder(new Cart(new CartItem(product)), info);
        }

        private static string CreateGoogleReceipt(string purchaseToken, string orderId)
        {
            var raw = $"{{\"orderId\":\"{orderId}\",\"productId\":\"coins_100\",\"purchaseToken\":\"{purchaseToken}\"}}";
            var wrapper = $"{{\"json\":{Quote(raw)},\"signature\":\"signature\"}}";
            return CreateUnifiedReceipt(wrapper, purchaseToken);
        }

        private static string CreateUnifiedReceipt(string payload, string transactionId = "transaction") =>
            $"{{\"Store\":\"GooglePlay\",\"TransactionID\":{Quote(transactionId)},\"Payload\":{Quote(payload)}}}";

        private static string Quote(string value) =>
            "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        private sealed class TestOrderInfo : IOrderInfo
        {
            public TestOrderInfo(string receipt, string transactionId, bool google)
            {
                Receipt = receipt;
                TransactionID = transactionId;
                Google = google ? new TestGoogleOrderInfo() : null;
            }

            public IAppleOrderInfo Apple => null;
            public IGoogleOrderInfo Google { get; }
            public List<IPurchasedProductInfo> PurchasedProductInfo { get; set; } = new();
            public string Receipt { get; }
            public string TransactionID { get; }
        }

        private sealed class TestGoogleOrderInfo : IGoogleOrderInfo
        {
            public string ObfuscatedAccountId { get; set; }
            public string ObfuscatedProfileId { get; set; }
        }
    }
}
