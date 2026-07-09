using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Runtime.UI;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Evo.Infrastructure.Runtime.UI.Lists
{
    public abstract class BindableListView<TItemViewModel, TItemView> : MonoBehaviour, IUiUnbindable, IDisposable
        where TItemView : Component, IUiBindable<TItemViewModel>
    {
        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<TItemView> _views = new();
        private readonly List<TItemViewModel> _items = new();
        private IUiItemViewFactory<TItemView> _factory;
        private bool _isDisposed;
        private int _version;

        [SerializeField] private Transform _content;
        [SerializeField] private bool _hideContentWhileBinding;

        protected virtual Transform Content => _content != null ? _content : transform;
        protected virtual bool HideContentWhileBinding => _hideContentWhileBinding;

        public Transform ContentTransform => Content;
        public IReadOnlyList<TItemViewModel> Items => _items;
        public IReadOnlyList<TItemView> Views => _views;
        public int Count => _items.Count;

        public void Bind(ObservableList<TItemViewModel> items)
        {
            BindAsync(items).Forget();
        }

        public async UniTask BindAsync(ObservableList<TItemViewModel> items)
        {
            Unbind();

            if (_isDisposed || items == null)
            {
                return;
            }

            EnsureFactory();
            if (_factory == null)
            {
                return;
            }

            var version = ++_version;
            await SetItemsAsync(items, () => version == _version);

            if (_isDisposed || version != _version)
            {
                return;
            }

            items.ObserveAdd()
                .Subscribe(addEvent => AddAsync(addEvent.Index, addEvent.Value, version).Forget())
                .AddTo(_subscriptions);

            items.ObserveRemove()
                .Subscribe(removeEvent => RemoveAt(removeEvent.Index))
                .AddTo(_subscriptions);

            items.ObserveReplace()
                .Subscribe(replaceEvent => ReplaceAsync(replaceEvent.Index, replaceEvent.NewValue, version).Forget())
                .AddTo(_subscriptions);

            items.ObserveMove()
                .Subscribe(moveEvent => Move(moveEvent.OldIndex, moveEvent.NewIndex))
                .AddTo(_subscriptions);

            items.ObserveReset()
                .Subscribe(_ => SetItemsAsync(items, () => version == _version).Forget())
                .AddTo(_subscriptions);
        }

        public UniTask SetItemsAsync(IReadOnlyList<TItemViewModel> items)
        {
            return SetItemsAsync(items, null);
        }

        public async UniTask SetItemsAsync(IReadOnlyList<TItemViewModel> items, Func<bool> isCurrent)
        {
            var operationVersion = _version;
            if (_isDisposed)
            {
                DisposeItems(items);
                return;
            }

            EnsureFactory();
            if (Content == null || _factory == null)
            {
                DisposeItems(items);
                return;
            }

            if (items == null || items.Count == 0)
            {
                Clear();
                return;
            }

            var contentObject = Content.gameObject;
            var contentWasActive = contentObject.activeSelf;
            if (HideContentWhileBinding)
            {
                contentObject.SetActive(false);
            }

            try
            {
                if (IsStale(isCurrent, operationVersion))
                {
                    DisposeItems(items);
                    return;
                }

                TrimViews(items.Count);

                var existingCount = _views.Count;
                for (var i = 0; i < existingCount; i++)
                {
                    if (IsStale(isCurrent, operationVersion))
                    {
                        DisposeItems(items, i);
                        return;
                    }

                    ReplaceAt(i, items[i], false);
                }

                if (existingCount >= items.Count)
                {
                    OnListChanged();
                    return;
                }

                var createTasks = new UniTask<TItemView>[items.Count - existingCount];
                for (var i = 0; i < createTasks.Length; i++)
                {
                    createTasks[i] = _factory.CreateAsync(Content, existingCount + i);
                }

                var newViews = await UniTask.WhenAll(createTasks);
                if (IsStale(isCurrent, operationVersion))
                {
                    ReleaseViews(newViews);
                    DisposeItems(items, existingCount);
                    return;
                }

                for (var i = 0; i < newViews.Length; i++)
                {
                    AddCreatedView(existingCount + i, newViews[i], items[existingCount + i]);
                }

                OnListChanged();
            }
            finally
            {
                if (!_isDisposed && Content != null && HideContentWhileBinding)
                {
                    contentObject.SetActive(contentWasActive);
                }
            }
        }

        public void Clear()
        {
            for (var i = _views.Count - 1; i >= 0; i--)
            {
                ReleaseAt(i);
            }

            _views.Clear();
            _items.Clear();
            OnListChanged();
        }

        public void Unbind()
        {
            _version++;
            _subscriptions.Clear();
            Clear();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Unbind();
            _subscriptions.Dispose();
            if (_factory is IDisposable disposableFactory)
            {
                disposableFactory.Dispose();
            }

            _factory = null;
        }

        protected abstract IUiItemViewFactory<TItemView> CreateFactory();

        protected virtual void OnListChanged()
        {
        }

        protected virtual void OnItemViewBound(TItemView view, TItemViewModel viewModel, int index, bool isNew)
        {
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        private void EnsureFactory()
        {
            _factory ??= CreateFactory();
        }

        private async UniTask AddAsync(int index, TItemViewModel item, int version)
        {
            if (_isDisposed || version != _version || Content == null)
            {
                DisposeItem(item);
                return;
            }

            EnsureFactory();
            if (_factory == null)
            {
                DisposeItem(item);
                return;
            }

            var normalizedIndex = Mathf.Clamp(index, 0, _views.Count);
            var view = await _factory.CreateAsync(Content, normalizedIndex);
            if (_isDisposed || version != _version)
            {
                ReleaseView(view);
                DisposeItem(item);
                return;
            }

            AddCreatedView(normalizedIndex, view, item);
            OnListChanged();
        }

        private async UniTask ReplaceAsync(int index, TItemViewModel item, int version)
        {
            if (index >= 0 && index < _views.Count)
            {
                ReplaceAt(index, item, false);
                OnListChanged();
                return;
            }

            await AddAsync(index, item, version);
        }

        private void AddCreatedView(int index, TItemView view, TItemViewModel item)
        {
            if (view == null)
            {
                DisposeItem(item);
                return;
            }

            var normalizedIndex = Mathf.Clamp(index, 0, _views.Count);
            view.transform.SetParent(Content, false);
            view.transform.SetSiblingIndex(normalizedIndex);
            view.Bind(item);

            _views.Insert(normalizedIndex, view);
            _items.Insert(normalizedIndex, item);
            OnItemViewBound(view, item, normalizedIndex, true);
        }

        private void ReplaceAt(int index, TItemViewModel item, bool isNew)
        {
            if (index < 0 || index >= _views.Count)
            {
                DisposeItem(item);
                return;
            }

            DisposeItem(_items[index]);
            _items[index] = item;
            _views[index].Bind(item);
            OnItemViewBound(_views[index], item, index, isNew);
        }

        private void RemoveAt(int index)
        {
            if (index < 0 || index >= _views.Count)
            {
                return;
            }

            ReleaseAt(index);
            OnListChanged();
        }

        private void Move(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= _views.Count)
            {
                return;
            }

            var normalizedNewIndex = Mathf.Clamp(newIndex, 0, _views.Count - 1);
            var view = _views[oldIndex];
            var item = _items[oldIndex];

            _views.RemoveAt(oldIndex);
            _items.RemoveAt(oldIndex);
            _views.Insert(normalizedNewIndex, view);
            _items.Insert(normalizedNewIndex, item);
            view.transform.SetSiblingIndex(normalizedNewIndex);
            OnListChanged();
        }

        private void TrimViews(int targetCount)
        {
            for (var i = _views.Count - 1; i >= targetCount; i--)
            {
                ReleaseAt(i);
            }
        }

        private void ReleaseAt(int index)
        {
            if (index < 0 || index >= _views.Count)
            {
                return;
            }

            var view = _views[index];
            ReleaseView(view);
            _views.RemoveAt(index);

            if (index < _items.Count)
            {
                DisposeItem(_items[index]);
                _items.RemoveAt(index);
            }
        }

        private void ReleaseView(TItemView view)
        {
            if (view == null)
            {
                return;
            }

            if (view is IUiUnbindable unbindable)
            {
                unbindable.Unbind();
            }

            _factory?.Release(view);
        }

        private void ReleaseViews(IReadOnlyList<TItemView> views)
        {
            if (views == null)
            {
                return;
            }

            for (var i = 0; i < views.Count; i++)
            {
                ReleaseView(views[i]);
            }
        }

        private bool IsStale(Func<bool> isCurrent, int operationVersion)
        {
            return _isDisposed || operationVersion != _version || isCurrent != null && !isCurrent();
        }

        private static void DisposeItems(IReadOnlyList<TItemViewModel> items, int startIndex = 0)
        {
            if (items == null)
            {
                return;
            }

            for (var i = Math.Max(0, startIndex); i < items.Count; i++)
            {
                DisposeItem(items[i]);
            }
        }

        private static void DisposeItem(TItemViewModel item)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
