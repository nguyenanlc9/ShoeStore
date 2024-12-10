//using Microsoft.AspNetCore.Mvc;
//using ShoeStore.Models;

//namespace ShoeStore.Areas.Admin.Controllers
//{
//    public class DashboardController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public DashboardController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetRevenueStatistics(int year)
//        {
//            var statistics = await _context.Orders
//                .Where(o => o.OrderDate.Year == year && o.Status == OrderStatus.Completed)
//                .GroupBy(o => new { Month = o.OrderDate.Month })
//                .Select(g => new RevenueStatistics
//                {
//                    Period = "T" + g.Key.Month,
//                    Revenue = g.Sum(o => o.TotalAmount),
//                    Orders = g.Count()
//                })
//                .OrderBy(s => s.Period)
//                .ToListAsync();

//            return Json(statistics);
//        }
//    }

//}
