using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Debug;
using UnityEngine.Purchasing;

namespace Evo.Infrastructure.Services.Purchases.UnityIap
{
    public sealed class UnityIapPurchaseAdapter : IPurchaseAdapter
    {
        internal const string Id = "unity-iap";
        private const string Source = "Unity IAP Purchase Adapter";

        private readonly Dictionary<string, string> _productByStoreId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PendingOrder> _pendingByTransactionId = new(StringComparer.Ordinal);
        private readonly HashSet<string> _confirmedTransactionIds = new(StringComparer.Ordinal);
        private readonly List<PurchaseStoreProduct> _products = new();
        private StoreController _controller;
        private UniTaskCompletionSource<List<Product>> _productsFetch;
        private UniTaskCompletionSource<Orders> _purchasesFetch;
        private UniTaskCompletionSource<PurchaseAdapterResult> _activePurchase;
        private string _activeStoreProductId;
        private bool _isInitialized;
        private bool _isAvailable;
        private bool _disposed;
        private bool _warningLogged;

        public string AdapterId => Id;
        public bool IsInitialized => _isInitialized;
        public bool IsAvailable => _isAvailable;
        public IReadOnlyList<PurchaseStoreProduct> Products => _products;

        public async UniTask InitializeAsync(
            IReadOnlyList<PurchaseAdapterProductDefinition> products,
            CancellationToken cancellationToken)
        {
            if (_isInitialized || _disposed)
                return;

            try
            {
                var definitions = BuildDefinitions(products);
                if (definitions.Count == 0)
                {
                    WarnOnce("No valid Unity IAP product mappings were configured.");
                    return;
                }

                _controller = UnityIAPServices.StoreController();
                Subscribe();
                await _controller.Connect().AsUniTask().AttachExternalCancellation(cancellationToken);

                _productsFetch = new UniTaskCompletionSource<List<Product>>();
                _controller.FetchProducts(definitions);
                var fetched = await _productsFetch.Task.AttachExternalCancellation(cancellationToken);
                UpdateProducts(fetched);
                if (_products.Count == 0)
                {
                    WarnOnce("Unity IAP returned no configured products.");
                    return;
                }

                _purchasesFetch = new UniTaskCompletionSource<Orders>();
                _controller.FetchPurchases();
                await _purchasesFetch.Task.AttachExternalCancellation(cancellationToken);
                _isAvailable = _products.Any(product => product.IsAvailable);
            }
            catch (OperationCanceledException)
            {
                WarnOnce("Unity IAP initialization was cancelled.");
            }
            catch (Exception ex)
            {
                WarnOnce($"Unity IAP initialization failed: {ex.Message}");
            }
            finally
            {
                _isInitialized = true;
            }
        }

        public async UniTask<PurchaseAdapterResult> PurchaseAsync(
            string productId,
            string storeProductId,
            CancellationToken cancellationToken)
        {
            if (!_isInitialized)
                return new PurchaseAdapterResult(PurchaseStatus.NotInitialized);
            if (!_isAvailable || _controller == null)
                return new PurchaseAdapterResult(PurchaseStatus.Unavailable);
            if (string.IsNullOrWhiteSpace(storeProductId))
                return new PurchaseAdapterResult(PurchaseStatus.InvalidProduct);
            if (_activePurchase != null)
                return new PurchaseAdapterResult(PurchaseStatus.StoreFailure, error: "Another purchase is already in progress.");

            var product = _controller.GetProducts()
                .FirstOrDefault(item => MatchesProduct(item, storeProductId));
            if (product == null || !product.availableToPurchase)
                return new PurchaseAdapterResult(PurchaseStatus.ProductUnavailable);

            try
            {
                _activeStoreProductId = storeProductId;
                _activePurchase = new UniTaskCompletionSource<PurchaseAdapterResult>();
                _controller.PurchaseProduct(product);
                return await _activePurchase.Task.AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Unity IAP does not expose a request correlation ID before the order callback.
                // Keep late callbacks from being mistaken for a subsequent purchase of the same SKU.
                _isAvailable = false;
                return new PurchaseAdapterResult(PurchaseStatus.Cancelled);
            }
            catch (Exception ex)
            {
                WarnOnce($"Unity IAP purchase failed: {ex.Message}");
                return new PurchaseAdapterResult(PurchaseStatus.SdkException, error: ex.Message);
            }
            finally
            {
                _activePurchase = null;
                _activeStoreProductId = null;
            }
        }

        public async UniTask<IReadOnlyList<PurchaseTransaction>> RestoreAsync(CancellationToken cancellationToken)
        {
            if (!_isInitialized || !_isAvailable || _controller == null)
                return Array.Empty<PurchaseTransaction>();

            try
            {
                _purchasesFetch = new UniTaskCompletionSource<Orders>();
                _controller.FetchPurchases();
                var orders = await _purchasesFetch.Task.AttachExternalCancellation(cancellationToken);
                return BuildRestoredTransactions(orders);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                WarnOnce($"Unity IAP restore failed: {ex.Message}");
                return Array.Empty<PurchaseTransaction>();
            }
        }

        public UniTask<bool> ConfirmAsync(PurchaseTransaction transaction, CancellationToken cancellationToken)
        {
            if (_controller == null || cancellationToken.IsCancellationRequested ||
                string.IsNullOrWhiteSpace(transaction.TransactionId))
                return UniTask.FromResult(false);

            // Confirmed non-consumables/subscriptions are returned during restore. They still
            // need project fulfillment, but must not be confirmed in the store a second time.
            if (_confirmedTransactionIds.Remove(transaction.TransactionId))
                return UniTask.FromResult(true);
            if (!_pendingByTransactionId.TryGetValue(transaction.TransactionId, out var order))
                return UniTask.FromResult(false);

            try
            {
                _controller.ConfirmPurchase(order);
                _pendingByTransactionId.Remove(transaction.TransactionId);
                return UniTask.FromResult(true);
            }
            catch (Exception ex)
            {
                WarnOnce($"Unity IAP confirmation failed: {ex.Message}");
                return UniTask.FromResult(false);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Unsubscribe();
            _controller = null;
            _productsFetch?.TrySetCanceled();
            _purchasesFetch?.TrySetCanceled();
            _activePurchase?.TrySetResult(new PurchaseAdapterResult(PurchaseStatus.Unavailable));
        }

        private List<ProductDefinition> BuildDefinitions(IReadOnlyList<PurchaseAdapterProductDefinition> products)
        {
            var definitions = new List<ProductDefinition>();
            _productByStoreId.Clear();
            if (products == null)
                return definitions;

            foreach (var product in products)
            {
                if (string.IsNullOrWhiteSpace(product.ProductId) || string.IsNullOrWhiteSpace(product.StoreProductId) ||
                    _productByStoreId.ContainsKey(product.StoreProductId))
                    continue;

                _productByStoreId.Add(product.StoreProductId, product.ProductId);
                definitions.Add(new ProductDefinition(product.StoreProductId, ConvertType(product.ProductType)));
            }

            return definitions;
        }

        private void UpdateProducts(IEnumerable<Product> products)
        {
            _products.Clear();
            if (products == null)
                return;

            foreach (var product in products.Where(product => product?.definition != null))
            {
                var metadata = product.metadata;
                _products.Add(new PurchaseStoreProduct(
                    product.definition.storeSpecificId,
                    product.availableToPurchase,
                    metadata?.localizedTitle,
                    metadata?.localizedDescription,
                    metadata?.localizedPriceString,
                    metadata?.localizedPrice ?? 0,
                    metadata?.isoCurrencyCode));
            }
        }

        private void Subscribe()
        {
            _controller.OnProductsFetched += OnProductsFetched;
            _controller.OnProductsFetchFailed += OnProductsFetchFailed;
            _controller.OnPurchasesFetched += OnPurchasesFetched;
            _controller.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            _controller.OnPurchasePending += OnPurchasePending;
            _controller.OnPurchaseFailed += OnPurchaseFailed;
            _controller.OnPurchaseDeferred += OnPurchaseDeferred;
            _controller.OnStoreDisconnected += OnStoreDisconnected;
        }

        private void Unsubscribe()
        {
            if (_controller == null)
                return;
            _controller.OnProductsFetched -= OnProductsFetched;
            _controller.OnProductsFetchFailed -= OnProductsFetchFailed;
            _controller.OnPurchasesFetched -= OnPurchasesFetched;
            _controller.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            _controller.OnPurchasePending -= OnPurchasePending;
            _controller.OnPurchaseFailed -= OnPurchaseFailed;
            _controller.OnPurchaseDeferred -= OnPurchaseDeferred;
            _controller.OnStoreDisconnected -= OnStoreDisconnected;
        }

        private void OnProductsFetched(List<Product> products) => _productsFetch?.TrySetResult(products);
        private void OnProductsFetchFailed(ProductFetchFailed failure) =>
            _productsFetch?.TrySetException(new InvalidOperationException(failure?.FailureReason ?? "Product fetch failed."));
        private void OnPurchasesFetched(Orders orders) => _purchasesFetch?.TrySetResult(orders);
        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure) =>
            _purchasesFetch?.TrySetException(new InvalidOperationException(failure?.Message ?? "Purchase fetch failed."));
        private void OnStoreDisconnected(StoreConnectionFailureDescription failure) =>
            WarnOnce($"Unity IAP store disconnected: {failure?.Message ?? "unknown error"}");

        private void OnPurchasePending(PendingOrder order)
        {
            var transaction = ToTransaction(order, false);
            if (string.IsNullOrWhiteSpace(transaction.TransactionId))
            {
                if (IsActiveOrder(order))
                    _activePurchase?.TrySetResult(new PurchaseAdapterResult(
                        PurchaseStatus.StoreFailure, error: "Unity IAP returned a pending order without a transaction ID."));
                return;
            }

            _pendingByTransactionId[transaction.TransactionId] = order;
            if (IsActiveOrder(order))
                _activePurchase?.TrySetResult(new PurchaseAdapterResult(PurchaseStatus.Succeeded, transaction));
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            if (!IsActiveOrder(order))
                return;
            var status = order.FailureReason == PurchaseFailureReason.UserCancelled
                ? PurchaseStatus.Cancelled
                : PurchaseStatus.StoreFailure;
            _activePurchase?.TrySetResult(new PurchaseAdapterResult(status, error: order.Details));
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            if (IsActiveOrder(order))
                _activePurchase?.TrySetResult(new PurchaseAdapterResult(PurchaseStatus.Deferred));
        }

        private IReadOnlyList<PurchaseTransaction> BuildRestoredTransactions(Orders orders)
        {
            if (orders == null)
                return Array.Empty<PurchaseTransaction>();
            var result = new List<PurchaseTransaction>();
            foreach (var order in orders.PendingOrders ?? Array.Empty<PendingOrder>())
            {
                var transaction = ToTransaction(order, true);
                if (string.IsNullOrWhiteSpace(transaction.TransactionId))
                    continue;
                _pendingByTransactionId[transaction.TransactionId] = order;
                result.Add(transaction);
            }
            foreach (var order in orders.ConfirmedOrders ?? Array.Empty<ConfirmedOrder>())
            {
                var product = FirstProduct(order);
                if (product?.definition == null || product.definition.type == ProductType.Consumable)
                    continue;
                var transaction = product.definition.type == ProductType.Subscription
                    ? ToTransaction(order, true)
                    : ToConfirmedEntitlementTransaction(order, product);
                if (string.IsNullOrWhiteSpace(transaction.TransactionId))
                    continue;
                _confirmedTransactionIds.Add(transaction.TransactionId);
                result.Add(transaction);
            }
            return result;
        }

        private PurchaseTransaction ToConfirmedEntitlementTransaction(ConfirmedOrder order, Product product)
        {
            var storeId = product.definition.storeSpecificId ?? string.Empty;
            _productByStoreId.TryGetValue(storeId, out var productId);
            var metadata = product.metadata;
            // Confirmed non-consumables and subscriptions are entitlements, not pending orders.
            // Unity IAP may omit TransactionID/Receipt here, so use a stable entitlement identity.
            return new PurchaseTransaction(
                "unity-iap-entitlement:" + storeId,
                productId ?? string.Empty,
                storeId,
                Id,
                order?.Info?.Receipt,
                price: metadata?.localizedPrice ?? 0,
                currencyCode: metadata?.isoCurrencyCode,
                isRestored: true);
        }

        private PurchaseTransaction ToTransaction(Order order, bool restored)
        {
            var product = FirstProduct(order);
            var storeId = product?.definition?.storeSpecificId ?? string.Empty;
            _productByStoreId.TryGetValue(storeId, out var productId);
            var metadata = product?.metadata;
            return new PurchaseTransaction(
                ResolveTransactionId(order, storeId),
                productId ?? string.Empty,
                storeId,
                Id,
                order?.Info?.Receipt,
                price: metadata?.localizedPrice ?? 0,
                currencyCode: metadata?.isoCurrencyCode,
                isRestored: restored);
        }

        private static string ResolveTransactionId(Order order, string storeProductId)
        {
            var transactionId = order?.Info?.TransactionID;
            if (!string.IsNullOrWhiteSpace(transactionId))
                return transactionId;

            var receipt = order?.Info?.Receipt;
            if (string.IsNullOrWhiteSpace(receipt) || string.IsNullOrWhiteSpace(storeProductId))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(storeProductId + "\n" + receipt);
            var hash = sha256.ComputeHash(bytes);
            return "unity-iap-recovery-" + BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }

        private bool IsActiveOrder(Order order)
        {
            var product = FirstProduct(order);
            return product != null && MatchesProduct(product, _activeStoreProductId);
        }

        private static Product FirstProduct(Order order) => order?.CartOrdered?.Items()?.FirstOrDefault()?.Product;
        private static bool MatchesProduct(Product product, string storeProductId) =>
            product?.definition != null &&
            (string.Equals(product.definition.storeSpecificId, storeProductId, StringComparison.Ordinal) ||
             string.Equals(product.definition.id, storeProductId, StringComparison.Ordinal));

        private static ProductType ConvertType(PurchaseProductType type) => type switch
        {
            PurchaseProductType.NonConsumable => ProductType.NonConsumable,
            PurchaseProductType.Subscription => ProductType.Subscription,
            _ => ProductType.Consumable
        };

        private void WarnOnce(string message)
        {
            if (_warningLogged)
                return;
            _warningLogged = true;
            EvoDebug.LogWarning(message, Source);
        }
    }
}
