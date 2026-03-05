namespace _Project.Scripts.Infrastructure.Services.ScenePayload
{
    public interface IScenePayloadService
    {
        void SetPayload(string sceneKey, object payload);
        bool TryGetPayload<T>(string sceneKey, out T payload);
        bool ConsumePayload<T>(string sceneKey, out T payload);
        void ClearPayload(string sceneKey);
    }
}
