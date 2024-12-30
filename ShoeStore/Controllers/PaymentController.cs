using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Services.Payment;
using Microsoft.Extensions.Configuration;
using ShoeStore.Models.Payment;
using ShoeStore.Services;

namespace ShoeStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMemberRankService _memberRankService;

        public PaymentController(
            IVnPayService vnPayService,
            ApplicationDbContext context,
            IConfiguration configuration,
            IMemberRankService memberRankService)
        {
            _vnPayService = vnPayService;
            _context = context;
            _configuration = configuration;
            _memberRankService = memberRankService;
        }

        [HttpGet]
        public async Task<IActionResult> ProcessVnPay(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("Checkout", "Cart");
                }

                var orderDetails = string.Join(", ", order.OrderDetails.Select(od => 
                    $"{od.Product.Name} x {od.Quantity}"));

                var paymentInfo = new VNPayInformationModel
                {
                    Amount = order.TotalAmount,
                    OrderDescription = $"Thanh toan don hang {order.OrderCode}: {orderDetails}",
                    Name = order.OrderUsName,
                    OrderType = "billpayment",
                    OrderId = order.OrderCode,
                    PhoneNumber = order.PhoneNumber,
                    Email = order.User?.Email ?? "",
                    Address = order.ShippingAddress
                };

                var vnPayUrl = await _vnPayService.CreatePaymentUrl(paymentInfo, HttpContext);
                return Redirect(vnPayUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi tạo URL thanh toán: " + ex.Message;
                return RedirectToAction("Checkout", "Cart");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback([FromQuery] string vnp_ResponseCode, [FromQuery] string vnp_TxnRef)
        {
            try
            {
                var vnpayResponse = _vnPayService.PaymentExecute(Request.Query);
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == vnp_TxnRef);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("PaymentFail");
                }

                if (vnp_ResponseCode == "00")
                {
                    order.Status = OrderStatus.Processing;
                    order.PaymentStatus = PaymentStatus.Completed;
                    await _context.SaveChangesAsync();
                    
                    await UpdateUserRankAfterPayment(order.UserId, order.TotalAmount);
                    
                    if (!string.IsNullOrEmpty(order.OrderCoupon))
                    {
                        var coupon = await _context.Coupons
                            .FirstOrDefaultAsync(c => c.CouponCode == order.OrderCoupon);
                            
                        if (coupon != null && coupon.Quantity > 0)
                        {
                            coupon.Quantity--;
                            if (coupon.Quantity == 0)
                            {
                                coupon.Status = false;
                            }
                            _context.Coupons.Update(coupon);
                            await _context.SaveChangesAsync();
                        }
                    }

                    return RedirectToAction("Thankyou", "Cart", new { orderId = order.OrderId });
                }
                else
                {
                    order.PaymentStatus = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    TempData["Error"] = $"Thanh toán thất bại. Mã lỗi: {vnp_ResponseCode}";
                    return RedirectToAction("PaymentFail");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra trong quá trình xử lý thanh toán";
                return RedirectToAction("PaymentFail");
            }
        }

        private async Task UpdateUserRankAfterPayment(int userId, decimal orderAmount)
        {
            try 
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    Console.WriteLine($"Before payment - User {userId}:");
                    Console.WriteLine($"TotalSpent: {user.TotalSpent}");
                    Console.WriteLine($"Order amount: {orderAmount}");

                    user.TotalSpent += orderAmount;
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"After payment - New TotalSpent: {user.TotalSpent}");
                    await _memberRankService.UpdateUserRank(userId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating rank after payment: {ex.Message}");
                throw;
            }
        }

        public IActionResult PaymentFail()
        {
            return View();
        }
    }
}