namespace WebApplication1.Services.Shipping
{
    // Carrier-neutral types. Each IShippingProvider speaks these in/out so the
    // checkout flow doesn't need to know whether it's talking to Courier Guy,
    // DHL, FedEx, or Aramex.

    public record ShipAddress(
        string StreetAddress,
        string LocalArea,
        string City,
        string Zone,
        string CountryCode,   // ISO-2, e.g. "ZA", "US"
        string PostalCode,
        string? Company = null,
        string Type = "residential");

    public record ShipContact(string Name, string MobileNumber, string Email);

    public record ShipParcel(
        double LengthCm = 30,
        double WidthCm = 20,
        double HeightCm = 10,
        double WeightKg = 1,
        string Description = "Automotive clips and fasteners");

    public record ShipmentRequest(
        ShipAddress Origin,
        ShipContact OriginContact,
        ShipAddress Destination,
        ShipContact DestinationContact,
        List<ShipParcel> Parcels,
        decimal DeclaredValue,
        string? CustomerReference = null,
        string? ServiceLevelCode = null);

    public record ShippingQuote(
        string ProviderKey,        // "tcg", "dhl-express", "fedex", "aramex"
        string ProviderDisplayName, // "The Courier Guy", "DHL Express", ...
        string? ServiceLevelCode,   // carrier-specific (e.g. "ECO", "P", "PRIORITY_OVERNIGHT")
        string? ServiceLevelName,   // human readable
        decimal Rate,               // in CurrencyCode
        string CurrencyCode,        // ISO-4217
        DateTime? EstimatedDeliveryFrom,
        DateTime? EstimatedDeliveryTo);

    public record BookingResult(
        bool Success,
        string? ErrorMessage,
        string? ProviderKey,
        string? TrackingReference,
        string? ProviderShipmentId,
        decimal? Rate,
        string? CurrencyCode);

    public record TrackingResult(
        bool Success,
        string? ErrorMessage,
        string? Status,
        string? StatusLabel,
        DateTime? EstimatedDeliveryFrom,
        DateTime? EstimatedDeliveryTo,
        List<TrackingEvent>? Events);

    public record TrackingEvent(DateTime Date, string? Status, string? Message, string? Location);
}
