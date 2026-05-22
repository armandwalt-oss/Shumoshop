namespace WebApplication1.Services.Shipping
{
    /// <summary>
    /// One implementation per carrier (Courier Guy, DHL Express, FedEx,
    /// Aramex). All providers speak the same carrier-neutral types so
    /// checkout / admin code stays simple.
    /// </summary>
    public interface IShippingProvider
    {
        /// <summary>Short stable key (no spaces). Used in DB / config.</summary>
        string ProviderKey { get; }

        /// <summary>Human-readable name shown in the admin / checkout UI.</summary>
        string DisplayName { get; }

        /// <summary>Returns true when the provider can serve the destination country.</summary>
        bool SupportsDestination(string destinationCountryCode);

        /// <summary>
        /// Returns true if the provider is ready to make real API calls. Stubs
        /// without credentials return false; the registry then skips them so
        /// the user never sees a "no rates" error from an unconfigured carrier.
        /// </summary>
        bool IsConfigured { get; }

        Task<List<ShippingQuote>> GetQuotesAsync(ShipmentRequest request);

        Task<BookingResult> BookAsync(ShipmentRequest request);

        Task<TrackingResult> TrackAsync(string trackingReference);
    }
}
