using System;
using R3;
using TMPro;
using UnityEngine.UI;

namespace _Project.Scripts.Application.UI
{
    public static class MVVMExtensions
    {
        public static void Bind<T>(this ReactiveProperty<T> source, Action<T> onNext, CompositeDisposable disposables)
        {
            if (source == null || onNext == null || disposables == null)
            {
                return;
            }

            source.Subscribe(onNext).AddTo(disposables);
        }

        public static void Bind(this ReactiveProperty<string> source, TMP_Text text, CompositeDisposable disposables)
        {
            if (source == null || text == null || disposables == null)
            {
                return;
            }

            text.text = source.Value ?? string.Empty;
            source.Subscribe(value => text.text = value ?? string.Empty).AddTo(disposables);
        }

        public static void Bind(this ReactiveProperty<float> source, TMP_Text text, CompositeDisposable disposables, string format = null)
        {
            if (source == null || text == null || disposables == null)
            {
                return;
            }

            text.text = FormatFloat(source.Value, format);
            source.Subscribe(value => text.text = FormatFloat(value, format)).AddTo(disposables);
        }

        public static void Bind(this ReactiveProperty<int> source, TMP_Text text, CompositeDisposable disposables, string format = null)
        {
            if (source == null || text == null || disposables == null)
            {
                return;
            }

            text.text = FormatInt(source.Value, format);
            source.Subscribe(value => text.text = FormatInt(value, format)).AddTo(disposables);
        }

        public static void Bind(this Button button, Action onClick, CompositeDisposable disposables)
        {
            if (button == null || disposables == null)
            {
                return;
            }

            button.OnClickAsObservable()
                .Subscribe(_ => onClick?.Invoke())
                .AddTo(disposables);
        }

        private static string FormatFloat(float value, string format)
        {
            return string.IsNullOrEmpty(format) ? value.ToString("0.###") : string.Format(format, value);
        }

        private static string FormatInt(int value, string format)
        {
            return string.IsNullOrEmpty(format) ? value.ToString() : string.Format(format, value);
        }

    }
}
