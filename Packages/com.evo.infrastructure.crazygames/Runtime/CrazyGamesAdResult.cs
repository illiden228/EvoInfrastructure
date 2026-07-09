namespace Evo.Infrastructure.Services.CrazyGames
{
    public readonly struct CrazyGamesAdResult
    {
        public CrazyGamesAdResult(bool shown, string error)
        {
            Shown = shown;
            Error = error;
        }

        public bool Shown { get; }
        public string Error { get; }
    }
}
