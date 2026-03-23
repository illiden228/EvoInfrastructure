using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _Project.Scripts.Application.UI;
using _Project.Scripts.Application.UI.Transitions;
using _Project.Scripts.Application.UI.Views;
using _Project.Scripts.Infrastructure.Services.Debug;
using _Project.Scripts.Infrastructure.Services.ResourceProvider;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace _Project.Scripts.Infrastructure.Services.UI
{
    public sealed class UiService : IUiService
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
        private readonly IResourceProviderService _resources;
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<Type, List<UiViewEntry>> _entriesByViewModel = new();
        private readonly Dictionary<string, UiViewBase> _cachedViews = new();
        private readonly Dictionary<Type, UiViewBase> _sceneViewsByType = new();
        private readonly Dictionary<Type, UiViewEntry> _entryByViewType = new();
        private readonly Dictionary<UiLayer, LayerState> _layers = new();
        private GameObject _root;

        public UiService(UiSystemConfig config, IResourceProviderService resources, IObjectResolver resolver)
        {
            _config = config;
            _resources = resources;
            _resolver = resolver;

            Reload();
        }

        public void Reload()
        {
            BuildCacheFromConfig();
            EnsureLayers();
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
            var viewModelType = entry?.GetViewModelType() ?? GetViewModelTypeFromView(view.GetType());
            if (viewModelType == null)
            {
                EvoDebug.LogWarning($"Missing ViewModel type for {view.GetType().Name}.", nameof(UiService));
                return UniTask.FromResult<UiHandle>(null);
            }

            var layer = options?.LayerOverride ?? entry?.Layer ?? UiLayer.Hud;
            var mode = options?.OpenModeOverride ?? entry?.OpenMode ?? UiOpenMode.Queue;
            var keepHistory = options != null ? options.KeepHistory : entry?.KeepHistory ?? false;
            var keepAlive = options?.KeepAliveOverride ?? entry?.KeepAlive ?? true;

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

            return OpenInstanceInternal(view, viewModelType, layer, mode, keepAlive, keepHistory);
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

            if (entry.IsSceneView)
            {
                TryAutoRegisterSceneView(entry);
            }

            var layer = options?.LayerOverride ?? entry.Layer;
            var mode = options?.OpenModeOverride ?? entry.OpenMode;
            var keepAlive = options?.KeepAliveOverride ?? entry.KeepAlive;
            var keepHistory = options != null ? options.KeepHistory : entry.KeepHistory;

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

            return OpenInternal(entry, viewId, layer, mode, keepAlive, keepHistory);
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

            await HideAndDispose(handle, true);
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
            bool keepHistory)
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
                    await HideAndDispose(current, false);
                    state.Stack.RemoveAt(state.Stack.Count - 1);
                    state.History.Push(current);
                    hidden = current;
                }
                else
                {
                    await CloseAsync(current);
                }
            }

            var viewModelType = entry.GetViewModelType();
            var view = await GetViewInstance(entry, viewId, layer);
            if (view == null)
            {
                return null;
            }

            var viewModel = CreateViewModel(viewModelType);
            if (viewModel == null)
            {
                EvoDebug.LogWarning($"Failed to create ViewModel for {viewModelType?.Name}.", nameof(UiService));
                return null;
            }

            view.Bind(viewModel);
            viewModel.OnShow();

            var handle = new UiHandle(this, view, viewModel, layer, keepAlive || entry.IsSceneView);
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
            bool keepHistory)
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
                    await HideAndDispose(current, false);
                    state.Stack.RemoveAt(state.Stack.Count - 1);
                    state.History.Push(current);
                    hidden = current;
                }
                else
                {
                    await CloseAsync(current);
                }
            }

            var viewModel = CreateViewModel(viewModelType);
            if (viewModel == null)
            {
                EvoDebug.LogWarning($"Failed to create ViewModel for {viewModelType?.Name}.", nameof(UiService));
                return null;
            }

            view.Bind(viewModel);
            viewModel.OnShow();

            var handle = new UiHandle(this, view, viewModel, layer, keepAlive || IsSceneView(view));
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
                var viewType = entry.GetViewType();
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
                    $"Missing view prefab for {entry.GetViewModelType()?.Name}.",
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

        private async UniTask HideAndDispose(UiHandle handle, bool disposeViewModel)
        {
            if (handle == null || handle.View == null)
            {
                return;
            }

            var view = handle.View;
            var viewModel = handle.ViewModel;
            viewModel?.OnHide();

            var transition = view.GetTransition();
            if (transition != null)
            {
                await transition.HideAsync(view);
            }

            if (handle.KeepAlive)
            {
                view.gameObject.SetActive(false);
            }
            else
            {
                UnityEngine.Object.Destroy(view.gameObject);
            }

            if (disposeViewModel)
            {
                view.Unbind();
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
                    var viewModelType = request.ViewModelType ?? GetViewModelTypeFromView(request.View.GetType());
                    if (viewModelType == null)
                    {
                        request.Tcs.TrySetResult(null);
                        continue;
                    }

                    var layer = options.LayerOverride ?? UiLayer.Hud;
                    var mode = options.OpenModeOverride ?? UiOpenMode.Queue;
                    var keepHistory = options.KeepHistory;
                    var keepAlive = options.KeepAliveOverride ?? true;
                    handle = await OpenInstanceInternal(request.View, viewModelType, layer, mode, keepAlive, keepHistory);
                }
                else
                {
                    var layer = options.LayerOverride ?? entry.Layer;
                    var mode = options.OpenModeOverride ?? entry.OpenMode;
                    var keepAlive = options.KeepAliveOverride ?? entry.KeepAlive;
                    var keepHistory = options.KeepHistory || entry.KeepHistory;
                    handle = await OpenInternal(entry, request.ViewId, layer, mode, keepAlive, keepHistory);
                }
                request.Tcs.TrySetResult(handle);
            }

            state.ProcessingQueue = false;
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
                ? entry.GetViewModelType()
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

                var type = entry.GetViewModelType();
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

                var viewType = entry.GetViewType();
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

        private IUiViewModel CreateViewModel(Type viewModelType)
        {
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

        private static string GetCacheKey(UiViewEntry entry, string viewId)
        {
            if (entry == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(viewId))
            {
                return $"{entry.ViewModelTypeName}:{viewId}";
            }

            return $"{entry.ViewModelTypeName}:{entry.Id}";
        }

        private void TryAutoRegisterSceneView(UiViewEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            var viewType = entry.GetViewType();
            if (viewType == null)
            {
                EvoDebug.LogWarning("Scene view entry has no ViewType.", nameof(UiService));
                return;
            }

            if (_sceneViewsByType.ContainsKey(viewType))
            {
                return;
            }

            var view = FindSceneView(viewType);
            if (view == null)
            {
                EvoDebug.LogWarning(
                    $"Scene view not found for type '{viewType.Name}'.",
                    nameof(UiService));
                return;
            }

            RegisterSceneView(view);
        }

        private static UiViewBase FindSceneView(Type viewType)
        {
#if UNITY_2022_2_OR_NEWER
            var found = UnityEngine.Object.FindObjectsByType(viewType, FindObjectsInactive.Include, FindObjectsSortMode.None);
            return GetRootSceneView(found);
#else
            var objects = UnityEngine.Object.FindObjectsOfType(viewType, true);
            return GetRootSceneView(objects);
#endif
        }

        private static UiViewBase GetRootSceneView(UnityEngine.Object[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] is not UiViewBase view)
                {
                    continue;
                }

                var parentView = view.transform.parent != null
                    ? view.transform.parent.GetComponentInParent<UiViewBase>()
                    : null;
                if (parentView == null)
                {
                    return view;
                }
            }

            return objects[0] as UiViewBase;
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

        private bool IsSceneView(UiViewBase view)
        {
            if (view == null)
            {
                return false;
            }

            return _sceneViewsByType.TryGetValue(view.GetType(), out var registered) && registered == view;
        }

        private static Type GetViewModelTypeFromView(Type viewType)
        {
            while (viewType != null)
            {
                if (viewType.IsGenericType && viewType.GetGenericTypeDefinition() == typeof(UiView<>))
                {
                    var args = viewType.GetGenericArguments();
                    if (args.Length == 1)
                    {
                        return args[0];
                    }
                }

                viewType = viewType.BaseType;
            }

            return null;
        }
    }
}
