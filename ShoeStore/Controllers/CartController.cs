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

namespace ShoeStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartController(
            ApplicationDbContext context, 
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult Index()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Size)
                .Where(ci => ci.UserId == userInfo.UserID)
                .ToList();

            return View(cartItems);
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int cartItemId, int quantity)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập";
                return RedirectToAction("Index");
            }

            var cartItem = _context.CartItems
                .Include(ci => ci.Product)
                .ThenInclude(p => p.ProductSizeStocks)
                .FirstOrDefault(ci => ci.CartItemId == cartItemId && ci.UserId == userInfo.UserID);

            if (cartItem != null)
            {
                var stock = cartItem.Product.ProductSizeStocks
                    .FirstOrDefault(pss => pss.SizeID == cartItem.SizeId)?.StockQuantity ?? 0;

                if (quantity > stock)
                {
                    TempData["Error"] = $"Số lượng sản phẩm {cartItem.Product.Name} vượt quá tồn kho";
                }
                else if (quantity < 1)
                {
                    TempData["Error"] = "Số lượng phải lớn hơn 0";
                }
                else
                {
                    cartItem.Quantity = quantity;
                    _context.SaveChanges();
                    TempData["Success"] = "Đã cập nhật số lượng";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveItem(int cartItemId)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập";
                return RedirectToAction("Index");
            }

            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.CartItemId == cartItemId && ci.UserId == userInfo.UserID);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng";
            }

            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Size)
                .Where(ci => ci.UserId == userInfo.UserID)
                .ToList();

            if (string.IsNullOrEmpty(userInfo.Address))
            {
                TempData["Error"] = "Vui lòng cập nhật địa chỉ giao hàng trước khi thanh toán";
                return RedirectToAction("Profile", "Account");
            }

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            return View(cartItems);
        }

        [HttpPost]
        public IActionResult ApplyCoupon(string couponCode)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            if (string.IsNullOrEmpty(couponCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá" });
            }

            var coupon = _context.Coupons
                .FirstOrDefault(c => c.CouponCode == couponCode && 
                                    c.Status && 
                                    DateTime.Now >= c.DateStart && 
                                    DateTime.Now <= c.DateEnd &&
                                    c.Quantity > 0);

            if (coupon == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn" });
            }

            HttpContext.Session.Set("AppliedCoupon", coupon);
            return Json(new { success = true, message = $"Đã áp dụng mã giảm giá: {coupon.DiscountPercentage}%" });
        }

        [HttpPost]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove("AppliedCoupon");
            return Json(new { success = true, message = "Đã hủy mã giảm giá" });
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string FullName, string Address, string Email, string Phone, string Notes, PaymentMethod paymentMethod)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(Address))
            {
                Address = userInfo.Address;
            }

            var cartItems = _context.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Size)
                .Where(ci => ci.UserId == userInfo.UserID)
                .ToList();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            var appliedCoupon = HttpContext.Session.Get<Coupon>("AppliedCoupon");
            decimal subtotal = cartItems.Sum(x => (x.Product.Price - x.Product.DiscountPrice) * x.Quantity);
            decimal discount = appliedCoupon != null ? (subtotal * appliedCoupon.DiscountPercentage / 100) : 0;
            decimal total = subtotal - discount;

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = userInfo.UserID,
                OrderUsName = userInfo.FullName,
                OrderDate = DateTime.Now,
                TotalAmount = total,
                Status = OrderStatus.Pending,
                ShippingAddress = Address,
                PhoneNumber = Phone,
                Notes = Notes ?? string.Empty,
                CouponId = appliedCoupon?.CouponId,
                OrderCode = GenerateOrderCode(),
                OrderDescription = string.Empty,
                OrderCoupon = appliedCoupon?.CouponCode ?? string.Empty,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentMethod == PaymentMethod.Cash ? 
                    PaymentStatus.Completed : PaymentStatus.Pending,
                OrderStatus = true
            };

            // Thêm và lưu order trước
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();  // Lưu order để có OrderId

            // Sau đó mới tạo và thêm OrderDetails
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,  // Bây giờ đã có OrderId hợp lệ
                    ProductId = item.ProductId,
                    SizeId = item.SizeId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price - item.Product.DiscountPrice
                };

                // Cập nhật số lượng tồn kho
                var stock = _context.ProductSizeStocks
                    .FirstOrDefault(pss => pss.ProductID == item.ProductId && pss.SizeID == item.SizeId);
                if (stock != null)
                {
                    stock.StockQuantity -= item.Quantity;
                }

                _context.OrderDetails.Add(orderDetail);
            }

            // Xóa giỏ hàng
            _context.CartItems.RemoveRange(cartItems);
            
            // Xóa mã giảm giá đã áp dụng
            HttpContext.Session.Remove("AppliedCoupon");

            // Lưu các thay đổi còn lại
            await _context.SaveChangesAsync();

            // Xử lý theo phương thức thanh toán
            switch (paymentMethod)
            {
                case PaymentMethod.VNPay:
                    return RedirectToAction("ProcessVnPay", "Payment", new { orderId = order.OrderId });

                case PaymentMethod.Momo:
                    return RedirectToAction("ProcessPayment", "Momo", new { orderId = order.OrderId });

                case PaymentMethod.PayPal:
                    return RedirectToAction("ProcessPayment", "PayPal", new { orderId = order.OrderId });

                case PaymentMethod.Visa:
                    return RedirectToAction("ProcessPayment", "Visa", new { orderId = order.OrderId });

                default: // Cash
                    return RedirectToAction("Thankyou", "Cart", new { orderId = order.OrderId });
            }
        }

        private string GenerateOrderCode()
        {
            return $"DH{DateTime.Now:yyyyMMddHHmmss}";
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = _context.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Size)
                .Where(ci => ci.UserId == userInfo.UserID)
                .ToList();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index");
            }

            // Tạo đơn hàng mới
            var order = new Order
            {
                UserId = userInfo.UserID,
                OrderUsName = model.FullName,
                OrderCode = GenerateOrderCode(),
                OrderDate = DateTime.Now,
                ShippingAddress = model.Address,
                PhoneNumber = model.PhoneNumber,
                Notes = model.Notes,
                Status = OrderStatus.Pending,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                TotalAmount = cartItems.Sum(x => (x.Product.Price - x.Product.DiscountPrice) * x.Quantity)
            };

            _context.Orders.Add(order);

            // Thêm chi tiết đơn hàng
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    Order = order,
                    ProductId = item.ProductId,
                    SizeId = item.SizeId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price - item.Product.DiscountPrice
                };

                // Cập nhật số lượng tồn kho
                var stock = _context.ProductSizeStocks
                    .FirstOrDefault(pss => pss.ProductID == item.ProductId && pss.SizeID == item.SizeId);
                if (stock != null)
                {
                    stock.StockQuantity -= item.Quantity;
                }

                _context.OrderDetails.Add(orderDetail);
            }

            // Xóa giỏ hàng
            _context.CartItems.RemoveRange(cartItems);
            
            // Xóa mã giảm giá đã áp dụng
            HttpContext.Session.Remove("AppliedCoupon");

            await _context.SaveChangesAsync();

            // Xử lý theo phương thức thanh toán
            if (model.PaymentMethod == PaymentMethod.Cash)
            {
                order.Status = OrderStatus.Processing;
                order.PaymentStatus = PaymentStatus.Pending;
                await _context.SaveChangesAsync();
                return RedirectToAction("Thankyou", "Payment", new { orderId = order.OrderId });
            }
            else if (model.PaymentMethod == PaymentMethod.VNPay)
            {
                return RedirectToAction("ProcessVnPay", "Payment", new { orderId = order.OrderId });
            }
            else if (model.PaymentMethod == PaymentMethod.Momo)
            {
                return RedirectToAction("ProcessPayment", "Momo", new { orderId = order.OrderId });
            }
            
            TempData["Error"] = "Phương thức thanh toán không hợp lệ";
            return RedirectToAction("Checkout");
        }

        public async Task<IActionResult> Thankyou(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Size)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }
    }
}
