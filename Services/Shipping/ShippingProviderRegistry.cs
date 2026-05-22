namespace WebApplication1.Services.Shipping
{
    public interface IShippingProviderRegistry
    {
        /// <summary>All registered providers, configured or not (for admin views).</summary>
        IReadOnlyList<IShippingProvider> AllProviders { get; }

        /// <summary>Providers that can ship to <paramref name="destinationCountry"/> AND have credentials.</summary>
        IEnumerable<IShippingProvider> Available(string destinationCountry);

        /// <summary>
        /// Fetch rate quotes from every available provider for the destination,
        /// merged into one list. Failures from one provider don't block the others.
        /// </summary>
        Task<List<ShippingQuote>> GetAllQuotesAsync(ShipmentRequest request);

        /// <summary>Look up a provider by its short key (e.g. "tcg", "dhl-express").</summary>
        IShippingProvider? Get(string providerKey);
    }

    public class ShippingProviderRegistry : IShippingProviderRegistry
    {
        private readonly IReadOnlyList<IShippingProvider> _providers;
        private readonly ILogger<ShippingProviderRegistry> _logger;

        public ShippingProviderRegistry(
            IEnumerable<IShippingProvider> providers,
            ILogger<ShippingProviderRegistry> logger)
        {
            _providers = providers.ToList();
            _logger = logger;
        }

        public IReadOnlyList<IShippingProvider> AllProviders => _providers;

        public IEnumerable<IShippingProvider> Available(string destinationCountry) =>
            _providers.Where(p => p.IsConfigured && p.SupportsDestination(destinationCountry));

        public async Task<List<ShippingQuote>> GetAllQuotesAsync(ShipmentRequest request)
        {
            var providers = Available(request.Destination.CountryCode).ToList();
            if (providers.Count == 0)
            {
                _logger.LogInformation("No configured shipping providers for destination {Country}.",
                    request.Destination.CountryCode);
                return new();
            }

            // Run in parallel — each carrier's GetQuotes is an HTTP round trip.
            var tasks = providers.Select(async p =>
            {
                try { return await p.GetQuotesAsync(request); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Provider {Key} threw fetching quotes — skipping.", p.ProviderKey);
                    return new List<ShippingQuote>();
                }
            });

            var resultsByProvider = await Task.WhenAll(tasks);
            return resultsByProvider.SelectMany(x => x)
                .OrderBy(q => q.Rate)
                .ToList();
        }

        public IShippingProvider? Get(string providerKey) =>
            _providers.FirstOrDefault(p =>
                string.Equals(p.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase));
    }
}
