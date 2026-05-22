using WebApplication1.Models;

namespace WebApplication1.Services.Payments
{
    /// <summary>
    /// PayPal — stub. When the client provides PayPal REST app credentials,
    /// install the official PayPal Checkout SDK (`dotnet add package
    /// PayPalCheckoutSdk`) and fill in the TODOs below.
    ///
    /// Required config in User Secrets / appsettings.Production.json:
    ///   "Payments:PayPal:ClientId":      "..."
    ///   "Payments:PayPal:ClientSecret":  "..."
    ///   "Payments:PayPal:Mode":          "sandbox" | "live"
    /// Currencies supported: 25 PayPal-supported currencies.
    /// </summary>
    public class PayPalPaymentProvider : IPaymentProvider
    {
        private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "USD", "EUR", "GBP", "AUD", "CAD", "NZD", "JPY", "CHF", "SEK",
            "NOK", "DKK", "HKD", "SGD", "ILS", "MXN", "PLN", "BRL", "TWD",
            "THB", "PHP", "CZK", "HUF", "RUB"
        };

        private readonly IConfiguration _config;
        private readonly IFeatureFlagService _flags;
        private readonly ILogger<PayPalPaymentProvider> _logger;

        public PayPalPaymentProvider(
            IConfiguration config,
            IFeatureFlagService flags,
            ILogger<PayPalPaymentProvider> logger)
        {
            _config = config;
            _flags = flags;
            _logger = logger;
        }

        public string ProviderKey => "paypal";
        public string DisplayName => "PayPal";

        public bool SupportsCurrency(string currencyCode) => SupportedCurrencies.Contains(currencyCode);

        public async Task<bool> IsAvailableAsync()
        {
            if (string.IsNullOrWhiteSpace(_config["Payments:PayPal:ClientId"])) return false;
            return await _flags.IsEnabledAsync(FeatureFlags.PaymentsPayPal);
        }

        public Task<PaymentInitiation> InitiateAsync(Order order, string currencyCode)
        {
            // TODO: Install PayPalCheckoutSdk.
            // TODO: Create an OrdersCreateRequest with intent=CAPTURE,
            //       purchase_units = [{ amount = { currency_code, value } }].
            //       Execute via PayPalHttpClient → response.Result.Id is the
            //       approval URL token; return as RedirectUrl.
            _logger.LogWarning("PayPal: InitiateAsync not implemented — credentials still pending.");
            return Task.FromResult(new PaymentInitiation(
                false,
                "PayPal integration pending. Provide REST app Client ID + Secret to activate.",
                null, null, ProviderKey));
        }
    }
}
