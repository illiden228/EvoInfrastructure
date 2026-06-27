namespace Evo.Infrastructure.Services.Audio
{
    public readonly struct AudioLayerKey : System.IEquatable<AudioLayerKey>
    {
        public AudioLayerKey(string id)
        {
            Id = string.IsNullOrWhiteSpace(id) ? AudioLayers.Default.Id : id.Trim();
        }

        public string Id { get; }

        public bool Equals(AudioLayerKey other)
        {
            return string.Equals(GetSafeId(Id), GetSafeId(other.Id), System.StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is AudioLayerKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return GetSafeId(Id).GetHashCode();
        }

        public override string ToString()
        {
            return GetSafeId(Id);
        }

        public static bool operator ==(AudioLayerKey left, AudioLayerKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AudioLayerKey left, AudioLayerKey right)
        {
            return !left.Equals(right);
        }

        public static implicit operator AudioLayerKey(string id)
        {
            return new AudioLayerKey(id);
        }

        private static string GetSafeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? AudioLayers.Default.Id : id;
        }
    }

    public static class AudioLayers
    {
        public static readonly AudioLayerKey Default = new("default");
        public static readonly AudioLayerKey Background = new("background");
        public static readonly AudioLayerKey Effects = new("effects");
        public static readonly AudioLayerKey UiEffects = new("ui_effects");
    }

    public enum AudioLayer
    {
        Background = 0,
        Effects = 1,
        UiEffects = 2
    }
}
