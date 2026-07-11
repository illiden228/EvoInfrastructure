using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Identity;
using GooglePlayGames;

namespace Evo.Infrastructure.GooglePlayGames.Identity
{
    public sealed class GooglePlayGamesIdentityProvider : IPlayerIdentityProvider
    {
        private readonly IGooglePlayGamesSession _session;
        public GooglePlayGamesIdentityProvider(IGooglePlayGamesSession session) => _session = session;
        public string ProviderId => "google-play-games";
        public int Priority => 100;
        public bool IsAvailable => _session.State != GooglePlayGamesAuthenticationState.Unavailable;
        public PlayerAuthenticationState State => MapState(_session.State);
        public PlayerIdentity Current => CreateIdentity();

        public async UniTask<PlayerAuthenticationResult> AuthenticateAsync(PlayerAuthenticationMode mode, CancellationToken cancellationToken = default)
        {
            var success = await _session.AuthenticateAsync(mode == PlayerAuthenticationMode.Interactive, cancellationToken);
            return success
                ? new PlayerAuthenticationResult(PlayerAuthenticationState.Authenticated, CreateIdentity())
                : new PlayerAuthenticationResult(MapState(_session.State), PlayerIdentity.Empty, "Google Play Games authentication failed.");
        }

        private static PlayerIdentity CreateIdentity()
        {
            var user = PlayGamesPlatform.Instance.localUser;
            return user != null && user.authenticated
                ? new PlayerIdentity("google-play-games", user.id, user.userName, string.Empty)
                : PlayerIdentity.Empty;
        }

        private static PlayerAuthenticationState MapState(GooglePlayGamesAuthenticationState state)
        {
            return state switch
            {
                GooglePlayGamesAuthenticationState.Authenticating => PlayerAuthenticationState.Authenticating,
                GooglePlayGamesAuthenticationState.Authenticated => PlayerAuthenticationState.Authenticated,
                GooglePlayGamesAuthenticationState.Unavailable => PlayerAuthenticationState.Unavailable,
                GooglePlayGamesAuthenticationState.Failed => PlayerAuthenticationState.Failed,
                GooglePlayGamesAuthenticationState.NotStarted => PlayerAuthenticationState.NotInitialized,
                _ => PlayerAuthenticationState.Unauthenticated
            };
        }
    }
}
