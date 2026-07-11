namespace Evo.Infrastructure.Services.CrazyGames
{
    public readonly struct CrazyGamesUserInfo
    {
        public CrazyGamesUserInfo(string username, string avatarUrl)
        {
            Username = username ?? string.Empty;
            AvatarUrl = avatarUrl ?? string.Empty;
        }

        public string Username { get; }
        public string AvatarUrl { get; }
    }
}
