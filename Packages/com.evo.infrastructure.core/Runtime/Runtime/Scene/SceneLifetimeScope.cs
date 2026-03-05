using _Project.Scripts.Runtime.Bootstrap;
using VContainer;
using VContainer.Unity;

public class SceneLifetimeScope : LifetimeScope
{
    protected override LifetimeScope FindParent()
    {
        var parent = LifetimeScope.Find<RuntimeProjectLifetimeScope>();
        if (parent != null)
        {
            return parent;
        }

        return VContainerSettings.Instance != null
            ? VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance()
            : null;
    }

    protected override void Configure(IContainerBuilder builder)
    {
    }
}
