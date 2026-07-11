namespace Evo.Infrastructure.Services.Identity
{
    public readonly struct PlayerAuthenticationResult
    {
        public PlayerAuthenticationResult(PlayerAuthenticationState state, PlayerIdentity identity, string error = "")
        {
            State = state;
            Identity = identity;
            Error = error ?? string.Empty;
        }

        public PlayerAuthenticationState State { get; }
        public PlayerIdentity Identity { get; }
        public string Error { get; }
        public bool IsSuccess => State == PlayerAuthenticationState.Authenticated;
    }
}
