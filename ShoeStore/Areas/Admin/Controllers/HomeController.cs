using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Filters;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Models.ViewModels;
using System.Linq;

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

            var dashboardViewModel = new DashboardViewModel
            {
                TotalOrders = _context.Orders.Count(),
                NewOrders = _context.Orders.Where(o => o.Status == OrderStatus.Pending).Count(),
                TotalRevenue = _context.Orders
                    .Where(o => o.PaymentStatus == PaymentStatus.Completed 
                        && o.OrderDate >= startDate 
                        && o.OrderDate < endDate)
                    .Sum(o => o.TotalAmount),
                TotalProducts = _context.Products.Count(),
                LowStockProducts = _context.ProductSizeStocks
                    .Where(p => p.StockQuantity < 10)
                    .Count(),
                TotalCustomers = _context.Users.Where(u => u.RoleID == 2).Count(),
                RecentOrders = _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate < endDate)
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
                dashboardViewModel.DailyRevenue = _context.Orders
                    .Where(o => o.PaymentStatus == PaymentStatus.Completed 
                        && o.OrderDate.Month == today.Month 
                        && o.OrderDate.Year == today.Year)
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
