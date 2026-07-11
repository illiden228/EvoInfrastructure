using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Debug;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases.Yandex
{
    public sealed class YandexPurchaseAdapter : IPurchaseAdapter
    {
        private const string Source = nameof(YandexPurchaseAdapter);
        private const string RecoveryKeyPrefix = "evo.purchases.yandex.pending.";

        private readonly YandexPurchasesOptions _options;
        private readonly IYandexPaymentsBridge _bridge;
        private readonly List<PurchaseStoreProduct> _products = new();
        private readonly Dictionary<string, string> _offersByStoreId =
            new(StringComparer.OrdinalIgnoreCase);

        private UniTaskCompletionSource<bool> _catalogReceived;
        private UniTaskCompletionSource<PurchaseAdapterResult> _pendingPurchase;
        private string _pendingOfferId;
        private string _pendingStoreProductId;
        private bool _missingSdkWarningLogged;
        private bool _disposed;

        internal YandexPurchaseAdapter(YandexPurchasesOptions options, IYandexPaymentsBridge bridge)
        {
            _options = options ?? new YandexPurchasesOptions();
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _bridge.CatalogReceived += OnCatalogReceived;
            _bridge.PurchaseSucceeded += OnPurchaseSucceeded;
            _bridge.PurchaseFailed += OnPurchaseFailed;
        }

        public string AdapterId => YandexPurchaseAdapterFactory.DefaultAdapterId;
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
                IndexProducts(products);
                if (!_bridge.IsAvailable)
                {
                    LogMissingSdkOnce();
                    return;
                }

                RefreshProducts();
                if (_products.Count == 0 && _options.CatalogTimeout > TimeSpan.Zero)
                {
                    _catalogReceived = new UniTaskCompletionSource<bool>();
                    await WaitForCatalogAsync(cancellationToken);
                    RefreshProducts();
                }

                IsAvailable = true;
            }
            catch (OperationCanceledException)
            {
                IsAvailable = false;
            }
            catch (Exception exception)
            {
                IsAvailable = false;
                EvoDebug.LogWarning($"PluginYG2 Payments initialization failed: {exception.Message}", Source);
            }
            finally
            {
                _catalogReceived = null;
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
                return new PurchaseAdapterResult(PurchaseStatus.NotInitialized);
            }

            if (!IsAvailable || string.IsNullOrWhiteSpace(storeProductId))
            {
                return new PurchaseAdapterResult(PurchaseStatus.Unavailable);
            }

            if (_pendingPurchase != null)
            {
                return new PurchaseAdapterResult(PurchaseStatus.StoreFailure,
                    error: "Another Yandex purchase is already in progress.");
            }

            try
            {
                _pendingOfferId = offerId;
                _pendingStoreProductId = storeProductId;
                EnsureRecoveryId(storeProductId);
                _pendingPurchase = new UniTaskCompletionSource<PurchaseAdapterResult>();
                _bridge.Buy(storeProductId);

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (_options.PurchaseTimeout > TimeSpan.Zero)
                {
                    timeout.CancelAfter(_options.PurchaseTimeout);
                }

                try
                {
                    return await _pendingPurchase.Task.AttachExternalCancellation(timeout.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // PluginYG2 exposes no request/transaction correlation. Disable new purchases for this
                    // session so a late callback cannot be mistaken for a subsequent purchase of the same ID.
                    IsAvailable = false;
                    return new PurchaseAdapterResult(PurchaseStatus.Timeout);
                }
            }
            catch (OperationCanceledException)
            {
                IsAvailable = false;
                return new PurchaseAdapterResult(PurchaseStatus.Cancelled);
            }
            catch (Exception exception)
            {
                return new PurchaseAdapterResult(PurchaseStatus.SdkException, error: exception.Message);
            }
            finally
            {
                _pendingPurchase = null;
                _pendingOfferId = null;
                _pendingStoreProductId = null;
            }
        }

        public UniTask<IReadOnlyList<PurchaseTransaction>> RestoreAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsAvailable)
            {
                return UniTask.FromResult<IReadOnlyList<PurchaseTransaction>>(
                    Array.Empty<PurchaseTransaction>());
            }

            RefreshProducts();
            var restored = _bridge.Products
                .Where(product => !product.Consumed &&
                                  _offersByStoreId.ContainsKey(product.Id) &&
                                  PlayerPrefs.HasKey(RecoveryKey(product.Id)))
                .Select(product => CreateTransaction(
                    _offersByStoreId[product.Id], product.Id, true))
                .ToArray();
            return UniTask.FromResult<IReadOnlyList<PurchaseTransaction>>(restored);
        }

        public async UniTask<bool> ConfirmAsync(
            PurchaseTransaction transaction,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsAvailable || string.IsNullOrWhiteSpace(transaction.StoreProductId))
            {
                return false;
            }

            try
            {
                if (!_bridge.Consume(transaction.StoreProductId))
                {
                    return false;
                }

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (_options.ConsumeTimeout > TimeSpan.Zero)
                {
                    timeout.CancelAfter(_options.ConsumeTimeout);
                }

                while (!timeout.IsCancellationRequested)
                {
                    var product = _bridge.Products.FirstOrDefault(item =>
                        string.Equals(
                            item.Id,
                            transaction.StoreProductId,
                            StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(product.Id) && product.Consumed)
                    {
                        PlayerPrefs.DeleteKey(RecoveryKey(transaction.StoreProductId));
                        PlayerPrefs.Save();
                        return true;
                    }

                    await UniTask.Delay(100, cancellationToken: timeout.Token);
                }

                return false;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                EvoDebug.LogWarning($"PluginYG2 purchase consumption failed: {exception.Message}", Source);
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _bridge.CatalogReceived -= OnCatalogReceived;
            _bridge.PurchaseSucceeded -= OnPurchaseSucceeded;
            _bridge.PurchaseFailed -= OnPurchaseFailed;
            _bridge.Dispose();
            _pendingPurchase?.TrySetResult(new PurchaseAdapterResult(PurchaseStatus.Cancelled));
        }

        private async UniTask WaitForCatalogAsync(CancellationToken cancellationToken)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_options.CatalogTimeout);
            try
            {
                await _catalogReceived.Task.AttachExternalCancellation(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // An empty/late catalog is a valid unavailable-product state, not a failed initialization.
            }
        }

        private void IndexProducts(IReadOnlyList<PurchaseAdapterProductDefinition> products)
        {
            _offersByStoreId.Clear();
            if (products == null)
            {
                return;
            }

            foreach (var product in products)
            {
                if (!string.IsNullOrWhiteSpace(product.StoreProductId) &&
                    !string.IsNullOrWhiteSpace(product.OfferId))
                {
                    _offersByStoreId[product.StoreProductId] = product.OfferId;
                }
            }
        }

        private void RefreshProducts()
        {
            _products.Clear();
            foreach (var product in _bridge.Products)
            {
                _products.Add(new PurchaseStoreProduct(
                    product.Id,
                    true,
                    product.Title,
                    product.Description,
                    product.LocalizedPrice,
                    product.Price,
                    product.CurrencyCode,
                    product.ImageUrl));
            }
        }

        private void OnCatalogReceived()
        {
            RefreshProducts();
            _catalogReceived?.TrySetResult(true);
        }

        private void OnPurchaseSucceeded(string storeProductId)
        {
            if (_pendingPurchase == null ||
                !string.Equals(storeProductId, _pendingStoreProductId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _pendingPurchase.TrySetResult(new PurchaseAdapterResult(
                PurchaseStatus.Succeeded,
                CreateTransaction(_pendingOfferId, storeProductId, false)));
        }

        private void OnPurchaseFailed(string storeProductId)
        {
            if (_pendingPurchase == null ||
                !string.Equals(storeProductId, _pendingStoreProductId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            PlayerPrefs.DeleteKey(RecoveryKey(storeProductId));
            PlayerPrefs.Save();
            _pendingPurchase.TrySetResult(new PurchaseAdapterResult(PurchaseStatus.StoreFailure));
        }

        private PurchaseTransaction CreateTransaction(string offerId, string storeProductId, bool restored)
        {
            var storeProduct = _bridge.Products.FirstOrDefault(product =>
                string.Equals(product.Id, storeProductId, StringComparison.OrdinalIgnoreCase));
            return new PurchaseTransaction(
                EnsureRecoveryId(storeProductId),
                offerId,
                storeProductId,
                AdapterId,
                price: storeProduct.Price,
                currencyCode: storeProduct.CurrencyCode,
                purchaseTime: DateTimeOffset.UtcNow,
                isRestored: restored);
        }

        private static string EnsureRecoveryId(string storeProductId)
        {
            var key = RecoveryKey(storeProductId);
            var transactionId = PlayerPrefs.GetString(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                return transactionId;
            }

            transactionId = $"yandex:{storeProductId}:{Guid.NewGuid():N}";
            PlayerPrefs.SetString(key, transactionId);
            PlayerPrefs.Save();
            return transactionId;
        }

        private static string RecoveryKey(string storeProductId)
        {
            return RecoveryKeyPrefix + (storeProductId ?? string.Empty);
        }

        private void LogMissingSdkOnce()
        {
            if (_missingSdkWarningLogged)
            {
                return;
            }

            _missingSdkWarningLogged = true;
            EvoDebug.LogWarning(
                "PluginYG2 Payments module is unavailable. Enable the module so Payments_yg is defined.",
                Source);
        }
    }
}
