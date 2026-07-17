# Evo Infrastructure Loading

The loading pipeline applies bounded execution by default: 45 seconds per step,
180 seconds per operation, 15 seconds for loading presentation and 30 seconds
for transition scene operations. A failed scene transition is retried once after
cleanup, and concurrent transitions are serialized.

```csharp
features.UseLoading(
    new SceneTransitionOptions
    {
        AwaitLoadingPresentationBeforeSceneLoad = true
    },
    new LoadingExecutionOptions
    {
        StepTimeoutSeconds = 45f,
        OperationTimeoutSeconds = 180f,
        PresentationTimeoutSeconds = 15f,
        TransitionTimeoutSeconds = 30f,
        IgnoreTimeoutWhenApplicationNotFocused = true,
        OperationRetryCount = 1,
        RetryDelaySeconds = 0.75f,
        StepOrderMode = LoadingStepOrderMode.Registration
    },
    new StartupLoadingOptions
    {
        Enabled = true
    });
```

Application startup loading should use `IApplicationStartupLoadingPipeline`
instead of manually running bootstrap steps and then calling
`ISceneLoadingPipeline.LoadSceneAsync`. The startup pipeline shows the loading
presentation before bootstrap steps when
`AwaitLoadingPresentationBeforeSceneLoad` is enabled, sends `Ready`, `Started`
and `Finished` exactly once, and runs bootstrap steps plus
`ISceneLoadingPipeline.CreateSteps(startupScene, LoadSceneMode.Single)` as one
continuous loading plan.

```csharp
public sealed class RuntimeEntryPoint : IAsyncStartable
{
    private readonly IReadOnlyList<ILoadingStep> _bootstrapSteps;
    private readonly IApplicationStartupLoadingPipeline _startupLoading;
    private readonly AssetReference _startupScene;

    public UniTask StartAsync(CancellationToken cancellationToken)
    {
        return _startupLoading.LoadStartupAsync(
            _bootstrapSteps,
            _startupScene,
            LoadSceneMode.Single,
            cancellationToken: cancellationToken);
    }
}
```

Regular scene transitions after startup keep using
`ISceneLoadingPipeline.LoadSceneAsync`; that method owns a separate lifecycle for
post-start scene changes.

Projects that keep the transition-scene name in a project-owned config register
an explicit `ITransitionSceneProvider`. The loading package does not scan project
assemblies or reflect project config properties:

```csharp
builder.Register<ITransitionSceneProvider, ProjectTransitionSceneProvider>(Lifetime.Singleton);
```

`LoadSceneMode.Single` reloads of the currently active Addressables scene use its
stable scene identity and unload the old instance before loading the replacement.
When no transition scene is configured, the pipeline creates a temporary empty
holding scene so duplicate target instances are never loaded at the same time.

Loading steps must observe the supplied cancellation token. Timeout failures are
reported as `TimeoutException` with the active step or phase in the message.
Timeouts are driven by the Unity PlayerLoop, so timeout cancellation and the
pipeline continuation run on the Unity main thread. By default, elapsed time is
not accumulated while the application is unfocused and
`Application.runInBackground` is disabled. Set
`IgnoreTimeoutWhenApplicationNotFocused` to `false` to use wall-clock timeout
behavior; applications that enable `Application.runInBackground` also keep
counting time while unfocused.

Steps that need their own deadline implement `ILoadingStepTimeout` next to their
existing order and weight metadata:

```csharp
public sealed class LoadGameplayStep : ILoadingStep, ILoadingStepTimeout
{
    public int Order => 10;
    public float Weight => 1f;
    public float TimeoutSeconds => 30f;
    // Execute(...)
}
```

A positive value overrides the global fallback, zero uses `StepTimeoutSeconds`,
and a negative value disables the individual timeout. No new options field is
required when a new step is added.

`OrderProperty` remains the default ordering mode for compatibility. New projects
should select `Registration`; both project and gameplay steps then execute in the
same order in which VContainer registered them. The exact resolved plan is logged
through EvoDebug before execution.
