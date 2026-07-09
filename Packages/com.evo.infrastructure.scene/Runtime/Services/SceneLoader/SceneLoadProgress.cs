namespace Evo.Infrastructure.Services.SceneLoader
{
    public readonly struct SceneLoadProgress
    {
        public readonly SceneLoadInfo Info;
        public readonly float Progress;

        public SceneLoadProgress(SceneLoadInfo info, float progress)
        {
            Info = info;
            Progress = progress;
        }
    }
}
