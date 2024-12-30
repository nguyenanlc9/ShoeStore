using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Filters;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IActionResult> Index(string period = "month")
        {
            var today = DateTime.Today;
            var startOfDay = today;
            var endOfDay = today.AddDays(1).AddTicks(-1);

            var viewModel = new DashboardViewModel
            {
                // Tổng số đơn hàng
                TotalOrders = await _context.Orders.CountAsync(),
                
                // Đơn hàng mới (trong ngày)
                NewOrders = await _context.Orders
                    .Where(o => o.OrderDate >= startOfDay && o.OrderDate <= endOfDay)
                    .CountAsync(),
                
                // Tổng doanh thu
                TotalRevenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed)
                    .SumAsync(o => o.TotalAmount),
                
                // Sản phẩm sắp hết hàng (dưới 10 sản phẩm)
                LowStockProducts = await _context.ProductSizeStocks
                    .Where(p => p.StockQuantity < 10)
                    .CountAsync(),
                
                // 5 đơn hàng gần nhất
                RecentOrders = await _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync(),

                SelectedPeriod = period
            };

            // Top sản phẩm bán chạy
            viewModel.TopSellingProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .GroupBy(od => od.Product)
                .Select(g => new TopSellingProduct
                {
                    Product = g.Key,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Price * od.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            // Doanh thu theo ngày trong tháng hiện tại
            if (period == "month")
            {
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                viewModel.DailyRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= startOfMonth && 
                               o.OrderDate <= endOfMonth && 
                               o.Status == OrderStatus.Completed)
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new DailyRevenueData
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();
            }

            return View(viewModel);
        }
    }
}
