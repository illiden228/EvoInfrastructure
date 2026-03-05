using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _Project.Scripts.Infrastructure.Services.Debug;

namespace _Project.Scripts.Infrastructure.Services.Save
{
    public sealed class SaveService : ISaveService
    {
        private const string YANDEX_BACKEND_ID = "yandex";
        private readonly List<ISaveBackend> _backends;

        public SaveService(IReadOnlyList<ISaveBackend> backends)
        {
            _backends = backends != null ? new List<ISaveBackend>(backends) : new List<ISaveBackend>();
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
            var activeBackends = GetActiveBackends();

            for (var i = 0; i < activeBackends.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var backend = activeBackends[i];
                if (backend == null)
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
            var activeBackends = GetActiveBackends();
            for (var i = 0; i < activeBackends.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var backend = activeBackends[i];
                if (backend == null)
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

        private static bool IsValid(SaveEnvelope envelope)
        {
            return envelope != null && envelope.schemaVersion > 0 && envelope.profile != null;
        }

        private List<ISaveBackend> GetActiveBackends()
        {
            var active = new List<ISaveBackend>(_backends.Count);
            var hasYandex = false;

            for (var i = 0; i < _backends.Count; i++)
            {
                var backend = _backends[i];
                if (backend == null || !backend.IsAvailable)
                {
                    continue;
                }

                if (string.Equals(backend.BackendId, YANDEX_BACKEND_ID, StringComparison.OrdinalIgnoreCase))
                {
                    hasYandex = true;
                    break;
                }
            }

            for (var i = 0; i < _backends.Count; i++)
            {
                var backend = _backends[i];
                if (backend == null || !backend.IsAvailable)
                {
                    continue;
                }

                if (hasYandex &&
                    !string.Equals(backend.BackendId, YANDEX_BACKEND_ID, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                active.Add(backend);
            }

            return active;
        }
    }
}
