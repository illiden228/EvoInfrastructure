namespace Evo.Infrastructure.Services.Purchases.Yandex
{
    internal readonly struct YandexStoreProduct
    {
        public YandexStoreProduct(
            string id,
            string title,
            string description,
            string imageUrl,
            string localizedPrice,
            decimal price,
            string currencyCode,
            bool consumed)
        {
            Id = id;
            Title = title;
            Description = description;
            ImageUrl = imageUrl;
            LocalizedPrice = localizedPrice;
            Price = price;
            CurrencyCode = currencyCode;
            Consumed = consumed;
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string ImageUrl { get; }
        public string LocalizedPrice { get; }
        public decimal Price { get; }
        public string CurrencyCode { get; }
        public bool Consumed { get; }
    }
}
