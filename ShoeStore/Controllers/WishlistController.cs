using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Filters;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ShoeStore.Utils;

namespace ShoeStore.Controllers
{
    [UserAuthorize] 
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            var wishlist = await _context.Wishlists
                .Include(w => w.Product)
                .Where(w => w.UserId == userInfo.UserID)
                .OrderByDescending(w => w.AddedDate)
                .ToListAsync();

            return View(wishlist);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");

            // Kiểm tra sản phẩm tồn tại
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            // Kiểm tra sản phẩm đã có trong wishlist chưa
            var existingItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userInfo.UserID && w.ProductId == productId);

            if (existingItem != null)
            {
                return Json(new { success = false, message = "Sản phẩm đã có trong danh sách yêu thích" });
            }

            // Thêm vào wishlist
            var wishlistItem = new Wishlist
            {
                UserId = userInfo.UserID,
                ProductId = productId,
                Status = WishlistStatus.InStock,
                AddedDate = DateTime.Now
            };

            _context.Wishlists.Add(wishlistItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm vào danh sách yêu thích" });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveById(int wishlistId)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.WishlistId == wishlistId && w.UserId == userInfo.UserID);

            if (wishlistItem == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong danh sách yêu thích" });
            }

            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa sản phẩm khỏi danh sách yêu thích" });
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            var userWishlist = await _context.Wishlists
                .Where(w => w.UserId == userInfo.UserID)
                .ToListAsync();

            if (userWishlist.Any())
            {
                _context.Wishlists.RemoveRange(userWishlist);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Đã xóa toàn bộ danh sách yêu thích" });
        }

        [HttpGet]
        public async Task<IActionResult> GetWishlistCount()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return Json(new { count = 0 });
            }

            var count = await _context.Wishlists.CountAsync(w => w.UserId == userInfo.UserID);
            return Json(new { count });
        }
    }
} 