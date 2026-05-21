using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    [Authorize] // All actions require login
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
        }

        // ============================================
        // DASHBOARD - Overview/Home Page
        // ============================================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Get user's recent orders
            var recentOrders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // Get wishlist count - FIXED
            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistItems)
                .FirstOrDefaultAsync(w => w.UserId == user.Id);
            var wishlistCount = wishlist?.WishlistItems?.Count ?? 0;

            // Get addresses count
            var addressCount = await _context.Addresses
                .CountAsync(a => a.UserId == user.Id);

            ViewBag.User = user;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.WishlistCount = wishlistCount;
            ViewBag.AddressCount = addressCount;
            ViewBag.TotalOrders = await _context.Orders.CountAsync(o => o.UserId == user.Id);

            return View();
        }

        // ============================================
        // PROFILE - View Profile
        // ============================================
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View(user);
        }

        // ============================================
        // EDIT PROFILE - Edit Profile Information
        // ============================================
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Update user properties
            user.Name = model.Name;
            user.Surname = model.Surname; // ✅ ADDED: Save surname field
            user.PhoneNumber = model.PhoneNumber;
            user.StreetAddress = model.StreetAddress;
            user.City = model.City;
            user.State = model.State;
            user.PostalCode = model.PostalCode;
            user.EmailNotifications = model.EmailNotifications;
            user.SMSNotifications = model.SMSNotifications;
            user.PromotionalEmails = model.PromotionalEmails;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(user);
        }

        // ============================================
        // CHANGE PASSWORD
        // ============================================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "New password and confirmation password do not match.");
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Password changed successfully!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View();
        }

        // ============================================
        // ORDER HISTORY - List All Orders
        // ============================================
        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ============================================
        // ORDER DETAILS - View Single Order
        // ============================================
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // ============================================
        // ADDRESSES - List All Addresses
        // ============================================
        public async Task<IActionResult> Addresses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var addresses = await _context.Addresses
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.IsDefaultShipping)
                .ThenByDescending(a => a.IsDefaultBilling)
                .ToListAsync();

            return View(addresses);
        }

        // ============================================
        // ADD ADDRESS
        // ============================================
        [HttpGet]
        public IActionResult AddAddress()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(Address model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (ModelState.IsValid)
            {
                model.UserId = user.Id;

                // If this is set as default, remove default from other addresses
                if (model.IsDefaultShipping)
                {
                    var otherAddresses = await _context.Addresses
                        .Where(a => a.UserId == user.Id && a.IsDefaultShipping)
                        .ToListAsync();
                    foreach (var addr in otherAddresses)
                    {
                        addr.IsDefaultShipping = false;
                    }
                }

                if (model.IsDefaultBilling)
                {
                    var otherAddresses = await _context.Addresses
                        .Where(a => a.UserId == user.Id && a.IsDefaultBilling)
                        .ToListAsync();
                    foreach (var addr in otherAddresses)
                    {
                        addr.IsDefaultBilling = false;
                    }
                }

                _context.Addresses.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Address added successfully!";
                return RedirectToAction("Addresses");
            }

            return View(model);
        }

        // ============================================
        // EDIT ADDRESS
        // ============================================
        [HttpGet]
        public async Task<IActionResult> EditAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (address == null)
            {
                return NotFound();
            }

            return View(address);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(Address model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (ModelState.IsValid)
            {
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == user.Id);

                if (address == null)
                {
                    return NotFound();
                }

                // Update address properties
                address.AddressType = model.AddressType;
                address.Label = model.Label;
                address.FullName = model.FullName;
                address.StreetAddress = model.StreetAddress;
                address.ApartmentUnit = model.ApartmentUnit;
                address.City = model.City;
                address.State = model.State;
                address.PostalCode = model.PostalCode;
                address.Country = model.Country;
                address.PhoneNumber = model.PhoneNumber;
                address.DeliveryInstructions = model.DeliveryInstructions;

                // Handle default flags
                if (model.IsDefaultShipping && !address.IsDefaultShipping)
                {
                    var otherAddresses = await _context.Addresses
                        .Where(a => a.UserId == user.Id && a.Id != address.Id && a.IsDefaultShipping)
                        .ToListAsync();
                    foreach (var addr in otherAddresses)
                    {
                        addr.IsDefaultShipping = false;
                    }
                    address.IsDefaultShipping = true;
                }
                else if (!model.IsDefaultShipping)
                {
                    address.IsDefaultShipping = false;
                }

                if (model.IsDefaultBilling && !address.IsDefaultBilling)
                {
                    var otherAddresses = await _context.Addresses
                        .Where(a => a.UserId == user.Id && a.Id != address.Id && a.IsDefaultBilling)
                        .ToListAsync();
                    foreach (var addr in otherAddresses)
                    {
                        addr.IsDefaultBilling = false;
                    }
                    address.IsDefaultBilling = true;
                }
                else if (!model.IsDefaultBilling)
                {
                    address.IsDefaultBilling = false;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Address updated successfully!";
                return RedirectToAction("Addresses");
            }

            return View(model);
        }

        // ============================================
        // DELETE ADDRESS
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (address == null)
            {
                return NotFound();
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Address deleted successfully!";
            return RedirectToAction("Addresses");
        }

        // ============================================
        // SETTINGS - Account Settings
        // ============================================
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            user.EmailNotifications = model.EmailNotifications;
            user.SMSNotifications = model.SMSNotifications;
            user.PromotionalEmails = model.PromotionalEmails;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Settings updated successfully!";
                return RedirectToAction("Settings");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(user);
        }

        // ============================================
        // RETURNS - My Returns List
        // ============================================
        public async Task<IActionResult> MyReturns()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var returns = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(returns);
        }

        // ============================================
        // REQUEST RETURN - Show Form
        // ============================================
        [HttpGet]
        public async Task<IActionResult> RequestReturn()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Get user's orders from the last 30 days (return window)
            var eligibleOrders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id &&
                           o.OrderDate >= DateTime.Now.AddDays(-30) &&
                           (o.Status == "Delivered" || o.Status == "Shipped"))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Orders = eligibleOrders;

            // Create a JSON object with order items for JavaScript
            var orderItemsDict = new Dictionary<int, object>();
            foreach (var order in eligibleOrders)
            {
                orderItemsDict[order.Id] = order.OrderItems.Select(oi => new
                {
                    id = oi.Id,
                    productName = oi.ProductName,
                    productSKU = oi.ProductSKU,
                    quantity = oi.Quantity,
                    totalPrice = oi.TotalPrice,
                    imageUrl = oi.ImageUrl ?? "/images/no-image.jpg"
                }).ToList();
            }

            ViewBag.OrderItemsJson = Newtonsoft.Json.JsonConvert.SerializeObject(orderItemsDict);

            return View(new ReturnRequest());
        }

        // ============================================
        // REQUEST RETURN - Submit Form
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReturn(ReturnRequest model, IFormFile Photo1, IFormFile Photo2, IFormFile Photo3)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // REMOVE validation errors for fields that are set by controller, not form
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Order");
            ModelState.Remove("OrderItem");
            ModelState.Remove("Status");
            ModelState.Remove("AdminNotes");
            ModelState.Remove("TrackingNumber");
            ModelState.Remove("ReturnAuthorizationNumber");
            ModelState.Remove("PhotoPath1");
            ModelState.Remove("PhotoPath2");
            ModelState.Remove("PhotoPath3");
            ModelState.Remove("DetailedReason");
            ModelState.Remove("Photo1");
            ModelState.Remove("Photo2");
            ModelState.Remove("Photo3");

            if (ModelState.IsValid)
            {
                // Verify the order belongs to the user
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == model.OrderId && o.UserId == user.Id);

                if (order == null)
                {
                    ModelState.AddModelError("", "Invalid order selected.");

                    // Reload form data
                    var reloadOrders = await _context.Orders
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Product)
                        .Where(o => o.UserId == user.Id &&
                                   o.OrderDate >= DateTime.Now.AddDays(-30) &&
                                   (o.Status == "Delivered" || o.Status == "Shipped"))
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync();

                    ViewBag.Orders = reloadOrders;

                    var reloadDict = new Dictionary<int, object>();
                    foreach (var ord in reloadOrders)
                    {
                        reloadDict[ord.Id] = ord.OrderItems.Select(oi => new
                        {
                            id = oi.Id,
                            productName = oi.ProductName,
                            productSKU = oi.ProductSKU,
                            quantity = oi.Quantity,
                            totalPrice = oi.TotalPrice,
                            imageUrl = oi.ImageUrl ?? "/images/no-image.jpg"
                        }).ToList();
                    }
                    ViewBag.OrderItemsJson = Newtonsoft.Json.JsonConvert.SerializeObject(reloadDict);

                    return View(model);
                }

                // Verify the order item belongs to the order
                var orderItem = order.OrderItems.FirstOrDefault(oi => oi.Id == model.OrderItemId);
                if (orderItem == null)
                {
                    ModelState.AddModelError("", "Invalid item selected.");

                    // Reload form data
                    var reloadOrders = await _context.Orders
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Product)
                        .Where(o => o.UserId == user.Id &&
                                   o.OrderDate >= DateTime.Now.AddDays(-30) &&
                                   (o.Status == "Delivered" || o.Status == "Shipped"))
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync();

                    ViewBag.Orders = reloadOrders;

                    var reloadDict = new Dictionary<int, object>();
                    foreach (var ord in reloadOrders)
                    {
                        reloadDict[ord.Id] = ord.OrderItems.Select(oi => new
                        {
                            id = oi.Id,
                            productName = oi.ProductName,
                            productSKU = oi.ProductSKU,
                            quantity = oi.Quantity,
                            totalPrice = oi.TotalPrice,
                            imageUrl = oi.ImageUrl ?? "/images/no-image.jpg"
                        }).ToList();
                    }
                    ViewBag.OrderItemsJson = Newtonsoft.Json.JsonConvert.SerializeObject(reloadDict);

                    return View(model);
                }

                // Set return request properties
                model.UserId = user.Id;
                model.Status = "Pending";
                model.RequestDate = DateTime.Now;
                model.RefundAmount = orderItem.TotalPrice;
                model.IsRefundIssued = false;

                // Handle photo uploads (optional - implement file upload logic here)
                // For now, we'll skip the actual file upload implementation

                _context.ReturnRequests.Add(model);
                await _context.SaveChangesAsync();

                // Email the customer a confirmation. Best-effort — never block
                // the return submission on a mail failure.
                try
                {
                    await _emailService.SendReturnRequestConfirmationAsync(model, user);
                }
                catch
                {
                    // Already logged inside EmailService; swallow here.
                }

                TempData["SuccessMessage"] = $"Return request submitted successfully! Your request ID is #{model.Id}. You'll receive an email with further instructions.";
                return RedirectToAction("MyReturns");
            }

            // If we got here, something failed, reload the form
            var eligibleOrders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id &&
                           o.OrderDate >= DateTime.Now.AddDays(-30) &&
                           (o.Status == "Delivered" || o.Status == "Shipped"))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Orders = eligibleOrders;

            var orderItemsDict = new Dictionary<int, object>();
            foreach (var order in eligibleOrders)
            {
                orderItemsDict[order.Id] = order.OrderItems.Select(oi => new
                {
                    id = oi.Id,
                    productName = oi.ProductName,
                    productSKU = oi.ProductSKU,
                    quantity = oi.Quantity,
                    totalPrice = oi.TotalPrice,
                    imageUrl = oi.ImageUrl ?? "/images/no-image.jpg"
                }).ToList();
            }

            ViewBag.OrderItemsJson = Newtonsoft.Json.JsonConvert.SerializeObject(orderItemsDict);

            return View(model);
        }

        // ============================================
        // RETURN DETAILS - View Single Return
        // ============================================
        public async Task<IActionResult> ReturnDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var returnRequest = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

            if (returnRequest == null)
            {
                return NotFound();
            }

            return View(returnRequest);
        }

        // ============================================
        // REORDER — clone a past order's items into the user's active cart.
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Index", "Login");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order is null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(Orders));
            }

            // Find or create the user's cart.
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart is null)
            {
                cart = new Cart
                {
                    UserId = user.Id,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            int added = 0;
            int skippedOutOfStock = 0;
            int skippedMissing = 0;

            foreach (var item in order.OrderItems)
            {
                // Pull the live product so we use today's price + stock, not the
                // historical values captured on the OrderItem snapshot.
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product is null)
                {
                    skippedMissing++;
                    continue;
                }

                if (!product.InStock || product.StockQuantity <= 0)
                {
                    skippedOutOfStock++;
                    continue;
                }

                // Cap the requested quantity at currently-available stock.
                var requested = Math.Min(item.Quantity, product.StockQuantity);

                var existing = cart.CartItems.FirstOrDefault(ci => ci.ProductId == product.Id);
                if (existing is not null)
                {
                    var newTotal = existing.Quantity + requested;
                    existing.Quantity = Math.Min(newTotal, product.StockQuantity);
                }
                else
                {
                    cart.CartItems.Add(new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = product.Id,
                        Quantity = requested,
                        UnitPrice = product.Price,
                        AddedDate = DateTime.Now
                    });
                }
                added++;
            }

            cart.LastModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // Build a friendly summary message.
            var parts = new List<string> { $"Added {added} item(s) to your cart." };
            if (skippedOutOfStock > 0) parts.Add($"{skippedOutOfStock} were out of stock.");
            if (skippedMissing > 0)    parts.Add($"{skippedMissing} are no longer available.");
            TempData["SuccessMessage"] = string.Join(" ", parts);

            return RedirectToAction("Index", "Cart");
        }

        // ============================================
        // INVOICE — printable HTML invoice. The browser's Print → Save as PDF
        // does the PDF generation; no extra dependency required.
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Index", "Login");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order is null) return NotFound();

            return View("Invoice", order);
        }

        // ============================================
        // DELETE ACCOUNT — soft-delete with PII anonymisation.
        // We KEEP the user row and all Orders/OrderItems for audit / tax
        // retention. We REMOVE personally-identifiable data so the customer's
        // right to be forgotten is honoured for the parts we don't legally
        // need to keep.
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(string confirmEmail)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Index", "Login");

            // Belt and braces — make the customer retype their email to confirm.
            if (!string.Equals(confirmEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Email confirmation did not match. Account was NOT deleted.";
                return RedirectToAction(nameof(Settings));
            }

            var deletedTag = Guid.NewGuid().ToString("N").Substring(0, 8);

            // 1) Anonymise the user row itself.
            //    Email/UserName are renamed so they can't sign back in and
            //    the address can be reused for a fresh sign-up.
            user.IsDeleted = true;
            user.DeactivatedDate = DateTime.Now;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.Email = $"deleted-{deletedTag}@deleted.local";
            user.NormalizedEmail = user.Email.ToUpperInvariant();
            user.UserName = user.Email;
            user.NormalizedUserName = user.Email.ToUpperInvariant();
            user.EmailConfirmed = false;
            user.Name = "Deleted";
            user.Surname = "User";
            user.PhoneNumber = null;
            user.StreetAddress = "";
            user.City = "";
            user.State = "";
            user.PostalCode = "";
            user.ProfilePictureUrl = null;
            user.EmailNotifications = false;
            user.SMSNotifications = false;
            user.PromotionalEmails = false;
            // Invalidate any existing auth cookies / tokens.
            await _userManager.UpdateSecurityStampAsync(user);
            // Force the password to a random unknowable value.
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, resetToken, Guid.NewGuid().ToString("N") + "Aa1!");
            await _userManager.UpdateAsync(user);

            // 2) Anonymise saved addresses — these are PII we don't need to keep.
            //    Order rows preserve a snapshot of the shipping address on the
            //    Order itself, so deleting addresses here doesn't lose audit data.
            var addresses = await _context.Addresses.Where(a => a.UserId == user.Id).ToListAsync();
            _context.Addresses.RemoveRange(addresses);

            // 3) Wishlists are pure personal preference — no audit value.
            var wishlists = await _context.Wishlists
                .Include(w => w.WishlistItems)
                .Where(w => w.UserId == user.Id)
                .ToListAsync();
            foreach (var w in wishlists)
            {
                _context.WishlistItems.RemoveRange(w.WishlistItems);
            }
            _context.Wishlists.RemoveRange(wishlists);

            // 4) Carts — same as wishlists, no audit value.
            var carts = await _context.Carts
                .Include(c => c.CartItems)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();
            foreach (var c in carts)
            {
                _context.CartItems.RemoveRange(c.CartItems);
            }
            _context.Carts.RemoveRange(carts);

            await _context.SaveChangesAsync();

            await _signInManager.SignOutAsync();

            TempData["SuccessMessage"] = "Your account has been deleted. Order history is retained for audit purposes only.";
            return RedirectToAction("Index", "Home");
        }

        // ============================================
        // DOWNLOAD MY DATA — POPIA-compliant data export. Streams a JSON file
        // with the user's profile, addresses, orders, order items, returns.
        // ============================================
        [HttpGet]
        public async Task<IActionResult> DownloadMyData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Index", "Login");

            var data = new
            {
                ExportedAt = DateTime.UtcNow,
                Profile = new
                {
                    user.Id,
                    user.Email,
                    user.Name,
                    user.Surname,
                    user.PhoneNumber,
                    user.StreetAddress,
                    user.City,
                    user.State,
                    user.PostalCode,
                    user.DateJoined,
                    user.LastLoginDate,
                    user.EmailNotifications,
                    user.SMSNotifications,
                    user.PromotionalEmails
                },
                Addresses = await _context.Addresses
                    .Where(a => a.UserId == user.Id)
                    .Select(a => new
                    {
                        a.Label,
                        a.AddressType,
                        a.FullName,
                        a.StreetAddress,
                        a.ApartmentUnit,
                        a.City,
                        a.State,
                        a.PostalCode,
                        a.Country,
                        a.PhoneNumber,
                        a.IsDefaultShipping,
                        a.IsDefaultBilling
                    })
                    .ToListAsync(),
                Orders = await _context.Orders
                    .Where(o => o.UserId == user.Id)
                    .Include(o => o.OrderItems)
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new
                    {
                        o.OrderNumber,
                        o.OrderDate,
                        o.Status,
                        o.PaymentStatus,
                        o.Subtotal,
                        o.ShippingCost,
                        o.Tax,
                        o.TotalAmount,
                        o.ShippingAddress,
                        o.ShippingCity,
                        o.ShippingState,
                        o.ShippingPostalCode,
                        o.TrackingNumber,
                        o.Carrier,
                        Items = o.OrderItems.Select(oi => new
                        {
                            oi.ProductName,
                            oi.ProductSKU,
                            oi.Quantity,
                            oi.UnitPrice,
                            oi.TotalPrice
                        })
                    })
                    .ToListAsync(),
                Returns = await _context.ReturnRequests
                    .Where(r => r.UserId == user.Id)
                    .OrderByDescending(r => r.RequestDate)
                    .Select(r => new
                    {
                        r.RequestDate,
                        r.Status,
                        r.ReturnReason,
                        r.DetailedReason,
                        r.RefundAmount,
                        r.IsRefundIssued
                    })
                    .ToListAsync()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var filename = $"shumoshop-my-data-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            return File(bytes, "application/json", filename);
        }
    }
}