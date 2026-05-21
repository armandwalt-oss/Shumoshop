using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Services
{
    public class CartCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CartCleanupService> _logger;
        private readonly IConfiguration _configuration;

        public CartCleanupService(
            IServiceProvider serviceProvider,
            ILogger<CartCleanupService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🧹 Cart Cleanup Service started");

            // Wait 1 minute before first run (let app start fully)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAbandonedCartsAsync();

                    var intervalHours = _configuration.GetValue<int>("CartCleanup:CleanupIntervalHours", 24);
                    await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error occurred during cart cleanup");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry in 1 hour
                }
            }
        }

        private async Task CleanupAbandonedCartsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var guestRetentionDays = _configuration.GetValue<int>("CartCleanup:GuestCartRetentionDays", 14);
                var userRetentionDays = _configuration.GetValue<int>("CartCleanup:UserCartRetentionDays", 60);

                var cutoffDateGuest = DateTime.Now.AddDays(-guestRetentionDays);
                var cutoffDateUser = DateTime.Now.AddDays(-userRetentionDays);

                _logger.LogInformation($"🔍 Starting cart cleanup - Guest cutoff: {cutoffDateGuest:yyyy-MM-dd}, User cutoff: {cutoffDateUser:yyyy-MM-dd}");

                // Find abandoned guest carts (no UserId, old LastModifiedDate)
                var abandonedGuestCarts = await context.Carts
                    .Where(c => c.UserId == null && c.LastModifiedDate < cutoffDateGuest)
                    .ToListAsync();

                // Find abandoned user carts (has UserId, old LastModifiedDate)
                var abandonedUserCarts = await context.Carts
                    .Where(c => c.UserId != null && c.LastModifiedDate < cutoffDateUser)
                    .ToListAsync();

                var totalCartsToDelete = abandonedGuestCarts.Count + abandonedUserCarts.Count;

                if (totalCartsToDelete > 0)
                {
                    _logger.LogInformation($"📦 Found {abandonedGuestCarts.Count} abandoned guest carts and {abandonedUserCarts.Count} abandoned user carts");

                    // Get all cart IDs to delete
                    var guestCartIds = abandonedGuestCarts.Select(c => c.Id).ToList();
                    var userCartIds = abandonedUserCarts.Select(c => c.Id).ToList();
                    var allCartIds = guestCartIds.Concat(userCartIds).ToList();

                    // Delete cart items first
                    var itemsToDelete = await context.CartItems
                        .Where(ci => allCartIds.Contains(ci.CartId))
                        .ToListAsync();

                    context.CartItems.RemoveRange(itemsToDelete);

                    // Delete carts
                    context.Carts.RemoveRange(abandonedGuestCarts);
                    context.Carts.RemoveRange(abandonedUserCarts);

                    await context.SaveChangesAsync();

                    _logger.LogInformation($"✅ Successfully cleaned up {totalCartsToDelete} abandoned carts and {itemsToDelete.Count} cart items");
                }
                else
                {
                    _logger.LogInformation("✨ No abandoned carts to clean up");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cleaning up abandoned carts");
                throw;
            }
        }
    }
}