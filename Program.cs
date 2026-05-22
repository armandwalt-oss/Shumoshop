using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Middleware;
using WebApplication1.Models;
using WebApplication1.Services; // ADD THIS LINE

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// AddSessionStateTempDataProvider switches TempData from cookie storage
// (limited to ~4 KB) to session storage. Required for the bulk CSV upload
// flow which stashes the parsed file in TempData between preview and commit.
builder.Services.AddControllersWithViews()
    .AddSessionStateTempDataProvider();

// Add this line to register your DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IPaymentService, PayFastService>();

// The Courier Guy shipping integration.
// Registered as a typed HttpClient so we get a properly pooled HttpMessageHandler.
builder.Services.AddHttpClient<ICourierGuyService, CourierGuyService>();

// Bulk product CSV import used by DevChangesController.
builder.Services.AddScoped<ProductCsvImportService>();

// Geo / market resolution. Powers the country switcher in the header
// and the upcoming geo-pricing + international shipping work.
builder.Services.AddScoped<ICountryService, CountryService>();

// Feature flags — Dev role toggles experimental features at runtime
// without redeploying. Used to hide international features until ready.
builder.Services.AddScoped<IFeatureFlagService, FeatureFlagService>();

// Geo-pricing — per-country price overrides + the resolver used by views
// to decide which price + currency to render.
builder.Services.AddScoped<IPriceResolver, PriceResolver>();
builder.Services.AddScoped<CountryPriceImportService>();

// Product-view analytics. Logs one row per Shop/Product detail hit; powers
// the "Top Viewed Products" admin widget. Best-effort writes.
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Shipping providers — Courier Guy is the only one with a real live
// integration today. DHL Express / FedEx / Aramex are stubs that activate
// the moment their credentials drop into User Secrets / appsettings. All
// four are registered as IShippingProvider so the registry can enumerate
// them and route by destination country.
builder.Services.AddScoped<WebApplication1.Services.Shipping.IShippingProvider, WebApplication1.Services.Shipping.CourierGuyShippingProvider>();
builder.Services.AddScoped<WebApplication1.Services.Shipping.IShippingProvider, WebApplication1.Services.Shipping.DhlExpressShippingProvider>();
builder.Services.AddScoped<WebApplication1.Services.Shipping.IShippingProvider, WebApplication1.Services.Shipping.FedexShippingProvider>();
builder.Services.AddScoped<WebApplication1.Services.Shipping.IShippingProvider, WebApplication1.Services.Shipping.AramexShippingProvider>();
builder.Services.AddScoped<WebApplication1.Services.Shipping.IShippingProviderRegistry, WebApplication1.Services.Shipping.ShippingProviderRegistry>();

// Payment providers — PayFast is live for ZAR. Stripe (multi-currency) and
// PayPal are stubs that activate when their credentials drop in. Each
// implements IPaymentProvider; the registry picks the right ones at
// checkout based on the active currency.
builder.Services.AddScoped<WebApplication1.Services.Payments.IPaymentProvider, WebApplication1.Services.Payments.PayFastPaymentProvider>();
builder.Services.AddScoped<WebApplication1.Services.Payments.IPaymentProvider, WebApplication1.Services.Payments.StripePaymentProvider>();
builder.Services.AddScoped<WebApplication1.Services.Payments.IPaymentProvider, WebApplication1.Services.Payments.PayPalPaymentProvider>();
builder.Services.AddScoped<WebApplication1.Services.Payments.IPaymentProviderRegistry, WebApplication1.Services.Payments.PaymentProviderRegistry>();

// Antiforgery — allow the token to be sent via a header so JS fetch() calls
// can POST JSON without embedding a hidden <input> in a form.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Add Memory Cache for performance
builder.Services.AddMemoryCache();

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/Index";
    options.LogoutPath = "/Login/Logout";
    options.AccessDeniedPath = "/Login/AccessDenied";
});

// ============================================
// ADD SESSION SUPPORT FOR SHOPPING CART
// ============================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7); // Cart session lasts 7 days
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpContextAccessor for accessing session in CartController
builder.Services.AddHttpContextAccessor();

// ============================================
// ADD CART CLEANUP BACKGROUND SERVICE
// ============================================
builder.Services.AddHostedService<CartCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Use custom global exception handler
    app.UseGlobalExceptionHandler();
    app.UseHsts();
}
else
{
    // In development, use the default developer exception page
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseStaticFiles();
app.UseResponseCompression();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ============================================
// ADD SESSION MIDDLEWARE
// ============================================
app.UseSession();

// Resolves the visitor's active country (from cookie today; IP-based in a
// future slice) and publishes it on HttpContext.Items for views/controllers.
app.UseActiveCountry();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedRolesAsync(roleManager);

    // Seed demo data (categories, subcategories, products, stores, demo users)
    // from the real MAB product catalogue shipped in Data/Seed/products.json.
    // Idempotent — only inserts what's missing, safe to call on every startup.
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var env = services.GetRequiredService<IWebHostEnvironment>();
    var seederLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
    await DatabaseSeeder.SeedAsync(dbContext, userManager, env, seederLogger);
}

app.Run();