using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShoeStore.Models;
using System.Collections.Generic;

namespace ShoeStore.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Slider> Slider { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<ProductSizeStock> ProductSizeStocks { get; set; }
        public DbSet<Size> Size { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ContactUser> ContactUsers { get; set; }

        public DbSet<MemberRank> MemberRanks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình Role-User relationship
            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleID);

            // Seed data cho Role
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = 1, RoleName = "Admin" },
                new Role { RoleID = 2, RoleName = "User" }
            );

            // Cấu hình cho CartItem
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.User)
                .WithMany()
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Size)
                .WithMany()
                .HasForeignKey(ci => ci.SizeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed data cho MemberRanks
            modelBuilder.Entity<MemberRank>().HasData(
                new MemberRank 
                { 
                    RankId = 1, 
                    RankName = "Bronze", 
                    MinimumSpent = 0, 
                    DiscountPercent = 0,
                    Description = "Thành viên mới" 
                },
                new MemberRank 
                { 
                    RankId = 2, 
                    RankName = "Silver", 
                    MinimumSpent = 5000000, 
                    DiscountPercent = 5,
                    Description = "Giảm 5% mọi đơn hàng" 
                },
                new MemberRank 
                { 
                    RankId = 3, 
                    RankName = "Gold", 
                    MinimumSpent = 20000000, 
                    DiscountPercent = 10,
                    Description = "Giảm 10% mọi đơn hàng" 
                },
                new MemberRank 
                { 
                    RankId = 4, 
                    RankName = "Platinum", 
                    MinimumSpent = 50000000, 
                    DiscountPercent = 15,
                    Description = "Giảm 15% mọi đơn hàng" 
                }
            );

            modelBuilder.Entity<User>()
                .Property(u => u.TotalSpent)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);
        }
    }
}
