using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Index()
        {
            var newProducts = await _context.Products
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .Include(p => p.ProductImages)
                .Where(p => p.IsNew)
                .Take(8)
                .ToListAsync();

            var hotProducts = await _context.Products
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .Include(p => p.ProductImages)
                .Where(p => p.IsHot)
                .Take(8)
                .ToListAsync();

            var saleProducts = await _context.Products
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .Include(p => p.ProductImages)
                .Where(p => p.IsSale)
                .Take(8)
                .ToListAsync();

            ViewBag.NewProducts = newProducts;
            ViewBag.HotProducts = hotProducts;
            ViewBag.SaleProducts = saleProducts;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }


    }
}
