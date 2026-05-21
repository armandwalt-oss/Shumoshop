using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    public class OrderTrackingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderTrackingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: OrderTracking
        public ActionResult Index()
        {
            return View();
        }

        // POST: OrderTracking/Track
        [HttpPost]
        public async Task<ActionResult> Track(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
            {
                TempData["ErrorMessage"] = "Please enter an order number.";
                return RedirectToAction("Index");
            }

            // Search in the Orders table (not OrderTrackings)
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderNumber.ToLower() == orderNumber.ToLower());

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found. Please check your order number.";
                return RedirectToAction("Index");
            }

            return View("TrackResult", order);
        }
    }
}