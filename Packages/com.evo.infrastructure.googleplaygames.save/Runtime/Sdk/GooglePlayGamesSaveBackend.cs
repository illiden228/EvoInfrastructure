using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Core.Async;
using Evo.Infrastructure.Services.Debug;
using Evo.Infrastructure.Services.Save;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using UnityEngine;
using VContainer;

namespace Evo.Infrastructure.GooglePlayGames.Save
{
    public sealed class GooglePlayGamesSaveBackend : ISaveBackend
    {
        private const string Source = nameof(GooglePlayGamesSaveBackend);
        private readonly IGooglePlayGamesSession _session;
        private readonly GooglePlayGamesSaveOptions _options;
        private readonly Func<int, CancellationToken, UniTask> _delay;
        private readonly Action<string> _logWarning;
        private readonly AsyncGate _operationGate = new();
        private bool _timeoutLogged;
        private bool _malformedPayloadLogged;
        private bool _sdkExceptionLogged;

        [Inject]
        public GooglePlayGamesSaveBackend(IGooglePlayGamesSession session, GooglePlayGamesSaveOptions options)
            : this(
                session,
                options,
                (timeout, cancellationToken) => UniTask.Delay(timeout, cancellationToken: cancellationToken),
                message => EvoDebug.LogWarning(message, Source))
        {
        }

        internal GooglePlayGamesSaveBackend(
            IGooglePlayGamesSession session,
            GooglePlayGamesSaveOptions options,
            Func<int, CancellationToken, UniTask> delay,
            Action<string> logWarning)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            _logWarning = logWarning ?? throw new ArgumentNullException(nameof(logWarning));
        }

        public string BackendId => "google-play-games";
        public int Priority => _options.priority;
        public bool IsAvailable => _session.IsAuthenticated && !string.IsNullOrWhiteSpace(_options.slotName);

        public async UniTask<SaveEnvelope> LoadAsync(CancellationToken cancellationToken = default)
        {
            using var operationLease = await _operationGate.EnterAsync(cancellationToken);
            return await LoadInternalAsync(cancellationToken);
        }

        public async UniTask<bool> SaveAsync(SaveEnvelope envelope, CancellationToken cancellationToken = default)
        {
            using var operationLease = await _operationGate.EnterAsync(cancellationToken);
            return await SaveInternalAsync(envelope, cancellationToken);
        }

        private async UniTask<SaveEnvelope> LoadInternalAsync(CancellationToken cancellationToken)
        {
            if (!IsAvailable) return null;
            try
            {
                var metadata = await OpenAsync(cancellationToken);
                if (metadata == null) return null;
                var completion = new UniTaskCompletionSource<byte[]>();
                var active = true;
                PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(metadata, (status, bytes) =>
                {
                    if (active) completion.TrySetResult(status == SavedGameRequestStatus.Success ? bytes : null);
                });
                try
                {
                    var data = await AwaitWithTimeoutAsync(completion.Task, "read", cancellationToken);
                    if (data == null || data.Length == 0) return null;
                    var envelope = JsonUtility.FromJson<SaveEnvelope>(Encoding.UTF8.GetString(data));
                    if (envelope == null || envelope.schemaVersion <= 0) LogMalformedPayloadOnce();
                    return envelope?.schemaVersion > 0 ? envelope : null;
                }
                finally { active = false; }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                LogSdkExceptionOnce(exception, "load saved game");
                return null;
            }
        }

        private async UniTask<bool> SaveInternalAsync(SaveEnvelope envelope, CancellationToken cancellationToken)
        {
            if (!IsAvailable || envelope == null) return false;
            try
            {
                var metadata = await OpenAsync(cancellationToken);
                if (metadata == null) return false;
                var update = new SavedGameMetadataUpdate.Builder().WithUpdatedDescription("Evo save").Build();
                var completion = new UniTaskCompletionSource<bool>();
                var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope));
                var active = true;
                PlayGamesPlatform.Instance.SavedGame.CommitUpdate(metadata, update, bytes, (status, _) =>
                {
                    if (active) completion.TrySetResult(status == SavedGameRequestStatus.Success);
                });
                try { return await AwaitWithTimeoutAsync(completion.Task, "commit", cancellationToken); }
                finally { active = false; }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                LogSdkExceptionOnce(exception, "commit saved game");
                return false;
            }
        }

        private async UniTask<ISavedGameMetadata> OpenAsync(CancellationToken cancellationToken)
        {
            var completion = new UniTaskCompletionSource<ISavedGameMetadata>();
            var active = true;
            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(
                _options.slotName,
                DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseMostRecentlySaved,
                (status, metadata) =>
                {
                    if (active) completion.TrySetResult(status == SavedGameRequestStatus.Success ? metadata : null);
                });
            try { return await AwaitWithTimeoutAsync(completion.Task, "open", cancellationToken); }
            finally { active = false; }
        }

        internal async UniTask<T> AwaitWithTimeoutAsync<T>(UniTask<T> operation, string operationName, CancellationToken cancellationToken)
        {
            var timeout = Math.Max(1000, _options.operationTimeoutMs);
            var result = await UniTask.WhenAny(operation, _delay(timeout, cancellationToken));
            if (result.hasResultLeft) return result.result;
            if (!_timeoutLogged)
            {
                _timeoutLogged = true;
                _logWarning($"Saved Games {operationName} timed out after {timeout} ms.");
            }
            return default;
        }

        private void LogMalformedPayloadOnce(string details = null)
        {
            if (_malformedPayloadLogged) return;
            _malformedPayloadLogged = true;
            var suffix = string.IsNullOrWhiteSpace(details) ? string.Empty : $" Details: {details}";
            EvoDebug.LogWarning($"Saved Games returned an invalid Evo save payload.{suffix}", Source);
        }

        private void LogSdkExceptionOnce(Exception exception, string operation)
        {
            if (_sdkExceptionLogged) return;
            _sdkExceptionLogged = true;
            EvoDebug.LogWarning($"Google Play Games failed to {operation}: {exception.Message}", Source);
        }
    }
}
