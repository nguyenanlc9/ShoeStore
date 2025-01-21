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

                // Kiểm tra và thêm PaymentMethodConfigs
                if (!context.PaymentMethodConfigs.Any())
                {
                    context.PaymentMethodConfigs.AddRange(
                        new PaymentMethodConfig 
                        { 
                            Id = 1, 
                            Type = PaymentMethodType.COD, 
                            Name = "COD", 
                            Status = PaymentMethodStatus.Active, 
                            Description = "Thanh toán qua COD",
                            MaintenanceMessage = "Phương thức thanh toán COD tạm thời không khả dụng",
                            LastUpdated = DateTime.Now
                        },
                        new PaymentMethodConfig 
                        { 
                            Id = 2, 
                            Type = PaymentMethodType.VNPay, 
                            Name = "VNPay", 
                            Status = PaymentMethodStatus.Active, 
                            Description = "Thanh toán qua ví VNPay",
                            MaintenanceMessage = "Hệ thống VNPay đang được bảo trì",
                            LastUpdated = DateTime.Now
                        },
                        new PaymentMethodConfig 
                        { 
                            Id = 3, 
                            Type = PaymentMethodType.Momo, 
                            Name = "Momo", 
                            Status = PaymentMethodStatus.Active, 
                            Description = "Thanh toán qua Momo",
                            MaintenanceMessage = "Hệ thống Momo đang được bảo trì",
                            LastUpdated = DateTime.Now
                        },
                        new PaymentMethodConfig 
                        { 
                            Id = 4, 
                            Type = PaymentMethodType.ZaloPay, 
                            Name = "ZaloPay", 
                            Status = PaymentMethodStatus.Active, 
                            Description = "Thanh toán qua ZaloPay",
                            MaintenanceMessage = "Hệ thống ZaloPay đang được bảo trì",
                            LastUpdated = DateTime.Now
                        },
                        new PaymentMethodConfig 
                        { 
                            Id = 5, 
                            Type = PaymentMethodType.Visa, 
                            Name = "Visa", 
                            Status = PaymentMethodStatus.Active, 
                            Description = "Thanh toán qua thẻ Visa",
                            MaintenanceMessage = "Hệ thống thanh toán Visa đang được bảo trì",
                            LastUpdated = DateTime.Now
                        }
                    );
                    context.SaveChanges();
                }


                // Thêm các size giày mặc định
                if (!context.Size.Any())
                {
                    var sizes = new List<Size>();
                    for (int i = 35; i <= 45; i++)
                    {
                        sizes.Add(new Size { SizeValue = i });
                    }
                    context.Size.AddRange(sizes);
                    context.SaveChanges();
                }

                // Thêm các danh mục sản phẩm mặc định
                if (!context.Categories.Any())
                {
                    context.Categories.AddRange(
                        new Category 
                        { 
                            Name = "Giày thể thao",
                            
                        },
                        new Category 
                        { 
                            Name = "Giày chạy bộ",
                           
                        },
                        new Category 
                        { 
                            Name = "Giày thời trang",
                          
                        },
                        new Category 
                        { 
                            Name = "Giày đá banh",
                           
                        }
                    );
                    context.SaveChanges();
                }

                // Thêm các thương hiệu mặc định
                if (!context.Brands.Any())
                {
                    context.Brands.AddRange(
                        new Brand 
                        { 
                            Name = "Nike",

                        },
                        new Brand 
                        { 
                            Name = "Adidas",
                           
                        },
                        new Brand 
                        { 
                            Name = "Puma",
                            
                        },
                        new Brand 
                        { 
                            Name = "New Balance",
                           
                        }
                    );
                    context.SaveChanges();
                }

            }
        }
    }
} 