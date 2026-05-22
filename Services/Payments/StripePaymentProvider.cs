using WebApplication1.Models;

namespace WebApplication1.Services.Payments
{
    /// <summary>
    /// Stripe — stub. When the client provides Stripe live keys, install the
    /// official Stripe.net NuGet (`dotnet add package Stripe.net`) and fill
    /// in the three TODOs below. The architecture is in place; only the
    /// SDK calls are missing.
    ///
    /// Required config in User Secrets / appsettings.Production.json:
    ///   "Payments:Stripe:PublishableKey":  "pk_live_..."
    ///   "Payments:Stripe:SecretKey":       "sk_live_..."
    ///   "Payments:Stripe:WebhookSecret":   "whsec_..."
    /// Webhook endpoint: POST /Payment/StripeWebhook
    /// Currencies supported: USD, EUR, GBP, AUD, CAD, NZD, AED, INR, NGN, etc.
    ///   (Stripe lists 130+ — see https://stripe.com/docs/currencies)
    /// </summary>
    public class StripePaymentProvider : IPaymentProvider
    {
        private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "USD", "EUR", "GBP", "AUD", "CAD", "NZD", "AED", "INR", "NGN",
            "JPY", "CHF", "SEK", "NOK", "DKK", "HKD", "SGD", "ZAR"
        };

        private readonly IConfiguration _config;
        private readonly IFeatureFlagService _flags;
        private readonly ILogger<StripePaymentProvider> _logger;

        public StripePaymentProvider(
            IConfiguration config,
            IFeatureFlagService flags,
            ILogger<StripePaymentProvider> logger)
        {
            _config = config;
            _flags = flags;
            _logger = logger;
        }

        public string ProviderKey => "stripe";
        public string DisplayName => "Stripe (card)";

        public bool SupportsCurrency(string currencyCode) => SupportedCurrencies.Contains(currencyCode);

        public async Task<bool> IsAvailableAsync()
        {
            if (string.IsNullOrWhiteSpace(_config["Payments:Stripe:SecretKey"])) return false;
            return await _flags.IsEnabledAsync(FeatureFlags.PaymentsStripe);
        }

        public Task<PaymentInitiation> InitiateAsync(Order order, string currencyCode)
        {
            // TODO: Install Stripe.net.
            // TODO: var options = new PaymentIntentCreateOptions {
            //          Amount = (long)(order.TotalAmount * 100),
            //          Currency = currencyCode.ToLowerInvariant(),
            //          Metadata = new() { { "order_number", order.OrderNumber } }
            //       };
            //       var intent = await new PaymentIntentService().CreateAsync(options);
            //       return new(true, null, null, intent.ClientSecret, ProviderKey);
            _logger.LogWarning("Stripe: InitiateAsync not implemented — credentials still pending.");
            return Task.FromResult(new PaymentInitiation(
                false,
                "Stripe integration pending. Provide Stripe Live keys + webhook secret to activate.",
                null, null, ProviderKey));
        }
    }
}
