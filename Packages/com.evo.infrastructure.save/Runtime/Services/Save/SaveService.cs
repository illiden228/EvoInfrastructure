using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Debug;

namespace Evo.Infrastructure.Services.Save
{
    public sealed class SaveService : ISaveService
    {
        private readonly List<ISaveBackend> _backends;
        private readonly SaveBackendSelectionPolicy _selectionPolicy;

        public SaveService(IReadOnlyList<ISaveBackend> backends, SaveStorageOptions options = null)
        {
            _backends = backends != null ? new List<ISaveBackend>(backends) : new List<ISaveBackend>();
            _selectionPolicy = options?.backendSelection ?? new SaveBackendSelectionPolicy();
            _backends.Sort((left, right) =>
            {
                var leftPriority = left != null ? left.Priority : int.MinValue;
                var rightPriority = right != null ? right.Priority : int.MinValue;
                return rightPriority.CompareTo(leftPriority);
            });
        }

        public async UniTask<SaveEnvelope> LoadLatestValidAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            SaveEnvelope selected = null;
            var selectedPriority = int.MinValue;

            for (var i = 0; i < _backends.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var backend = _backends[i];
                if (!_selectionPolicy.CanLoad(backend))
                {
                    continue;
                }

                try
                {
                    var candidate = await backend.LoadAsync(cancellationToken);
                    if (!IsValid(candidate))
                    {
                        continue;
                    }

                    if (selected == null || candidate.updatedAtUnixMs > selected.updatedAtUnixMs ||
                        candidate.updatedAtUnixMs == selected.updatedAtUnixMs && backend.Priority > selectedPriority)
                    {
                        selected = candidate;
                        selectedPriority = backend.Priority;
                    }
                }
                catch (Exception ex)
                {
                    EvoDebug.LogWarning($"Load failed in backend '{backend.BackendId}': {ex.Message}", nameof(SaveService));
                }
            }

            return selected;
        }

        public async UniTask SaveAsync(SaveEnvelope envelope, System.Threading.CancellationToken cancellationToken = default)
        {
            if (!IsValid(envelope))
            {
                return;
            }

            envelope.updatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (var i = 0; i < _backends.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var backend = _backends[i];
                if (!_selectionPolicy.CanSave(backend))
                {
                    continue;
                }

                try
                {
                    await backend.SaveAsync(envelope, cancellationToken);
                }
                catch (Exception ex)
                {
                    EvoDebug.LogWarning($"Save failed in backend '{backend.BackendId}': {ex.Message}", nameof(SaveService));
                }
            }
        }

        public async UniTask<T> LoadPayloadAsync<T>(System.Threading.CancellationToken cancellationToken = default)
            where T : class
        {
            return await LoadPayloadAsync<T>(SaveEnvelope.GetDefaultPayloadKey<T>(), cancellationToken);
        }

        public async UniTask<T> LoadPayloadAsync<T>(string key, System.Threading.CancellationToken cancellationToken = default)
            where T : class
        {
            var envelope = await LoadLatestValidAsync(cancellationToken);
            return envelope != null && envelope.TryGetPayload<T>(key, out var payload)
                ? payload
                : default;
        }

        public async UniTask<string> LoadPayloadJsonAsync(
            string key,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var envelope = await LoadLatestValidAsync(cancellationToken);
            return envelope != null && envelope.TryGetPayloadJson(key, out var json)
                ? json
                : null;
        }

        public async UniTask SavePayloadAsync<T>(
            T payload,
            int version = 1,
            System.Threading.CancellationToken cancellationToken = default)
            where T : class
        {
            await SavePayloadAsync(SaveEnvelope.GetDefaultPayloadKey<T>(), payload, version, cancellationToken);
        }

        public async UniTask SavePayloadAsync<T>(
            string key,
            T payload,
            int version = 1,
            System.Threading.CancellationToken cancellationToken = default)
            where T : class
        {
            var envelope = await LoadLatestValidAsync(cancellationToken) ?? new SaveEnvelope();
            envelope.SetPayload(key, payload, version);
            await SaveAsync(envelope, cancellationToken);
        }

        public async UniTask SavePayloadJsonAsync(
            string key,
            string json,
            int version = 1,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var envelope = await LoadLatestValidAsync(cancellationToken) ?? new SaveEnvelope();
            envelope.SetPayloadJson(key, json, version);
            await SaveAsync(envelope, cancellationToken);
        }

        private static bool IsValid(SaveEnvelope envelope)
        {
            return envelope != null && envelope.schemaVersion > 0;
        }
    }
}
