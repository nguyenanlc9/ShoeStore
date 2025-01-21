using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Filters;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Models.ViewModels;
using System.Linq;
using System.Collections.Generic;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string period = "all")
        {
            var today = DateTime.Today;
            var startDate = today;
            var endDate = today.AddDays(1);

            // Xác định khoảng thời gian dựa trên period
            switch (period.ToLower())
            {
                case "today":
                    startDate = today;
                    break;
                case "month":
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = startDate.AddMonths(1);
                    break;
                case "year":
                    startDate = new DateTime(today.Year, 1, 1);
                    endDate = startDate.AddYears(1);
                    break;
                default:
                    startDate = DateTime.MinValue;
                    endDate = DateTime.MaxValue;
                    break;
            }

            var orders = _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate < endDate)
                .AsNoTracking();

            // Tính toán đánh giá trung bình an toàn
            var reviews = _context.Reviews
                .Where(r => r.CreatedAt >= startDate && r.CreatedAt < endDate)
                .ToList();

            var totalReviews = reviews.Count;
            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            var dashboardViewModel = new DashboardViewModel
            {
                TotalOrders = orders.Count(),
                NewOrders = orders.Count(o => o.Status == OrderStatus.Pending 
                    || (o.Status == OrderStatus.Processing 
                        && o.OrderDate.Date == DateTime.Today)),
                TotalRevenue = orders
                    .Where(o => o.PaymentStatus == PaymentStatus.Completed)
                    .Sum(o => o.TotalAmount),
                TotalProducts = _context.Products.Count(),
                LowStockProducts = _context.ProductSizeStocks
                    .Where(p => p.StockQuantity < 10)
                    .Count(),
                TotalCustomers = _context.Users.Where(u => u.RoleID == 2).Count(),

                // 1. Thống kê đơn hàng theo trạng thái
                OrderStatusStats = new Dictionary<OrderStatus, int>
                {
                    { OrderStatus.Pending, orders.Count(o => o.Status == OrderStatus.Pending) },
                    { OrderStatus.Processing, orders.Count(o => o.Status == OrderStatus.Processing) },
                    { OrderStatus.Shipping, orders.Count(o => o.Status == OrderStatus.Shipping) },
                    { OrderStatus.Completed, orders.Count(o => o.Status == OrderStatus.Completed) },
                    { OrderStatus.Cancelled, orders.Count(o => o.Status == OrderStatus.Cancelled) }
                },

                // 2. Thống kê đơn hàng theo phương thức thanh toán
                PaymentMethodStats = new Dictionary<string, int>
                {
                    { "COD", orders.Count(o => o.PaymentMethod == PaymentMethod.COD) },
                    { "VNPay", orders.Count(o => o.PaymentMethod == PaymentMethod.VNPay) },
                    { "Momo", orders.Count(o => o.PaymentMethod == PaymentMethod.Momo) }
                },

                // 3. Thống kê đánh giá sản phẩm
                TotalReviews = totalReviews,
                AverageRating = averageRating,

                // 4. Thống kê người dùng mới
                NewUsers = _context.Users
                    .Where(u => u.RoleID == 2)
                    .Count(),

                // 5. Top khách hàng chi tiêu cao nhất
                TopSpendingCustomers = _context.Users
                    .Where(u => u.RoleID == 2)
                    .Select(u => new TopSpendingCustomer
                    {
                        User = u,
                        TotalSpent = _context.Orders
                            .Where(o => o.UserId == u.UserID && 
                                   o.PaymentStatus == PaymentStatus.Completed &&
                                   o.OrderDate >= startDate && 
                                   o.OrderDate < endDate)
                            .Sum(o => o.TotalAmount)
                    })
                    .Where(t => t.TotalSpent > 0)
                    .OrderByDescending(t => t.TotalSpent)
                    .Take(5)
                    .ToList(),

                RecentOrders = orders
                    .Include(o => o.User)  // Include user information
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToList(),

                TopSellingProducts = _context.OrderDetails
                    .Include(od => od.Product)
                    .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate < endDate)
                    .GroupBy(od => od.ProductId)
                    .Select(g => new TopSellingProduct
                    {
                        Product = g.First().Product,
                        TotalSold = g.Sum(x => x.Quantity),
                        TotalRevenue = g.Sum(x => x.Price * x.Quantity)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .Take(5)
                    .ToList(),

                SelectedPeriod = period
            };

            // Thêm doanh thu theo từng ngày trong tháng hiện tại
            if (period == "month")
            {
                dashboardViewModel.DailyRevenue = orders
                    .Where(o => o.PaymentStatus == PaymentStatus.Completed)
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new DailyRevenueData
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();
            }

            return View(dashboardViewModel);
        }
    }
}
