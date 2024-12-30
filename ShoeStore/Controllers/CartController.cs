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
using ShoeStore.Services;
using ShoeStore.Services.APIAddress;

namespace ShoeStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemberRankService _memberRankService;
        private readonly IAddressService _addressService;

        public CartController(
            ApplicationDbContext context, 
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IMemberRankService memberRankService,
            IAddressService addressService)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _memberRankService = memberRankService;
            _addressService = addressService;
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

        public async Task<IActionResult> Checkout()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Size)
                .Where(ci => ci.UserId == userInfo.UserID)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            // Lấy thông tin member rank của user
            var user = await _context.Users
                .Include(u => u.MemberRank)
                .FirstOrDefaultAsync(u => u.UserID == userInfo.UserID);

            decimal subtotal = cartItems.Sum(x => (x.Product.Price - x.Product.DiscountPrice) * x.Quantity);
            decimal finalTotal = subtotal;
            decimal discountAmount = 0;

            if (user?.MemberRank != null)
            {
                discountAmount = subtotal * (user.MemberRank.DiscountPercent / 100m);
                finalTotal = subtotal - discountAmount;
            }

            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                SubTotal = subtotal,
                DiscountAmount = discountAmount,
                FinalTotal = finalTotal,
                MemberRankDiscountPercent = user?.MemberRank?.DiscountPercent,
                MemberRankName = user?.MemberRank?.RankName,
                FullName = user?.FullName,
                PhoneNumber = user?.Phone,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string couponCode)
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

            // Tính toán giá trị giảm giá
            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userInfo.UserID)
                .ToListAsync();

            decimal subtotal = cartItems.Sum(x => (x.Product.Price - x.Product.DiscountPrice) * x.Quantity);
            
            // Tính giảm giá thành viên
            var user = await _context.Users
                .Include(u => u.MemberRank)
                .FirstOrDefaultAsync(u => u.UserID == userInfo.UserID);
            
            decimal memberDiscountAmount = 0;
            if (user?.MemberRank != null)
            {
                memberDiscountAmount = subtotal * (user.MemberRank.DiscountPercent / 100m);
            }

            // Tính giảm giá từ mã
            decimal couponDiscountAmount = subtotal * (coupon.DiscountPercentage / 100m);
            
            // Tổng tiền sau khi trừ cả hai loại giảm giá
            decimal finalTotal = subtotal - memberDiscountAmount - couponDiscountAmount;

            HttpContext.Session.Set("AppliedCoupon", coupon);

            return Json(new { 
                success = true, 
                message = $"Đã áp dụng mã giảm giá: {coupon.DiscountPercentage}%",
                discountAmount = couponDiscountAmount.ToString("N0"),
                finalTotal = finalTotal.ToString("N0")
            });
        }

        [HttpPost]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove("AppliedCoupon");
            return Json(new { success = true, message = "Đã hủy mã giảm giá" });
        }

        private string GenerateOrderCode()
        {
            return $"DH{DateTime.Now:yyyyMMddHHmmss}";
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            try
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                if (userInfo == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Include(ci => ci.Size)
                    .Where(ci => ci.UserId == userInfo.UserID)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    return RedirectToAction("Index");
                }

                // Lấy coupon đã áp dụng từ session một lần duy nhất ở đầu phương thức
                var appliedCoupon = HttpContext.Session.Get<Coupon>("AppliedCoupon");

                decimal subtotal = cartItems.Sum(x => (x.Product.Price - x.Product.DiscountPrice) * x.Quantity);
                
                // Tính giảm giá thành viên
                decimal memberDiscountAmount = 0;
                var currentUser = await _context.Users
                    .Include(u => u.MemberRank)
                    .FirstOrDefaultAsync(u => u.UserID == userInfo.UserID);
                    
                if (currentUser?.MemberRank != null)
                {
                    memberDiscountAmount = subtotal * (currentUser.MemberRank.DiscountPercent / 100m);
                }

                // Tính giảm giá từ mã giảm giá đã áp dụng
                decimal couponDiscountAmount = 0;
                if (appliedCoupon != null)
                {
                    couponDiscountAmount = subtotal * (appliedCoupon.DiscountPercentage / 100m);
                }

                // Tổng tiền sau khi trừ cả hai loại giảm giá
                decimal finalTotal = subtotal - memberDiscountAmount - couponDiscountAmount;

                var fullAddress = await BuildFullAddress(
                    _addressService,
                    model.ProvinceCode,
                    model.DistrictCode,
                    model.WardCode,
                    model.AddressDetail
                );

                var order = new Order
                {
                    UserId = userInfo.UserID,
                    OrderUsName = model.FullName,
                    OrderCode = GenerateOrderCode(),
                    OrderDate = DateTime.Now,
                    ShippingAddress = fullAddress,
                    PhoneNumber = model.PhoneNumber,
                    Notes = model.Notes,
                    Status = OrderStatus.Pending,
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = PaymentStatus.Pending,
                    TotalAmount = finalTotal,
                    CouponId = appliedCoupon?.CouponId,
                    OrderCoupon = appliedCoupon?.CouponCode
                };

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
                    var stock = await _context.ProductSizeStocks
                        .FirstOrDefaultAsync(pss => pss.ProductID == item.ProductId && pss.SizeID == item.SizeId);
                    if (stock != null)
                    {
                        stock.StockQuantity -= item.Quantity;
                    }

                    _context.OrderDetails.Add(orderDetail);
                }

                // Xóa giỏ hàng
                _context.CartItems.RemoveRange(cartItems);

                // Xử lý giảm số lượng coupon nếu có
                if (appliedCoupon != null)
                {
                    var coupon = await _context.Coupons.FindAsync(appliedCoupon.CouponId);
                    if (coupon != null && coupon.Quantity > 0)
                    {
                        coupon.Quantity--;
                        if (coupon.Quantity == 0)
                        {
                            coupon.Status = false;
                        }
                        _context.Coupons.Update(coupon);
                    }
                    else
                    {
                        return RedirectToAction("Checkout", new { error = "Mã giảm giá đã hết lượt sử dụng" });
                    }
                }

                // Cập nhật TotalSpent cho user
                if (currentUser != null)
                {
                    currentUser.TotalSpent += finalTotal;
                    _context.Users.Update(currentUser);
                }

                // Lưu tất cả thay đổi vào database
                await _context.SaveChangesAsync();

                // Xóa coupon khỏi session sau khi đã xử lý xong
                HttpContext.Session.Remove("AppliedCoupon");

                // Xử lý theo phương thức thanh toán
                switch (model.PaymentMethod)
                {
                    case PaymentMethod.Cash:
                        order.Status = OrderStatus.Processing;
                        order.PaymentStatus = PaymentStatus.Pending;
                        await _context.SaveChangesAsync();
                        return RedirectToAction("Thankyou", "Cart", new { orderId = order.OrderId });

                    case PaymentMethod.VNPay:
                        await _context.SaveChangesAsync(); // Lưu order trước khi chuyển sang VNPay
                        return RedirectToAction("ProcessVnPay", "Payment", new { 
                            orderId = order.OrderId,
                            amount = finalTotal // Truyền tổng tiền đã tính cả giảm giá
                        });

                    case PaymentMethod.Momo:
                        await _context.SaveChangesAsync();
                        return RedirectToAction("ProcessPayment", "Momo", new { 
                            orderId = order.OrderId,
                            amount = finalTotal
                        });

                    default:
                        TempData["Error"] = "Phương thức thanh toán không hợp lệ";
                        return RedirectToAction("Checkout");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Checkout: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi xử lý đơn hàng: " + ex.Message;
                return RedirectToAction("Checkout");
            }
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

        private async Task UpdateUserRankAfterPayment(int userId, decimal orderAmount)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.TotalSpent += orderAmount;
                await _context.SaveChangesAsync();
                await _memberRankService.UpdateUserRank(userId);
            }
        }

        private async Task<string> BuildFullAddress(
            IAddressService addressService,
            int provinceCode,
            int districtCode,
            int wardCode,
            string addressDetail)
        {
            var province = await addressService.GetProvinceName(provinceCode);
            var district = await addressService.GetDistrictName(districtCode);
            var ward = await addressService.GetWardName(wardCode);

            return $"{addressDetail}, {ward}, {district}, {province}";
        }
    }
}

