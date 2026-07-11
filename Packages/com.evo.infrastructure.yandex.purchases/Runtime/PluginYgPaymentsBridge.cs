using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Evo.Infrastructure.Services.Purchases.Yandex
{
    internal sealed class PluginYgPaymentsBridge : IYandexPaymentsBridge
    {
        private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly Type _yg2Type;
        private readonly List<EventSubscription> _subscriptions = new();

        public PluginYgPaymentsBridge()
        {
#if Payments_yg
            _yg2Type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("YG.YG2", false))
                .FirstOrDefault(type => type != null);

            if (_yg2Type == null)
            {
                return;
            }

            Subscribe("onGetPayments", (Action)(() => CatalogReceived?.Invoke()));
            Subscribe("onPurchaseSuccess", (Action<string>)(id => PurchaseSucceeded?.Invoke(id)));
            Subscribe("onPurchaseFailed", (Action<string>)(id => PurchaseFailed?.Invoke(id)));
#endif
        }

        public bool IsAvailable => _yg2Type != null;

        public IReadOnlyList<YandexStoreProduct> Products
        {
            get
            {
                if (_yg2Type == null || GetStaticValue(_yg2Type, "purchases") is not IEnumerable purchases)
                {
                    return Array.Empty<YandexStoreProduct>();
                }

                var result = new List<YandexStoreProduct>();
                foreach (var purchase in purchases)
                {
                    if (purchase == null)
                    {
                        continue;
                    }

                    var id = ReadString(purchase, "id");
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    result.Add(new YandexStoreProduct(
                        id,
                        ReadString(purchase, "title"),
                        ReadString(purchase, "description"),
                        ReadString(purchase, "imageURI"),
                        ReadString(purchase, "price"),
                        ReadDecimal(purchase, "priceValue"),
                        ReadString(purchase, "priceCurrencyCode"),
                        ReadBool(purchase, "consumed")));
                }

                return result;
            }
        }

        public event Action CatalogReceived;
        public event Action<string> PurchaseSucceeded;
        public event Action<string> PurchaseFailed;

        public void Buy(string storeProductId)
        {
            InvokeStatic("BuyPayments", new[] { typeof(string) }, storeProductId);
        }

        public bool Consume(string storeProductId)
        {
            InvokeStatic("ConsumePurchaseByID", new[] { typeof(string), typeof(bool) }, storeProductId, false);
            return true;
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Remove();
            }

            _subscriptions.Clear();
        }

        private void InvokeStatic(string methodName, Type[] parameterTypes, params object[] arguments)
        {
            if (_yg2Type == null)
            {
                throw new InvalidOperationException("PluginYG2 Payments is unavailable.");
            }

            var method = _yg2Type.GetMethod(methodName, StaticFlags, null, parameterTypes, null);
            if (method == null)
            {
                throw new MissingMethodException(_yg2Type.FullName, methodName);
            }

            method.Invoke(null, arguments);
        }

        private void Subscribe(string memberName, Delegate handler)
        {
            var eventInfo = _yg2Type.GetEvent(memberName, StaticFlags);
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(null, handler);
                _subscriptions.Add(new EventSubscription(() => eventInfo.RemoveEventHandler(null, handler)));
                return;
            }

            var field = _yg2Type.GetField(memberName, StaticFlags);
            if (field == null || !typeof(Delegate).IsAssignableFrom(field.FieldType))
            {
                return;
            }

            field.SetValue(null, Delegate.Combine(field.GetValue(null) as Delegate, handler));
            _subscriptions.Add(new EventSubscription(() =>
                field.SetValue(null, Delegate.Remove(field.GetValue(null) as Delegate, handler))));
        }

        private static object GetStaticValue(Type type, string name)
        {
            return type.GetField(name, StaticFlags)?.GetValue(null) ??
                   type.GetProperty(name, StaticFlags)?.GetValue(null);
        }

        private static object GetInstanceValue(object instance, string name)
        {
            var type = instance.GetType();
            return type.GetField(name, InstanceFlags)?.GetValue(instance) ??
                   type.GetProperty(name, InstanceFlags)?.GetValue(instance);
        }

        private static string ReadString(object instance, string name)
        {
            return Convert.ToString(GetInstanceValue(instance, name), CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static decimal ReadDecimal(object instance, string name)
        {
            var value = GetInstanceValue(instance, name);
            if (value is IConvertible convertible)
            {
                try
                {
                    return convertible.ToDecimal(CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    // Plugin versions may expose priceValue as a localized string.
                }
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0m;
        }

        private static bool ReadBool(object instance, string name)
        {
            var value = GetInstanceValue(instance, name);
            return value is bool boolean && boolean;
        }

    }
}
