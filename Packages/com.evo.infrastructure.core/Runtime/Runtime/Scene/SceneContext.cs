using _Project.Scripts.Infrastructure.Services.ScenePayload;

namespace _Project.Scripts.Application.Scene
{
    public interface ISceneContext
    {
        string SceneKey { get; }
        bool TryGetPayload<T>(out T payload);
        bool ConsumePayload<T>(out T payload);
    }

    public sealed class SceneContext : ISceneContext
    {
        private readonly SceneKey _sceneKey;
        private readonly IScenePayloadService _payloads;

        public string SceneKey => _sceneKey.Value;

        public SceneContext(SceneKey sceneKey, IScenePayloadService payloads)
        {
            _sceneKey = sceneKey;
            _payloads = payloads;
        }

        public bool TryGetPayload<T>(out T payload)
        {
            return _payloads.TryGetPayload(SceneKey, out payload);
        }

        public bool ConsumePayload<T>(out T payload)
        {
            return _payloads.ConsumePayload(SceneKey, out payload);
        }
    }

    public readonly struct SceneKey
    {
        public readonly string Value;

        public SceneKey(string value)
        {
            Value = value;
        }
    }
}
