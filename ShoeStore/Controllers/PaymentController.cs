using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Services.Payment;
using Microsoft.Extensions.Configuration;
using ShoeStore.Models.Payment;
using ShoeStore.Services.MemberRanking;
using ShoeStore.Services.Email;

namespace ShoeStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMemberRankService _memberRankService;
        private readonly IEmailService _emailService;

        public PaymentController(
            IVnPayService vnPayService,
            ApplicationDbContext context,
            IConfiguration configuration,
            IMemberRankService memberRankService,
            IEmailService emailService)
        {
            _vnPayService = vnPayService;
            _context = context;
            _configuration = configuration;
            _memberRankService = memberRankService;
            _emailService = emailService;
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

                if (order.PaymentStatus == PaymentStatus.Completed)
                {
                    TempData["Error"] = "Đơn hàng này đã được thanh toán trước đó";
                    return RedirectToAction("PaymentFail");
                }

                order.PaymentStatus = PaymentStatus.Pending;
                order.PaymentMethod = PaymentMethod.VNPay;
                await _context.SaveChangesAsync();

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
                Console.WriteLine($"=== VNPay Callback Started ===");
                Console.WriteLine($"Response Code: {vnp_ResponseCode}");
                Console.WriteLine($"Transaction Ref: {vnp_TxnRef}");

                if (!Request.Query.ContainsKey("vnp_SecureHash"))
                {
                    Console.WriteLine("Missing secure hash");
                    TempData["Error"] = "Yêu cầu không hợp lệ";
                    return RedirectToAction("PaymentFail");
                }

                var vnpayResponse = _vnPayService.PaymentExecute(Request.Query);
                Console.WriteLine($"VNPay Response - Success: {vnpayResponse.Success}, ResponseCode: {vnpayResponse.VnPayResponseCode}");

                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderCode == vnp_TxnRef);

                if (order == null)
                {
                    Console.WriteLine($"Order not found: {vnp_TxnRef}");
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("PaymentFail");
                }

                Console.WriteLine($"Order found - ID: {order.OrderId}, Status: {order.Status}, PaymentStatus: {order.PaymentStatus}");

                if (order.PaymentStatus == PaymentStatus.Completed)
                {
                    TempData["Error"] = "Đơn hàng này đã được thanh toán trước đó";
                    return RedirectToAction("PaymentFail");
                }

                if (!vnpayResponse.Success)
                {
                    Console.WriteLine("Payment validation failed");
                    order.PaymentStatus = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    TempData["Error"] = "Xác thực thanh toán thất bại";
                    return RedirectToAction("PaymentFail");
                }

                var vnp_Amount = Request.Query["vnp_Amount"].ToString();
                var orderAmount = ((long)(order.TotalAmount * 100)).ToString();
                if (vnp_Amount != orderAmount)
                {
                    Console.WriteLine($"Amount mismatch - VNPay: {vnp_Amount}, Order: {orderAmount}");
                    order.PaymentStatus = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    TempData["Error"] = "Số tiền thanh toán không khớp";
                    return RedirectToAction("PaymentFail");
                }

                if (vnp_ResponseCode != "00")
                {
                    Console.WriteLine($"Payment failed with response code: {vnp_ResponseCode}");
                    order.PaymentStatus = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    TempData["Error"] = $"Thanh toán thất bại. Mã lỗi: {vnp_ResponseCode}";
                    return RedirectToAction("PaymentFail");
                }

                Console.WriteLine("Payment successful, updating order status...");
                order.Status = OrderStatus.Processing;
                order.PaymentStatus = PaymentStatus.Completed;
                await _context.SaveChangesAsync();

                if (order.User != null)
                {
                    await UpdateUserRankAfterPayment(order.User.UserID, order.TotalAmount);
                }

                return RedirectToAction("Thankyou", "Cart", new { orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PaymentCallback: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
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