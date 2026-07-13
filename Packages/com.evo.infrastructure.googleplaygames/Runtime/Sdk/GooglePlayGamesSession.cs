using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Evo.Infrastructure.Services.Debug;
using VContainer;
using VContainer.Unity;

namespace Evo.Infrastructure.GooglePlayGames
{
    public sealed class GooglePlayGamesSession : IGooglePlayGamesSession, IAsyncStartable
    {
        private const string Source = nameof(GooglePlayGamesSession);
        private readonly GooglePlayGamesOptions _options;
        private readonly Action<bool, Action<SignInStatus>> _authenticate;
        private readonly Func<int, CancellationToken, UniTask> _delay;
        private readonly Action<string> _logWarning;
        private UniTask<bool> _authenticationTask;
        private bool _authenticationInFlight;
        private int _requestGeneration;
        private bool _timeoutLogged;

        [Inject]
        public GooglePlayGamesSession(GooglePlayGamesOptions options)
            : this(
                options,
                (interactive, callback) =>
                {
                    if (interactive)
                    {
                        PlayGamesPlatform.Instance.ManuallyAuthenticate(callback);
                    }
                    else
                    {
                        PlayGamesPlatform.Instance.Authenticate(callback);
                    }
                },
                (timeout, cancellationToken) => UniTask.Delay(timeout, cancellationToken: cancellationToken),
                message => EvoDebug.LogWarning(message, Source))
        {
        }

        internal GooglePlayGamesSession(
            GooglePlayGamesOptions options,
            Action<bool, Action<SignInStatus>> authenticate,
            Func<int, CancellationToken, UniTask> delay,
            Action<string> logWarning)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _authenticate = authenticate ?? throw new ArgumentNullException(nameof(authenticate));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            _logWarning = logWarning ?? throw new ArgumentNullException(nameof(logWarning));
        }
        public GooglePlayGamesAuthenticationState State { get; private set; }
        public bool IsInitialized => State != GooglePlayGamesAuthenticationState.NotStarted && State != GooglePlayGamesAuthenticationState.Authenticating;
        public bool IsAuthenticated => State == GooglePlayGamesAuthenticationState.Authenticated;

        public UniTask<bool> AuthenticateAsync(bool interactive = false, CancellationToken cancellationToken = default)
        {
            if (!_authenticationInFlight)
            {
                _authenticationInFlight = true;
                _authenticationTask = AuthenticateCoreAsync(interactive, cancellationToken).Preserve();
                return _authenticationTask;
            }

            return _authenticationTask.AttachExternalCancellation(cancellationToken);
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            await AuthenticateAsync(false, cancellationToken);
        }

        private async UniTask<bool> AuthenticateCoreAsync(bool interactive, CancellationToken operationCancellationToken)
        {
            try
            {
                if (IsAuthenticated) return true;
                State = GooglePlayGamesAuthenticationState.Authenticating;
                var completion = new UniTaskCompletionSource<SignInStatus>();
                var generation = ++_requestGeneration;
                void Complete(SignInStatus status)
                {
                    if (generation == _requestGeneration) completion.TrySetResult(status);
                }
                _authenticate(interactive, Complete);
                var timeout = Math.Max(1000, _options.authenticationTimeoutMs);
                var winner = await UniTask.WhenAny(completion.Task, _delay(timeout, operationCancellationToken));
                var status = winner.hasResultLeft ? winner.result : SignInStatus.Canceled;
                if (!winner.hasResultLeft)
                {
                    ++_requestGeneration;
                    if (!_timeoutLogged)
                    {
                        _timeoutLogged = true;
                        _logWarning($"Authentication timed out after {timeout} ms.");
                    }
                }
                State = status == SignInStatus.Success
                    ? GooglePlayGamesAuthenticationState.Authenticated
                    : GooglePlayGamesAuthenticationState.Unauthenticated;
            }
            catch (OperationCanceledException)
            {
                ++_requestGeneration;
                State = GooglePlayGamesAuthenticationState.Unauthenticated;
                throw;
            }
            catch { State = GooglePlayGamesAuthenticationState.Failed; }
            finally { _authenticationInFlight = false; }
            return IsAuthenticated;
        }
    }
}
