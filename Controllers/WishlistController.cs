using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Wishlist
        public async Task<IActionResult> Index()
        {
            var wishlist = await GetOrCreateWishlistAsync();

            var wishlistItems = await _context.WishlistItems
                .Where(w => w.WishlistId == wishlist.Id)
                .Include(w => w.Product)
                .ToListAsync();

            return View(wishlistItems);
        }

        // POST: Add to Wishlist
        [HttpPost]
        public async Task<IActionResult> AddToWishlist(int productId)
        {
            try
            {
                var wishlist = await GetOrCreateWishlistAsync();

                // Check if product already in wishlist
                var existingItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.WishlistId == wishlist.Id && w.ProductId == productId);

                if (existingItem != null)
                {
                    return Json(new { success = false, message = "Product already in wishlist" });
                }

                // Check if product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Add to wishlist
                var wishlistItem = new WishlistItem
                {
                    WishlistId = wishlist.Id,
                    ProductId = productId,
                    AddedDate = DateTime.Now
                };

                _context.WishlistItems.Add(wishlistItem);
                await _context.SaveChangesAsync();

                // Get updated count
                var count = await _context.WishlistItems
                    .CountAsync(w => w.WishlistId == wishlist.Id);

                return Json(new { success = true, message = "Added to wishlist", wishlistCount = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error adding to wishlist: " + ex.Message });
            }
        }

        // POST: Remove from Wishlist
        [HttpPost]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            try
            {
                var wishlist = await GetOrCreateWishlistAsync();

                var wishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.WishlistId == wishlist.Id && w.ProductId == productId);

                if (wishlistItem == null)
                {
                    return Json(new { success = false, message = "Item not found in wishlist" });
                }

                _context.WishlistItems.Remove(wishlistItem);
                await _context.SaveChangesAsync();

                // Get updated count
                var count = await _context.WishlistItems
                    .CountAsync(w => w.WishlistId == wishlist.Id);

                return Json(new { success = true, message = "Removed from wishlist", wishlistCount = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error removing from wishlist: " + ex.Message });
            }
        }

        // GET: Wishlist Count
        [HttpGet]
        public async Task<IActionResult> GetWishlistCount()
        {
            var wishlist = await GetOrCreateWishlistAsync();

            var count = await _context.WishlistItems
                .CountAsync(w => w.WishlistId == wishlist.Id);

            return Json(new { count = count });
        }

        // POST: Move to Cart
        [HttpPost]
        public async Task<IActionResult> MoveToCart(int productId)
        {
            try
            {
                var wishlist = await GetOrCreateWishlistAsync();

                // Remove from wishlist
                var wishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.WishlistId == wishlist.Id && w.ProductId == productId);

                if (wishlistItem != null)
                {
                    _context.WishlistItems.Remove(wishlistItem);
                    await _context.SaveChangesAsync();
                }

                // Add to cart (you can redirect to cart controller)
                return RedirectToAction("AddToCart", "Cart", new { productId = productId, quantity = 1 });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error moving to cart: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Helper method to get or create wishlist
        private async Task<Wishlist> GetOrCreateWishlistAsync()
        {
            Wishlist? wishlist = null;

            // Check if user is logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.Identity.Name;
                wishlist = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId);

                if (wishlist == null)
                {
                    wishlist = new Wishlist
                    {
                        UserId = userId,
                        CreatedDate = DateTime.Now
                    };
                    _context.Wishlists.Add(wishlist);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // Guest user - use session
                var sessionId = HttpContext.Session.GetString("WishlistSessionId");
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = Guid.NewGuid().ToString();
                    HttpContext.Session.SetString("WishlistSessionId", sessionId);
                }

                wishlist = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.SessionId == sessionId);

                if (wishlist == null)
                {
                    wishlist = new Wishlist
                    {
                        SessionId = sessionId,
                        CreatedDate = DateTime.Now
                    };
                    _context.Wishlists.Add(wishlist);
                    await _context.SaveChangesAsync();
                }
            }

            return wishlist;
        }
    }
}