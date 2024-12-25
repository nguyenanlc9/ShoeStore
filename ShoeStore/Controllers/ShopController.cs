using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ShoeStore.Utils;

namespace ShoeStore.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Shop
        public IActionResult Index(int? categoryId)
        {
            var query = _context.Products.AsQueryable();
            
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            var products = query.ToList();
            ViewBag.Categories = _context.Categories
                .Select(c => new
                {
                    c.CategoryId,
                    c.Name,
                    ProductCount = _context.Products.Count(p => p.CategoryId == c.CategoryId)
                }).ToList();
            
            ViewBag.SelectedCategoryId = categoryId;
            return View(products);
        }

        // GET: /Shop/Detail/{id}
        public IActionResult Detail(int id)
        {
            // Lấy sản phẩm theo id kèm theo size và số lượng tồn
            var product = _context.Products
                                .Include(p => p.Brands)
                                .Include(p => p.Categories)
                                .Include(p => p.ProductSizeStocks)
                                    .ThenInclude(pss => pss.Size)
                                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy các sản phẩm liên quan (cùng category)
            var relatedProducts = _context.Products
                                        .Include(p => p.Brands)
                                        .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id)
                                        .Take(4)
                                        .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            // Lấy đánh giá của sản phẩm
            ViewBag.Reviews = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return View(product);
        }

        [HttpPost]
        [Authorize]
        public IActionResult AddReview(int productId, int rating, string comment)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = userInfo.UserID,
                Rating = rating,
                Comment = comment
            };

            _context.Reviews.Add(review);

            // Cập nhật rating trung bình của sản phẩm
            var product = _context.Products.Find(productId);
            var reviews = _context.Reviews.Where(r => r.ProductId == productId);
            product.Rating = (int)Math.Round(reviews.Average(r => r.Rating));
            product.ReviewCount = reviews.Count();

            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}
