using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Utils;
using Microsoft.Extensions.Configuration;
using ShoeStore.Services.Payment;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using ShoeStore.Models.Payment;
using ShoeStore.Helpers;
using ShoeStore.Models.ViewModels;
using ShoeStore.Services.APIAddress;
using ShoeStore.Services.Email;
using ShoeStore.Services.MemberRanking;
using ShoeStore.Areas.Admin.Controllers;

namespace ShoeStore.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách yêu thích
        [HttpGet]
        public async Task<IActionResult> Index(int userId)
        {


            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var wishlists = _context.Wishlist
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userInfo.UserID)
                .ToList();

            return View(wishlists);
        }

        // Thêm sản phẩm vào danh sách yêu thích
        [HttpPost]
        public async Task<IActionResult> AddToWishlist(int userId, int productId)
        {
            // Kiểm tra sản phẩm đã có trong wishlist chưa
            var existingItem = await _context.Wishlist
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem != null)
            {
                return Json(new { success = false, message = "Sản phẩm đã có trong danh sách yêu thích." });
            }

            // Tạo một mục mới trong wishlist
            var wishlistItem = new Wishlist
            {
                UserId = userId,
                ProductId = productId
            };

            _context.Wishlist.Add(wishlistItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Sản phẩm đã được thêm vào danh sách yêu thích." });
        }

        // Xóa sản phẩm khỏi danh sách yêu thích
        [HttpPost]
        [Route("Wishlist/RemoveById")]
        public async Task<IActionResult> RemoveFromWishlist(int wishlistId)
        {
            // Kiểm tra người dùng đã đăng nhập hay chưa
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện thao tác này." });
            }

            // Kiểm tra sản phẩm trong danh sách yêu thích
            var wishlistItem = await _context.Wishlist
                .FirstOrDefaultAsync(ci => ci.WishlistId == wishlistId && ci.UserId == userInfo.UserID);

            if (wishlistItem == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong danh sách yêu thích." });
            }

            // Xóa sản phẩm khỏi danh sách yêu thích
            _context.Wishlist.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa sản phẩm khỏi danh sách yêu thích." });
        }
    }
}
