using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// One row per product detail page hit, paired with read-side helpers
    /// for the admin "Top Viewed Products" widget. Writes are best-effort —
    /// a failure here should never break a page load for the customer.
    /// </summary>
    public interface IAnalyticsService
    {
        Task LogProductViewAsync(int productId, HttpContext context);

        /// <summary>
        /// Top N products ordered by view count over the last <paramref name="daysBack"/> days.
        /// </summary>
        Task<List<TopProductRow>> GetTopProductsAsync(int daysBack = 7, int topN = 10);

        /// <summary>Total product views in the period.</summary>
        Task<int> GetTotalViewsAsync(int daysBack = 7);
    }

    public record TopProductRow(int ProductId, string Name, string SKU, int ViewCount);

    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogProductViewAsync(int productId, HttpContext context)
        {
            try
            {
                // Optional user id (null for anonymous browsers).
                var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                // Optional country code (from ActiveCountryMiddleware).
                string? countryCode = null;
                if (context.Items[CountryKeys.HttpContextItemKey] is Country country)
                {
                    countryCode = country.Code;
                }

                _context.ProductViews.Add(new ProductView
                {
                    ProductId = productId,
                    ViewedAt = DateTime.UtcNow,
                    UserId = userId,
                    CountryCode = countryCode
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Never block a page load on analytics.
                _logger.LogWarning(ex, "Failed to log product view for product {ProductId}", productId);
            }
        }

        public async Task<List<TopProductRow>> GetTopProductsAsync(int daysBack = 7, int topN = 10)
        {
            var since = DateTime.UtcNow.AddDays(-daysBack);

            // EF can't translate Join+OrderBy-by-constructed-record in one go,
            // so we split into two queries: aggregate in SQL, then attach
            // Product names client-side. The number of rows after the Top-N
            // step is tiny so the second query is cheap.
            var aggregated = await _context.ProductViews
                .Where(pv => pv.ViewedAt >= since)
                .GroupBy(pv => pv.ProductId)
                .Select(g => new { ProductId = g.Key, ViewCount = g.Count() })
                .OrderByDescending(x => x.ViewCount)
                .Take(topN)
                .ToListAsync();

            if (aggregated.Count == 0) return new();

            var ids = aggregated.Select(a => a.ProductId).ToList();
            var productsById = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, p.Name, p.SKU })
                .ToDictionaryAsync(p => p.Id, p => new { p.Name, p.SKU });

            return aggregated
                .Where(a => productsById.ContainsKey(a.ProductId))
                .Select(a => new TopProductRow(
                    a.ProductId,
                    productsById[a.ProductId].Name,
                    productsById[a.ProductId].SKU,
                    a.ViewCount))
                .ToList();
        }

        public async Task<int> GetTotalViewsAsync(int daysBack = 7)
        {
            var since = DateTime.UtcNow.AddDays(-daysBack);
            return await _context.ProductViews.CountAsync(pv => pv.ViewedAt >= since);
        }
    }
}
