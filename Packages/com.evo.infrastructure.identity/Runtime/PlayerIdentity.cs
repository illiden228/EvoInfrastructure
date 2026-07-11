namespace Evo.Infrastructure.Services.Identity
{
    public readonly struct PlayerIdentity
    {
        public static PlayerIdentity Empty => new(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);

        public PlayerIdentity(string providerId, string playerId, string displayName, string avatarUrl)
        {
            ProviderId = providerId ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            AvatarUrl = avatarUrl ?? string.Empty;
        }

        public string ProviderId { get; }
        public string PlayerId { get; }
        public string DisplayName { get; }
        public string AvatarUrl { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerId);
    }
}
