using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Filters;
using ShoeStore.Models;
using ShoeStore.Models.ViewModels;
using ShoeStore.Models.Enums;
using System.Linq;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string period = "month")
        {
            var today = DateTime.Today;
            var startDate = today;
            var endDate = today.AddDays(1);

            // Xác định khoảng thời gian dựa trên period
            switch (period.ToLower())
            {
                case "week":
                    startDate = today.AddDays(-7);
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

            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate < endDate)
                .ToListAsync();

            // Thống kê doanh thu theo ngày
            var dailyRevenue = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Thống kê đơn hàng theo phương thức thanh toán
            var paymentStats = orders
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            // Thống kê khách hàng mới
            var newCustomers = await _context.Users
                .Where(u => u.RoleID == 2 && u.RegisterDate >= startDate && u.RegisterDate < endDate)
                .CountAsync();

            var dashboardViewModel = new DashboardViewModel
            {
                SelectedPeriod = period,
                TotalOrders = orders.Count,
                NewOrders = orders.Count(o => o.Status == OrderStatus.Pending),
                TotalRevenue = orders
                    .Where(o => o.Status == OrderStatus.Completed)
                    .Sum(o => o.TotalAmount),
                TotalCustomers = await _context.Users.CountAsync(u => u.RoleID == 2),
                NewCustomers = newCustomers,
                DailyRevenue = dailyRevenue.Select(x => new DailyRevenueData
                {
                    Date = x.Date,
                    Revenue = x.Revenue
                }).ToList(),
                PaymentStats = paymentStats.Select(x => new PaymentStats
                {
                    Method = x.Method,
                    Count = x.Count,
                    Revenue = x.Revenue
                }).ToList()
            };

            return View(dashboardViewModel);
        }
    }
}
