using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // EXISTING DbSets (don't change these)
        public DbSet<ContactUs> Contacts { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<OrderTracking> OrderTrackings { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }

        // NEW DbSets for My Account feature
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Address> Addresses { get; set; }

        // NEW: SubCategories DbSet
        public DbSet<SubCategory> SubCategories { get; set; }

        // NEW: Shipping configuration (admin-editable, single row).
        public DbSet<ShippingSettings> ShippingSettings { get; set; }

        // NEW: Markets ShumoShop ships to. Drives geo-pricing + carrier choice.
        public DbSet<Country> Countries { get; set; }

        // NEW: Named feature toggles, editable by Dev role from the admin UI.
        public DbSet<FeatureFlag> FeatureFlags { get; set; }

        // NEW: Per-country price overrides for products.
        public DbSet<ProductPrice> ProductPrices { get; set; }

        // NEW: Product view log. One row per product detail page hit, used
        // for the admin "Top Viewed Products" widget. No IP / UA stored.
        public DbSet<ProductView> ProductViews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // EXISTING Cart relationships (don't change these)
            modelBuilder.Entity<Cart>()
                .HasMany(c => c.CartItems)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // EXISTING Cart indexes (don't change these)
            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserId);

            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.SessionId);

            // ============================================
            // NEW RELATIONSHIPS FOR MY ACCOUNT FEATURE
            // ============================================

            // Order relationships
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem relationships
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Don't delete product if it's in an order

            // Address relationships
            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Decimal precision for Order
            modelBuilder.Entity<Order>()
                .Property(o => o.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.ShippingCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.Tax)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            // Decimal precision for OrderItem
            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice)
                .HasPrecision(18, 2);

            // Indexes for better performance
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.UserId);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<Address>()
                .HasIndex(a => a.UserId);

            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.ProductId);

            // ============================================
            // RETURN REQUEST RELATIONSHIPS
            // ============================================

            // ReturnRequest relationships - FIX CASCADE DELETE ISSUE
            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.OrderItem)
                .WithMany()
                .HasForeignKey(r => r.OrderItemId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Decimal precision for ReturnRequest
            modelBuilder.Entity<ReturnRequest>()
                .Property(r => r.RefundAmount)
                .HasPrecision(18, 2);

            // Indexes for ReturnRequest
            modelBuilder.Entity<ReturnRequest>()
                .HasIndex(r => r.UserId);

            modelBuilder.Entity<ReturnRequest>()
                .HasIndex(r => r.OrderId);

            modelBuilder.Entity<ReturnRequest>()
                .HasIndex(r => r.Status);

            modelBuilder.Entity<ReturnRequest>()
                .HasIndex(r => r.ReturnAuthorizationNumber);

            // ============================================
            // NEW: SUBCATEGORY RELATIONSHIPS
            // ============================================

            // Category-SubCategory relationship
            modelBuilder.Entity<SubCategory>()
                .HasOne(sc => sc.Category)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Product-SubCategory relationship (optional)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.SubCategory)
                .WithMany(sc => sc.Products)
                .HasForeignKey(p => p.SubCategoryId)
                .OnDelete(DeleteBehavior.SetNull); // Set to null if subcategory deleted

            // Indexes for SubCategories
            modelBuilder.Entity<SubCategory>()
                .HasIndex(sc => sc.CategoryId);

            modelBuilder.Entity<SubCategory>()
                .HasIndex(sc => sc.Name);

            // Product SKU should be globally unique so bulk-import + manual
            // edits can't accidentally produce two products with the same
            // catalogue code. Matches the unique index already on
            // Order.OrderNumber.
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            // ProductPrice — one row per (product, country). Lookup hot path
            // is by Product+Country, so a unique composite index covers both
            // the constraint and the fast read.
            modelBuilder.Entity<ProductPrice>()
                .HasIndex(pp => new { pp.ProductId, pp.CountryCode })
                .IsUnique();

            modelBuilder.Entity<ProductPrice>()
                .HasOne(pp => pp.Product)
                .WithMany()
                .HasForeignKey(pp => pp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductPrice>()
                .HasOne(pp => pp.Country)
                .WithMany()
                .HasForeignKey(pp => pp.CountryCode)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductView — index on (ProductId, ViewedAt) is the hot path
            // for "top N over the last W days" queries. ViewedAt alone is
            // useful for the timeline / retention queries.
            modelBuilder.Entity<ProductView>()
                .HasIndex(pv => new { pv.ProductId, pv.ViewedAt });
            modelBuilder.Entity<ProductView>()
                .HasIndex(pv => pv.ViewedAt);
        }
    }
}
