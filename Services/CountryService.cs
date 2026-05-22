using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// Constants pulled out of the interface so older C# language modes
    /// can still compile (interface constants need C# 8+ and explicit
    /// public access; safer to live on a static class).
    /// </summary>
    public static class CountryKeys
    {
        /// <summary>Cookie key used to persist the visitor's chosen country.</summary>
        public const string CountryCookieName = "shumoshop_country";

        /// <summary>HttpContext.Items key the middleware uses to publish the active country.</summary>
        public const string HttpContextItemKey = "ActiveCountry";
    }

    public interface ICountryService
    {
        /// <summary>Returns all active countries, ordered for display.</summary>
        Task<List<Country>> GetActiveCountriesAsync();

        /// <summary>Returns the country with the given ISO-2 code, or null.</summary>
        Task<Country?> GetCountryAsync(string code);

        /// <summary>
        /// Resolves the visitor's active country in this priority order:
        ///   1. Explicit cookie ("shumoshop_country")
        ///   2. Future: IP-based detection (MaxMind GeoLite2 / ipapi.co)
        ///   3. Site default — South Africa
        /// Always returns a non-null country.
        /// </summary>
        Task<Country> ResolveActiveCountryAsync(HttpContext context);
    }

    public class CountryService : ICountryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CountryService> _logger;

        public CountryService(ApplicationDbContext context, ILogger<CountryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Country>> GetActiveCountriesAsync() =>
            await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

        public async Task<Country?> GetCountryAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var norm = code.Trim().ToUpperInvariant();
            return await _context.Countries
                .FirstOrDefaultAsync(c => c.Code == norm && c.IsActive);
        }

        public async Task<Country> ResolveActiveCountryAsync(HttpContext context)
        {
            // 1. Cookie override always wins.
            if (context.Request.Cookies.TryGetValue(CountryKeys.CountryCookieName, out var cookieCode))
            {
                var fromCookie = await GetCountryAsync(cookieCode);
                if (fromCookie is not null) return fromCookie;
            }

            // 2. TODO: IP-based detection. For now we fall straight through.

            // 3. Site default — South Africa, or whatever DisplayOrder=1 is.
            var fallback = await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .FirstOrDefaultAsync();

            if (fallback is not null) return fallback;

            // Safety net — every code path that uses this service expects a
            // non-null return. If the Countries table is somehow empty, hand
            // back an in-memory ZA so the site keeps rendering.
            _logger.LogWarning("No active countries in database — falling back to in-memory ZA.");
            return new Country
            {
                Code = "ZA",
                Name = "South Africa",
                CurrencyCode = "ZAR",
                CurrencySymbol = "R",
                FlagEmoji = "🇿🇦",
                IsActive = true,
                DisplayOrder = 1
            };
        }
    }
}
