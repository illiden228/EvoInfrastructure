namespace Evo.Infrastructure.Services.Purchases.UnityIap
{
    public sealed class UnityIapPurchaseAdapterFactory : IPurchaseAdapterFactory
    {
        public string AdapterId => UnityIapPurchaseAdapter.Id;

        public IPurchaseAdapter Create()
        {
            return new UnityIapPurchaseAdapter();
        }
    }
}
