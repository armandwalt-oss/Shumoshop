using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Middleware;
using WebApplication1.Models;
using WebApplication1.Services; // ADD THIS LINE

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
app.UseStaticFiles();
app.UseResponseCompression();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ============================================
// ADD SESSION MIDDLEWARE
// ============================================
app.UseSession();

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