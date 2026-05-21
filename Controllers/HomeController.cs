using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string category = null)
        {
            // Get all categories from the Categories table with their subcategories
            var categories = await _context.Categories
                .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Get Special Offers (Products marked as special) - FIRST
            var specialOffers = await _context.Products
                .Where(p => p.InStock && p.IsSpecial)
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Get New Arrivals (Products marked as new arrivals) - SECOND
            var newArrivals = await _context.Products
                .Where(p => p.InStock && p.IsNewArrival)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            // Get Featured Products (Top products) - THIRD
            var featuredProducts = await _context.Products
                .Where(p => p.InStock)
                .OrderByDescending(p => p.IsSpecial)
                .ThenByDescending(p => p.IsNewArrival)
                .ThenBy(p => p.Name)
                .Take(12)
                .ToListAsync();

            // Get Products by Category for Dynamic Carousels
            var categoryProducts = new Dictionary<string, List<Product>>();

            foreach (var cat in categories)
            {
                var products = await _context.Products
                    .Where(p => p.InStock && p.Category.Name == cat.Name)
                    .OrderBy(p => p.Name)
                    .Take(12) // Limit to 12 products per category carousel
                    .ToListAsync();

                if (products.Any())
                {
                    categoryProducts[cat.Name] = products;
                }
            }

            // Pass data to view
            ViewBag.SelectedCategory = category;
            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.SpecialOffers = specialOffers;
            ViewBag.NewArrivals = newArrivals;
            ViewBag.CategoryProducts = categoryProducts; // NEW: For dynamic carousels

            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}