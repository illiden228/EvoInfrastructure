using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Debug;
using Evo.Infrastructure.Services.PlatformInfo.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases
{
    public sealed class PurchaseService : IPurchaseService
    {
        private const string SOURCE = nameof(PurchaseService);
        private readonly IReadOnlyList<IPurchaseAdapterFactory> _factories;
        private readonly IReadOnlyList<IPurchaseFulfillmentHandler> _handlers;
        private readonly IConfigService _configs;
        private readonly PurchaseServiceOptions _options;
        private readonly List<PurchaseProduct> _products = new();
        private IPurchaseAdapter _adapter;
        private bool _initializing;
        private UniTaskCompletionSource _initializationCompletion;

        public PurchaseService(IEnumerable<IPurchaseAdapterFactory> factories,
            IEnumerable<IPurchaseFulfillmentHandler> handlers, IConfigService configs, PurchaseServiceOptions options)
        {
            _factories = factories?.Where(x => x != null).ToArray() ?? Array.Empty<IPurchaseAdapterFactory>();
            _handlers = handlers?.Where(x => x != null).ToArray() ?? Array.Empty<IPurchaseFulfillmentHandler>();
            _configs = configs;
            _options = options ?? new PurchaseServiceOptions();
        }

        public bool IsInitialized { get; private set; }
        public bool IsAvailable => IsInitialized && _adapter?.IsAvailable == true && _products.Any(x => x.IsAvailable);
        public string ActiveAdapterId => _adapter?.AdapterId ?? string.Empty;
        public IReadOnlyList<PurchaseProduct> Products => _products;
        public event Action CatalogChanged;
        public event Action<PurchaseTransaction> PurchaseCompleted;

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (IsInitialized)
            {
                return;
            }

            if (_initializing)
            {
                if (_initializationCompletion != null)
                {
                    await _initializationCompletion.Task.AttachExternalCancellation(cancellationToken);
                }

                return;
            }

            _initializing = true;
            _initializationCompletion = new UniTaskCompletionSource();
            try
            {
                LoadConfigs(out var catalog, out var routing, out var platformCatalog);
                LogConfigIssues("routing", PurchaseRoutingValidator.Validate(routing, platformCatalog));
                if (!PurchasePlatformIdUtility.TryGetCurrentPlatformId(platformCatalog, out var platformId))
                {
                    EvoDebug.LogWarning("Purchases require a known PlatformCatalog.CurrentPlatformId.", SOURCE);
                    return;
                }

                var factory = PurchaseRoutingResolver.SelectFactory(
                    routing,
                    _factories,
                    platformCatalog,
                    Application.isEditor);
                if (factory == null)
                {
                    EvoDebug.LogWarning("No unambiguous purchase adapter is configured.", SOURCE);
                    return;
                }

                _adapter = factory.Create();
                var definitions = BuildAdapterProductDefinitions(
                    catalog,
                    factory.AdapterId,
                    platformId,
                    platformCatalog);
                await InitializeAdapterAsync(definitions, cancellationToken);
                ApplyStoreProducts();
                CatalogChanged?.Invoke();

                if (_options.AutoRestorePendingPurchases && _adapter.IsAvailable)
                {
                    await RestoreAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Initialization always reaches a terminal state; cancellation leaves the service unavailable.
            }
            catch (Exception exception)
            {
                EvoDebug.LogWarning($"Purchase initialization failed: {exception.Message}", SOURCE);
            }
            finally
            {
                _initializing = false;
                IsInitialized = true;
                _initializationCompletion.TrySetResult();
            }
        }

        public bool TryGetProduct(string productId, out PurchaseProduct product)
        {
            product = _products.FirstOrDefault(x => string.Equals(x.Id, productId, StringComparison.OrdinalIgnoreCase));
            return product != null;
        }

        public async UniTask<PurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default)
        {
            if (!IsInitialized)
            {
                return new PurchaseResult(PurchaseStatus.NotInitialized);
            }

            if (_adapter?.IsAvailable != true)
            {
                return new PurchaseResult(PurchaseStatus.Unavailable);
            }

            if (!TryGetProduct(productId, out var product))
            {
                return new PurchaseResult(PurchaseStatus.InvalidProduct);
            }

            if (!product.IsAvailable)
            {
                return new PurchaseResult(PurchaseStatus.ProductUnavailable);
            }

            try
            {
                using var timeout = CreateTimeout(cancellationToken, _options.PurchaseTimeoutSeconds);
                var result = await _adapter.PurchaseAsync(product.Id, product.StoreProductId, timeout.Token)
                    .AttachExternalCancellation(timeout.Token);
                if (timeout.IsCancellationRequested)
                {
                    return new PurchaseResult(cancellationToken.IsCancellationRequested
                        ? PurchaseStatus.Cancelled
                        : PurchaseStatus.Timeout);
                }

                return result.Success
                    ? await FulfillAndConfirmAsync(product, result.Transaction, timeout.Token)
                    : new PurchaseResult(result.Status, result.Transaction, result.Error);
            }
            catch (OperationCanceledException)
            {
                return new PurchaseResult(cancellationToken.IsCancellationRequested
                    ? PurchaseStatus.Cancelled
                    : PurchaseStatus.Timeout);
            }
            catch (Exception exception)
            {
                return new PurchaseResult(PurchaseStatus.SdkException, error: exception.Message);
            }
        }

        public async UniTask<IReadOnlyList<PurchaseResult>> RestoreAsync(CancellationToken cancellationToken = default)
        {
            if (_adapter?.IsAvailable != true)
            {
                return Array.Empty<PurchaseResult>();
            }

            try
            {
                using var timeout = CreateTimeout(cancellationToken, _options.RestoreTimeoutSeconds);
                var transactions = await _adapter.RestoreAsync(timeout.Token).AttachExternalCancellation(timeout.Token);
                var results = new List<PurchaseResult>(transactions?.Count ?? 0);
                if (transactions == null)
                {
                    return results;
                }

                foreach (var transaction in transactions)
                {
                    if (timeout.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        if (!TryGetProduct(transaction.ProductId, out var product))
                        {
                            product = _products.FirstOrDefault(candidate =>
                                string.Equals(
                                    candidate.StoreProductId,
                                    transaction.StoreProductId,
                                    StringComparison.OrdinalIgnoreCase));
                        }

                        results.Add(product == null
                            ? new PurchaseResult(PurchaseStatus.InvalidProduct, transaction)
                            : await FulfillAndConfirmAsync(product, transaction, timeout.Token));
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        results.Add(new PurchaseResult(PurchaseStatus.Timeout, transaction));
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        results.Add(new PurchaseResult(
                            PurchaseStatus.SdkException,
                            transaction,
                            exception.Message));
                    }
                }
                return results;
            }
            catch (Exception exception)
            {
                EvoDebug.LogWarning($"Purchase restore failed: {exception.Message}", SOURCE);
                return Array.Empty<PurchaseResult>();
            }
        }

        public void Dispose()
        {
            _adapter?.Dispose();
            _adapter = null;
        }

        private void LoadConfigs(
            out PurchaseCatalogConfig catalog,
            out PurchaseRoutingConfig routing,
            out PlatformCatalog platformCatalog)
        {
            catalog = null;
            routing = null;
            platformCatalog = null;
            if (_configs == null)
            {
                return;
            }

            _configs.TryGet(out catalog);
            _configs.TryGet(out routing);
            _configs.TryGet(out platformCatalog);
        }

        private IReadOnlyList<PurchaseAdapterProductDefinition> BuildAdapterProductDefinitions(
            PurchaseCatalogConfig catalog,
            string adapterId,
            string platformId,
            PlatformCatalog platformCatalog)
        {
            var catalogIssues = PurchaseCatalogValidator.Validate(catalog, platformCatalog);
            LogConfigIssues("catalog", catalogIssues);

            var invalidProductIds = new HashSet<string>(
                catalogIssues
                    .Where(issue => issue.Severity == PurchaseCatalogIssueSeverity.Error &&
                                    !string.IsNullOrWhiteSpace(issue.ProductId))
                    .Select(issue => issue.ProductId),
                StringComparer.OrdinalIgnoreCase);

            _products.Clear();
            _products.AddRange(PurchaseCatalogResolver.Resolve(catalog, adapterId, platformId));

            var duplicateStoreIds = FindDuplicateStoreProductIds();
            foreach (var duplicateStoreId in duplicateStoreIds)
            {
                EvoDebug.LogWarning(
                    $"Store product ID '{duplicateStoreId}' maps to multiple logical products and was disabled.",
                    SOURCE);
            }

            return _products
                .Where(product => product.IsEnabled &&
                                !invalidProductIds.Contains(product.Id) &&
                                !string.IsNullOrWhiteSpace(product.StoreProductId) &&
                                !duplicateStoreIds.Contains(product.StoreProductId))
                .Select(product => new PurchaseAdapterProductDefinition(
                    product.Id,
                    product.StoreProductId,
                    product.ProductType))
                .ToArray();
        }

        private void LogConfigIssues(string configName, IReadOnlyList<PurchaseCatalogIssue> issues)
        {
            foreach (var issue in issues)
            {
                EvoDebug.LogWarning($"Purchase {configName} {issue.Severity}: {issue}", SOURCE);
            }
        }

        private HashSet<string> FindDuplicateStoreProductIds()
        {
            return new HashSet<string>(
                _products
                    .Where(product => product.IsEnabled &&
                                    !string.IsNullOrWhiteSpace(product.StoreProductId))
                    .GroupBy(product => product.StoreProductId, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key),
                StringComparer.OrdinalIgnoreCase);
        }

        private async UniTask InitializeAdapterAsync(
            IReadOnlyList<PurchaseAdapterProductDefinition> definitions,
            CancellationToken cancellationToken)
        {
            using var timeout = CreateTimeout(
                cancellationToken,
                _options.InitializationTimeoutSeconds);
            await _adapter
                .InitializeAsync(definitions, timeout.Token)
                .AttachExternalCancellation(timeout.Token);
        }

        private async UniTask<PurchaseResult> FulfillAndConfirmAsync(PurchaseProduct product,
            PurchaseTransaction transaction, CancellationToken cancellationToken)
        {
            var handlers = _handlers.Where(x => x.CanFulfill(product.FulfillmentKey)).Take(2).ToArray();
            if (handlers.Length != 1)
            {
                return new PurchaseResult(PurchaseStatus.FulfillmentUnavailable, transaction,
                    handlers.Length == 0
                        ? $"No fulfillment handler accepts '{product.FulfillmentKey}'."
                        : $"Multiple fulfillment handlers accept '{product.FulfillmentKey}'.");
            }

            var handler = handlers[0];
            var fulfillment = await handler.FulfillAsync(product, transaction, cancellationToken);
            if (!fulfillment.Success)
            {
                return new PurchaseResult(
                    PurchaseStatus.FulfillmentFailed,
                    transaction,
                    fulfillment.Error);
            }

            if (!await _adapter.ConfirmAsync(transaction, cancellationToken))
            {
                return new PurchaseResult(PurchaseStatus.StoreFailure, transaction, "Store confirmation failed.");
            }

            PurchaseCompleted?.Invoke(transaction);
            return new PurchaseResult(PurchaseStatus.Succeeded, transaction);
        }

        private void ApplyStoreProducts()
        {
            foreach (var product in _products)
            {
                PurchaseStoreProduct store = default;
                if (_adapter.Products != null)
                {
                    store = _adapter.Products.FirstOrDefault(x =>
                        string.Equals(x.StoreProductId, product.StoreProductId, StringComparison.OrdinalIgnoreCase));
                }

                if (string.IsNullOrWhiteSpace(store.StoreProductId))
                {
                    continue;
                }

                product.IsAvailable = product.IsEnabled && store.IsAvailable;
                product.LocalizedTitle = store.Title;
                product.LocalizedDescription = store.Description;
                product.LocalizedPrice = store.LocalizedPrice;
                product.Price = store.Price;
                product.CurrencyCode = store.CurrencyCode;
                product.ImageUrl = store.ImageUrl;
            }
        }

        private static CancellationTokenSource CreateTimeout(CancellationToken parent, float seconds)
        {
            var source = CancellationTokenSource.CreateLinkedTokenSource(parent);
            if (seconds > 0f)
            {
                source.CancelAfter(TimeSpan.FromSeconds(seconds));
            }

            return source;
        }
    }
}
