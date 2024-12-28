using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using System.Diagnostics;

namespace Project_BE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
        public IActionResult Index()
        {
            ViewData["HotContact"] = _context.Contacts
                .AsNoTracking()
                .OrderBy(x => x.ContactName)
                .ToList();

            // Lấy sản phẩm nổi bật (5 sản phẩm mới nhất)
            ViewData["FeaturedProducts"] = _context.Products
                .Include(p => p.ProductSizeStocks)
                    .ThenInclude(ps => ps.Size)
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .Where(p => p.Status == ProductStatus.Available)
                .OrderByDescending(p => p.UpdatedDate)
                .Take(5)
                .ToList();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }


    }
}
