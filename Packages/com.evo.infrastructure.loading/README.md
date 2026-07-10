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
        OperationRetryCount = 1,
        RetryDelaySeconds = 0.75f,
        StepOrderMode = LoadingStepOrderMode.Registration
    });
```

Loading steps must observe the supplied cancellation token. Timeout failures are
reported as `TimeoutException` with the active step or phase in the message.

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
