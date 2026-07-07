namespace Evo.Infrastructure.Services.Ads.Adapters
{
    public sealed class CrazyGamesAdsAdapterFactory : IAdsAdapterFactory
    {
        private const string DEFAULT_ADAPTER_ID = "crazy";

        public string AdapterId => DEFAULT_ADAPTER_ID;

        public IAdsAdapter Create()
        {
            return new CrazyGamesAdsAdapter();
        }
    }
}
