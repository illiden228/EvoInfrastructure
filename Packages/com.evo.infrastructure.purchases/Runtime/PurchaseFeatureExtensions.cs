using Evo.Infrastructure.DI;
using VContainer;

namespace Evo.Infrastructure.Services.Purchases
{
    public static class PurchaseFeatureExtensions
    {
        public static EvoFeatureRegistry UsePurchases(this EvoFeatureRegistry features,
            PurchaseServiceOptions options = null)
        {
            features.Builder.RegisterInstance(options ?? new PurchaseServiceOptions());
            features.Builder.Register<PurchaseService>(Lifetime.Singleton).As<IPurchaseService>();
            return features;
        }
    }
}
