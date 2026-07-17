using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Runtime.UI;
using Evo.Infrastructure.Runtime.UI.Transitions;
using Evo.Infrastructure.Runtime.UI.Views;
using Evo.Infrastructure.Services.Debug;
using Evo.Infrastructure.Services.ResourceProvider;
using Evo.Infrastructure.Services.SceneLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Evo.Infrastructure.Services.UI
{
    public sealed class UiService : IUiService, IDisposable
    {
        private sealed class LayerState
        {
            public readonly List<UiHandle> Stack = new();
            public readonly Stack<UiHandle> History = new();
            public readonly Queue<OpenRequest> Queue = new();
            public Transform Container;
            public bool ProcessingQueue;
        }

        private readonly struct OpenRequest
        {
            public readonly Type ViewModelType;
            public readonly string ViewId;
            public readonly UiOpenOptions Options;
            public readonly UniTaskCompletionSource<UiHandle> Tcs;
            public readonly UiViewBase View;

            public OpenRequest(Type viewModelType, string viewId, UiOpenOptions options, UniTaskCompletionSource<UiHandle> tcs)
            {
                ViewModelType = viewModelType;
                ViewId = viewId;
                Options = options;
                Tcs = tcs;
                View = null;
            }

            public OpenRequest(UiViewBase view, Type viewModelType, UiOpenOptions options, UniTaskCompletionSource<UiHandle> tcs)
            {
                View = view;
                ViewModelType = viewModelType;
                ViewId = view != null ? view.name : null;
                Options = options;
                Tcs = tcs;
            }
        }

        private readonly UiSystemConfig _config;
        private readonly UiBindingRegistry _bindings;
        private readonly IResourceProviderService _resources;
        private readonly IObjectResolver _resolver;
        private readonly ISceneLoaderService _sceneLoader;
        private readonly Dictionary<Type, List<UiViewEntry>> _entriesByViewModel = new();
        private readonly Dictionary<string, UiViewBase> _cachedViews = new();
        private readonly Dictionary<Type, UiViewBase> _sceneViewsByType = new();
        private readonly Dictionary<Type, UiViewEntry> _entryByViewType = new();
        private readonly Dictionary<UiLayer, LayerState> _layers = new();
        private GameObject _root;
        private bool _closingSceneBoundViews;

        public UiService(
            UiSystemConfig config,
            UiBindingRegistry bindings,
            IResourceProviderService resources,
            IObjectResolver resolver)
        {
            _config = config;
            _bindings = bindings;
            _resources = resources;
            _resolver = resolver;
            _sceneLoader = TryResolveSceneLoader(resolver);

            Reload();
            if (_sceneLoader != null)
            {
                _sceneLoader.SceneLoadStarted += OnSceneLoadStarted;
            }
        }

        public void Reload()
        {
            BuildCacheFromConfig();
            EnsureLayers();
        }

        public void Dispose()
        {
            if (_sceneLoader != null)
            {
                _sceneLoader.SceneLoadStarted -= OnSceneLoadStarted;
            }
        }

        public void RegisterSceneView(UiViewBase view)
        {
            if (view == null)
            {
                return;
            }

            var type = view.GetType();
            if (_sceneViewsByType.TryGetValue(type, out var existing) && existing == view)
            {
                return;
            }

            _sceneViewsByType[type] = view;
            EnsureEventSystem();
            EvoDebug.Log(
                $"Scene view registered: {view.GetType().Name}",
                nameof(UiService));
        }

        public UniTask<UiHandle> ShowAsync(UiViewBase view, UiOpenOptions options = null)
        {
            if (view == null)
            {
                return UniTask.FromResult<UiHandle>(null);
            }

            return OpenAsync(view, options);
        }

        public UniTask<UiHandle> OpenAsync(UiViewBase view, UiOpenOptions options = null)
        {
            if (view == null)
            {
                return UniTask.FromResult<UiHandle>(null);
            }

            var existing = FindHandle(view);
            if (existing != null)
            {
                return UniTask.FromResult(existing);
            }

            RegisterSceneView(view);
            var entry = _entryByViewType.TryGetValue(view.GetType(), out var configEntry) ? configEntry : null;
            var viewModelType = GetViewModelType(entry);
            if (viewModelType == null)
            {
                EvoDebug.LogWarning($"Missing ViewModel type for {view.GetType().Name}.", nameof(UiService));
                return UniTask.FromResult<UiHandle>(null);
            }

            var layer = options?.LayerOverride ?? entry?.Layer ?? UiLayer.Hud;
            var mode = options?.OpenModeOverride ?? entry?.OpenMode ?? UiOpenMode.Queue;
            var keepHistory = options != null ? options.KeepHistory : entry?.KeepHistory ?? false;
            var keepAlive = options?.KeepAliveOverride ?? entry?.KeepAlive ?? true;
            var keepAcrossSceneLoads = options?.KeepAcrossSceneLoadsOverride ?? entry?.KeepAcrossSceneLoads ?? false;

            if (mode == UiOpenMode.Queue)
            {
                var state = GetLayerState(layer);
                if (state != null && state.Stack.Count > 0)
                {
                    var tcs = new UniTaskCompletionSource<UiHandle>();
                    state.Queue.Enqueue(new OpenRequest(view, viewModelType, options, tcs));
                    return tcs.Task;
                }
            }

            return OpenInstanceInternal(view, viewModelType, layer, mode, keepAlive, keepHistory, keepAcrossSceneLoads, options?.Context, options?.ContextType, options?.ContextPayload, options?.ViewModelFactory);
        }

        public async UniTask HideAsync(UiViewBase view)
        {
            if (view == null)
            {
                return;
            }

            for (var i = 0; i < _layers.Count; i++)
            {
                var state = GetLayerState((UiLayer)i);
                if (state == null)
                {
                    continue;
                }

                for (var j = state.Stack.Count - 1; j >= 0; j--)
                {
                    var handle = state.Stack[j];
                    if (handle != null && handle.View == view)
                    {
                        await CloseAsync(handle);
                        return;
                    }
                }
            }
        }

        public UniTask<UiHandle> OpenAsync<TViewModel>(string viewId = null, UiOpenOptions options = null)
            where TViewModel : class, IUiViewModel
        {
            return OpenAsync(typeof(TViewModel), viewId, options);
        }

        public UiOpenBuilder<TViewModel> Open<TViewModel>(string viewId = null, UiOpenOptions options = null)
            where TViewModel : class, IUiViewModel
        {
            return new UiOpenBuilder<TViewModel>(this, viewId, options);
        }

        public UniTask<UiHandle> OpenAsync<TViewModel, TContext>(
            TContext context,
            string viewId = null,
            UiOpenOptions options = null)
            where TViewModel : class, IUiViewModel, IUiContextReceiver<TContext>
        {
            return OpenAsync<TViewModel>(viewId, WithContext(options, context));
        }

        public UniTask<UiHandle> OpenAsync(Type viewModelType, string viewId = null, UiOpenOptions options = null)
        {
            if (viewModelType == null)
            {
                EvoDebug.LogWarning($"Missing ViewModel type for view '{viewId ?? "unknown"}'.", nameof(UiService));
                return UniTask.FromResult<UiHandle>(null);
            }

            var entry = ResolveEntry(viewModelType, viewId);
            if (entry == null)
            {
                EvoDebug.LogWarning(
                    $"Missing config for {viewModelType.Name} ({viewId ?? "default"}).",
                    nameof(UiService));
                return UniTask.FromResult<UiHandle>(null);
            }

            var layer = options?.LayerOverride ?? entry.Layer;
            var mode = options?.OpenModeOverride ?? entry.OpenMode;
            var keepAlive = options?.KeepAliveOverride ?? entry.KeepAlive;
            var keepHistory = options != null ? options.KeepHistory : entry.KeepHistory;
            var keepAcrossSceneLoads = options?.KeepAcrossSceneLoadsOverride ?? entry.KeepAcrossSceneLoads;

            var state = GetLayerState(layer);
            if (state == null)
            {
                EvoDebug.LogWarning($"Missing layer container for {layer}.", nameof(UiService));
                return UniTask.FromResult<UiHandle>(null);
            }

            if (mode == UiOpenMode.Queue && state.Stack.Count > 0)
            {
                var tcs = new UniTaskCompletionSource<UiHandle>();
                state.Queue.Enqueue(new OpenRequest(viewModelType, viewId, options, tcs));
                return tcs.Task;
            }

            return OpenInternal(entry, viewId, layer, mode, keepAlive, keepHistory, keepAcrossSceneLoads, options?.Context, options?.ContextType, options?.ContextPayload, options?.ViewModelFactory);
        }

        internal async UniTask CloseAsync(UiHandle handle)
        {
            if (handle == null || handle.View == null)
            {
                return;
            }

            var state = GetLayerState(handle.Layer);
            if (state == null)
            {
                return;
            }

            if (!state.Stack.Remove(handle))
            {
                state.History.TryPop(out _);
            }

            await HideAndDispose(handle, true, false);
            handle.MarkClosed();

            if (state.Stack.Count == 0 && state.History.Count > 0)
            {
                var previous = state.History.Pop();
                await ShowFromHistory(previous);
                state.Stack.Add(previous);
            }

            if (state.Stack.Count == 0)
            {
                ProcessQueue(state).Forget();
            }
        }

        private async UniTask<UiHandle> OpenInternal(
            UiViewEntry entry,
            string viewId,
            UiLayer layer,
            UiOpenMode mode,
            bool keepAlive,
            bool keepHistory,
            bool keepAcrossSceneLoads,
            object context,
            Type contextType,
            IUiContextPayload contextPayload,
            Func<IUiViewModel> viewModelFactory)
        {
            var state = GetLayerState(layer);
            if (state == null)
            {
                return null;
            }

            UiHandle hidden = null;
            if (mode == UiOpenMode.Replace && state.Stack.Count > 0)
            {
                var current = state.Stack[state.Stack.Count - 1];
                if (keepHistory)
                {
                    await HideAndDispose(current, false, false);
                    state.Stack.RemoveAt(state.Stack.Count - 1);
                    state.History.Push(current);
                    hidden = current;
                }
                else
                {
                    await CloseAsync(current);
                }
            }

            var viewModelType = GetViewModelType(entry);
            var view = await GetViewInstance(entry, viewId, layer);
            if (view == null)
            {
                return null;
            }

            var viewModel = CreateViewModel(viewModelType, viewModelFactory);
            if (viewModel == null)
            {
                EvoDebug.LogWarning($"Failed to create ViewModel for {viewModelType?.Name}.", nameof(UiService));
                return null;
            }

            ApplyContext(viewModel, context, contextType, contextPayload);
            view.Bind(viewModel);
            viewModel.OnShow();

            var handle = new UiHandle(this, view, viewModel, layer, keepAlive || entry.IsSceneView, keepAcrossSceneLoads);
            state.Stack.Add(handle);

            await ShowView(view);

            if (hidden != null && hidden.View != null)
            {
                hidden.View.gameObject.SetActive(false);
            }

            return handle;
        }

        private async UniTask<UiHandle> OpenInstanceInternal(
            UiViewBase view,
            Type viewModelType,
            UiLayer layer,
            UiOpenMode mode,
            bool keepAlive,
            bool keepHistory,
            bool keepAcrossSceneLoads,
            object context,
            Type contextType,
            IUiContextPayload contextPayload,
            Func<IUiViewModel> viewModelFactory)
        {
            var state = GetLayerState(layer);
            if (state == null)
            {
                return null;
            }

            UiHandle hidden = null;
            if (mode == UiOpenMode.Replace && state.Stack.Count > 0)
            {
                var current = state.Stack[state.Stack.Count - 1];
                if (keepHistory)
                {
                    await HideAndDispose(current, false, false);
                    state.Stack.RemoveAt(state.Stack.Count - 1);
                    state.History.Push(current);
                    hidden = current;
                }
                else
                {
                    await CloseAsync(current);
                }
            }

            var viewModel = CreateViewModel(viewModelType, viewModelFactory);
            if (viewModel == null)
            {
                EvoDebug.LogWarning($"Failed to create ViewModel for {viewModelType?.Name}.", nameof(UiService));
                return null;
            }

            ApplyContext(viewModel, context, contextType, contextPayload);
            view.Bind(viewModel);
            viewModel.OnShow();

            var handle = new UiHandle(this, view, viewModel, layer, keepAlive || IsSceneView(view), keepAcrossSceneLoads);
            state.Stack.Add(handle);

            await ShowView(view);

            if (hidden != null && hidden.View != null)
            {
                hidden.View.gameObject.SetActive(false);
            }

            return handle;
        }

        private async UniTask<UiViewBase> GetViewInstance(UiViewEntry entry, string viewId, UiLayer layer)
        {
            if (entry.IsSceneView)
            {
                var viewType = GetViewType(entry);
                if (viewType != null && _sceneViewsByType.TryGetValue(viewType, out var sceneView))
                {
                    return sceneView;
                }

                EvoDebug.LogWarning($"Scene view missing for type '{viewType?.Name}'.", nameof(UiService));
                return null;
            }

            var cacheKey = GetCacheKey(entry, viewId);
            if (entry.KeepAlive && !string.IsNullOrEmpty(cacheKey) && _cachedViews.TryGetValue(cacheKey, out var cached))
            {
                cached.gameObject.SetActive(true);
                return cached;
            }

            if (_resources == null || entry.ViewPrefab == null)
            {
                EvoDebug.LogWarning(
                    $"Missing view prefab for {GetViewModelType(entry)?.Name}.",
                    nameof(UiService));
                return null;
            }

            var instance = await _resources.InstantiateAsync(entry.ViewPrefab);
            if (instance == null)
            {
                return null;
            }

            var view = instance.GetComponent<UiViewBase>();
            if (view == null)
            {
                EvoDebug.LogWarning("Instantiated prefab without UiViewBase.", nameof(UiService));
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            var container = GetLayerState(layer)?.Container;
            if (container != null)
            {
                view.transform.SetParent(container, false);
            }

            if (entry.KeepAlive && !string.IsNullOrEmpty(cacheKey))
            {
                _cachedViews[cacheKey] = view;
            }

            return view;
        }

        private async UniTask ShowView(UiViewBase view)
        {
            if (view == null)
            {
                return;
            }

            view.gameObject.SetActive(true);

            var transition = view.GetTransition();
            if (transition != null)
            {
                await transition.ShowAsync(view);
            }
        }

        private async UniTask HideAndDispose(UiHandle handle, bool disposeViewModel, bool forceDestroyView)
        {
            if (handle == null)
            {
                return;
            }

            var view = handle.View;
            if (view == null)
            {
                if (disposeViewModel)
                {
                    handle.ViewModel?.Dispose();
                }

                return;
            }

            var viewModel = handle.ViewModel;
            viewModel?.OnHide();

            if (!forceDestroyView)
            {
                var transition = view.GetTransition();
                if (transition != null)
                {
                    await transition.HideAsync(view);
                }

                if (view == null)
                {
                    if (disposeViewModel)
                    {
                        viewModel?.Dispose();
                    }

                    return;
                }
            }

            if ((handle.KeepAlive && !forceDestroyView) || !IsServiceOwnedView(view))
            {
                if (view == null)
                {
                    if (disposeViewModel)
                    {
                        viewModel?.Dispose();
                    }

                    return;
                }

                view.gameObject.SetActive(false);
            }
            else
            {
                RemoveCachedView(view);
                if (view == null)
                {
                    if (disposeViewModel)
                    {
                        viewModel?.Dispose();
                    }

                    return;
                }

                UnityEngine.Object.Destroy(view.gameObject);
            }

            if (disposeViewModel)
            {
                if (view != null)
                {
                    view.Unbind();
                }

                viewModel?.Dispose();
            }
        }

        private async UniTask ShowFromHistory(UiHandle handle)
        {
            if (handle == null || handle.View == null)
            {
                return;
            }

            handle.View.gameObject.SetActive(true);
            handle.ViewModel?.OnShow();
            await ShowView(handle.View);
        }

        private async UniTaskVoid ProcessQueue(LayerState state)
        {
            if (state == null || state.ProcessingQueue)
            {
                return;
            }

            state.ProcessingQueue = true;
            while (state.Queue.Count > 0 && state.Stack.Count == 0)
            {
                var request = state.Queue.Dequeue();
                var entry = ResolveEntry(request.ViewModelType, request.ViewId);
                if (entry == null && request.View == null)
                {
                    request.Tcs.TrySetResult(null);
                    continue;
                }

                var options = request.Options ?? new UiOpenOptions();
                UiHandle handle;
                if (request.View != null)
                {
                    var viewModelType = request.ViewModelType ?? GetViewModelTypeByView(request.View.GetType());
                    if (viewModelType == null)
                    {
                        request.Tcs.TrySetResult(null);
                        continue;
                    }

                    var layer = options.LayerOverride ?? UiLayer.Hud;
                    var mode = options.OpenModeOverride ?? UiOpenMode.Queue;
                    var keepHistory = options.KeepHistory;
                    var keepAlive = options.KeepAliveOverride ?? true;
                    var keepAcrossSceneLoads = options.KeepAcrossSceneLoadsOverride ?? false;
                    handle = await OpenInstanceInternal(request.View, viewModelType, layer, mode, keepAlive, keepHistory, keepAcrossSceneLoads, options.Context, options.ContextType, options.ContextPayload, options.ViewModelFactory);
                }
                else
                {
                    var layer = options.LayerOverride ?? entry.Layer;
                    var mode = options.OpenModeOverride ?? entry.OpenMode;
                    var keepAlive = options.KeepAliveOverride ?? entry.KeepAlive;
                    var keepHistory = options.KeepHistory || entry.KeepHistory;
                    var keepAcrossSceneLoads = options.KeepAcrossSceneLoadsOverride ?? entry.KeepAcrossSceneLoads;
                    handle = await OpenInternal(entry, request.ViewId, layer, mode, keepAlive, keepHistory, keepAcrossSceneLoads, options.Context, options.ContextType, options.ContextPayload, options.ViewModelFactory);
                }
                request.Tcs.TrySetResult(handle);
            }

            state.ProcessingQueue = false;
        }

        private void OnSceneLoadStarted(SceneLoadInfo info)
        {
            CloseSceneBoundViewsAsync().Forget();
        }

        private async UniTaskVoid CloseSceneBoundViewsAsync()
        {
            if (_closingSceneBoundViews)
            {
                return;
            }

            _closingSceneBoundViews = true;
            try
            {
                foreach (var pair in _layers)
                {
                    await CloseSceneBoundViews(pair.Value, false);
                }

                RemoveDestroyedSceneViews();
            }
            finally
            {
                _closingSceneBoundViews = false;
            }
        }

        private async UniTask CloseSceneBoundViews(LayerState state, bool processQueueAfter)
        {
            if (state == null)
            {
                return;
            }

            ClearSceneBoundQueue(state);

            for (var i = state.Stack.Count - 1; i >= 0; i--)
            {
                var handle = state.Stack[i];
                if (handle == null)
                {
                    continue;
                }

                if (ShouldKeepHandleAcrossSceneChange(handle))
                {
                    continue;
                }

                state.Stack.RemoveAt(i);
                await CloseSceneBoundHandle(handle);
            }

            if (state.History.Count == 0)
            {
                if (processQueueAfter && state.Stack.Count == 0)
                {
                    ProcessQueue(state).Forget();
                }

                return;
            }

            var keptHistory = new Stack<UiHandle>();
            while (state.History.Count > 0)
            {
                var handle = state.History.Pop();
                if (handle == null)
                {
                    continue;
                }

                if (ShouldKeepHandleAcrossSceneChange(handle))
                {
                    keptHistory.Push(handle);
                    continue;
                }

                await CloseSceneBoundHandle(handle);
            }

            while (keptHistory.Count > 0)
            {
                state.History.Push(keptHistory.Pop());
            }

            if (processQueueAfter && state.Stack.Count == 0)
            {
                ProcessQueue(state).Forget();
            }
        }

        private async UniTask CloseSceneBoundHandle(UiHandle handle)
        {
            try
            {
                await HideAndDispose(handle, true, true);
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Failed to close scene-bound UI '{handle?.ViewModel?.GetType().Name ?? "unknown"}'. {ex.GetType().Name}: {ex.Message}",
                    nameof(UiService));
            }
            finally
            {
                handle?.MarkClosed();
            }
        }

        private static bool ShouldKeepHandleAcrossSceneChange(UiHandle handle)
        {
            if (handle == null || handle.View == null)
            {
                return false;
            }

            return handle.KeepAcrossSceneLoads;
        }

        private void ClearSceneBoundQueue(LayerState state)
        {
            if (state.Queue.Count == 0)
            {
                return;
            }

            var keptRequests = new Queue<OpenRequest>();
            while (state.Queue.Count > 0)
            {
                var request = state.Queue.Dequeue();
                if (ShouldKeepQueuedRequestAcrossSceneLoads(request))
                {
                    keptRequests.Enqueue(request);
                    continue;
                }

                request.Tcs.TrySetResult(null);
            }

            while (keptRequests.Count > 0)
            {
                state.Queue.Enqueue(keptRequests.Dequeue());
            }
        }

        private bool ShouldKeepQueuedRequestAcrossSceneLoads(OpenRequest request)
        {
            if (request.Options?.KeepAcrossSceneLoadsOverride.HasValue == true)
            {
                return request.Options.KeepAcrossSceneLoadsOverride.Value;
            }

            UiViewEntry entry = null;
            if (request.View != null)
            {
                _entryByViewType.TryGetValue(request.View.GetType(), out entry);
            }
            else
            {
                entry = ResolveEntry(request.ViewModelType, request.ViewId);
            }

            return entry?.KeepAcrossSceneLoads ?? false;
        }

        private UiViewEntry ResolveEntry(Type viewModelType, string viewId)
        {
            if (viewModelType == null)
            {
                return null;
            }

            if (!_entriesByViewModel.TryGetValue(viewModelType, out var list) || list.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(viewId))
            {
                for (var i = 0; i < list.Count; i++)
                {
                    if (string.Equals(list[i].Id, viewId, StringComparison.Ordinal))
                    {
                        return list[i];
                    }
                }
            }

            return list[0];
        }

        private Type GetViewModelTypeByView(Type viewType)
        {
            if (viewType == null)
            {
                return null;
            }

            return _entryByViewType.TryGetValue(viewType, out var entry)
                ? GetViewModelType(entry)
                : null;
        }

        private void BuildCacheFromConfig()
        {
            _entriesByViewModel.Clear();
            _entryByViewType.Clear();
            if (_config == null || _config.Views == null)
            {
                return;
            }

            for (var i = 0; i < _config.Views.Count; i++)
            {
                var entry = _config.Views[i];
                if (entry == null)
                {
                    continue;
                }

                var type = GetViewModelType(entry);
                if (type == null)
                {
                    continue;
                }

                if (!_entriesByViewModel.TryGetValue(type, out var list))
                {
                    list = new List<UiViewEntry>();
                    _entriesByViewModel[type] = list;
                }

                list.Add(entry);

                var viewType = GetViewType(entry);
                if (viewType != null && !_entryByViewType.ContainsKey(viewType))
                {
                    _entryByViewType[viewType] = entry;
                }
            }
        }

        private void EnsureLayers()
        {
            if (_root == null)
            {
                _root = new GameObject("UiRoot");
                UnityEngine.Object.DontDestroyOnLoad(_root);
            }

            if (_layers.Count > 0)
            {
                return;
            }

            var sharedCanvasScaler = _config != null ? _config.SharedCanvasScaler : null;
            var sharedCanvas = CreateCanvas(_root.transform, "UiCanvas", 0, sharedCanvasScaler);
            var layers = _config != null ? _config.Layers : null;

            if (layers != null && layers.Count > 0)
            {
                for (var i = 0; i < layers.Count; i++)
                {
                    var definition = layers[i];
                    var layer = (UiLayer)i;
                    var requestedOrder = definition?.SortingOrder ?? 0;
                    var buildMode = definition?.BuildMode ?? UiLayerBuildMode.SharedCanvas;
                    var useOwnCanvas = buildMode == UiLayerBuildMode.OwnCanvas ||
                        (buildMode == UiLayerBuildMode.SharedCanvas && requestedOrder != 0);
                    if (buildMode == UiLayerBuildMode.SharedCanvas && requestedOrder != 0)
                    {
                        EvoDebug.LogWarning(
                            $"Layer '{definition?.Name ?? layer.ToString()}' requested SortingOrder={requestedOrder} " +
                            "but uses SharedCanvas. Forcing OwnCanvas.",
                            nameof(UiService));
                    }
                    var container = CreateLayerContainer(
                        definition?.Name ?? layer.ToString(),
                        useOwnCanvas
                            ? CreateCanvas(
                                _root.transform,
                                $"{layer}Canvas",
                                requestedOrder,
                                ResolveCanvasScalerSettings(definition, sharedCanvasScaler))
                            : sharedCanvas,
                        requestedOrder != 0 ? requestedOrder : i);
                    _layers[layer] = new LayerState { Container = container };
                }
            }
            else
            {
                foreach (UiLayer layer in Enum.GetValues(typeof(UiLayer)))
                {
                    var container = CreateLayerContainer(layer.ToString(), sharedCanvas, (int)layer);
                    _layers[layer] = new LayerState { Container = container };
                }
            }

            EnsureEventSystem();
        }

        private LayerState GetLayerState(UiLayer layer)
        {
            _layers.TryGetValue(layer, out var state);
            return state;
        }

        private IUiViewModel CreateViewModel(Type viewModelType, Func<IUiViewModel> factory)
        {
            if (factory != null)
            {
                try
                {
                    return factory();
                }
                catch (Exception ex)
                {
                    EvoDebug.LogWarning(
                        $"ViewModel factory for '{viewModelType?.Name}' failed. {ex.Message}",
                        nameof(UiService));
                    return null;
                }
            }

            if (_resolver == null || viewModelType == null)
            {
                return null;
            }

            try
            {
                return _resolver.Resolve(viewModelType) as IUiViewModel;
            }
            catch (Exception ex)
            {
                EvoDebug.LogWarning(
                    $"Failed to resolve ViewModel '{viewModelType.Name}'. {ex.Message}",
                    nameof(UiService));
                return null;
            }
        }

        private static ISceneLoaderService TryResolveSceneLoader(IObjectResolver resolver)
        {
            if (resolver == null)
            {
                return null;
            }

            try
            {
                return resolver.TryResolve(typeof(ISceneLoaderService), out var resolved)
                    ? resolved as ISceneLoaderService
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static void ApplyContext(IUiViewModel viewModel, object context, Type explicitContextType, IUiContextPayload contextPayload)
        {
            if (viewModel == null)
            {
                return;
            }

            if (contextPayload != null)
            {
                contextPayload.Apply(viewModel);
                return;
            }

            if (context != null || explicitContextType != null)
            {
                EvoDebug.LogWarning(
                    "Untyped UI context is no longer supported. Use OpenAsync<TViewModel, TContext> or UiOpenBuilder.WithContext<TContext>.",
                    nameof(UiService));
            }
        }

        private static UiOpenOptions WithContext<TContext>(UiOpenOptions options, TContext context)
        {
            return new UiOpenOptions
            {
                LayerOverride = options?.LayerOverride,
                OpenModeOverride = options?.OpenModeOverride,
                KeepAliveOverride = options?.KeepAliveOverride,
                KeepHistory = options?.KeepHistory ?? false,
                KeepAcrossSceneLoadsOverride = options?.KeepAcrossSceneLoadsOverride,
                ViewModelFactory = options?.ViewModelFactory,
                ContextType = typeof(TContext),
                ContextPayload = new UiContextPayload<TContext>(context)
            };
        }

        private static string GetCacheKey(UiViewEntry entry, string viewId)
        {
            if (entry == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(viewId))
            {
                return $"{entry.EffectiveBindingId}:{viewId}";
            }

            return $"{entry.EffectiveBindingId}:{entry.Id}";
        }

        private static Transform CreateCanvas(Transform parent, string name, int sortingOrder = 0)
        {
            return CreateCanvas(parent, name, sortingOrder, null);
        }

        private static Transform CreateCanvas(
            Transform parent,
            string name,
            int sortingOrder,
            UiCanvasScalerSettings scalerSettings)
        {
            var canvasObject = new GameObject(name);
            canvasObject.transform.SetParent(parent, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.overrideSorting = true;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            ApplyCanvasScaler(scaler, scalerSettings);
            canvasObject.AddComponent<GraphicRaycaster>();

            return canvasObject.transform;
        }

        private static UiCanvasScalerSettings ResolveCanvasScalerSettings(
            UiLayerDefinition definition,
            UiCanvasScalerSettings fallback)
        {
            if (definition != null && definition.OverrideCanvasScaler && definition.CanvasScaler != null)
            {
                return definition.CanvasScaler;
            }

            return fallback;
        }

        private static void ApplyCanvasScaler(CanvasScaler scaler, UiCanvasScalerSettings settings)
        {
            if (scaler == null)
            {
                return;
            }

            if (settings == null)
            {
                return;
            }

            scaler.uiScaleMode = settings.UiScaleMode;
            scaler.referenceResolution = settings.ReferenceResolution;
            scaler.screenMatchMode = settings.ScreenMatchMode;
            scaler.matchWidthOrHeight = Mathf.Clamp01(settings.MatchWidthOrHeight);
            scaler.scaleFactor = settings.ScaleFactor <= 0f ? 1f : settings.ScaleFactor;
            scaler.referencePixelsPerUnit = settings.ReferencePixelsPerUnit <= 0f ? 100f : settings.ReferencePixelsPerUnit;
        }

        private static Transform CreateLayerContainer(string name, Transform parent, int order)
        {
            var layerObject = new GameObject($"{name}");
            layerObject.transform.SetParent(parent, false);
            var rect = layerObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            return layerObject.transform;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            UnityEngine.Object.DontDestroyOnLoad(eventSystem);
        }

        private UiHandle FindHandle(UiViewBase view)
        {
            foreach (var state in _layers.Values)
            {
                for (var i = 0; i < state.Stack.Count; i++)
                {
                    var handle = state.Stack[i];
                    if (handle != null && handle.View == view)
                    {
                        return handle;
                    }
                }
            }

            return null;
        }

        private void RemoveCachedView(UiViewBase view)
        {
            if (view == null || _cachedViews.Count == 0)
            {
                return;
            }

            var keysToRemove = new List<string>();
            foreach (var pair in _cachedViews)
            {
                if (pair.Value == view)
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            for (var i = 0; i < keysToRemove.Count; i++)
            {
                _cachedViews.Remove(keysToRemove[i]);
            }
        }

        private void RemoveDestroyedSceneViews()
        {
            if (_sceneViewsByType.Count == 0)
            {
                return;
            }

            var keysToRemove = new List<Type>();
            foreach (var pair in _sceneViewsByType)
            {
                if (pair.Value == null)
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            for (var i = 0; i < keysToRemove.Count; i++)
            {
                _sceneViewsByType.Remove(keysToRemove[i]);
            }
        }

        private bool IsSceneView(UiViewBase view)
        {
            if (view == null)
            {
                return false;
            }

            return _sceneViewsByType.TryGetValue(view.GetType(), out var registered) && registered == view;
        }

        private bool IsServiceOwnedView(UiViewBase view)
        {
            return view != null &&
                   _root != null &&
                   view.transform.IsChildOf(_root.transform);
        }

        private Type GetViewModelType(UiViewEntry entry)
        {
            return TryGetBinding(entry, out var binding) ? binding.ViewModelType : null;
        }

        private Type GetViewType(UiViewEntry entry)
        {
            return TryGetBinding(entry, out var binding) ? binding.ViewType : null;
        }

        private bool TryGetBinding(UiViewEntry entry, out UiBinding binding)
        {
            binding = null;
            if (entry == null || _bindings == null)
            {
                return false;
            }

            if (_bindings.TryGet(entry.EffectiveBindingId, out binding))
            {
                return true;
            }

            EvoDebug.LogWarning(
                $"UI binding '{entry.EffectiveBindingId}' is not registered for view entry '{entry.Id}'.",
                nameof(UiService));
            return false;
        }
    }
}
