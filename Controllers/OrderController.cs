using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Helpers;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize] // User must be logged in to checkout
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ICourierGuyService _courierGuy;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            ApplicationDbContext context,
            IEmailService emailService,
            ICourierGuyService courierGuy,
            IConfiguration configuration,
            ILogger<OrderController> logger)
        {
            _context = context;
            _emailService = emailService;
            _courierGuy = courierGuy;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: Order/Checkout
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get user's cart
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            // Check if cart is empty
            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items before checkout.";
                return RedirectToAction("Index", "Cart");
            }

            // Check if any items are out of stock
            foreach (var item in cart.CartItems)
            {
                if (!item.Product.InStock || item.Product.StockQuantity < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"{item.Product.Name} is out of stock or has insufficient quantity.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // Get user's saved addresses
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefaultShipping)
                .ToListAsync();

            ViewBag.SavedAddresses = addresses;
            ViewBag.Cart = cart;

            // Create checkout view model with user info
            var user = await _context.Users.FindAsync(userId);

            var checkoutModel = new CheckoutViewModel
            {
                Email = user?.Email ?? "",
                CustomerName = user?.Name ?? user?.UserName ?? "",
                CustomerSurname = user?.Surname ?? "", // ✅ ADDED
                PhoneNumber = user?.PhoneNumber ?? ""
            };

            return View("~/Views/Account/Checkout.cshtml", checkoutModel);
        }

        // POST: Order/ProcessCheckout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload addresses for the view
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var addresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefaultShipping)
                    .ToListAsync();

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                ViewBag.SavedAddresses = addresses;
                ViewBag.Cart = cart;

                return View("~/Views/Account/Checkout.cshtml", model);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get user's cart
            var userCart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == currentUserId);

            if (userCart == null || !userCart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            // Calculate totals
            decimal subtotal = userCart.SubTotal;
            decimal shippingCost = await CalculateShippingAsync(subtotal, model);
            decimal tax = CalculateTax(subtotal);
            decimal totalAmount = subtotal + shippingCost + tax;

            // Generate unique order number
            string orderNumber = GenerateOrderNumber();

            // Create the order
            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = currentUserId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                Tax = tax,
                TotalAmount = totalAmount,

                // Shipping information
                ShippingAddress = model.ShippingAddress,
                ShippingCity = model.ShippingCity,
                ShippingState = model.ShippingState,
                ShippingPostalCode = model.ShippingPostalCode,

                // Billing information (use shipping if same)
                BillingAddress = model.SameAsShipping ? model.ShippingAddress : model.BillingAddress,
                BillingCity = model.SameAsShipping ? model.ShippingCity : model.BillingCity,
                BillingState = model.SameAsShipping ? model.ShippingState : model.BillingState,
                BillingPostalCode = model.SameAsShipping ? model.ShippingPostalCode : model.BillingPostalCode,

                // Contact information
                CustomerName = model.CustomerName,
                CustomerSurname = model.CustomerSurname, // ✅ ADDED (requires Order model field + migration)
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,

                // Payment information - BYPASSED FOR NOW
                PaymentMethod = "Payment Pending",
                PaymentStatus = "Pending",

                // Customer comments
                CustomerComments = model.OrderNotes,

                // Set estimated delivery (e.g., 5-7 business days)
                EstimatedDelivery = DateTime.Now.AddDays(7)
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Save to get Order ID

            // Create order items from cart items
            foreach (var cartItem in userCart.CartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.Name,
                    ProductSKU = cartItem.Product.SKU,
                    ImageUrl = cartItem.Product.ImageUrl,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    TotalPrice = cartItem.TotalPrice
                };

                _context.OrderItems.Add(orderItem);

                // Update product stock
                var product = await _context.Products.FindAsync(cartItem.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= cartItem.Quantity;
                    _context.Update(product);
                }
            }

            // Clear the cart
            _context.CartItems.RemoveRange(userCart.CartItems);

            // Save all changes
            await _context.SaveChangesAsync();

            // ============================================
            // EMAIL WILL BE SENT AFTER PAYMENT SUCCEEDS
            // ============================================
            // Email confirmation is now sent from PaymentController
            // after PayFast confirms payment via ITN

            // Optionally save the address if user checked "save address"
            if (model.SaveAddress)
            {
                var address = new Address
                {
                    UserId = currentUserId,
                    AddressType = "Shipping",
                    Label = "Saved from order",
                    FullName = $"{model.CustomerName} {model.CustomerSurname}".Trim(), // ✅ UPDATED
                    StreetAddress = model.ShippingAddress,
                    City = model.ShippingCity,
                    State = model.ShippingState,
                    PostalCode = model.ShippingPostalCode,
                    PhoneNumber = model.PhoneNumber,
                    IsDefaultShipping = false,
                    IsDefaultBilling = false
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();
            }

            // Redirect to payment processing
            return RedirectToAction("Process", "Payment", new { orderId = order.Id });
        }

        // GET: Order/OrderConfirmation
        public async Task<IActionResult> OrderConfirmation(string orderNumber)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View("~/Views/Account/OrderConfirmation.cshtml", order);
        }

        // Helper Methods

        /// <summary>
        /// Format: ORD-YYYYMMDD-XXXX (e.g. ORD-20260521-0001).
        ///
        /// The OLD implementation was:
        ///   var count = _context.Orders.Count(...) + 1;
        /// Two concurrent checkouts both read the same count and produce
        /// the same number, then one of the saves fails on the unique
        /// index we now have on OrderNumber.
        ///
        /// This version finds Max(sequence) + 1 inside a short retry loop
        /// and appends a 4-char random suffix on the final attempt as a
        /// last-resort dedupe. Combined with the DB unique index, a
        /// duplicate is impossible.
        /// </summary>
        private string GenerateOrderNumber()
        {
            const int maxAttempts = 5;
            var today = DateTime.Now.ToString("yyyyMMdd");
            var prefix = $"ORD-{today}-";

            // Highest sequence used today.
            var maxToday = _context.Orders
                .Where(o => o.OrderDate.Date == DateTime.Today)
                .Select(o => o.OrderNumber)
                .Where(n => n.StartsWith(prefix))
                .ToList()
                .Select(n =>
                {
                    var tail = n.Substring(prefix.Length);
                    var seqPart = tail.Length >= 4 ? tail.Substring(0, 4) : tail;
                    return int.TryParse(seqPart, out var v) ? v : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var seq = maxToday + 1 + attempt;
                var candidate = $"{prefix}{seq:D4}";

                if (attempt == maxAttempts - 1)
                {
                    // Final attempt — append a short random suffix.
                    candidate += "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
                }

                if (!_context.Orders.Any(o => o.OrderNumber == candidate))
                {
                    return candidate;
                }
            }

            // Unreachable, but keeps the compiler happy.
            return $"{prefix}{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}";
        }

        /// <summary>
        /// Computes the shipping cost for an order at checkout time.
        ///
        /// Reads <see cref="ShippingSettings"/> from the database (single row,
        /// admin-editable). If the subtotal meets the free-shipping threshold,
        /// returns zero. Otherwise, if <c>UseLiveTcgRates</c> is on AND a
        /// delivery address is supplied, asks The Courier Guy for a live
        /// door-to-door quote and adds the configured markup. If TCG fails
        /// for any reason — no API key, network error, etc. — falls back to
        /// the flat <c>DoorToDoorRate</c> so the customer can still check out.
        /// </summary>
        private async Task<decimal> CalculateShippingAsync(decimal subtotal, CheckoutViewModel? deliveryAddress = null)
        {
            // Load settings or fall back to defaults if no row exists yet.
            var settings = await _context.ShippingSettings
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync()
                ?? new ShippingSettings();

            if (subtotal >= settings.FreeShippingThreshold)
            {
                return 0m;
            }

            // Without an address we can't ask TCG for a live quote, so use the
            // flat rate. Same fallback when live rates are disabled.
            if (!settings.UseLiveTcgRates || deliveryAddress is null)
            {
                return settings.DoorToDoorRate;
            }

            try
            {
                var collection = ShippingAddressHelper.GetDispatchAddress(_configuration, _logger);
                var delivery = new TcgAddress
                {
                    Type = "residential",
                    StreetAddress = deliveryAddress.ShippingAddress,
                    LocalArea = deliveryAddress.ShippingCity, // best-effort
                    City = deliveryAddress.ShippingCity,
                    Zone = deliveryAddress.ShippingState,
                    Country = "ZA",
                    Code = deliveryAddress.ShippingPostalCode
                };

                var parcels = new List<TcgParcel> { new TcgParcel() };

                var result = await _courierGuy.GetRatesAsync(collection, delivery, parcels, declaredValue: subtotal);

                if (result.Success && result.Data is { Count: > 0 })
                {
                    var cheapest = result.Data.OrderBy(r => r.Rate).First();
                    return cheapest.Rate + settings.MarkupRand;
                }

                _logger.LogWarning(
                    "Live TCG rate lookup returned no results — falling back to flat rate. Error: {Err}",
                    result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Live TCG rate lookup threw — falling back to flat rate.");
            }

            return settings.DoorToDoorRate;
        }

        private decimal CalculateTax(decimal subtotal)
        {
            // 15% VAT (South African tax)
            return subtotal * 0.15m;
        }
    }

    // Checkout ViewModel
    public class CheckoutViewModel
    {
        [Required]
        [Display(Name = "Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Surname")]
        public string CustomerSurname { get; set; } = string.Empty; // ✅ ADDED

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Street Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        public string ShippingCity { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Province/State")]
        public string ShippingState { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Postal Code")]
        public string ShippingPostalCode { get; set; } = string.Empty;

        // Billing Address (optional if same as shipping)
        public bool SameAsShipping { get; set; } = true;

        [Display(Name = "Billing Address")]
        public string? BillingAddress { get; set; }

        [Display(Name = "Billing City")]
        public string? BillingCity { get; set; }

        [Display(Name = "Billing Province/State")]
        public string? BillingState { get; set; }

        [Display(Name = "Billing Postal Code")]
        public string? BillingPostalCode { get; set; }

        // Additional options
        public bool SaveAddress { get; set; } = false;

        [Display(Name = "Order Notes (Optional)")]
        [StringLength(500)]
        public string? OrderNotes { get; set; }
    }
}
