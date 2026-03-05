using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using _Project.Scripts.Application.UI.Views;

namespace _Project.Scripts.Application.UI.Transitions
{
    [Serializable]
    public sealed class CanvasGroupFadeTransition : IUiTransition
    {
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private Ease ease = Ease.OutQuad;

        public async UniTask ShowAsync(UiViewBase view)
        {
            var group = GetOrCreateGroup(view);
            if (group == null)
            {
                return;
            }

            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = true;

            await Tween.Alpha(group, 1f, duration, ease);
        }

        public async UniTask HideAsync(UiViewBase view)
        {
            var group = GetOrCreateGroup(view);
            if (group == null)
            {
                return;
            }

            group.blocksRaycasts = false;
            group.interactable = false;

            await Tween.Alpha(group, 0f, duration, ease);
        }

        private static CanvasGroup GetOrCreateGroup(UiViewBase view)
        {
            if (view == null)
            {
                return null;
            }

            var group = view.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = view.gameObject.AddComponent<CanvasGroup>();
            }

            return group;
        }
    }
}
