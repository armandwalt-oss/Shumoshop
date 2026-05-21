using WebApplication1.Services;

namespace WebApplication1.Helpers
{
    /// <summary>
    /// Centralised source for ShumoShop's dispatch / collection address used
    /// when booking Courier Guy shipments. Reads from appsettings
    /// <c>Shipping:DispatchAddress</c> so there's a single source of truth and
    /// no risk of placeholder values ending up on a live booking.
    ///
    /// If essential fields are blank a warning is logged but the call still
    /// proceeds — the TCG API will return its own error for an invalid
    /// address, which is more useful than an unhandled exception.
    /// </summary>
    public static class ShippingAddressHelper
    {
        private const string PlaceholderMarker = "Your ";

        public static TcgAddress GetDispatchAddress(IConfiguration configuration, ILogger? logger = null)
        {
            var section = configuration.GetSection("Shipping:DispatchAddress");

            var address = new TcgAddress
            {
                Type = section["Type"] ?? "business",
                Company = section["Company"] ?? "ShumoShop",
                StreetAddress = section["StreetAddress"] ?? string.Empty,
                LocalArea = section["LocalArea"] ?? string.Empty,
                City = section["City"] ?? "Centurion",
                Zone = section["Zone"] ?? "Gauteng",
                Country = section["Country"] ?? "ZA",
                Code = section["Code"] ?? string.Empty
            };

            WarnIfPlaceholders(address, logger);
            return address;
        }

        public static TcgContact GetDispatchContact(IConfiguration configuration, string? roleOverride = null)
        {
            var section = configuration.GetSection("Shipping:DispatchContact");

            return new TcgContact
            {
                Name = roleOverride ?? section["Name"] ?? "ShumoShop Dispatch",
                MobileNumber = section["MobileNumber"] ?? string.Empty,
                Email = section["Email"] ?? string.Empty
            };
        }

        private static void WarnIfPlaceholders(TcgAddress address, ILogger? logger)
        {
            bool hasPlaceholder =
                string.IsNullOrWhiteSpace(address.StreetAddress) ||
                string.IsNullOrWhiteSpace(address.Code) ||
                address.StreetAddress.StartsWith(PlaceholderMarker, StringComparison.OrdinalIgnoreCase) ||
                address.LocalArea.StartsWith(PlaceholderMarker, StringComparison.OrdinalIgnoreCase) ||
                address.Code.StartsWith(PlaceholderMarker, StringComparison.OrdinalIgnoreCase);

            if (hasPlaceholder)
            {
                logger?.LogWarning(
                    "ShumoShop dispatch address contains placeholder or empty values. " +
                    "Set Shipping:DispatchAddress in appsettings.json " +
                    "before booking Courier Guy shipments. The booking attempt " +
                    "will proceed but may be rejected by the TCG API.");
            }
        }
    }
}
