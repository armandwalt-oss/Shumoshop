using WebApplication1.Models;

namespace WebApplication1.Services.Payments
{
    public record PaymentInitiation(
        bool Success,
        string? ErrorMessage,
        // Either a hosted redirect URL (PayFast / PayPal-style) ...
        string? RedirectUrl,
        // ... or an inline form / client secret to hand to the front-end (Stripe Elements).
        string? ClientSecret,
        string? ProviderKey);

    /// <summary>
    /// Carrier-neutral payment provider. PayFast (ZAR / SA), Stripe (global,
    /// 130+ currencies) and PayPal each implement this. Provider selection at
    /// checkout is driven by the active country / currency.
    /// </summary>
    public interface IPaymentProvider
    {
        string ProviderKey { get; }
        string DisplayName { get; }

        /// <summary>True if API keys are configured AND the provider's feature flag is on.</summary>
        Task<bool> IsAvailableAsync();

        /// <summary>True if this provider can charge in the given ISO-4217 currency.</summary>
        bool SupportsCurrency(string currencyCode);

        /// <summary>Build whatever the customer needs (redirect URL / client secret) to pay this order.</summary>
        Task<PaymentInitiation> InitiateAsync(Order order, string currencyCode);
    }
}
