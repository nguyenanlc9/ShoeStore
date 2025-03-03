﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Payment;
using ShoeStore.Services;
using ShoeStore.Services.Email;
using ShoeStore.Services.Order;
using ShoeStore.Services.Payment;
using ShoeStore.Services.APIAddress;
using ShoeStore.Services.MemberRanking;
using ShoeStore.Data;
using ShoeStore.Services.ZaloPay;
using ShoeStore.Models.Payment.Momo;
using ShoeStore.Services.Momo;
using ShoeStore.Services.ReCaptcha;
using ShoeStore.Services.GHN;
using Microsoft.AspNetCore.SignalR;
using ShoeStore.Services.Excel;
using ShoeStore.Hubs;

namespace ShoeStore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHostedService<OrderProcessingService>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<IEmailService, EmailService>();

            // Cấu hình DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Cấu hình Session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Cấu hình Cookie Authentication
            builder.Services.AddAuthentication(options => 
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                options.CallbackPath = "/signin-google";
            })
            .AddFacebook(options =>
            {
                options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
                options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
                options.Fields.Add("name");
                options.Fields.Add("email");
                options.SaveTokens = true;
            });

            // Cấu hình HTTP Context Accessor
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IVnPayService, VnPayService>();

            builder.Services.AddScoped<IMemberRankService, MemberRankService>();

            // Đăng ký HttpClient và AddressService
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IAddressService, AddressService>();

            // Configure ZaloPay
            builder.Services.Configure<ZaloPayConfig>(builder.Configuration.GetSection("ZaloPay"));
            builder.Services.AddHttpClient<IZaloPayService, ZaloPayService>();
            builder.Services.AddScoped<IZaloPayService, ZaloPayService>();

            // Configure Momo
            builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("Momo"));
            builder.Services.AddHttpClient<IMomoService, MomoService>();
            builder.Services.AddScoped<IMomoService, MomoService>();

            // Add services to the container.
            builder.Services.AddScoped<IShippingService, ShippingService>();

            builder.Services.Configure<GoogleReCaptchaConfig>(builder.Configuration.GetSection("GoogleReCaptcha"));
            builder.Services.AddHttpClient<IGoogleReCaptchaService, GoogleReCaptchaService>();

            builder.Services.AddHttpClient();


            // Đăng ký GHN Service
            builder.Services.AddScoped<IGHNService, GHNService>();
            builder.Services.AddScoped<IGHNAddressService, GHNAddressService>();

            // Đăng ký ExcelService
            builder.Services.AddScoped<IExcelService, ExcelService>();

            // Đăng ký NotificationService
            builder.Services.AddScoped<INotificationService, NotificationService>();

            builder.Services.AddSignalR();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            // Thêm middleware xử lý status code
            app.UseStatusCodePagesWithReExecute("/Error/{0}");

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Sử dụng Session
            app.UseSession();

            app.UseRouting();

            // Thứ tự quan trọng: Authentication trước Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Cấu hình Area route
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            // Cấu hình API route

            // Cấu hình Default route
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Add SignalR Hub endpoint
            app.MapHub<NotificationHub>("/notificationHub");

            // Khởi tạo dữ liệu mặc định nếu cần
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }

            // Seed data
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    SeedData.Initialize(services);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred seeding the DB.");
                }
            }

            // Tạo thư mục lưu ảnh nếu chưa tồn tại
            var returnsImagePath = Path.Combine(app.Environment.WebRootPath, "images", "returns");
            if (!Directory.Exists(returnsImagePath))
            {
                Directory.CreateDirectory(returnsImagePath);
            }

    
            app.Run();
        }
    }
}