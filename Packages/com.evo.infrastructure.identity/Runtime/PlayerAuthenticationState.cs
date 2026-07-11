namespace Evo.Infrastructure.Services.Identity
{
    public enum PlayerAuthenticationState
    {
        NotInitialized,
        Authenticating,
        Authenticated,
        Unauthenticated,
        Unavailable,
        Failed
    }
}
