using System;

namespace Evo.Infrastructure.Services.Purchases.Yandex
{
    internal sealed class EventSubscription
    {
        private Action _remove;

        public EventSubscription(Action remove)
        {
            _remove = remove;
        }

        public void Remove()
        {
            _remove?.Invoke();
            _remove = null;
        }
    }
}
