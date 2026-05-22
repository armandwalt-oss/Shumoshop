using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// A price ready to render in the UI: amount + currency code + formatted
    /// display string. Falls back to ZAR when no per-country override exists.
    /// </summary>
    public record DisplayPrice(decimal Amount, string CurrencyCode, string CurrencySymbol, string Display);

    public interface IPriceResolver
    {
        /// <summary>
        /// Resolve a single product's display price for the active country.
        /// </summary>
        Task<DisplayPrice> ResolveAsync(Product product, HttpContext context);

        /// <summary>
        /// Resolve display prices for many products at once. Faster than
        /// looping because we batch the ProductPrice lookup into one query.
        /// </summary>
        Task<Dictionary<int, DisplayPrice>> ResolveManyAsync(IEnumerable<Product> products, HttpContext context);
    }

    public class PriceResolver : IPriceResolver
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeatureFlagService _flags;

        // Default (ZAR) price displayed when geo-pricing is off OR no
        // country-specific override exists.
        private static DisplayPrice ZarFallback(decimal amount) =>
            new(amount, "ZAR", "R", $"R {amount:N2}");

        public PriceResolver(ApplicationDbContext context, IFeatureFlagService flags)
        {
            _context = context;
            _flags = flags;
        }

        public async Task<DisplayPrice> ResolveAsync(Product product, HttpContext context)
        {
            // Geo-pricing must be turned on AND we must have an active country.
            if (!await _flags.IsEnabledAsync(FeatureFlags.GeoPricing))
                return ZarFallback(product.Price);

            if (context.Items[CountryKeys.HttpContextItemKey] is not Country country)
                return ZarFallback(product.Price);

            // ZA always uses the base Product.Price (ZAR).
            if (country.Code.Equals("ZA", StringComparison.OrdinalIgnoreCase))
                return ZarFallback(product.Price);

            var override_ = await _context.ProductPrices
                .AsNoTracking()
                .FirstOrDefaultAsync(pp => pp.ProductId == product.Id && pp.CountryCode == country.Code);

            if (override_ is null)
            {
                // No price set for this market — fall back to the base ZAR
                // price (we don't auto-convert; pricing decisions are the
                // client's, not ours).
                return ZarFallback(product.Price);
            }

            return new DisplayPrice(
                override_.Price,
                country.CurrencyCode,
                country.CurrencySymbol,
                $"{country.CurrencySymbol} {override_.Price:N2}");
        }

        public async Task<Dictionary<int, DisplayPrice>> ResolveManyAsync(
            IEnumerable<Product> products, HttpContext context)
        {
            var byId = products.ToDictionary(p => p.Id);

            if (!await _flags.IsEnabledAsync(FeatureFlags.GeoPricing) ||
                context.Items[CountryKeys.HttpContextItemKey] is not Country country ||
                country.Code.Equals("ZA", StringComparison.OrdinalIgnoreCase))
            {
                return byId.ToDictionary(kvp => kvp.Key, kvp => ZarFallback(kvp.Value.Price));
            }

            var ids = byId.Keys.ToList();
            var overrides = await _context.ProductPrices
                .AsNoTracking()
                .Where(pp => pp.CountryCode == country.Code && ids.Contains(pp.ProductId))
                .ToDictionaryAsync(pp => pp.ProductId, pp => pp.Price);

            return byId.ToDictionary(
                kvp => kvp.Key,
                kvp => overrides.TryGetValue(kvp.Key, out var price)
                    ? new DisplayPrice(price, country.CurrencyCode, country.CurrencySymbol, $"{country.CurrencySymbol} {price:N2}")
                    : ZarFallback(kvp.Value.Price));
        }
    }
}
