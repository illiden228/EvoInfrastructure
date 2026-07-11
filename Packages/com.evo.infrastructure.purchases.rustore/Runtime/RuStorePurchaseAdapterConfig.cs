using Evo.Infrastructure.Services.Config;
using UnityEngine;

namespace Evo.Infrastructure.Services.Purchases.RuStore
{
    [GameConfig("Purchases")]
    [CreateAssetMenu(fileName = "RuStorePurchaseAdapterConfig", menuName = "Project/Purchases/RuStore Config")]
    public sealed class RuStorePurchaseAdapterConfig : ScriptableObject, IGameConfig
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private RuStorePurchaseFlow purchaseFlow = RuStorePurchaseFlow.OneStep;
        [SerializeField] private RuStorePaymentTheme paymentTheme = RuStorePaymentTheme.Light;

        public bool Enabled => enabled;
        public RuStorePurchaseFlow PurchaseFlow => purchaseFlow;
        public RuStorePaymentTheme PaymentTheme => paymentTheme;
    }
}
