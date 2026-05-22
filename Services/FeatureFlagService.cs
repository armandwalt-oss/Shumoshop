using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface IFeatureFlagService
    {
        Task<bool> IsEnabledAsync(string name);
        Task<List<FeatureFlag>> GetAllAsync();
        Task SetEnabledAsync(string name, bool enabled, string? userId);
        void InvalidateCache();
    }

    /// <summary>
    /// Reads named boolean toggles from the database, with a short in-memory
    /// cache so the hot path (e.g. checking "international.country_switcher"
    /// on every page render) doesn't pound SQL.
    ///
    /// Cache TTL is intentionally short — admin changes propagate within 60s
    /// without needing a manual flush. The SetEnabledAsync path also
    /// invalidates the cache so the toggling admin sees their change instantly.
    /// </summary>
    public class FeatureFlagService : IFeatureFlagService
    {
        private const string CacheKey = "FeatureFlags.All";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FeatureFlagService> _logger;

        public FeatureFlagService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<FeatureFlagService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> IsEnabledAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            var all = await LoadAllCachedAsync();
            return all.TryGetValue(name, out var enabled) && enabled;
        }

        public async Task<List<FeatureFlag>> GetAllAsync()
        {
            // Always read fresh from DB for admin views — we want to show the
            // actual stored state plus UpdatedAt/UpdatedByUserId, not a snapshot.
            return await _context.FeatureFlags
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        public async Task SetEnabledAsync(string name, bool enabled, string? userId)
        {
            var flag = await _context.FeatureFlags.FindAsync(name);
            if (flag is null)
            {
                // First time the flag is being set — insert it.
                flag = new FeatureFlag
                {
                    Name = name,
                    IsEnabled = enabled,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = userId
                };
                _context.FeatureFlags.Add(flag);
            }
            else
            {
                flag.IsEnabled = enabled;
                flag.UpdatedAt = DateTime.UtcNow;
                flag.UpdatedByUserId = userId;
            }

            await _context.SaveChangesAsync();
            InvalidateCache();

            _logger.LogInformation("Feature flag {Name} set to {Enabled} by {User}",
                name, enabled, userId ?? "(unknown)");
        }

        public void InvalidateCache() => _cache.Remove(CacheKey);

        private async Task<Dictionary<string, bool>> LoadAllCachedAsync()
        {
            if (_cache.TryGetValue<Dictionary<string, bool>>(CacheKey, out var cached) && cached is not null)
            {
                return cached;
            }

            var fresh = await _context.FeatureFlags
                .ToDictionaryAsync(f => f.Name, f => f.IsEnabled, StringComparer.OrdinalIgnoreCase);

            _cache.Set(CacheKey, fresh, CacheTtl);
            return fresh;
        }
    }
}
