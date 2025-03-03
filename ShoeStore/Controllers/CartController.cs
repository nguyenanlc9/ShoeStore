﻿using Microsoft.AspNetCore.Mvc;
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
using ShoeStore.Services.Momo;
using ShoeStore.Services.GHN;
using Microsoft.AspNetCore.SignalR;
using ShoeStore.Services;

namespace ShoeStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemberRankService _memberRankService;
        private readonly IGHNAddressService _ghnAddressService;
        private readonly IEmailService _emailService;
        private readonly IMomoService _momoService;
        private readonly INotificationService _notificationService;

        public CartController(
            ApplicationDbContext context, 
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IMemberRankService memberRankService,
            IGHNAddressService ghnAddressService,
            IEmailService emailService,
            IMomoService momoService,
            INotificationService notificationService)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _memberRankService = memberRankService;
            _ghnAddressService = ghnAddressService;
            _emailService = emailService;
            _momoService = momoService;
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Auth");
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
                return RedirectToAction("Login", "Auth");
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

            decimal subtotal = cartItems.Sum(x => {
                decimal finalPrice = x.Product.DiscountPrice > 0 ? x.Product.DiscountPrice : x.Product.Price;
                return finalPrice * x.Quantity;
            });
            decimal finalTotal = subtotal;
            decimal memberDiscountAmount = 0;
            decimal couponDiscountAmount = 0;

            if (user?.MemberRank != null)
            {
                memberDiscountAmount = subtotal * (user.MemberRank.DiscountPercent / 100m);
                finalTotal = subtotal - memberDiscountAmount;
            }

            // Lấy coupon từ session nếu có
            var appliedCoupon = HttpContext.Session.Get<Coupon>("AppliedCoupon");
            if (appliedCoupon != null)
            {
                couponDiscountAmount = subtotal * (appliedCoupon.DiscountPercentage / 100m);
                finalTotal -= couponDiscountAmount;
            }

            // Lấy các phương thức thanh toán
            var availablePaymentMethods = await _context.PaymentMethodConfigs
                .Where(p => p.Status != PaymentMethodStatus.Hidden)
                .OrderBy(p => (int)p.Type)
                .ToListAsync();

            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                SubTotal = subtotal,
                DiscountAmount = memberDiscountAmount,
                CouponDiscountAmount = couponDiscountAmount,
                FinalTotal = finalTotal,
                MemberRankDiscountPercent = user?.MemberRank?.DiscountPercent,
                MemberRankName = user?.MemberRank?.RankName,
                FullName = user?.FullName,
                PhoneNumber = user?.Phone,
                AppliedCoupon = appliedCoupon
            };

            ViewBag.PaymentMethods = availablePaymentMethods;
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

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.CouponCode == couponCode && 
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

            if (!cartItems.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống" });
            }

            decimal subtotal = cartItems.Sum(x => {
                decimal finalPrice = x.Product.DiscountPrice > 0 ? x.Product.DiscountPrice : x.Product.Price;
                return finalPrice * x.Quantity;
            });
            
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

            HttpContext.Session.Set("AppliedCoupon", coupon);

            return Json(new { 
                success = true, 
                message = $"Đã áp dụng mã giảm giá: {coupon.DiscountPercentage}%",
                subtotal = subtotal.ToString("N0"),
                memberDiscountAmount = memberDiscountAmount.ToString("N0"),
                discountAmount = couponDiscountAmount.ToString("N0"),
                couponPercentage = coupon.DiscountPercentage
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
                    return RedirectToAction("Login", "Auth");
                }

                // Kiểm tra số điện thoại
                if (string.IsNullOrEmpty(model.PhoneNumber) || 
                    model.PhoneNumber == "Chưa cập nhật" || 
                    !System.Text.RegularExpressions.Regex.IsMatch(model.PhoneNumber, @"^\d+$"))
                {
                    TempData["Error"] = "Vui lòng cập nhật số điện thoại hợp lệ trước khi đặt hàng";
                    return RedirectToAction("EditProfile", "Account");
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

                decimal subtotal = cartItems.Sum(x => {
                    decimal finalPrice = x.Product.DiscountPrice > 0 ? x.Product.DiscountPrice : x.Product.Price;
                    return finalPrice * x.Quantity;
                });
                
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

                // Tổng tiền sau khi trừ cả hai loại giảm giá và cộng phí vận chuyển
                decimal totalDiscount = memberDiscountAmount + couponDiscountAmount;
                decimal finalTotal = subtotal - totalDiscount + model.ShipFeeGHN;

                var fullAddress = await BuildFullAddress(
                    _ghnAddressService,
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
                    Address = model.AddressDetail,
                    ProvinceCode = model.ProvinceCode.ToString(),
                    DistrictCode = model.DistrictCode.ToString(),
                    WardCode = model.WardCode.ToString(),
                    DistrictId = model.DistrictCode,
                    PhoneNumber = model.PhoneNumber,
                    Notes = model.Notes,
                    Status = OrderStatus.Pending,
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = PaymentStatus.Pending,
                    SubTotal = subtotal,
                    ShipFeeGHN = model.ShipFeeGHN,
                    MemberDiscount = memberDiscountAmount,
                    CouponDiscount = couponDiscountAmount,
                    Discount = totalDiscount,
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
                        Price = item.Product.DiscountPrice > 0 ? item.Product.DiscountPrice : item.Product.Price
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

                // Xóa coupon khỏi session sau khi đã xử lý xong
                HttpContext.Session.Remove("AppliedCoupon");

                // Xử lý theo phương thức thanh toán
                switch (model.PaymentMethod)
                {
                    case PaymentMethod.COD:
                        order.Status = OrderStatus.Processing;
                        order.PaymentStatus = PaymentStatus.Pending;
                        await _context.SaveChangesAsync();

                        // Tạo thông báo mới
                        var notification = new Notification
                        {
                            Message = $"Đơn hàng COD mới #{order.OrderId} từ {order.OrderUsName}",
                            Type = "new",
                            ReferenceId = order.OrderId.ToString(),
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            Url = $"/Admin/Order/Details/{order.OrderId}"
                        };

                        _context.Notifications.Add(notification);
                        await _context.SaveChangesAsync();

                        // Gửi thông báo realtime cho admin
                        await _notificationService.SendOrderNotification(
                            notification.Message,
                            notification.ReferenceId,
                            notification.Type
                        );

                        return RedirectToAction("Thankyou", "Cart", new { orderId = order.OrderId });

                    case PaymentMethod.VNPay:
                        await _context.SaveChangesAsync(); // Lưu order trước khi chuyển sang VNPay

                        // Tạo thông báo mới cho VNPay
                        var vnpayNotification = new Notification
                        {
                            Message = $"Đơn hàng VNPay mới #{order.OrderId} từ {order.OrderUsName}",
                            Type = "new",
                            ReferenceId = order.OrderId.ToString(),
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            Url = $"/Admin/Order/Details/{order.OrderId}"
                        };

                        _context.Notifications.Add(vnpayNotification);
                        await _context.SaveChangesAsync();

                        // Gửi thông báo realtime cho admin
                        await _notificationService.SendOrderNotification(
                            vnpayNotification.Message,
                            vnpayNotification.ReferenceId,
                            vnpayNotification.Type
                        );

                        return RedirectToAction("ProcessVnPay", "Payment", new { 
                            orderId = order.OrderId,
                            amount = finalTotal
                        });

                    case PaymentMethod.Momo:
                        await _context.SaveChangesAsync();

                        // Tạo thông báo mới cho Momo
                        var momoNotification = new Notification
                        {
                            Message = $"Đơn hàng Momo mới #{order.OrderId} từ {order.OrderUsName}",
                            Type = "new",
                            ReferenceId = order.OrderId.ToString(),
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            Url = $"/Admin/Order/Details/{order.OrderId}"
                        };

                        _context.Notifications.Add(momoNotification);
                        await _context.SaveChangesAsync();

                        // Gửi thông báo realtime cho admin
                        await _notificationService.SendOrderNotification(
                            momoNotification.Message,
                            momoNotification.ReferenceId,
                            momoNotification.Type
                        );

                        var orderInfo = new OrderInfoModel
                        {
                            OrderId = order.OrderCode,
                            Amount = (long)finalTotal,
                            OrderInfo = $"Thanh toán đơn hàng {order.OrderCode}",
                            FullName = order.OrderUsName
                        };
                        var response = await _momoService.CreatePaymentAsync(orderInfo);
                        if (response.ResultCode == 0)
                        {
                            return Redirect(response.PayUrl);
                        }
                        TempData["Error"] = $"Lỗi thanh toán Momo: {response.Message}";
                        return RedirectToAction("Checkout");

                    case PaymentMethod.ZaloPay:
                        await _context.SaveChangesAsync();
                        return RedirectToAction("ProcessPayment", "ZaloPay", new { 
                            orderId = order.OrderId
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
                .Include(o => o.User)
                .ThenInclude(u => u.MemberRank)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy thông tin địa chỉ từ GHN API
            try 
            {
                if (!string.IsNullOrEmpty(order.ProvinceCode))
                {
                    var provinces = await _ghnAddressService.GetProvinces();
                    var province = provinces.FirstOrDefault(p => p.ProvinceId == int.Parse(order.ProvinceCode));
                    ViewBag.ProvinceName = province?.ProvinceName;
                }
                
                if (!string.IsNullOrEmpty(order.DistrictCode))
                {
                    var districts = await _ghnAddressService.GetDistricts(int.Parse(order.ProvinceCode));
                    var district = districts.FirstOrDefault(d => d.DistrictId == int.Parse(order.DistrictCode));
                    ViewBag.DistrictName = district?.DistrictName;
                }
                
                if (!string.IsNullOrEmpty(order.WardCode))
                {
                    var wards = await _ghnAddressService.GetWards(int.Parse(order.DistrictCode));
                    var ward = wards.FirstOrDefault(w => w.WardCode == order.WardCode);
                    ViewBag.WardName = ward?.WardName;
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không throw exception
                Console.WriteLine($"Lỗi lấy thông tin địa chỉ: {ex.Message}");
            }

            // Tính toán giảm giá thành viên
            decimal memberDiscountAmount = 0;
            if (order.User?.MemberRank != null)
            {
                decimal subtotal = order.OrderDetails.Sum(od => od.Price * od.Quantity);
                memberDiscountAmount = subtotal * (order.User.MemberRank.DiscountPercent / 100m);
            }

            ViewBag.MemberDiscountAmount = memberDiscountAmount;

            // Chỉ gửi email khi đơn hàng đã thanh toán thành công hoặc là thanh toán COD
            if ((order.PaymentMethod == PaymentMethod.VNPay || order.PaymentMethod == PaymentMethod.Momo) && order.PaymentStatus == PaymentStatus.Completed
                || order.PaymentMethod == PaymentMethod.COD)
            {
                try
                {
                    var user = await _context.Users.FindAsync(order.UserId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        await _emailService.SendEmailAsync(
                            user.Email,
                            $"Xác nhận đơn hàng #{order.OrderCode}",
                            EmailTemplates.GetOrderConfirmationEmail(order)
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không throw exception để không ảnh hưởng đến việc hiển thị trang thank you
                    Console.WriteLine($"Lỗi gửi email: {ex.Message}");
                }
            }

            return View(order);
        }

        private async Task UpdateUserRankAfterPayment(int userId, decimal orderAmount)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // Chỉ cập nhật TotalSpent và rank khi đơn hàng hoàn thành
                var completedOrders = await _context.Orders
                    .Where(o => o.UserId == userId && o.Status == OrderStatus.Completed)
                    .SumAsync(o => o.TotalAmount);
                
                user.TotalSpent = completedOrders;
                await _context.SaveChangesAsync();
                await _memberRankService.UpdateUserRank(userId);
            }
        }

        private async Task<string> BuildFullAddress(
            IGHNAddressService ghnAddressService,
            int provinceCode,
            int districtCode,
            int wardCode,
            string addressDetail)
        {
            string provinceName = "", districtName = "", wardName = "";

            try
            {
                // Lấy tên tỉnh/thành phố
                var provinces = await ghnAddressService.GetProvinces();
                var province = provinces.FirstOrDefault(p => p.ProvinceId == provinceCode);
                provinceName = province?.ProvinceName ?? "";

                // Lấy tên quận/huyện
                var districts = await ghnAddressService.GetDistricts(provinceCode);
                var district = districts.FirstOrDefault(d => d.DistrictId == districtCode);
                districtName = district?.DistrictName ?? "";

                // Lấy tên phường/xã
                var wards = await ghnAddressService.GetWards(districtCode);
                var ward = wards.FirstOrDefault(w => w.WardCode == wardCode.ToString());
                wardName = ward?.WardName ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy thông tin địa chỉ: {ex.Message}");
            }

            return $"{addressDetail}, {wardName}, {districtName}, {provinceName}".TrimEnd(' ', ',');
        }
    }
}
