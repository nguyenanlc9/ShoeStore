using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShoeStore.Models;
using System.Collections.Generic;
using static NuGet.Packaging.PackagingConstants;

namespace ShoeStore.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Slider> Sliders { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<ProductSizeStock> ProductSizeStocks { get; set; }
        public DbSet<Size> Size { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ContactUser> ContactUsers { get; set; }
        public DbSet<Footer> Footers { get; set; }
        public DbSet<MemberRank> MemberRanks { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductHistory> ProductHistories { get; set; }
        public DbSet<ProductSizeStockHistory> ProductSizeStockHistories { get; set; }
        public DbSet<Contact> Contacts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình precision và scale cho các trường decimal
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.DiscountPrice)
                .HasPrecision(18, 2);

            // Cấu hình quan hệ giữa Product và Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Categories)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ giữa Product và Brand
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brands)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ giữa ProductSizeStock và Product
            modelBuilder.Entity<ProductSizeStock>()
                .HasOne(pss => pss.Product)
                .WithMany(p => p.ProductSizeStocks)
                .HasForeignKey(pss => pss.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ giữa ProductSizeStock và Size
            modelBuilder.Entity<ProductSizeStock>()
                .HasOne(pss => pss.Size)
                .WithMany(s => s.ProductSizeStocks)
                .HasForeignKey(pss => pss.SizeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ cho ProductSizeStockHistory
            modelBuilder.Entity<ProductSizeStockHistory>()
                .HasOne(h => h.Product)
                .WithMany()
                .HasForeignKey(h => h.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductSizeStockHistory>()
                .HasOne(h => h.Size)
                .WithMany()
                .HasForeignKey(h => h.SizeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình giá trị mặc định cho các trường bool
            modelBuilder.Entity<Product>()
                .Property(p => p.IsNew)
                .HasDefaultValue(false);

            modelBuilder.Entity<Product>()
                .Property(p => p.IsHot)
                .HasDefaultValue(false);

            modelBuilder.Entity<Product>()
                .Property(p => p.IsSale)
                .HasDefaultValue(false);
        }
    }
}
