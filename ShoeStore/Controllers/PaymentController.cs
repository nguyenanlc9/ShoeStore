using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Services.Payment;
using Microsoft.Extensions.Configuration;
using ShoeStore.Models.Payment;
using ShoeStore.Services.MemberRanking;
using ShoeStore.Services.Email;
using ShoeStore.Services;
using ShoeStore.Utils;

namespace ShoeStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMemberRankService _memberRankService;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public PaymentController(
            IVnPayService vnPayService,
            ApplicationDbContext context,
            IConfiguration configuration,
            IMemberRankService memberRankService,
            IEmailService emailService,
            INotificationService notificationService)
        {
            _vnPayService = vnPayService;
            _context = context;
            _configuration = configuration;
            _memberRankService = memberRankService;
            _emailService = emailService;
            _notificationService = notificationService;
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
        public async Task<IActionResult> PaymentCallback()
        {
            var vnpayData = Request.Query;
            string orderId = vnpayData["vnp_TxnRef"].ToString();
            string vnpayTranId = vnpayData["vnp_TransactionNo"].ToString();
            string vnpayResponseCode = vnpayData["vnp_ResponseCode"].ToString();
            string vnpayTransactionStatus = vnpayData["vnp_TransactionStatus"].ToString();
            string vnpaySecureHash = vnpayData["vnp_SecureHash"].ToString();
            string vnpayBankCode = vnpayData["vnp_BankCode"].ToString();
            string vnpayBankTranNo = vnpayData["vnp_BankTranNo"].ToString();
            string vnpayCardType = vnpayData["vnp_CardType"].ToString();
            string vnpayPayDate = vnpayData["vnp_PayDate"].ToString();
            decimal amount = decimal.Parse(vnpayData["vnp_Amount"].ToString()) / 100;

            // Tìm đơn hàng
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Size)
                .FirstOrDefaultAsync(o => o.OrderCode == orderId);
            
            if (order == null)
            {
                return RedirectToAction("Error", "Home", new { message = "Không tìm thấy đơn hàng" });
            }

            // Lưu thông tin giao dịch VNPay
            var transaction = new VNPayTransaction
            {
                OrderId = orderId,
                OrderRefId = order.OrderId,
                TransactionId = vnpayTranId,
                PaymentMethod = "VNPay",
                Amount = amount,
                PaymentTime = DateTime.ParseExact(vnpayPayDate, "yyyyMMddHHmmss", null),
                BankCode = vnpayBankCode,
                BankTranNo = vnpayBankTranNo,
                CardType = vnpayCardType,
                ResponseCode = vnpayResponseCode,
                TransactionStatus = vnpayTransactionStatus,
                SecureHash = vnpaySecureHash
            };

            _context.VNPayTransactions.Add(transaction);

            // Cập nhật trạng thái đơn hàng
            if (vnpayResponseCode == "00")
            {
                order.PaymentStatus = PaymentStatus.Completed;
                order.Status = OrderStatus.Processing;

                // Tạo thông báo cho admin
                var notification = new Notification
                {
                    Message = $"Đơn hàng mới #{order.OrderId} - Thanh toán VNPay thành công",
                    Type = "order_new",
                    ReferenceId = order.OrderId.ToString(),
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Url = $"/Admin/Order/Details/{order.OrderId}"
                };

                _context.Notifications.Add(notification);

                // Cập nhật rank nếu user đã đăng nhập
                if (order.UserId > 0)
                {
                    await UpdateUserRankAfterPayment(order.UserId, order.TotalAmount);
                }

                // Send confirmation email
                if (order.User != null && !string.IsNullOrEmpty(order.User.Email))
                {
                    var emailSubject = $"Xác nhận đơn hàng #{order.OrderCode}";
                    var emailBody = EmailTemplates.GetOrderConfirmationEmail(order);
                    await _emailService.SendEmailAsync(order.User.Email, emailSubject, emailBody);
                }

                await _context.SaveChangesAsync();

                // Gửi thông báo realtime
                _notificationService.SendOrderNotification(
                    notification.Message,
                    notification.ReferenceId,
                    notification.Type
                );

                return RedirectToAction("Thankyou", "Cart", new { orderId = order.OrderId });
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.Status = OrderStatus.Cancelled;

                // Tạo thông báo cho admin về giao dịch thất bại
                var notification = new Notification
                {
                    Message = $"Đơn hàng #{order.OrderId} - Thanh toán VNPay thất bại",
                    Type = "order_payment_failed",
                    ReferenceId = order.OrderId.ToString(),
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Url = $"/Admin/Order/Details/{order.OrderId}"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Gửi thông báo realtime
                _notificationService.SendOrderNotification(
                    notification.Message,
                    notification.ReferenceId,
                    notification.Type
                );

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