namespace Evo.Infrastructure.Services.CrazyGames
{
    public readonly struct CrazyGamesUserInfo
    {
        public CrazyGamesUserInfo(string username)
        {
            Username = username ?? string.Empty;
        }

        public string Username { get; }
    }
}
