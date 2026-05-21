using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    /// <summary>
    /// Seeds ShumoShop's demo database from the real MAB product catalogue
    /// shipped in <c>Data/Seed/products.json</c>. Also seeds delivery stores
    /// and demo admin/customer accounts.
    ///
    /// Idempotent — only inserts records that don't already exist, so it's
    /// safe to call on every app startup.
    /// </summary>
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            ILogger logger)
        {
            try
            {
                // Apply any pending migrations so the DB schema is up to date.
                await context.Database.MigrateAsync();

                await SeedCatalogueAsync(context, env, logger);
                await SeedStoresAsync(context, logger);
                await SeedDemoUsersAsync(userManager, logger);

                logger.LogInformation("✅ Database seeding complete.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error seeding database");
            }
        }

        // ------------------------------------------------------------------
        // CATALOGUE: 1 top-level Category, 22 SubCategories, 835 Products
        // Loaded from Data/Seed/products.json (generated from the client's
        // MAB Automotive Clips and Fasteners spreadsheet).
        // ------------------------------------------------------------------
        private static async Task SeedCatalogueAsync(
            ApplicationDbContext context, IWebHostEnvironment env, ILogger logger)
        {
            if (await context.Products.AnyAsync())
            {
                logger.LogInformation("Products already seeded, skipping catalogue seed.");
                return;
            }

            var seedFile = Path.Combine(env.ContentRootPath, "Data", "Seed", "products.json");
            if (!File.Exists(seedFile))
            {
                // Fall back to the AppContext location in case CopyToOutputDirectory
                // is the only path that has it (e.g. published output).
                seedFile = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "products.json");
            }

            if (!File.Exists(seedFile))
            {
                logger.LogWarning("Seed file not found at {Path} — skipping product seed.", seedFile);
                return;
            }

            logger.LogInformation("Loading product seed file: {Path}", seedFile);
            var json = await File.ReadAllTextAsync(seedFile);
            var seed = JsonSerializer.Deserialize<SeedFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (seed is null || seed.Products is null || seed.Products.Count == 0)
            {
                logger.LogWarning("Seed file deserialised to empty data — skipping product seed.");
                return;
            }

            // 1) Top-level Category — find-or-create by name. The previous
            //    implementation unconditionally inserted, so wiping Products
            //    and re-running this seeder produced duplicate categories.
            var topCategoryName = seed.Category?.Name ?? "Automotive Clips & Fasteners";
            var topCategory = await context.Categories
                .FirstOrDefaultAsync(c => c.Name == topCategoryName);

            if (topCategory is null)
            {
                topCategory = new Category
                {
                    Name = topCategoryName,
                    Description = seed.Category?.Description ?? "Automotive clips and fasteners.",
                    IconName = seed.Category?.IconName ?? "fa-solid fa-screwdriver-wrench",
                    DisplayOrder = 1,
                    IsActive = true
                };
                await context.Categories.AddAsync(topCategory);
                await context.SaveChangesAsync();
                logger.LogInformation("Created top-level category '{Name}'.", topCategoryName);
            }
            else
            {
                logger.LogInformation("Reusing existing top-level category '{Name}' (Id={Id}).",
                    topCategoryName, topCategory.Id);
            }

            // 2) SubCategories — find-or-create per (Name, parent CategoryId).
            //    Keep a lookup by raw key so we can wire products.
            var existingSubs = await context.SubCategories
                .Where(s => s.CategoryId == topCategory.Id)
                .ToListAsync();

            var subLookup = new Dictionary<string, SubCategory>(StringComparer.OrdinalIgnoreCase);
            if (seed.SubCategories is not null)
            {
                int created = 0, reused = 0;
                foreach (var s in seed.SubCategories)
                {
                    var existing = existingSubs.FirstOrDefault(es =>
                        string.Equals(es.Name, s.Name, StringComparison.OrdinalIgnoreCase));

                    if (existing is not null)
                    {
                        subLookup[s.RawKey] = existing;
                        reused++;
                    }
                    else
                    {
                        var sub = new SubCategory
                        {
                            Name = s.Name,
                            Description = $"{s.Name} — automotive clips and fasteners.",
                            IconName = "fa-solid fa-screwdriver-wrench",
                            DisplayOrder = s.DisplayOrder,
                            IsActive = true,
                            CategoryId = topCategory.Id
                        };
                        await context.SubCategories.AddAsync(sub);
                        subLookup[s.RawKey] = sub;
                        created++;
                    }
                }
                if (created > 0) await context.SaveChangesAsync();
                logger.LogInformation("Subcategories: created {Created}, reused {Reused}.", created, reused);
            }

            // 3) Products — bulk insert in batches to keep memory and SQL chatter sane
            const int batchSize = 250;
            var batch = new List<Product>(batchSize);
            int totalInserted = 0;

            foreach (var p in seed.Products)
            {
                subLookup.TryGetValue(p.Category ?? "", out var subCategory);

                batch.Add(new Product
                {
                    Name = string.IsNullOrWhiteSpace(p.Description) ? p.Code : p.Description,
                    SKU = p.Code,
                    CategoryId = topCategory.Id,
                    SubCategoryId = subCategory?.Id,
                    Description = BuildProductDescription(p),
                    Price = p.Price,
                    // ImageUrl is intentionally empty. When the client supplies
                    // product images, drop them into wwwroot/images/products/
                    // named {ImageCode}.jpg and update ImageUrl accordingly.
                    ImageUrl = "",
                    InStock = p.StockQuantity > 0,
                    StockQuantity = p.StockQuantity,
                    IsSpecial = p.IsSpecial,
                    IsNewArrival = p.IsNewArrival,
                    IsFeatured = p.IsFeatured,
                    CreatedDate = DateTime.Now
                });

                if (batch.Count >= batchSize)
                {
                    await context.Products.AddRangeAsync(batch);
                    await context.SaveChangesAsync();
                    totalInserted += batch.Count;
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Products.AddRangeAsync(batch);
                await context.SaveChangesAsync();
                totalInserted += batch.Count;
            }

            logger.LogInformation("Seeded {Count} products.", totalInserted);
        }

        private static string BuildProductDescription(SeedProduct p)
        {
            var pack = p.PackSize > 1
                ? $"Pack of {p.PackSize} ({p.UOM?.ToLower() ?? "each"})."
                : $"Sold individually ({p.UOM?.ToLower() ?? "each"}).";
            var price = p.Price > 0
                ? $" R{p.Price:N2} per pack."
                : "";
            return $"Item code {p.Code}. {pack}{price} Supplied by MAB.";
        }

        // ------------------------------------------------------------------
        // STORES (Find a Store)
        // ------------------------------------------------------------------
        private static async Task SeedStoresAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Stores.AnyAsync())
            {
                logger.LogInformation("Stores already seeded, skipping.");
                return;
            }

            var stores = new List<Store>
            {
                new() { Name = "ShumoShop Centurion",      Address = "138 Botha Avenue, Lyttleton",  City = "Centurion",     State = "Gauteng",        ZipCode = "0014", Phone = "012 555 0101", Email = "centurion@shumoshop.co.za",  Hours = "Mon-Sat 09:00-18:00, Sun 09:00-14:00", IsActive = true },
                new() { Name = "ShumoShop Sandton",        Address = "Sandton City Mall",            City = "Johannesburg",  State = "Gauteng",        ZipCode = "2196", Phone = "011 555 0202", Email = "sandton@shumoshop.co.za",    Hours = "Mon-Sun 09:00-19:00",                  IsActive = true },
                new() { Name = "ShumoShop Cape Town",      Address = "V&A Waterfront",               City = "Cape Town",     State = "Western Cape",   ZipCode = "8001", Phone = "021 555 0303", Email = "capetown@shumoshop.co.za",   Hours = "Mon-Sun 09:00-21:00",                  IsActive = true },
                new() { Name = "ShumoShop Durban",         Address = "Gateway Theatre of Shopping",  City = "Umhlanga",      State = "KwaZulu-Natal",  ZipCode = "4319", Phone = "031 555 0404", Email = "durban@shumoshop.co.za",     Hours = "Mon-Sun 09:00-19:00",                  IsActive = true },
                new() { Name = "ShumoShop Pretoria",       Address = "Menlyn Park Shopping Centre",  City = "Pretoria",      State = "Gauteng",        ZipCode = "0181", Phone = "012 555 0505", Email = "pretoria@shumoshop.co.za",   Hours = "Mon-Sat 09:00-18:00, Sun 09:00-15:00", IsActive = true },
                new() { Name = "ShumoShop Gqeberha",       Address = "Greenacres Shopping Centre",   City = "Gqeberha",      State = "Eastern Cape",   ZipCode = "6045", Phone = "041 555 0606", Email = "pe@shumoshop.co.za",         Hours = "Mon-Sat 09:00-18:00, Sun closed",      IsActive = true }
            };

            await context.Stores.AddRangeAsync(stores);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} stores.", stores.Count);
        }

        // ------------------------------------------------------------------
        // DEMO USERS (admin + customer)
        // ------------------------------------------------------------------
        private static async Task SeedDemoUsersAsync(
            UserManager<ApplicationUser> userManager, ILogger logger)
        {
            await EnsureUserAsync(userManager, logger,
                email: "admin@shumoshop.co.za",
                password: "Admin@123",
                name: "Demo",
                surname: "Admin",
                role: "Admin",
                streetAddress: "138 Botha Avenue, Lyttleton",
                city: "Centurion",
                state: "Gauteng",
                postalCode: "0014");

            await EnsureUserAsync(userManager, logger,
                email: "customer@shumoshop.co.za",
                password: "Customer@123",
                name: "Demo",
                surname: "Customer",
                role: "User",
                streetAddress: "12 Main Road",
                city: "Sandton",
                state: "Gauteng",
                postalCode: "2196");

            await EnsureUserAsync(userManager, logger,
                email: "dev@shumoshop.co.za",
                password: "Dev@123",
                name: "Demo",
                surname: "Dev",
                role: "Dev",
                streetAddress: "138 Botha Avenue, Lyttleton",
                city: "Centurion",
                state: "Gauteng",
                postalCode: "0014");
        }

        private static async Task EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            string email,
            string password,
            string name,
            string surname,
            string role,
            string streetAddress,
            string city,
            string state,
            string postalCode)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                return;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = name,
                Surname = surname,
                StreetAddress = streetAddress,
                City = city,
                State = state,
                PostalCode = postalCode,
                DateJoined = DateTime.Now
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                logger.LogInformation("Seeded demo {Role}: {Email} / {Password}", role, email, password);
            }
            else
            {
                logger.LogWarning("Failed to create demo {Role} {Email}: {Errors}",
                    role, email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // ------------------------------------------------------------------
        // DTOs for deserialising Data/Seed/products.json
        // ------------------------------------------------------------------
        private sealed class SeedFile
        {
            public SeedCategory? Category { get; set; }
            public List<SeedSubCategory>? SubCategories { get; set; }
            public List<SeedProduct>? Products { get; set; }
        }

        private sealed class SeedCategory
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string IconName { get; set; } = "";
        }

        private sealed class SeedSubCategory
        {
            public string Name { get; set; } = "";
            public string RawKey { get; set; } = "";
            public int DisplayOrder { get; set; }
        }

        private sealed class SeedProduct
        {
            public string Code { get; set; } = "";
            public string Description { get; set; } = "";
            public int PackSize { get; set; }
            public string? UOM { get; set; }
            public decimal Price { get; set; }
            public string? ImageCode { get; set; }
            public string? Category { get; set; }
            public bool IsSpecial { get; set; }
            public bool IsNewArrival { get; set; }
            public bool IsFeatured { get; set; }
            public int StockQuantity { get; set; }
        }
    }
}
