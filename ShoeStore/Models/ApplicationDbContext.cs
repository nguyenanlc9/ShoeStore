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

        public DbSet<ShoeStore.Models.Slider> Slider { get; set; } = default!;

        public DbSet<Role> Roles { get; set; }
        public DbSet<ShoeStore.Models.ProductSizeStock> ProductSizeStock { get; set; } = default!;
        public DbSet<ShoeStore.Models.Size> Size { get; set; } = default!;

        public DbSet<Review> Reviews { get; set; }
    }
}
