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
            var contacts = _context.Contacts.ToList();
            ViewBag.Contacts = contacts ?? new List<Contact>(); // Đảm bảo không null
            return View(new Contact()); // Truyền model rỗng
        }
        public IActionResult Index()
        {
            // Lấy slider có trạng thái active (Status = 1)
            var sliders = _context.Slider.Where(s => s.Status == 1).ToList();
            ViewBag.Sliders = sliders;

            // Lấy thông tin footer
            ViewData["FooterInfo"] = _context.Footers
                .OrderByDescending(f => f.FooterId)
                .FirstOrDefault();

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
