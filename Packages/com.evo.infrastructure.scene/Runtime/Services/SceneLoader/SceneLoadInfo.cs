using UnityEngine.SceneManagement;

namespace Evo.Infrastructure.Services.SceneLoader
{
    public readonly struct SceneLoadInfo
    {
        public readonly string SceneKey;
        public readonly LoadSceneMode Mode;
        public readonly bool ActivateOnLoad;
        public readonly int Priority;
        public readonly SceneLoadSource Source;
        public readonly bool IsReload;

        public SceneLoadInfo(
            string sceneKey,
            LoadSceneMode mode,
            bool activateOnLoad,
            int priority,
            SceneLoadSource source,
            bool isReload)
        {
            SceneKey = sceneKey;
            Mode = mode;
            ActivateOnLoad = activateOnLoad;
            Priority = priority;
            Source = source;
            IsReload = isReload;
        }
    }
}
