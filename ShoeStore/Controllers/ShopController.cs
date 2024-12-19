using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;

namespace ShoesStore.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context; // Inject DbContext
        }

        // GET: /Shop
        public IActionResult Index()
        {
            // Lấy dữ liệu từ bảng Products và Categories
            var products = _context.Products
                                   .Include(p => p.Brands)
                                   .Include(p => p.Categories)
                                   .ToList();
                           
            var categories = _context.Categories
                                    .Select(c => new
                                    {
                                        c.Name,
                                        ProductCount = _context.Products.Count(p => p.CategoryId == c.CategoryId)
                                    })
                                    .ToList();

            // Truyền dữ liệu sang View bằng ViewBag
            ViewBag.Categories = categories;
            
            return View(products);
        }

        // GET: /Shop/Detail/{id}
        public IActionResult Detail(int id)
        {
            // Lấy sản phẩm theo id
            var product = _context.Products
                                  .Include(p => p.Brands)
                                  .Include(p => p.Categories)
                                  .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
