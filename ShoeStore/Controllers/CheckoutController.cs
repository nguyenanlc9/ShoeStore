using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;

namespace ShoeStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userIdInt = int.Parse(userId);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.Size)
                .Where(c => c.UserId == userIdInt)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Lấy các phương thức thanh toán đang hoạt động
            var availablePaymentMethods = await _context.PaymentMethodConfigs
                .Where(p => p.Status == PaymentMethodStatus.Active)
                .OrderBy(p => p.Type)
                .ToListAsync();

            ViewBag.PaymentMethods = availablePaymentMethods;
            ViewBag.CartItems = cartItems;
            
            // Tính tổng tiền
            decimal total = cartItems.Sum(item => 
            {
                var price = item.Product.DiscountPrice > 0 ? item.Product.DiscountPrice : item.Product.Price;
                return price * item.Quantity;
            });
            ViewBag.Total = total;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string address, string phone, PaymentMethodType paymentMethod)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Kiểm tra xem phương thức thanh toán có khả dụng không
            var paymentMethodConfig = await _context.PaymentMethodConfigs
                .FirstOrDefaultAsync(p => p.Type == paymentMethod && p.Status == PaymentMethodStatus.Active);

            if (paymentMethodConfig == null)
            {
                TempData["error"] = "Phương thức thanh toán không khả dụng";
                return RedirectToAction(nameof(Index));
            }

            // Xử lý đặt hàng...
            // (phần code xử lý đặt hàng hiện tại của bạn)

            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }
    }
} 