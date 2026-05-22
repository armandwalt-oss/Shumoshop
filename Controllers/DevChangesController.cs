using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Dev")]
    public class DevChangesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ProductCsvImportService _csvImporter;
        private readonly CountryPriceImportService _priceImporter;
        private readonly ICountryService _countries;
        private readonly IFeatureFlagService _featureFlags;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DevChangesController>? _logger;

        public DevChangesController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ProductCsvImportService csvImporter,
            CountryPriceImportService priceImporter,
            ICountryService countries,
            IFeatureFlagService featureFlags,
            Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
            ILogger<DevChangesController>? logger = null)
        {
            _context = context;
            _env = env;
            _csvImporter = csvImporter;
            _priceImporter = priceImporter;
            _countries = countries;
            _featureFlags = featureFlags;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: DevChanges
        // Supports an optional searchTerm + page so the product list is
        // fully paginated. Defaults to first 25 ordered by name.
        public async Task<IActionResult> Index(
            string section = "products",
            string? searchTerm = null,
            int page = 1,
            int pageSize = 25)
        {
            // Clamp paging to sane values
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 25;
            if (pageSize > 200) pageSize = 200;

            var productQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var s = searchTerm.Trim();
                productQuery = productQuery.Where(p =>
                    p.Name.Contains(s) ||
                    p.SKU.Contains(s) ||
                    (p.Category != null && p.Category.Name.Contains(s)) ||
                    (p.SubCategory != null && p.SubCategory.Name.Contains(s)));
            }

            var matchingCount = await productQuery.CountAsync();
            var totalPages = matchingCount == 0 ? 1 : (int)Math.Ceiling(matchingCount / (double)pageSize);
            if (page > totalPages) page = totalPages;

            var products = await productQuery
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var subCategories = await _context.SubCategories
                .Include(sc => sc.Category)
                .OrderBy(sc => sc.DisplayOrder)
                .ToListAsync();

            ViewBag.Products            = products;
            ViewBag.Categories          = categories;
            ViewBag.SubCategories       = subCategories;
            ViewBag.ProductSearchTerm   = searchTerm;
            ViewBag.TotalProducts       = await _context.Products.CountAsync();
            ViewBag.TotalCategories     = categories.Count;
            ViewBag.TotalSubCategories  = subCategories.Count;
            ViewBag.ActiveSection       = section;

            // Paging metadata
            ViewBag.ProductPage          = page;
            ViewBag.ProductPageSize      = pageSize;
            ViewBag.ProductTotalPages    = totalPages;
            ViewBag.ProductMatchingCount = matchingCount;

            return View();
        }

        // Back-compat: the search form posts to /DevChanges/SearchProducts.
        // Forward to Index so paging + search live in one place.
        [HttpGet]
        public IActionResult SearchProducts(string? searchTerm, int page = 1, int pageSize = 25) =>
            RedirectToAction(nameof(Index), new
            {
                section = "products",
                searchTerm,
                page,
                pageSize
            });

        // GET: DevChanges/SearchCategories
        [HttpGet]
        public async Task<IActionResult> SearchCategories(string searchTerm)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .OrderBy(p => p.Name)
                .Take(10)
                .ToListAsync();

            var categories = string.IsNullOrWhiteSpace(searchTerm)
                ? await _context.Categories
                    .Include(c => c.SubCategories)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync()
                : await _context.Categories
                    .Include(c => c.SubCategories)
                    .Where(c => c.Name.Contains(searchTerm) || c.Description.Contains(searchTerm))
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

            var subCategories = await _context.SubCategories
                .Include(sc => sc.Category)
                .OrderBy(sc => sc.DisplayOrder)
                .ToListAsync();

            ViewBag.Products = products;
            ViewBag.Categories = categories;
            ViewBag.SubCategories = subCategories;
            ViewBag.CategorySearchTerm = searchTerm;
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();
            ViewBag.TotalSubCategories = subCategories.Count;
            ViewBag.ActiveSection = "categories";

            return View("Index");
        }

        // GET: DevChanges/SearchSubCategories
        [HttpGet]
        public async Task<IActionResult> SearchSubCategories(string searchTerm)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .OrderBy(p => p.Name)
                .Take(10)
                .ToListAsync();

            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var subCategories = string.IsNullOrWhiteSpace(searchTerm)
                ? await _context.SubCategories
                    .Include(sc => sc.Category)
                    .OrderBy(sc => sc.DisplayOrder)
                    .ToListAsync()
                : await _context.SubCategories
                    .Include(sc => sc.Category)
                    .Where(sc => sc.Name.Contains(searchTerm) ||
                                sc.Description.Contains(searchTerm) ||
                                sc.Category.Name.Contains(searchTerm))
                    .OrderBy(sc => sc.DisplayOrder)
                    .ToListAsync();

            ViewBag.Products = products;
            ViewBag.Categories = categories;
            ViewBag.SubCategories = subCategories;
            ViewBag.SubCategorySearchTerm = searchTerm;
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCategories = categories.Count;
            ViewBag.TotalSubCategories = await _context.SubCategories.CountAsync();
            ViewBag.ActiveSection = "subcategories";

            return View("Index");
        }

        // GET: DevChanges/ProductDetails/5
        public async Task<IActionResult> ProductDetails(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.SubCategories = await _context.SubCategories.Where(sc => sc.IsActive).ToListAsync();

            return View(product);
        }

        // POST: DevChanges/UpdateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(Product model, IFormFile? imageFile)
        {
            var product = await _context.Products.FindAsync(model.Id);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                product.Name = model.Name;
                product.SKU = model.SKU;
                product.Description = model.Description;
                product.Price = model.Price;
                product.CategoryId = model.CategoryId;
                product.SubCategoryId = model.SubCategoryId;
                product.StockQuantity = model.StockQuantity;
                product.IsFeatured = model.IsFeatured;
                product.IsNewArrival = model.IsNewArrival;
                product.IsSpecial = model.IsSpecial;

                // Image handling:
                //   • If a file was uploaded, save it to wwwroot/images/products/
                //     using the SKU as the filename (so re-uploads overwrite).
                //   • Otherwise keep whatever URL the admin typed (or leave
                //     existing ImageUrl alone if they cleared the box but
                //     didn't upload anything).
                if (imageFile != null && imageFile.Length > 0)
                {
                    var savedRelativeUrl = await SaveProductImageAsync(imageFile, product.SKU);
                    if (savedRelativeUrl is not null)
                    {
                        product.ImageUrl = savedRelativeUrl;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Image upload rejected. Allowed types: jpg, jpeg, png, webp. Max size 5 MB.";
                        return RedirectToAction(nameof(ProductDetails), new { id = model.Id });
                    }
                }
                else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
                {
                    product.ImageUrl = model.ImageUrl;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product updated successfully!";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating product {ProductId}", model.Id);
                TempData["ErrorMessage"] = "Error updating product.";
            }

            return RedirectToAction(nameof(ProductDetails), new { id = model.Id });
        }

        // POST: DevChanges/RemoveProductImage/5
        // Clears Product.ImageUrl and best-effort deletes the file from disk.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProductImage(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product is null) return NotFound();

            try
            {
                // Delete the file from disk if it lives under wwwroot/images/products/.
                // (External URLs are skipped — we only own the local ones.)
                if (!string.IsNullOrWhiteSpace(product.ImageUrl) &&
                    product.ImageUrl.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase))
                {
                    // Strip the cache-buster suffix if present, e.g. "?v=12345".
                    var relativePath = product.ImageUrl.Split('?')[0];
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var fullPath = Path.Combine(webRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                product.ImageUrl = "";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Image removed.";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing image for product {ProductId}", id);
                TempData["ErrorMessage"] = "Could not remove the image. See server log.";
            }

            return RedirectToAction(nameof(ProductDetails), new { id });
        }

        /// <summary>
        /// Saves an uploaded product image to wwwroot/images/products/
        /// using the product's SKU as the filename. Returns the relative URL
        /// to store in Product.ImageUrl, or null if the upload was rejected.
        /// </summary>
        private async Task<string?> SaveProductImageAsync(IFormFile file, string sku)
        {
            const long maxBytes = 5 * 1024 * 1024;          // 5 MB
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (file.Length > maxBytes) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return null;

            // Sanitise the SKU into a safe filename component.
            var safeSku = string.Concat((sku ?? "product")
                .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
            if (string.IsNullOrEmpty(safeSku)) safeSku = "product";

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var productsDir = Path.Combine(webRoot, "images", "products");
            Directory.CreateDirectory(productsDir);

            var filename = $"{safeSku}{ext}";
            var fullPath = Path.Combine(productsDir, filename);

            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            // Return the path the browser will use. Append a cache-buster so a
            // re-upload immediately reflects in the UI without a hard refresh.
            var cacheBust = DateTime.UtcNow.Ticks;
            return $"/images/products/{filename}?v={cacheBust}";
        }

        // POST: DevChanges/ToggleProductActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProductActive(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.InStock = !product.InStock;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Product '{product.Name}' is now {(product.InStock ? "Active" : "Inactive")}";
            return RedirectToAction(nameof(ProductDetails), new { id = product.Id });
        }


        // POST: DevChanges/ToggleCategoryActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryActive(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' is now {(category.IsActive ? "Active" : "Inactive")}";
            return RedirectToAction(nameof(Index), new { section = "categories" });
        }

        // POST: DevChanges/ToggleSubCategoryActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubCategoryActive(int id)
        {
            var subCategory = await _context.SubCategories.FindAsync(id);
            if (subCategory == null) return NotFound();

            subCategory.IsActive = !subCategory.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"SubCategory '{subCategory.Name}' is now {(subCategory.IsActive ? "Active" : "Inactive")}";
            return RedirectToAction(nameof(Index), new { section = "subcategories" });
        }

        // GET: DevChanges/CreateProduct
        public async Task<IActionResult> CreateProduct()
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.SubCategories = await _context.SubCategories.Where(sc => sc.IsActive).ToListAsync();
            return View();
        }

        // POST: DevChanges/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            try
            {
                product.CreatedDate = DateTime.Now;
                product.InStock = true;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction(nameof(Index), new { section = "products" });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error creating product.";
                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                ViewBag.SubCategories = await _context.SubCategories.Where(sc => sc.IsActive).ToListAsync();
                return View(product);
            }
        }

        // GET: DevChanges/CreateCategory
        public IActionResult CreateCategory()
        {
            return View();
        }

        // POST: DevChanges/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            try
            {
                category.IsActive = true;

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category created successfully!";
                return RedirectToAction(nameof(Index), new { section = "categories" });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error creating category.";
                return View(category);
            }
        }

        // GET: DevChanges/CreateSubCategory
        public async Task<IActionResult> CreateSubCategory()
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View();
        }

        // POST: DevChanges/CreateSubCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubCategory(SubCategory subCategory)
        {
            try
            {
                subCategory.IsActive = true;

                _context.SubCategories.Add(subCategory);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "SubCategory created successfully!";
                return RedirectToAction(nameof(Index), new { section = "subcategories" });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error creating subcategory.";
                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                return View(subCategory);
            }
        }

        // GET: DevChanges/CategoryDetails/5
        public async Task<IActionResult> CategoryDetails(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // POST: DevChanges/UpdateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(Category model)
        {
            var category = await _context.Categories.FindAsync(model.Id);
            if (category == null) return NotFound();

            try
            {
                category.Name = model.Name;
                category.Description = model.Description;
                category.IconName = model.IconName;
                category.DisplayOrder = model.DisplayOrder;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category updated successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error updating category.";
            }

            return RedirectToAction(nameof(CategoryDetails), new { id = model.Id });
        }

        // GET: DevChanges/SubCategoryDetails/5
        public async Task<IActionResult> SubCategoryDetails(int? id)
        {
            if (id == null) return NotFound();

            var subCategory = await _context.SubCategories
                .Include(sc => sc.Category)
                .Include(sc => sc.Products)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (subCategory == null) return NotFound();

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();

            return View(subCategory);
        }

        // POST: DevChanges/UpdateSubCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubCategory(SubCategory model)
        {
            var subCategory = await _context.SubCategories.FindAsync(model.Id);
            if (subCategory == null) return NotFound();

            try
            {
                subCategory.Name = model.Name;
                subCategory.Description = model.Description;
                subCategory.IconName = model.IconName;
                subCategory.CategoryId = model.CategoryId;
                subCategory.DisplayOrder = model.DisplayOrder;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "SubCategory updated successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error updating subcategory.";
            }

            return RedirectToAction(nameof(SubCategoryDetails), new { id = model.Id });
        }

        // ============================================================
        // FEATURE FLAGS — Dev-only toggle page. Lets us hide the
        // international features in production and flip them on
        // gradually as each slice is ready to expose.
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> FeatureFlags()
        {
            var flags = await _featureFlags.GetAllAsync();
            return View(flags);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeatureFlag(string name, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Flag name missing.";
                return RedirectToAction(nameof(FeatureFlags));
            }

            var userId = _userManager.GetUserId(User);
            await _featureFlags.SetEnabledAsync(name, enabled, userId);

            TempData["SuccessMessage"] = $"'{name}' set to {(enabled ? "ENABLED" : "DISABLED")}.";
            return RedirectToAction(nameof(FeatureFlags));
        }

        // ============================================================
        // COUNTRY PRICES — geo-pricing CSV upload, one file per country.
        // Lives on DevChanges (not Admin) because uploading per-country
        // pricing is a developer / config job, not a daily operational task.
        // The international.geo_pricing feature flag still gates whether
        // these prices are actually shown to customers.
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> CountryPrices()
        {
            var countries = await _countries.GetActiveCountriesAsync();

            var counts = await _context.ProductPrices
                .GroupBy(pp => pp.CountryCode)
                .Select(g => new { CountryCode = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CountryCode, x => x.Count);

            var totalProducts = await _context.Products.CountAsync();

            ViewBag.Countries = countries;
            ViewBag.PriceCounts = counts;
            ViewBag.TotalProducts = totalProducts;
            return View();
        }

        [HttpGet]
        public IActionResult CountryPricesTemplate(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode)) return BadRequest("Missing country code.");
            var currency = _context.Countries.AsNoTracking()
                .Where(c => c.Code == countryCode).Select(c => c.CurrencyCode).FirstOrDefault() ?? "";
            var csv = CountryPriceImportService.BuildTemplateCsv(countryCode, currency);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
                $"shumoshop-prices-{countryCode}.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CountryPricesPreview(string countryCode, IFormFile csvFile)
        {
            if (csvFile is null || csvFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please choose a CSV file before uploading.";
                return RedirectToAction(nameof(CountryPrices));
            }
            if (csvFile.Length > 10 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File too large (10 MB limit).";
                return RedirectToAction(nameof(CountryPrices));
            }

            string csv;
            using (var reader = new StreamReader(csvFile.OpenReadStream()))
                csv = await reader.ReadToEndAsync();

            var result = await _priceImporter.ParseAndValidateAsync(csv, countryCode);

            TempData["PendingPriceCsv"] = csv;
            TempData["PendingPriceCountry"] = countryCode;
            TempData.Keep("PendingPriceCsv");
            TempData.Keep("PendingPriceCountry");

            return View("CountryPricesPreview", result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CountryPricesCommit()
        {
            var csv = TempData["PendingPriceCsv"] as string;
            var countryCode = TempData["PendingPriceCountry"] as string;
            TempData.Keep("PendingPriceCsv");
            TempData.Keep("PendingPriceCountry");

            if (string.IsNullOrEmpty(csv) || string.IsNullOrEmpty(countryCode))
            {
                TempData["ErrorMessage"] = "No pending upload found. Please re-upload.";
                return RedirectToAction(nameof(CountryPrices));
            }

            var validated = await _priceImporter.ParseAndValidateAsync(csv, countryCode);
            var (inserted, updated) = await _priceImporter.CommitAsync(validated, _userManager.GetUserId(User));

            TempData.Remove("PendingPriceCsv");
            TempData.Remove("PendingPriceCountry");
            TempData["SuccessMessage"] =
                $"Country prices saved for {countryCode}: {inserted} inserted, {updated} updated, " +
                $"{validated.InvalidCount} invalid + {validated.UnknownSkuCount} unknown SKU(s) skipped.";

            return RedirectToAction(nameof(CountryPrices));
        }

        // ============================================================
        // BULK PRODUCT IMPORT (CSV)
        // GET  /DevChanges/BulkUpload          → upload form
        // POST /DevChanges/BulkUploadPreview   → parse + validate + render preview
        // POST /DevChanges/BulkUploadCommit    → insert the valid rows
        // GET  /DevChanges/BulkUploadTemplate  → download starter CSV
        // ============================================================

        [HttpGet]
        public IActionResult BulkUpload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUploadPreview(IFormFile csvFile)
        {
            if (csvFile is null || csvFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please choose a CSV file before uploading.";
                return RedirectToAction(nameof(BulkUpload));
            }

            const long maxBytes = 10 * 1024 * 1024;   // 10 MB
            if (csvFile.Length > maxBytes)
            {
                TempData["ErrorMessage"] = "File too large. The limit is 10 MB.";
                return RedirectToAction(nameof(BulkUpload));
            }

            string csv;
            using (var reader = new StreamReader(csvFile.OpenReadStream()))
            {
                csv = await reader.ReadToEndAsync();
            }

            var result = await _csvImporter.ParseAndValidateAsync(csv);

            // Stash the raw CSV so the commit action can re-parse without
            // making the admin upload again.
            TempData["PendingCsv"] = csv;
            TempData.Keep("PendingCsv");

            return View("BulkUploadPreview", result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUploadCommit()
        {
            var csv = TempData["PendingCsv"] as string;
            TempData.Keep("PendingCsv");

            if (string.IsNullOrWhiteSpace(csv))
            {
                TempData["ErrorMessage"] = "No pending upload found. Please re-upload the file.";
                return RedirectToAction(nameof(BulkUpload));
            }

            var validated = await _csvImporter.ParseAndValidateAsync(csv);
            var inserted = await _csvImporter.CommitAsync(validated);

            TempData.Remove("PendingCsv");
            TempData["SuccessMessage"] =
                $"Imported {inserted} product(s). {validated.WillSkipDuplicateCount} duplicate SKU(s) skipped. {validated.InvalidCount} invalid row(s) skipped.";

            return RedirectToAction(nameof(Index), new { section = "products" });
        }

        [HttpGet]
        public IActionResult BulkUploadTemplate()
        {
            var csv = ProductCsvImportService.BuildTemplateCsv();
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "shumoshop-product-import-template.csv");
        }
    }
}