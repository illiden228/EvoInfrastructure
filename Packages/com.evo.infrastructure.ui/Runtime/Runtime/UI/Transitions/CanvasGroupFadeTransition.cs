using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using Evo.Infrastructure.Runtime.UI.Views;
using UnityEngine.Scripting.APIUpdating;

namespace Evo.Infrastructure.Runtime.UI.Transitions
{
    [Serializable]
    [MovedFrom(false, null, "Evo.Infrastructure.Runtime", null)]
    public sealed class CanvasGroupFadeTransition : IUiTransition
    {
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private Ease ease = Ease.OutQuad;
        [SerializeField] private bool useUnscaledTime;

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

            await Tween.Alpha(group, 1f, duration, ease, useUnscaledTime: useUnscaledTime);
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

            await Tween.Alpha(group, 0f, duration, ease, useUnscaledTime: useUnscaledTime);
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
