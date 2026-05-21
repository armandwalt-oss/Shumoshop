using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Shop
        public async Task<IActionResult> Index(
            string category = null,
            string subCategory = null,
            string search = null,
            string sort = "name_asc",
            int page = 1,
            int pageSize = 24)
        {
            return View(await BuildListingVm(
                _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.SubCategory)
                    .AsQueryable(),
                pageTitle: "Shop Our Products",
                category: category,
                subCategory: subCategory,
                search: search,
                sort: sort,
                page: page,
                pageSize: pageSize));
        }

        // GET: /Shop/NewArrivals
        public async Task<IActionResult> NewArrivals(
            string category = null,
            string subCategory = null,
            string search = null,
            string sort = "newest",
            int page = 1,
            int pageSize = 24)
        {
            var baseQuery = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Where(p => p.IsNewArrival && p.InStock)
                .AsQueryable();

            return View("Index", await BuildListingVm(
                baseQuery,
                pageTitle: "New Arrivals",
                category: category,
                subCategory: subCategory,
                search: search,
                sort: sort,
                page: page,
                pageSize: pageSize));
        }

        // GET: /Shop/Featured
        public async Task<IActionResult> Featured(
            string category = null,
            string subCategory = null,
            string search = null,
            string sort = "name_asc",
            int page = 1,
            int pageSize = 24)
        {
            var baseQuery = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Where(p => p.IsFeatured && p.InStock)
                .AsQueryable();

            return View("Index", await BuildListingVm(
                baseQuery,
                pageTitle: "Featured Products",
                category: category,
                subCategory: subCategory,
                search: search,
                sort: sort,
                page: page,
                pageSize: pageSize));
        }

        // GET: /Shop/SubCategories?category=Lighting%20Spares
        [HttpGet]
        public async Task<IActionResult> SubCategories(string category)
        {
            category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();

            var query = _context.SubCategories
                .AsNoTracking()
                .Include(sc => sc.Category)
                .Where(sc => sc.IsActive);

            if (category != null)
                query = query.Where(sc => sc.Category != null && sc.Category.Name == category);

            var items = await query
                .OrderBy(sc => sc.DisplayOrder)
                .Select(sc => new { id = sc.Id, name = sc.Name })
                .ToListAsync();

            return Json(items);
        }

        // GET: /Shop/Product/5
        public async Task<IActionResult> Product(
            int id,
            string returnAction = "Index",
            string category = null,
            string subCategory = null,
            string search = null,
            string sort = null,
            int page = 1,
            int pageSize = 24)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            // SMART BACK (Featured / NewArrivals / Index)
            ViewBag.ReturnAction = string.IsNullOrWhiteSpace(returnAction) ? "Index" : returnAction;

            // pass back params to allow "Back" to remember state
            ViewBag.BackCategory = category;
            ViewBag.BackSubCategory = subCategory;
            ViewBag.BackSearch = search;
            ViewBag.BackSort = sort;
            ViewBag.BackPage = page;
            ViewBag.BackPageSize = pageSize;

            return View(product);
        }

        // ---- Helper: builds filters + sorting + paging + dropdown lists ----
        private async Task<ShopIndexViewModel> BuildListingVm(
            IQueryable<Product> productsQuery,
            string pageTitle,
            string category,
            string subCategory,
            string search,
            string sort,
            int page,
            int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 6 ? 6 : (pageSize > 96 ? 96 : pageSize);

            category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
            subCategory = string.IsNullOrWhiteSpace(subCategory) ? null : subCategory.Trim();
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            sort = string.IsNullOrWhiteSpace(sort) ? "name_asc" : sort.Trim().ToLowerInvariant();

            // Filters (server-side)
            if (category != null)
                productsQuery = productsQuery.Where(p => p.Category != null && p.Category.Name == category);

            if (subCategory != null)
                productsQuery = productsQuery.Where(p => p.SubCategory != null && p.SubCategory.Name == subCategory);

            if (search != null)
            {
                var like = $"%{search}%";
                productsQuery = productsQuery.Where(p =>
                    (p.Name != null && EF.Functions.Like(p.Name, like)) ||
                    (p.SKU != null && EF.Functions.Like(p.SKU, like)) ||
                    (p.Description != null && EF.Functions.Like(p.Description, like))
                );
            }

            // Sorting (server-side)
            productsQuery = sort switch
            {
                "newest" => productsQuery.OrderByDescending(p => p.CreatedDate).ThenBy(p => p.Name),
                "price_asc" => productsQuery.OrderBy(p => p.Price).ThenBy(p => p.Name),
                "price_desc" => productsQuery.OrderByDescending(p => p.Price).ThenBy(p => p.Name),
                _ => productsQuery.OrderBy(p => p.Name)
            };

            var totalCount = await productsQuery.CountAsync();

            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Dropdown lists
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var subCategoriesQuery = _context.SubCategories
                .AsNoTracking()
                .Include(sc => sc.Category)
                .Where(sc => sc.IsActive);

            if (!string.IsNullOrWhiteSpace(category))
                subCategoriesQuery = subCategoriesQuery.Where(sc => sc.Category != null && sc.Category.Name == category);

            var subCategories = await subCategoriesQuery
                .OrderBy(sc => sc.DisplayOrder)
                .ToListAsync();

            return new ShopIndexViewModel
            {
                Products = products,
                Categories = categories,
                SubCategories = subCategories,
                SelectedCategory = category,
                SelectedSubCategory = subCategory,
                SearchTerm = search,
                Sort = sort,
                PageTitle = pageTitle,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
