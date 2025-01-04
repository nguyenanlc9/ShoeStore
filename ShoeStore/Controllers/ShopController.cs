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
        public async Task<IActionResult> Index(int? categoryId, int? brandId, string sort, decimal? minPrice, decimal? maxPrice, int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Brands)
                .Include(p => p.Categories)
                .Include(p => p.ProductSizeStocks)
                .AsQueryable();

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
                ViewBag.SelectedCategoryId = categoryId;
                ViewBag.SelectedCategory = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            }

            // Lọc theo thương hiệu
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId);
                ViewBag.SelectedBrandId = brandId;
                ViewBag.SelectedBrand = await _context.Brands.FirstOrDefaultAsync(b => b.BrandId == brandId);
            }

            // Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                query = query.Where(p => (p.Price - p.DiscountPrice) >= minPrice.Value);
                ViewBag.MinPrice = minPrice;
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => (p.Price - p.DiscountPrice) <= maxPrice.Value);
                ViewBag.MaxPrice = maxPrice;
            }

            // Sắp xếp sản phẩm
            switch (sort)
            {
                case "name-asc":
                    query = query.OrderBy(p => p.Name);
                    ViewBag.SortLabel = "Tên, A đến Z";
                    break;
                case "name-desc":
                    query = query.OrderByDescending(p => p.Name);
                    ViewBag.SortLabel = "Tên, Z đến A";
                    break;
                case "price-asc":
                    query = query.OrderBy(p => p.Price - p.DiscountPrice);
                    ViewBag.SortLabel = "Giá, thấp đến cao";
                    break;
                case "price-desc":
                    query = query.OrderByDescending(p => p.Price - p.DiscountPrice);
                    ViewBag.SortLabel = "Giá, cao đến thấp";
                    break;
                default:
                    query = query.OrderByDescending(p => p.UpdatedDate);
                    ViewBag.SortLabel = "Mới nhất";
                    break;
            }
            ViewBag.CurrentSort = sort;

            // Get user's wishlist if authenticated
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var userWishlist = await _context.Wishlists
                    .Where(w => w.UserId == userId)
                    .Select(w => w.ProductId)
                    .ToListAsync();

                ViewBag.UserWishlist = userWishlist;
            }

            // Phân trang
            int pageSize = 9;
            var products = await query.ToListAsync();
            var pagedProducts = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(products.Count / (double)pageSize);
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < ViewBag.TotalPages;

            // Load categories và brands
            ViewBag.Categories = await _context.Categories
                .Select(c => new
                {
                    c.CategoryId,
                    c.Name,
                    ProductCount = _context.Products.Count(p => p.CategoryId == c.CategoryId)
                }).ToListAsync();

            ViewBag.Brands = await _context.Brands
                .Select(b => new
                {
                    b.BrandId,
                    b.Name,
                    ProductCount = _context.Products.Count(p => p.BrandId == b.BrandId)
                }).ToListAsync();

            return View(pagedProducts);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var existingWishlist = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingWishlist != null)
            {
                _context.Wishlists.Remove(existingWishlist);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Removed from wishlist successfully" });
            }
            else
            {
                var wishlist = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.Now
                };

                await _context.Wishlists.AddAsync(wishlist);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Added to wishlist successfully" });
            }
        }

        [Authorize]
        public async Task<IActionResult> Wishlist()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var wishlistedProducts = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.Product)
                .ToListAsync();

            return View(wishlistedProducts);
        }

        // Các action khác giữ nguyên...
    }
}
