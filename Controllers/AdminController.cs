using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin,Dev")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // ============================================
        // ADMIN DASHBOARD
        // ============================================
        public async Task<IActionResult> Index()
        {
            // Get statistics - with null safety
            var totalOrders = 0;
            var pendingOrders = 0;
            var totalRevenue = 0m;
            var pendingReturns = 0;
            var totalReturns = 0;

            // Check if Orders table exists
            try
            {
                totalOrders = await _context.Orders.CountAsync();
                pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
                totalRevenue = await _context.Orders
                    .Where(o => o.Status == "Delivered")
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            }
            catch
            {
                // Orders table doesn't exist yet
            }

            // Check if ReturnRequests table exists
            try
            {
                pendingReturns = await _context.ReturnRequests.CountAsync(r => r.Status == "Pending");
                totalReturns = await _context.ReturnRequests.CountAsync();
            }
            catch
            {
                // ReturnRequests table doesn't exist yet
            }

            var totalUsers = await _context.Users.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();

            // Low-stock count for the dashboard banner.
            int lowStockCount = 0;
            int lowStockThreshold = 10;
            try
            {
                var ss = await _context.ShippingSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
                if (ss != null) lowStockThreshold = ss.LowStockThreshold;
                lowStockCount = await _context.Products
                    .CountAsync(p => p.StockQuantity <= lowStockThreshold);
            }
            catch
            {
                // ShippingSettings table not migrated yet — silently skip.
            }
            ViewBag.LowStockCount = lowStockCount;
            ViewBag.LowStockThreshold = lowStockThreshold;

            // Recent orders - with null safety
            var recentOrders = new List<Order>();
            try
            {
                recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync();
            }
            catch
            {
                // Orders table doesn't exist yet
            }

            // Recent returns - with null safety
            var recentReturns = new List<ReturnRequest>();
            try
            {
                recentReturns = await _context.ReturnRequests
                    .Include(r => r.User)
                    .Include(r => r.Order)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(10)
                    .ToListAsync();
            }
            catch
            {
                // ReturnRequests table doesn't exist yet
            }

            ViewBag.TotalOrders = totalOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PendingReturns = pendingReturns;
            ViewBag.TotalReturns = totalReturns;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalCategories = totalCategories;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.RecentReturns = recentReturns;

            return View();
        }

        // ============================================
        // DASHBOARD ACTION (Alternative route)
        // ============================================
        public async Task<IActionResult> Dashboard()
        {
            return await Index();
        }

        // ============================================
        // ORDERS MANAGEMENT - List All Orders
        // ============================================
        public async Task<IActionResult> Orders(string status = "")
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            return View(orders);
        }

        // ============================================
        // ORDER DETAILS - View Single Order
        // ============================================
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // ============================================
        // UPDATE ORDER STATUS
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status, string carrier = "", string trackingNumber = "", string trackingLink = "")
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;

            // Update carrier if provided
            if (!string.IsNullOrEmpty(carrier))
            {
                order.Carrier = carrier;
            }

            // Update tracking number if provided
            if (!string.IsNullOrEmpty(trackingNumber))
            {
                order.TrackingNumber = trackingNumber;

                // Auto-generate tracking link based on carrier
                if (!string.IsNullOrEmpty(carrier) && carrier != "Custom")
                {
                    order.TrackingLink = GenerateTrackingUrl(carrier, trackingNumber);
                }
            }

            // If custom carrier, use the provided tracking link
            if (carrier == "Custom" && !string.IsNullOrEmpty(trackingLink))
            {
                order.TrackingLink = trackingLink;
            }

            // Update dates based on status
            if (status == "Shipped")
            {
                order.ShippedDate = DateTime.Now;
            }
            else if (status == "Delivered")
            {
                order.DeliveredDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // ============================================
            // SEND EMAIL NOTIFICATIONS
            // ============================================
            try
            {
                var user = await _context.Users.FindAsync(order.UserId);
                if (user != null)
                {
                    if (status == "Shipped")
                    {
                        await _emailService.SendShippingNotificationAsync(order, user);
                    }
                    else if (status == "Delivered")
                    {
                        await _emailService.SendDeliveryConfirmationAsync(order, user);
                    }
                    else if (status == "Cancelled")
                    {
                        await _emailService.SendOrderCancellationAsync(order, user);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't break the flow
                Console.WriteLine($"Failed to send status update email: {ex.Message}");
            }

            TempData["SuccessMessage"] = $"Order #{order.OrderNumber} status updated to {status}";
            return RedirectToAction("OrderDetails", new { id = id });
        }


        // ============================================
        // HELPER: GENERATE TRACKING URL
        // ============================================
        private string GenerateTrackingUrl(string carrier, string trackingNumber)
        {
            return carrier switch
            {
                "The Courier Guy" => $"https://www.thecourierguy.co.za/track-your-parcel?tracking_number={trackingNumber}",
                "DHL" => $"https://www.dhl.com/za-en/home/tracking/tracking-express.html?submit=1&tracking-id={trackingNumber}",
                "Aramex" => $"https://www.aramex.com/track/results?ShipmentNumber={trackingNumber}",
                "FedEx" => $"https://www.fedex.com/fedextrack/?trknbr={trackingNumber}",
                "RAM" => $"https://www.ram.co.za/track-trace?waybill={trackingNumber}",
                "Dawn Wing" => $"https://www.dawnwing.co.za/track-trace?waybill={trackingNumber}",
                _ => "" // Return empty for unknown carriers
            };
        }

        // ============================================
        // RETURNS MANAGEMENT - List All Returns
        // ============================================
        public async Task<IActionResult> Returns(string status = "")
        {
            var query = _context.ReturnRequests
                .Include(r => r.User)
                .Include(r => r.Order)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var returns = await query
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            return View(returns);
        }

        // ============================================
        // RETURN DETAILS - View Single Return
        // ============================================
        public async Task<IActionResult> ReturnDetails(int id)
        {
            var returnRequest = await _context.ReturnRequests
                .Include(r => r.User)
                .Include(r => r.Order)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (returnRequest == null)
            {
                return NotFound();
            }

            return View(returnRequest);
        }

        // ============================================
        // APPROVE RETURN
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReturn(int id, string adminNotes = "")
        {
            var returnRequest = await _context.ReturnRequests.FindAsync(id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            returnRequest.Status = "Approved";
            returnRequest.ApprovedDate = DateTime.Now;
            returnRequest.AdminNotes = adminNotes;

            // Generate Return Authorization Number
            returnRequest.ReturnAuthorizationNumber = $"RAN-{DateTime.Now:yyyyMMdd}-{id:D6}";

            await _context.SaveChangesAsync();

            // Email the customer that their return has been approved.
            try
            {
                var customer = await _userManager.FindByIdAsync(returnRequest.UserId);
                if (customer != null)
                {
                    await _emailService.SendReturnApprovalAsync(returnRequest, customer);
                }
            }
            catch
            {
                // EmailService logs failures internally.
            }

            TempData["SuccessMessage"] = $"Return request #{id} approved. RAN: {returnRequest.ReturnAuthorizationNumber}";
            return RedirectToAction("ReturnDetails", new { id = id });
        }

        // ============================================
        // REJECT RETURN
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReturn(int id, string adminNotes)
        {
            var returnRequest = await _context.ReturnRequests.FindAsync(id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            returnRequest.Status = "Rejected";
            returnRequest.AdminNotes = adminNotes;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Return request #{id} rejected";
            return RedirectToAction("ReturnDetails", new { id = id });
        }

        // ============================================
        // MARK RETURN AS RECEIVED
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReturnReceived(int id)
        {
            var returnRequest = await _context.ReturnRequests.FindAsync(id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            returnRequest.Status = "Received";
            returnRequest.ReceivedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Return request #{id} marked as received";
            return RedirectToAction("ReturnDetails", new { id = id });
        }

        // ============================================
        // ISSUE REFUND
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueRefund(int id)
        {
            var returnRequest = await _context.ReturnRequests.FindAsync(id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            returnRequest.Status = "Refunded";
            returnRequest.RefundedDate = DateTime.Now;
            returnRequest.IsRefundIssued = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Refund of R {returnRequest.RefundAmount:N2} issued for return #{id}";
            return RedirectToAction("ReturnDetails", new { id = id });
        }

        // ============================================
        // USERS MANAGEMENT - List All Users
        // ============================================
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.DateJoined)
                .ToListAsync();

            // Get order counts for each user
            var userOrderCounts = new Dictionary<string, int>();
            try
            {
                userOrderCounts = await _context.Orders
                    .GroupBy(o => o.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count);
            }
            catch
            {
                // Orders table doesn't exist yet
            }

            ViewBag.UserOrderCounts = userOrderCounts;

            return View(users);
        }

        // ============================================
        // USER DETAILS - View Single User
        // ============================================
        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Get user's current role
            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = roles.FirstOrDefault();

            // Get current logged-in user's role
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.CurrentUserRole = currentUserRoles.FirstOrDefault();

            var orders = new List<Order>();
            var returns = new List<ReturnRequest>();

            try
            {
                orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.UserId == id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            catch
            {
                // Orders table doesn't exist yet
            }

            try
            {
                returns = await _context.ReturnRequests
                    .Include(r => r.Order)
                    .Include(r => r.OrderItem)
                    .Where(r => r.UserId == id)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();
            }
            catch
            {
                // ReturnRequests table doesn't exist yet
            }

            ViewBag.User = user;
            ViewBag.Orders = orders;
            ViewBag.Returns = returns;

            return View();
        }

        // ============================================
        // UPDATE USER ROLE
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string userId, string newRole)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newRole))
            {
                TempData["ErrorMessage"] = "Invalid request";
                return RedirectToAction("UserDetails", new { id = userId });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction("Users");
            }

            // Get current roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove all current roles
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Add new role
            var result = await _userManager.AddToRoleAsync(user, newRole);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User role updated to {newRole} successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update user role";
            }

            return RedirectToAction("UserDetails", new { id = userId });
        }

        // ============================================
        // LOW STOCK REPORT
        // Lists every product whose StockQuantity <= LowStockThreshold from
        // ShippingSettings (defaults to 10).
        // ============================================
        [HttpGet]
        public async Task<IActionResult> LowStock()
        {
            var settings = await _context.ShippingSettings.OrderBy(s => s.Id).FirstOrDefaultAsync()
                           ?? new ShippingSettings();

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Where(p => p.StockQuantity <= settings.LowStockThreshold)
                .OrderBy(p => p.StockQuantity)
                .ThenBy(p => p.Name)
                .ToListAsync();

            ViewBag.Threshold = settings.LowStockThreshold;
            return View(products);
        }

        // ============================================
        // SHIPPING SETTINGS (single row, admin-editable)
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ShippingSettings()
        {
            var settings = await _context.ShippingSettings
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync();

            if (settings is null)
            {
                // Lazy-init: create the row on first visit using model defaults.
                settings = new ShippingSettings();
                _context.ShippingSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShippingSettings(ShippingSettings model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var settings = await _context.ShippingSettings
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync();

            if (settings is null)
            {
                settings = new ShippingSettings();
                _context.ShippingSettings.Add(settings);
            }

            settings.UseLiveTcgRates = model.UseLiveTcgRates;
            settings.DoorToDoorRate = model.DoorToDoorRate;
            settings.DoorToLockerRate = model.DoorToLockerRate;
            settings.DoorToKioskRate = model.DoorToKioskRate;
            settings.FreeShippingThreshold = model.FreeShippingThreshold;
            settings.MarkupRand = model.MarkupRand;
            settings.LowStockThreshold = model.LowStockThreshold;
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedByUserId = _userManager.GetUserId(User);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Shipping settings saved.";
            return RedirectToAction(nameof(ShippingSettings));
        }
    }
}