using NUnit.Framework;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases.RuStore.Tests
{
    public sealed class RuStorePurchaseAdapterConfigTests
    {
        [Test]
        public void NewConfig_UsesSafeOneStepDefaults()
        {
            var config = ScriptableObject.CreateInstance<RuStorePurchaseAdapterConfig>();
            try
            {
                Assert.That(config.Enabled, Is.True);
                Assert.That(config.PurchaseFlow, Is.EqualTo(RuStorePurchaseFlow.OneStep));
                Assert.That(config.PaymentTheme, Is.EqualTo(RuStorePaymentTheme.Light));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
