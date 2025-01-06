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
                .Include(p => p.ProductImages)
                .AsQueryable();

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
                ViewBag.SelectedCategoryId = categoryId;
            }

            // Lọc theo thương hiệu
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId);
                ViewBag.SelectedBrandId = brandId;
                ViewBag.SelectedBrand = _context.Brands.Find(brandId);
            }

            // Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                query = query.Where(p => (p.Price - p.DiscountPrice) >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => (p.Price - p.DiscountPrice) <= maxPrice.Value);
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

            var products = await query.ToListAsync();

            // Phân trang
            int pageSize = 9; // Số sản phẩm trên mỗi trang
            int totalItems = products.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo page không vượt quá totalPages
            page = Math.Max(1, Math.Min(page, totalPages));

            // Lấy sản phẩm cho trang hiện tại
            var pagedProducts = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Thêm thông tin phân trang vào ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            // Load categories cho sidebar
            ViewBag.Categories = _context.Categories
                .Select(c => new
                {
                    c.CategoryId,
                    c.Name,
                    ProductCount = _context.Products.Count(p => p.CategoryId == c.CategoryId)
                }).ToList();

            // Load brands cho dropdown với số lượng sản phẩm
            ViewBag.Brands = _context.Brands
                .Select(b => new
                {
                    b.BrandId,
                    b.Name,
                    ProductCount = _context.Products.Count(p => p.BrandId == b.BrandId)
                }).ToList();

            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.CurrentSort = sort;

            // Thêm ViewBag cho price range
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(pagedProducts);
        }

        // GET: /Shop/Detail/{id}
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .Include(p => p.ProductSizeStocks)
                    .ThenInclude(pss => pss.Size)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Load related products
            var relatedProducts = await _context.Products
                .Include(p => p.Brands)
                .Include(p => p.ProductImages)
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;
            ViewBag.Reviews = product.Reviews?.OrderByDescending(r => r.CreatedAt).ToList() ?? new List<Review>();

            return View(product);
        }

        [HttpPost]
        public IActionResult AddReview(int productId, int rating, string comment)
        {
            try
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá" });
                }

                // Kiểm tra xem người dùng đã đánh giá sản phẩm này chưa
                var existingReview = _context.Reviews
                    .FirstOrDefault(r => r.ProductId == productId && r.UserId == userInfo.UserID);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "Bạn đã đánh giá sản phẩm này rồi" });
                }

                var review = new Review
                {
                    ProductId = productId,
                    UserId = userInfo.UserID,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);

                // Cập nhật rating trung bình và số lượng đánh giá của sản phẩm
                var product = _context.Products.Find(productId);
                if (product != null)
                {
                    // Lấy tất cả reviews bao gồm cả review mới
                    var reviews = _context.Reviews.Where(r => r.ProductId == productId).ToList();
                    reviews.Add(review); // Thêm review mới vào danh sách để tính trung bình

                    // Kiểm tra có reviews không trước khi tính trung bình
                    if (reviews.Any())
                    {
                        product.Rating = (int)Math.Round(reviews.Average(r => r.Rating));
                        product.ReviewCount = reviews.Count;
                    }
                    else
                    {
                        // Nếu chưa có review nào, set rating bằng review đầu tiên
                        product.Rating = rating;
                        product.ReviewCount = 1;
                    }
                }

                _context.SaveChanges();

                return Json(new { success = true, message = "Đánh giá của bạn đã được ghi nhận" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int sizeId, int quantity)
        {
            try
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để mua hàng" });
                }

                // Kiểm tra tồn kho
                var sizeStock = _context.ProductSizeStocks
                    .FirstOrDefault(pss => pss.ProductID == productId && pss.SizeID == sizeId);

                if (sizeStock == null || sizeStock.StockQuantity < quantity)
                {
                    return Json(new { success = false, message = "Số lượng sản phẩm không đủ" });
                }

                // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
                var existingCartItem = _context.CartItems
                    .FirstOrDefault(ci => ci.UserId == userInfo.UserID && 
                                        ci.ProductId == productId && 
                                        ci.SizeId == sizeId);

                if (existingCartItem != null)
                {
                    // Nếu đã có, cập nhật số lượng
                    if (existingCartItem.Quantity + quantity > sizeStock.StockQuantity)
                    {
                        return Json(new { success = false, message = "Số lượng sản phẩm vượt quá tồn kho" });
                    }
                    existingCartItem.Quantity += quantity;
                }
                else
                {
                    // Nếu chưa có, tạo mới CartItem
                    var cartItem = new CartItem
                    {
                        UserId = userInfo.UserID,
                        ProductId = productId,
                        SizeId = sizeId,
                        Quantity = quantity,
                        CreatedAt = DateTime.Now
                    };
                    _context.CartItems.Add(cartItem);
                }

                _context.SaveChanges();

                // Lấy tổng số lượng trong giỏ hàng để trả về
                var cartCount = _context.CartItems
                    .Where(ci => ci.UserId == userInfo.UserID)
                    .Sum(ci => ci.Quantity);

                return Json(new { success = true, message = "Đã thêm vào giỏ hàng", cartCount = cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }

        }
        public IActionResult AddToWishlist(int productId)
        {
            try
            {
                // Lấy thông tin user từ session
                var user = HttpContext.Session.Get<User>("userInfo");
                if (user == null)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này" });
                }

                // Kiểm tra xem sản phẩm đã có trong wishlist chưa
                var existingWishlist = _context.Wishlist
                    .FirstOrDefault(w => w.UserId == user.UserID && w.ProductId == productId);

                if (existingWishlist != null)
                {
                    return Json(new { success = false, message = "Sản phẩm đã có trong danh sách yêu thích" });
                }

                // Thêm sản phẩm vào wishlist
                var wishlist = new Wishlist
                {
                    UserId = user.UserID,
                    ProductId = productId,
                };

                _context.Wishlist.Add(wishlist);
                _context.SaveChanges();

                return Json(new { success = true, message = "Đã thêm sản phẩm vào danh sách yêu thích" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
