using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// A named boolean toggle that gates an experimental / staged feature.
    /// Live in the database (not config) so a Dev user can flip them on from
    /// the admin UI without redeploying.
    ///
    /// Naming convention: dotted namespaces, lowercase, e.g.
    ///   international.country_switcher
    ///   international.geo_pricing
    ///   analytics.ga4
    /// </summary>
    public class FeatureFlag
    {
        [Key]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = false;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? UpdatedByUserId { get; set; }
    }

    /// <summary>
    /// Strongly-typed constants so the rest of the codebase doesn't sprinkle
    /// magic strings. If a flag rename is needed, change it once here.
    /// </summary>
    public static class FeatureFlags
    {
        // International expansion pillars
        public const string CountrySwitcher       = "international.country_switcher";
        public const string GeoPricing            = "international.geo_pricing";
        public const string InternationalShipping = "international.shipping";
        public const string PaymentsStripe        = "international.payments_stripe";
        public const string PaymentsPayPal        = "international.payments_paypal";

        // Analytics
        public const string AnalyticsGa4     = "analytics.ga4";
        public const string AnalyticsClarity = "analytics.clarity";

        // Demo / dev utility
        public const string DemoBypassPayments = "demo.bypass_payments";

        /// <summary>
        /// Default flag set seeded on startup. All start disabled so the
        /// SA-only flow is what every visitor sees until Dev flips them on.
        /// </summary>
        public static IEnumerable<FeatureFlag> Defaults() => new[]
        {
            new FeatureFlag { Name = CountrySwitcher,       IsEnabled = false, Description = "Show the country/currency switcher in the header." },
            new FeatureFlag { Name = GeoPricing,            IsEnabled = false, Description = "Apply per-country product prices (requires uploaded price lists)." },
            new FeatureFlag { Name = InternationalShipping, IsEnabled = false, Description = "Allow shipping carriers other than Courier Guy (DHL / FedEx / Aramex)." },
            new FeatureFlag { Name = PaymentsStripe,        IsEnabled = false, Description = "Accept Stripe payments for non-SA orders." },
            new FeatureFlag { Name = PaymentsPayPal,        IsEnabled = false, Description = "Accept PayPal payments." },
            new FeatureFlag { Name = AnalyticsGa4,          IsEnabled = false, Description = "Inject the Google Analytics 4 tag in the layout." },
            new FeatureFlag { Name = AnalyticsClarity,      IsEnabled = false, Description = "Inject the Microsoft Clarity session-recording tag." },
            new FeatureFlag { Name = DemoBypassPayments,    IsEnabled = false, Description = "DEMO ONLY — skip the PayFast redirect and mark orders Paid immediately. MUST be off in production." },
        };
    }
}
