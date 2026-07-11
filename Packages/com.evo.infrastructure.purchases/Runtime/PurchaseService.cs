using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Config;
using Evo.Infrastructure.Services.Debug;
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
        private readonly List<PurchaseOffer> _offers = new();
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
        public bool IsAvailable => IsInitialized && _adapter?.IsAvailable == true && _offers.Any(x => x.IsAvailable);
        public string ActiveAdapterId => _adapter?.AdapterId ?? string.Empty;
        public IReadOnlyList<PurchaseOffer> Offers => _offers;
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
                LoadConfigs(out var catalog, out var routing);
                var factory = SelectFactory(routing);
                if (factory == null)
                {
                    EvoDebug.LogWarning("No unambiguous purchase adapter is configured.", SOURCE);
                    return;
                }

                _adapter = factory.Create();
                var definitions = BuildAdapterProductDefinitions(catalog, factory.AdapterId);
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

        public bool TryGetOffer(string offerId, out PurchaseOffer offer)
        {
            offer = _offers.FirstOrDefault(x => string.Equals(x.Id, offerId, StringComparison.OrdinalIgnoreCase));
            return offer != null;
        }

        public async UniTask<PurchaseResult> PurchaseAsync(string offerId, CancellationToken cancellationToken = default)
        {
            if (!IsInitialized)
            {
                return new PurchaseResult(PurchaseStatus.NotInitialized);
            }

            if (_adapter?.IsAvailable != true)
            {
                return new PurchaseResult(PurchaseStatus.Unavailable);
            }

            if (!TryGetOffer(offerId, out var offer))
            {
                return new PurchaseResult(PurchaseStatus.InvalidOffer);
            }

            if (!offer.IsAvailable)
            {
                return new PurchaseResult(PurchaseStatus.ProductUnavailable);
            }

            try
            {
                using var timeout = CreateTimeout(cancellationToken, _options.PurchaseTimeoutSeconds);
                var result = await _adapter.PurchaseAsync(offer.Id, offer.StoreProductId, timeout.Token)
                    .AttachExternalCancellation(timeout.Token);
                if (timeout.IsCancellationRequested)
                {
                    return new PurchaseResult(cancellationToken.IsCancellationRequested
                        ? PurchaseStatus.Cancelled
                        : PurchaseStatus.Timeout);
                }

                return result.Success
                    ? await FulfillAndConfirmAsync(offer, result.Transaction, timeout.Token)
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
                        if (!TryGetOffer(transaction.OfferId, out var offer))
                        {
                            offer = _offers.FirstOrDefault(candidate =>
                                string.Equals(
                                    candidate.StoreProductId,
                                    transaction.StoreProductId,
                                    StringComparison.OrdinalIgnoreCase));
                        }

                        results.Add(offer == null
                            ? new PurchaseResult(PurchaseStatus.InvalidOffer, transaction)
                            : await FulfillAndConfirmAsync(offer, transaction, timeout.Token));
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
            out PurchaseRoutingConfig routing)
        {
            catalog = null;
            routing = null;
            if (_configs == null)
            {
                return;
            }

            _configs.TryGet(out catalog);
            _configs.TryGet(out routing);
        }

        private IReadOnlyList<PurchaseAdapterProductDefinition> BuildAdapterProductDefinitions(
            PurchaseCatalogConfig catalog,
            string adapterId)
        {
            var catalogIssues = PurchaseCatalogValidator.Validate(catalog);
            LogCatalogIssues(catalogIssues);

            var invalidOfferIds = new HashSet<string>(
                catalogIssues
                    .Where(issue => issue.Severity == PurchaseCatalogIssueSeverity.Error &&
                                    !string.IsNullOrWhiteSpace(issue.OfferId))
                    .Select(issue => issue.OfferId),
                StringComparer.OrdinalIgnoreCase);

            _offers.Clear();
            _offers.AddRange(PurchaseCatalogResolver.Resolve(catalog, adapterId, Application.platform));

            var duplicateStoreIds = FindDuplicateStoreProductIds();
            foreach (var duplicateStoreId in duplicateStoreIds)
            {
                EvoDebug.LogWarning(
                    $"Store product ID '{duplicateStoreId}' maps to multiple logical offers and was disabled.",
                    SOURCE);
            }

            return _offers
                .Where(offer => offer.IsEnabled &&
                                !invalidOfferIds.Contains(offer.Id) &&
                                !string.IsNullOrWhiteSpace(offer.StoreProductId) &&
                                !duplicateStoreIds.Contains(offer.StoreProductId))
                .Select(offer => new PurchaseAdapterProductDefinition(
                    offer.Id,
                    offer.StoreProductId,
                    offer.ProductType))
                .ToArray();
        }

        private void LogCatalogIssues(IReadOnlyList<PurchaseCatalogIssue> issues)
        {
            foreach (var issue in issues)
            {
                EvoDebug.LogWarning($"Purchase catalog {issue.Severity}: {issue}", SOURCE);
            }
        }

        private HashSet<string> FindDuplicateStoreProductIds()
        {
            return new HashSet<string>(
                _offers
                    .Where(offer => offer.IsEnabled &&
                                    !string.IsNullOrWhiteSpace(offer.StoreProductId))
                    .GroupBy(offer => offer.StoreProductId, StringComparer.OrdinalIgnoreCase)
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

        private async UniTask<PurchaseResult> FulfillAndConfirmAsync(PurchaseOffer offer,
            PurchaseTransaction transaction, CancellationToken cancellationToken)
        {
            var handlers = _handlers.Where(x => x.CanFulfill(offer.FulfillmentKey)).Take(2).ToArray();
            if (handlers.Length != 1)
            {
                return new PurchaseResult(PurchaseStatus.FulfillmentUnavailable, transaction,
                    handlers.Length == 0
                        ? $"No fulfillment handler accepts '{offer.FulfillmentKey}'."
                        : $"Multiple fulfillment handlers accept '{offer.FulfillmentKey}'.");
            }

            var handler = handlers[0];
            var fulfillment = await handler.FulfillAsync(offer, transaction, cancellationToken);
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

        private IPurchaseAdapterFactory SelectFactory(PurchaseRoutingConfig routing)
        {
            var platform = PurchaseCatalogResolver.CurrentPlatform;
            var candidates = (routing?.Adapters ?? Array.Empty<PurchaseAdapterBinding>())
                .Where(x => x != null && x.Enabled && (x.Platforms & platform) != 0)
                .Join(_factories, x => x.AdapterId, x => x.AdapterId,
                    (binding, factory) => (binding.Priority, Factory: factory), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(x => x.Priority)
                .ToArray();
            if (candidates.Length == 0)
            {
                return null;
            }

            if (candidates.Length > 1 && candidates[0].Priority == candidates[1].Priority)
            {
                return null;
            }

            return candidates[0].Factory;
        }

        private void ApplyStoreProducts()
        {
            foreach (var offer in _offers)
            {
                PurchaseStoreProduct store = default;
                if (_adapter.Products != null)
                {
                    store = _adapter.Products.FirstOrDefault(x =>
                        string.Equals(x.StoreProductId, offer.StoreProductId, StringComparison.OrdinalIgnoreCase));
                }

                if (string.IsNullOrWhiteSpace(store.StoreProductId))
                {
                    continue;
                }

                offer.IsAvailable = offer.IsEnabled && store.IsAvailable;
                offer.LocalizedTitle = store.Title;
                offer.LocalizedDescription = store.Description;
                offer.LocalizedPrice = store.LocalizedPrice;
                offer.Price = store.Price;
                offer.CurrencyCode = store.CurrencyCode;
                offer.ImageUrl = store.ImageUrl;
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
