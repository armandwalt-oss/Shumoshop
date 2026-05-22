namespace WebApplication1.Services.Shipping
{
    /// <summary>
    /// Adapter from the carrier-neutral IShippingProvider to the existing
    /// TCG-specific ICourierGuyService. Lets us keep the old per-carrier
    /// service exactly as-is while still exposing it through the new
    /// abstraction.
    /// </summary>
    public class CourierGuyShippingProvider : IShippingProvider
    {
        private readonly ICourierGuyService _tcg;
        private readonly IConfiguration _config;

        public CourierGuyShippingProvider(ICourierGuyService tcg, IConfiguration config)
        {
            _tcg = tcg;
            _config = config;
        }

        public string ProviderKey => "tcg";
        public string DisplayName => "The Courier Guy";

        // SA-domestic only. A US → ZA shipment uses the destination carrier,
        // not TCG.
        public bool SupportsDestination(string destinationCountryCode) =>
            string.Equals(destinationCountryCode, "ZA", StringComparison.OrdinalIgnoreCase);

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_config["CourierGuy:ApiKey"]);

        public async Task<List<ShippingQuote>> GetQuotesAsync(ShipmentRequest request)
        {
            var (collection, deliver, parcels) = Convert(request);
            var result = await _tcg.GetRatesAsync(collection, deliver, parcels, request.DeclaredValue);
            if (!result.Success || result.Data is null) return new();

            return result.Data.Select(r => new ShippingQuote(
                ProviderKey, DisplayName,
                r.ServiceLevelCode, r.ServiceLevelName,
                r.Rate, "ZAR",
                r.DeliveryDateFrom, r.DeliveryDateTo
            )).ToList();
        }

        public async Task<BookingResult> BookAsync(ShipmentRequest request)
        {
            var (collection, deliver, parcels) = Convert(request);

            var tcgReq = new TcgCreateShipmentRequest
            {
                CollectionAddress = collection,
                CollectionContact = new TcgContact
                {
                    Name = request.OriginContact.Name,
                    MobileNumber = request.OriginContact.MobileNumber,
                    Email = request.OriginContact.Email
                },
                DeliveryAddress = deliver,
                DeliveryContact = new TcgContact
                {
                    Name = request.DestinationContact.Name,
                    MobileNumber = request.DestinationContact.MobileNumber,
                    Email = request.DestinationContact.Email
                },
                Parcels = parcels,
                ServiceLevelCode = request.ServiceLevelCode ?? "ECO",
                CustomerReferenceName = "Order no.",
                CustomerReference = request.CustomerReference ?? "",
                DeclaredValue = request.DeclaredValue,
                CollectionMinDate = DateTime.UtcNow.ToString("yyyy-MM-ddT00:00:00Z"),
                MuteNotifications = false
            };

            var result = await _tcg.CreateShipmentAsync(tcgReq);
            if (!result.Success || result.Data is null)
                return new BookingResult(false, result.ErrorMessage, ProviderKey, null, null, null, null);

            return new BookingResult(true, null, ProviderKey,
                result.Data.CustomTrackingReference ?? result.Data.ShortTrackingReference,
                result.Data.Id.ToString(),
                result.Data.Rate, "ZAR");
        }

        public async Task<TrackingResult> TrackAsync(string trackingReference)
        {
            var r = await _tcg.TrackShipmentAsync(trackingReference);
            if (!r.Success || r.Data is null)
                return new TrackingResult(false, r.ErrorMessage, null, null, null, null, null);

            return new TrackingResult(true, null, r.Data.Status, r.Data.Status,
                r.Data.EstimatedDeliveryFrom, r.Data.EstimatedDeliveryTo,
                r.Data.TrackingEvents.Select(e => new TrackingEvent(e.Date, e.Status, e.Message, e.Location)).ToList());
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static (TcgAddress, TcgAddress, List<TcgParcel>) Convert(ShipmentRequest r)
        {
            TcgAddress ToTcg(ShipAddress a) => new()
            {
                Type = a.Type,
                Company = a.Company,
                StreetAddress = a.StreetAddress,
                LocalArea = a.LocalArea,
                City = a.City,
                Zone = a.Zone,
                Country = a.CountryCode,
                Code = a.PostalCode
            };
            var parcels = r.Parcels.Select(p => new TcgParcel
            {
                LengthCm = p.LengthCm, WidthCm = p.WidthCm, HeightCm = p.HeightCm,
                WeightKg = p.WeightKg, ParcelDescription = p.Description
            }).ToList();
            return (ToTcg(r.Origin), ToTcg(r.Destination), parcels);
        }
    }
}
