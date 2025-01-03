using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Utils;

namespace ShoeStore.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Kiểm tra xem đã có role chưa
                if (!context.Roles.Any())
                {
                    context.Roles.AddRange(
                        new Role { RoleName = "User" },
                        new Role { RoleName = "Admin" }
                    );
                    context.SaveChanges();
                }

                // Kiểm tra xem đã có admin chưa
                if (!context.Users.Any(u => u.RoleID == 2))
                {
                    var admin = new User
                    {
                        Username = "admin",
                        PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                        Email = "nguyenanlc9@gmail.com",
                        FullName = "Administrator",
                        Phone = "0123456789",
                        RoleID = 2, // Admin role
                        Status = true,
                        RegisterDate = DateTime.Now,
                        CreatedDate = DateTime.Now
                    };

                    context.Users.Add(admin);
                    context.SaveChanges();
                }
            }
        }
    }
} 