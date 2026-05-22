using WebApplication1.Models;

namespace WebApplication1.Services.Payments
{
    /// <summary>
    /// Adapter from the carrier-neutral IPaymentProvider to the existing
    /// PayFastService. Always available when PayFast keys are present
    /// because PayFast is the demo default (no feature flag check).
    /// </summary>
    public class PayFastPaymentProvider : IPaymentProvider
    {
        private readonly IPaymentService _payFast;
        private readonly IConfiguration _config;

        public PayFastPaymentProvider(IPaymentService payFast, IConfiguration config)
        {
            _payFast = payFast;
            _config = config;
        }

        public string ProviderKey => "payfast";
        public string DisplayName => "PayFast";

        public Task<bool> IsAvailableAsync() =>
            Task.FromResult(!string.IsNullOrWhiteSpace(_config["PayFast:MerchantId"]));

        public bool SupportsCurrency(string currencyCode) =>
            string.Equals(currencyCode, "ZAR", StringComparison.OrdinalIgnoreCase);

        public Task<PaymentInitiation> InitiateAsync(Order order, string currencyCode)
        {
            // PayFast flow is HTML form auto-POST to PayFast. The existing
            // PaymentController.Process already does this; we expose the URL
            // here for callers that want to discover the provider at runtime.
            return Task.FromResult(new PaymentInitiation(
                Success: true,
                ErrorMessage: null,
                RedirectUrl: $"/Payment/Process?orderId={order.Id}",
                ClientSecret: null,
                ProviderKey: ProviderKey));
        }
    }
}
