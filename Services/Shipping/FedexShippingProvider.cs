namespace WebApplication1.Services.Shipping
{
    /// <summary>
    /// FedEx International — stub.
    /// When the client provides FedEx Developer Portal credentials (API key,
    /// API secret, account number tied to a meter), fill in the live calls.
    ///
    /// Live API docs: https://developer.fedex.com/api/en-us/catalog.html
    /// Auth: OAuth2 client_credentials — POST /oauth/token, then Bearer.
    /// </summary>
    public class FedexShippingProvider : IShippingProvider
    {
        private readonly IConfiguration _config;
        private readonly ILogger<FedexShippingProvider> _logger;

        public FedexShippingProvider(IConfiguration config, ILogger<FedexShippingProvider> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string ProviderKey => "fedex";
        public string DisplayName => "FedEx";

        public bool SupportsDestination(string destinationCountryCode) =>
            !string.IsNullOrWhiteSpace(destinationCountryCode)
            && !destinationCountryCode.Equals("ZA", StringComparison.OrdinalIgnoreCase);

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_config["Shipping:Fedex:ApiKey"])
            && !string.IsNullOrWhiteSpace(_config["Shipping:Fedex:ApiSecret"])
            && !string.IsNullOrWhiteSpace(_config["Shipping:Fedex:AccountNumber"]);

        public Task<List<ShippingQuote>> GetQuotesAsync(ShipmentRequest request)
        {
            if (!IsConfigured) return Task.FromResult(new List<ShippingQuote>());
            // TODO: OAuth2 token, then POST /rate/v1/rates/quotes.
            _logger.LogWarning("FedEx: GetQuotesAsync not implemented yet.");
            return Task.FromResult(new List<ShippingQuote>());
        }

        public Task<BookingResult> BookAsync(ShipmentRequest request)
        {
            if (!IsConfigured)
                return Task.FromResult(new BookingResult(false, "FedEx is not configured.", ProviderKey, null, null, null, null));
            // TODO: POST /ship/v1/shipments.
            _logger.LogWarning("FedEx: BookAsync not implemented yet.");
            return Task.FromResult(new BookingResult(false, "FedEx integration pending. Provide FedEx Developer credentials to activate.", ProviderKey, null, null, null, null));
        }

        public Task<TrackingResult> TrackAsync(string trackingReference)
        {
            if (!IsConfigured)
                return Task.FromResult(new TrackingResult(false, "FedEx is not configured.", null, null, null, null, null));
            // TODO: POST /track/v1/trackingnumbers.
            _logger.LogWarning("FedEx: TrackAsync not implemented yet.");
            return Task.FromResult(new TrackingResult(false, "FedEx integration pending.", null, null, null, null, null));
        }
    }
}
