using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.ViewModels;
using System.Diagnostics;
using ShoeStore.Utils;

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
            // Lấy slider
            var sliders = await _context.Sliders.ToListAsync();
            ViewBag.Sliders = sliders;

            // Lấy 8 sản phẩm mới nhất
            var newProducts = await _context.Products
                .Include(p => p.Brands)
                .Include(p => p.Categories)
                .OrderByDescending(p => p.CreatedDate)
                .Take(8)
                .Select(p => new ProductViewModel { Product = p })
                .ToListAsync();
            ViewData["NewProducts"] = newProducts;

            // Lấy 8 sản phẩm đang giảm giá
            var saleProducts = await _context.Products
                .Include(p => p.Brands)
                .Include(p => p.Categories)
                .Where(p => p.DiscountPrice > 0)
                .OrderByDescending(p => p.DiscountPrice)
                .Take(8)
                .Select(p => new ProductViewModel { Product = p })
                .ToListAsync();
            ViewData["SaleProducts"] = saleProducts;

            // Lấy 8 sản phẩm hot (có nhiều đơn hàng nhất)
            var hotProducts = await _context.Products
                .Include(p => p.Brands)
                .Include(p => p.Categories)
                .Include(p => p.OrderDetails)
                .OrderByDescending(p => p.OrderDetails.Count)
                .Take(8)
                .Select(p => new ProductViewModel { Product = p })
                .ToListAsync();
            ViewData["HotProducts"] = hotProducts;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Returns()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return View(null);
            }

            // Lấy các đơn hàng trong 7 ngày gần đây và chưa có yêu cầu đổi trả
            var recentOrders = await _context.Orders
                .Where(o => o.UserId == userInfo.UserID 
                    && o.OrderDate >= DateTime.Now.AddDays(-7)
                    && !_context.ReturnRequests.Any(r => r.OrderId == o.OrderId))
                .ToListAsync();

            return View(recentOrders);
        }

        public async Task<IActionResult> Sale(int? categoryId, int? brandId, decimal? minPrice, decimal? maxPrice, string sortOrder)
        {
            // Lấy danh sách danh mục và thương hiệu cho filter
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedBrandId = brandId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentSort = sortOrder;

            // Query cơ bản
            var query = _context.Products
                .Include(p => p.ProductSizeStocks)
                    .ThenInclude(ps => ps.Size)
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .Where(p => p.Status == ProductStatus.Available 
                    && p.DiscountPrice > 0 
                    && p.DiscountPrice < p.Price);

            // Áp dụng các bộ lọc
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.DiscountPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.DiscountPrice <= maxPrice.Value);
            }

            // Áp dụng sắp xếp
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.DiscountPrice),
                "price_desc" => query.OrderByDescending(p => p.DiscountPrice),
                "discount_desc" => query.OrderByDescending(p => (p.Price - p.DiscountPrice) / p.Price),
                _ => query.OrderByDescending(p => (p.Price - p.DiscountPrice) / p.Price)
            };

            var saleProducts = await query
                .Select(p => new ProductViewModel
                {
                    Product = p,
                    AvailableSizes = p.ProductSizeStocks
                        .Where(ps => ps.StockQuantity > 0)
                        .Select(ps => ps.Size.SizeValue)
                        .ToList()
                })
                .ToListAsync();

            return View(saleProducts);
        }

        public async Task<IActionResult> NewArrivals(int? categoryId, int? brandId, decimal? minPrice, decimal? maxPrice, string sortOrder)
        {
            // Lấy danh sách danh mục và thương hiệu cho filter
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedBrandId = brandId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentSort = sortOrder;

            // Query cơ bản
            var query = _context.Products
                .Include(p => p.ProductSizeStocks)
                    .ThenInclude(ps => ps.Size)
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .Where(p => p.Status == ProductStatus.Available && p.IsNew);

            // Áp dụng các bộ lọc
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            if (minPrice.HasValue)
            {
                var min = minPrice.Value;
                query = query.Where(p => (p.DiscountPrice > 0 ? p.DiscountPrice : p.Price) >= min);
            }

            if (maxPrice.HasValue)
            {
                var max = maxPrice.Value;
                query = query.Where(p => (p.DiscountPrice > 0 ? p.DiscountPrice : p.Price) <= max);
            }

            // Áp dụng sắp xếp
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.DiscountPrice > 0 ? p.DiscountPrice : p.Price),
                "price_desc" => query.OrderByDescending(p => p.DiscountPrice > 0 ? p.DiscountPrice : p.Price),
                "date_desc" => query.OrderByDescending(p => p.CreatedDate),
                _ => query.OrderByDescending(p => p.CreatedDate)
            };

            var newProducts = await query
                .Select(p => new ProductViewModel
                {
                    Product = p,
                    AvailableSizes = p.ProductSizeStocks
                        .Where(ps => ps.StockQuantity > 0)
                        .Select(ps => ps.Size.SizeValue)
                        .ToList()
                })
                .ToListAsync();

            return View(newProducts);
        }

    }
}