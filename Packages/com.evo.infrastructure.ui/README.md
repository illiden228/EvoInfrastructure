# Evo Infrastructure UI

UI entries use a stable `BindingId`. Runtime type names are not serialized and
the player does not resolve UI types through reflection. Register every binding
explicitly at composition-root startup:

```csharp
features.UseUi(uiSystemConfig, bindings => bindings
    .Add<MainMenuView, MainMenuViewModel>("MainMenuView")
    .Add<SettingsView, SettingsViewModel>("SettingsView"));
```

For assets created before 0.5.24, an empty `BindingId` falls back to the entry
`Id`; opening still requires a matching typed registration. Rebuilding views in
the config editor writes `BindingId = Id`. Scene views must be registered through
`IUiService.RegisterSceneView` from an explicit serialized scene reference; the
service does not search the scene hierarchy.

Pass context through `OpenAsync<TViewModel, TContext>` or
`UiOpenBuilder.WithContext<TContext>`. Untyped `UiOpenOptions.Context` is retained
only for source compatibility and is ignored with a warning.
