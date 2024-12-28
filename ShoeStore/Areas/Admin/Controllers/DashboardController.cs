using Microsoft.AspNetCore.Mvc;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Filters;
using System.Globalization;

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

        public IActionResult Daily()
        {
            var today = DateTime.Today;
            var stats = _context.Orders
                .Where(o => o.OrderDate.Date == today)
                .GroupBy(o => new { o.OrderDate.Hour })
                .Select(g => new StatisticsViewModel
                {
                    Label = $"{g.Key.Hour}:00",
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            return View(stats);
        }

        public IActionResult Monthly()
        {
            var currentMonth = DateTime.Today.Month;
            var currentYear = DateTime.Today.Year;
            var stats = _context.Orders
                .Where(o => o.OrderDate.Month == currentMonth && o.OrderDate.Year == currentYear)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new StatisticsViewModel
                {
                    Label = g.Key.ToString("dd/MM"),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            return View(stats);
        }

        public IActionResult Yearly()
        {
            var currentYear = DateTime.Today.Year;
            var stats = _context.Orders
                .Where(o => o.OrderDate.Year == currentYear)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new StatisticsViewModel
                {
                    Label = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            return View(stats);
        }
    }
}
