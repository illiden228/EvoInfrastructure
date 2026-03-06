using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public abstract class SceneLifetimeScope<TParent> : LifetimeScope
    where TParent : LifetimeScope
{
    protected override LifetimeScope FindParent()
    {
        var parent = LifetimeScope.Find<TParent>();
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

public class SceneLifetimeScope : LifetimeScope
{
    protected override LifetimeScope FindParent()
    {
        var scopes = FindObjectsByType<LifetimeScope>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < scopes.Length; i++)
        {
            var scope = scopes[i];
            if (scope == null || ReferenceEquals(scope, this))
            {
                continue;
            }

            var typeName = scope.GetType().Name;
            if (typeName.IndexOf("ProjectLifetimeScope", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return scope;
            }
        }

        return VContainerSettings.Instance != null
            ? VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance()
            : null;
    }

    protected override void Configure(IContainerBuilder builder)
    {
    }
}
