namespace Evo.Infrastructure.Runtime.Loading
{
    public sealed class SceneTransitionOptions
    {
        public bool AwaitLoadingPresentationBeforeSceneLoad = true;
        public bool HideLoadingPresentationAfterLoadingFinished;
        public string TransitionSceneName;
    }
}
