using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Debug;
using RuStore;
using RuStore.PayClient;

namespace Evo.Infrastructure.Services.Purchases.RuStore
{
    public sealed class RuStorePurchaseAdapter : IPurchaseAdapter
    {
        internal const string AdapterIdValue = "rustore";
        private const string Source = "RuStore Purchase Adapter";

        private readonly RuStorePurchaseAdapterConfig _config;
        private readonly List<PurchaseStoreProduct> _products = new();
        private readonly Dictionary<string, string> _offerByStoreId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PurchaseStoreProduct> _productByStoreId = new(StringComparer.Ordinal);
        private readonly HashSet<string> _pendingTwoStepPurchases = new(StringComparer.Ordinal);
        private bool _warningLogged;
        private bool _disposed;

        public RuStorePurchaseAdapter(IConfigService configs)
        {
            configs?.TryGet(out _config);
        }

        public string AdapterId => AdapterIdValue;
        public bool IsInitialized { get; private set; }
        public bool IsAvailable { get; private set; }
        public IReadOnlyList<PurchaseStoreProduct> Products => _products;

        public async UniTask InitializeAsync(
            IReadOnlyList<PurchaseAdapterProductDefinition> products,
            CancellationToken cancellationToken)
        {
            if (IsInitialized || _disposed)
            {
                return;
            }

            try
            {
                if (_config == null || !_config.Enabled)
                {
                    WarnOnce("RuStore config is missing or disabled; adapter is unavailable.");
                    return;
                }

#if !UNITY_ANDROID
                WarnOnce("RuStore Pay is available only on Android players.");
                return;
#else
                var storeIds = CacheDefinitions(products);
                if (storeIds.Length == 0)
                {
                    WarnOnce("No RuStore product IDs were resolved from the purchase catalog.");
                    return;
                }

                var availability = await GetAvailabilityAsync(cancellationToken);
                if (!availability.isAvailable)
                {
                    WarnOnce($"RuStore payments are unavailable: {availability.cause}");
                    return;
                }

                var sdkProducts = await GetProductsAsync(storeIds, cancellationToken);
                CacheProducts(sdkProducts);
                IsAvailable = _products.Count > 0;
                if (!IsAvailable)
                {
                    WarnOnce("RuStore returned no configured products.");
                }
#endif
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                WarnOnce($"Initialization failed: {exception.Message}");
            }
            finally
            {
                IsInitialized = true;
            }
        }

        public async UniTask<PurchaseAdapterResult> PurchaseAsync(
            string offerId,
            string storeProductId,
            CancellationToken cancellationToken)
        {
            if (!IsInitialized)
            {
                return Failure(PurchaseStatus.NotInitialized, "RuStore adapter is not initialized.");
            }

            if (!IsAvailable || _disposed)
            {
                return Failure(PurchaseStatus.Unavailable, "RuStore purchases are unavailable.");
            }

            if (string.IsNullOrWhiteSpace(storeProductId) || !_productByStoreId.ContainsKey(storeProductId))
            {
                return Failure(PurchaseStatus.ProductUnavailable, "RuStore product is unavailable.");
            }

            try
            {
                var result = await PurchaseSdkAsync(storeProductId, cancellationToken);
                var transactionId = Value(result.purchaseId);
                if (string.IsNullOrWhiteSpace(transactionId))
                {
                    return Failure(PurchaseStatus.StoreFailure, "RuStore returned an empty purchase ID.");
                }

                if (result.purchaseType == PurchaseType.TWO_STEP)
                {
                    _pendingTwoStepPurchases.Add(transactionId);
                }

                return new PurchaseAdapterResult(
                    PurchaseStatus.Succeeded,
                    CreateTransaction(
                        transactionId,
                        offerId,
                        storeProductId,
                        Value(result.invoiceId),
                        Value(result.orderId),
                        false));
            }
            catch (OperationCanceledException)
            {
                return Failure(PurchaseStatus.Cancelled, "RuStore purchase was cancelled.");
            }
            catch (Exception exception)
            {
                var cancelled = exception.GetType().Name.IndexOf("Cancelled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                exception.Message.IndexOf("Cancelled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                exception.Message.IndexOf("Canceled", StringComparison.OrdinalIgnoreCase) >= 0;
                return Failure(cancelled ? PurchaseStatus.Cancelled : PurchaseStatus.StoreFailure, exception.Message);
            }
        }

        public async UniTask<IReadOnlyList<PurchaseTransaction>> RestoreAsync(CancellationToken cancellationToken)
        {
            if (!IsAvailable || _disposed)
            {
                return Array.Empty<PurchaseTransaction>();
            }

            try
            {
                var purchases = await GetPurchasesAsync(cancellationToken);
                var restored = new List<PurchaseTransaction>(purchases.Count);
                foreach (var purchase in purchases)
                {
                    var status = purchase.status?.ToString();
                    if (!string.Equals(status, "PAID", StringComparison.Ordinal) &&
                        !string.Equals(status, "CONFIRMED", StringComparison.Ordinal) &&
                        !string.Equals(status, "ACTIVE", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var storeProductId = GetProductId(purchase);
                    if (string.IsNullOrWhiteSpace(storeProductId) ||
                        !_offerByStoreId.TryGetValue(storeProductId, out var offerId))
                    {
                        continue;
                    }

                    var transactionId = Value(purchase.purchaseId);
                    if (string.IsNullOrWhiteSpace(transactionId))
                    {
                        continue;
                    }

                    if (string.Equals(status, "PAID", StringComparison.Ordinal))
                    {
                        _pendingTwoStepPurchases.Add(transactionId);
                    }

                    restored.Add(CreateTransaction(
                        transactionId,
                        offerId,
                        storeProductId,
                        Value(purchase.invoiceId),
                        Value(purchase.orderId),
                        true,
                        purchase.price?.value ?? 0,
                        Value(purchase.currency),
                        purchase.purchaseTime));
                }

                return restored;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                WarnOnce($"Restore failed: {exception.Message}");
                return Array.Empty<PurchaseTransaction>();
            }
        }

        public async UniTask<bool> ConfirmAsync(
            PurchaseTransaction transaction,
            CancellationToken cancellationToken)
        {
            if (!IsAvailable || _disposed || string.IsNullOrWhiteSpace(transaction.TransactionId))
            {
                return false;
            }

            if (!_pendingTwoStepPurchases.Contains(transaction.TransactionId))
            {
                return true;
            }

            try
            {
                await ConfirmTwoStepAsync(transaction.TransactionId, cancellationToken);
                _pendingTwoStepPurchases.Remove(transaction.TransactionId);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                WarnOnce($"Confirmation failed: {exception.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _disposed = true;
            IsAvailable = false;
            _products.Clear();
            _productByStoreId.Clear();
            _offerByStoreId.Clear();
            _pendingTwoStepPurchases.Clear();
        }

        private ProductId[] CacheDefinitions(IReadOnlyList<PurchaseAdapterProductDefinition> definitions)
        {
            _offerByStoreId.Clear();
            if (definitions == null)
            {
                return Array.Empty<ProductId>();
            }

            var result = new List<ProductId>(definitions.Count);
            foreach (var definition in definitions)
            {
                if (string.IsNullOrWhiteSpace(definition.StoreProductId) ||
                    _offerByStoreId.ContainsKey(definition.StoreProductId))
                {
                    continue;
                }

                _offerByStoreId.Add(definition.StoreProductId, definition.OfferId);
                result.Add(new ProductId(definition.StoreProductId));
            }

            return result.ToArray();
        }

        private void CacheProducts(IReadOnlyList<Product> sdkProducts)
        {
            _products.Clear();
            _productByStoreId.Clear();
            if (sdkProducts == null)
            {
                return;
            }

            foreach (var product in sdkProducts)
            {
                var id = Value(product?.productId);
                if (string.IsNullOrWhiteSpace(id) || !_offerByStoreId.ContainsKey(id))
                {
                    continue;
                }

                var storeProduct = new PurchaseStoreProduct(
                    id,
                    true,
                    Value(product.title),
                    Value(product.description),
                    Value(product.amountLabel),
                    CurrencyMinorUnitConverter.ToMajorUnits(
                        product.price?.value ?? 0,
                        Value(product.currency)),
                    Value(product.currency),
                    Value(product.imageUrl));
                _products.Add(storeProduct);
                _productByStoreId[id] = storeProduct;
            }
        }

        private async UniTask<ProductPurchaseResult> PurchaseSdkAsync(
            string storeProductId,
            CancellationToken cancellationToken)
        {
            var source = new UniTaskCompletionSource<ProductPurchaseResult>();
            using var registration = cancellationToken.Register(() => source.TrySetCanceled(cancellationToken));
            var parameters = new ProductPurchaseParams(new ProductId(storeProductId));
            var theme = _config.PaymentTheme == RuStorePaymentTheme.Dark ? SdkTheme.DARK : SdkTheme.LIGHT;
            Action<RuStoreError> failure = error => source.TrySetException(new InvalidOperationException(error?.ToString()));
            Action<ProductPurchaseResult> success = result => source.TrySetResult(result);

            if (_config.PurchaseFlow == RuStorePurchaseFlow.RequireTwoStep)
            {
                RuStorePayClient.Instance.PurchaseTwoStep(parameters, theme, failure, success);
            }
            else
            {
                var flow = _config.PurchaseFlow == RuStorePurchaseFlow.PreferTwoStep
                    ? PreferredPurchaseType.TWO_STEP
                    : PreferredPurchaseType.ONE_STEP;
                RuStorePayClient.Instance.Purchase(parameters, flow, theme, failure, success);
            }

            return await source.Task;
        }

        private static UniTask<PurchaseAvailabilityResult> GetAvailabilityAsync(CancellationToken token) =>
            CallbackTask<PurchaseAvailabilityResult>(token,
                (failure, success) => RuStorePayClient.Instance.GetPurchaseAvailability(failure, success));

        private static UniTask<List<Product>> GetProductsAsync(ProductId[] ids, CancellationToken token) =>
            CallbackTask<List<Product>>(token,
                (failure, success) => RuStorePayClient.Instance.GetProducts(ids, failure, success));

        private static UniTask<List<RuStore.PayClient.IPurchase>> GetPurchasesAsync(CancellationToken token) =>
            CallbackTask<List<RuStore.PayClient.IPurchase>>(token,
                (failure, success) => RuStorePayClient.Instance.GetPurchases(failure, success));

        private static UniTask ConfirmTwoStepAsync(string purchaseId, CancellationToken token)
        {
            var source = new UniTaskCompletionSource();
            var registration = token.Register(() => source.TrySetCanceled(token));
            RuStorePayClient.Instance.ConfirmTwoStepPurchase(
                new PurchaseId(purchaseId),
                null,
                error => source.TrySetException(new InvalidOperationException(error?.ToString())),
                () => source.TrySetResult());
            return AwaitAndDispose(source.Task, registration);
        }

        private static async UniTask<T> CallbackTask<T>(
            CancellationToken token,
            Action<Action<RuStoreError>, Action<T>> invoke)
        {
            var source = new UniTaskCompletionSource<T>();
            using var registration = token.Register(() => source.TrySetCanceled(token));
            invoke(
                error => source.TrySetException(new InvalidOperationException(error?.ToString())),
                value => source.TrySetResult(value));
            return await source.Task;
        }

        private static async UniTask AwaitAndDispose(UniTask task, CancellationTokenRegistration registration)
        {
            try { await task; }
            finally { registration.Dispose(); }
        }

        private PurchaseTransaction CreateTransaction(
            string transactionId,
            string offerId,
            string storeProductId,
            string invoiceId,
            string orderId,
            bool restored,
            long priceMinor = 0,
            string currency = null,
            DateTime? purchaseTime = null)
        {
            if (_productByStoreId.TryGetValue(storeProductId, out var product))
            {
                currency ??= product.CurrencyCode;
                if (priceMinor <= 0)
                {
                    return new PurchaseTransaction(transactionId, offerId, storeProductId, AdapterId,
                        invoiceId, transactionId, orderId, product.Price, currency,
                        ToOffset(purchaseTime), restored);
                }
            }

            return new PurchaseTransaction(transactionId, offerId, storeProductId, AdapterId,
                invoiceId, transactionId, orderId,
                CurrencyMinorUnitConverter.ToMajorUnits(priceMinor, currency), currency,
                ToOffset(purchaseTime), restored);
        }

        private static DateTimeOffset ToOffset(DateTime? time) =>
            time.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(time.Value, DateTimeKind.Utc)) : default;

        private static string GetProductId(RuStore.PayClient.IPurchase purchase) =>
            purchase is ProductPurchase product ? Value(product.productId) :
            purchase is SubscriptionPurchase subscription ? Value(subscription.productId) : string.Empty;

        private static string Value<T>(BaseValue<T> value) => value == null
            ? string.Empty
            : Convert.ToString(value.value, CultureInfo.InvariantCulture) ?? string.Empty;
        private static PurchaseAdapterResult Failure(PurchaseStatus status, string error) => new(status, error: error);

        private void WarnOnce(string message)
        {
            if (_warningLogged) return;
            _warningLogged = true;
            EvoDebug.LogWarning(message, Source);
        }
    }
}
