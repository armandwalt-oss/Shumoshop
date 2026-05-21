using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Security.Claims;

namespace WebApplication1.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var cart = await GetOrCreateCartAsync();

            if (cart == null)
            {
                return View(new Cart());
            }

            // Load cart with items and products
            var cartWithItems = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == cart.Id);

            return View(cartWithItems ?? new Cart());
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            if (!product.InStock || product.StockQuantity < quantity)
            {
                return Json(new { success = false, message = "Product out of stock" });
            }

            var cart = await GetOrCreateCartAsync();

            // Check if item already exists in cart
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

            if (existingItem != null)
            {
                // Update quantity
                int newQuantity = existingItem.Quantity + quantity;

                if (newQuantity > product.StockQuantity)
                {
                    return Json(new { success = false, message = $"Only {product.StockQuantity} items available" });
                }

                existingItem.Quantity = newQuantity;
                _context.Update(existingItem);
            }
            else
            {
                // Add new item
                var cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    AddedDate = DateTime.Now
                };
                _context.CartItems.Add(cartItem);
            }

            cart.LastModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // Get updated cart count
            var cartCount = await GetCartItemCountAsync();

            return Json(new
            {
                success = true,
                message = "Product added to cart",
                cartCount = cartCount
            });
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            if (quantity < 1)
            {
                return Json(new { success = false, message = "Quantity must be at least 1" });
            }

            var cartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Item not found" });
            }

            if (quantity > cartItem.Product.StockQuantity)
            {
                return Json(new { success = false, message = $"Only {cartItem.Product.StockQuantity} items available" });
            }

            cartItem.Quantity = quantity;

            var cart = await _context.Carts.FindAsync(cartItem.CartId);
            if (cart != null)
            {
                cart.LastModifiedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Calculate new totals
            var itemTotal = cartItem.TotalPrice;
            var cartTotal = await GetCartTotalAsync();

            return Json(new
            {
                success = true,
                itemTotal = itemTotal,
                cartTotal = cartTotal
            });
        }

        // POST: Cart/RemoveItem
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Item not found" });
            }

            _context.CartItems.Remove(cartItem);

            var cart = await _context.Carts.FindAsync(cartItem.CartId);
            if (cart != null)
            {
                cart.LastModifiedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            var cartCount = await GetCartItemCountAsync();
            var cartTotal = await GetCartTotalAsync();

            return Json(new
            {
                success = true,
                message = "Item removed from cart",
                cartCount = cartCount,
                cartTotal = cartTotal
            });
        }

        // POST: Cart/Clear
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var cart = await GetOrCreateCartAsync();

            if (cart != null)
            {
                var cartItems = await _context.CartItems
                    .Where(ci => ci.CartId == cart.Id)
                    .ToListAsync();

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Cart cleared" });
        }

        // GET: Cart/GetCartCount
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var count = await GetCartItemCountAsync();
            return Json(new { count = count });
        }

        // FIXED: Helper Methods
        private async Task<Cart> GetOrCreateCartAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Cart cart;

            if (!string.IsNullOrEmpty(userId))
            {
                // Logged-in user - ALWAYS check user cart first
                cart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart != null)
                {
                    // Found user cart, return it
                    return cart;
                }

                // No user cart found, check for session cart to merge
                var sessionId = GetOrCreateSessionId();
                var sessionCart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);

                if (sessionCart != null)
                {
                    // Merge session cart to user cart
                    sessionCart.UserId = userId;
                    sessionCart.SessionId = null;
                    cart = sessionCart;
                }
                else
                {
                    // Create new cart
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                // Guest user
                var sessionId = GetOrCreateSessionId();

                cart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);

                if (cart == null)
                {
                    cart = new Cart { SessionId = sessionId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }
            }

            return cart;
        }

        private string GetOrCreateSessionId()
        {
            // FIXED: First check cookie (persists across session regeneration)
            var cookieSessionId = _httpContextAccessor.HttpContext.Request.Cookies["CartSessionId"];

            if (!string.IsNullOrEmpty(cookieSessionId))
            {
                // Store in session too for quick access
                _httpContextAccessor.HttpContext.Session.SetString("CartSessionId", cookieSessionId);
                return cookieSessionId;
            }

            // Check session
            var sessionId = _httpContextAccessor.HttpContext.Session.GetString("CartSessionId");

            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                _httpContextAccessor.HttpContext.Session.SetString("CartSessionId", sessionId);

                // FIXED: Detect if we're in development (localhost)
                var isDevelopment = _httpContextAccessor.HttpContext.Request.Host.Host == "localhost"
                                    || _httpContextAccessor.HttpContext.Request.Host.Host == "127.0.0.1";

                // Store in cookie (lasts 30 days, persists across session regeneration)
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    Secure = !isDevelopment, // FALSE for localhost, TRUE for production
                    SameSite = SameSiteMode.Lax
                };
                _httpContextAccessor.HttpContext.Response.Cookies.Append("CartSessionId", sessionId, cookieOptions);
            }

            return sessionId;
        }

        private async Task<int> GetCartItemCountAsync()
        {
            var cart = await GetOrCreateCartAsync();

            if (cart == null)
                return 0;

            return await _context.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .SumAsync(ci => ci.Quantity);
        }

        private async Task<decimal> GetCartTotalAsync()
        {
            var cart = await GetOrCreateCartAsync();

            if (cart == null)
                return 0;

            var cartItems = await _context.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .ToListAsync();

            return cartItems.Sum(ci => ci.TotalPrice);
        }

        // Helper method to clear cart session (optional cleanup)
        public void ClearCartSession()
        {
            _httpContextAccessor.HttpContext.Session.Remove("CartSessionId");
            _httpContextAccessor.HttpContext.Response.Cookies.Delete("CartSessionId");
        }
    }
}