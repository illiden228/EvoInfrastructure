using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Evo.Infrastructure.Services.Debug;
using VContainer.Unity;

namespace Evo.Infrastructure.GooglePlayGames
{
    public sealed class GooglePlayGamesSession : IGooglePlayGamesSession, IAsyncStartable
    {
        private const string Source = nameof(GooglePlayGamesSession);
        private readonly SemaphoreSlim _authenticationLock = new(1, 1);
        private readonly GooglePlayGamesOptions _options;
        private int _requestGeneration;
        private bool _timeoutLogged;
        public GooglePlayGamesSession(GooglePlayGamesOptions options) => _options = options;
        public GooglePlayGamesAuthenticationState State { get; private set; }
        public bool IsInitialized => State != GooglePlayGamesAuthenticationState.NotStarted && State != GooglePlayGamesAuthenticationState.Authenticating;
        public bool IsAuthenticated => State == GooglePlayGamesAuthenticationState.Authenticated;

        public UniTask<bool> AuthenticateAsync(bool interactive = false, CancellationToken cancellationToken = default)
        {
            return AuthenticateSerializedAsync(interactive, cancellationToken);
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            await AuthenticateAsync(false, cancellationToken);
        }

        private async UniTask<bool> AuthenticateSerializedAsync(bool interactive, CancellationToken cancellationToken)
        {
            await _authenticationLock.WaitAsync(cancellationToken);
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
                if (interactive)
                    PlayGamesPlatform.Instance.ManuallyAuthenticate(Complete);
                else
                    PlayGamesPlatform.Instance.Authenticate(Complete);
                var timeout = Math.Max(1000, _options.authenticationTimeoutMs);
                var winner = await UniTask.WhenAny(completion.Task, UniTask.Delay(timeout, cancellationToken: cancellationToken));
                var status = winner.winArgumentIndex == 0 ? winner.result : SignInStatus.Canceled;
                if (winner.winArgumentIndex != 0)
                {
                    ++_requestGeneration;
                    if (!_timeoutLogged)
                    {
                        _timeoutLogged = true;
                        EvoDebug.LogWarning($"Authentication timed out after {timeout} ms.", Source);
                    }
                }
                State = status == SignInStatus.Success
                    ? GooglePlayGamesAuthenticationState.Authenticated
                    : GooglePlayGamesAuthenticationState.Unauthenticated;
            }
            catch (OperationCanceledException) { State = GooglePlayGamesAuthenticationState.Unauthenticated; throw; }
            catch { State = GooglePlayGamesAuthenticationState.Failed; }
            finally { _authenticationLock.Release(); }
            return IsAuthenticated;
        }
    }
}
