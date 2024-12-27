using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Models.Payment;
using ShoeStore.Services.Payment;
using Microsoft.Extensions.Configuration;

namespace ShoeStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IConfiguration _configuration;

        public PaymentController(ApplicationDbContext context, IVnPayService vnPayService, IConfiguration configuration)
        {
            _context = context;
            _vnPayService = vnPayService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> ProcessVnPay(int orderId)
        {
            try
            {
                Console.WriteLine($"Processing VNPay payment for orderId: {orderId}");

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    Console.WriteLine("Order not found");
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("Checkout", "Cart");
                }

                // Tạo mô tả chi tiết đơn hàng
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

                Console.WriteLine("Payment Info:");
                Console.WriteLine($"Amount: {paymentInfo.Amount}");
                Console.WriteLine($"OrderId: {paymentInfo.OrderId}");
                Console.WriteLine($"Description: {paymentInfo.OrderDescription}");

                // Kiểm tra thông tin cấu hình VNPay
                var vnpayConfig = new
                {
                    TmnCode = _configuration["VnPay:TmnCode"],
                    HashSecret = _configuration["VnPay:HashSecret"],
                    BaseUrl = _configuration["VnPay:BaseUrl"],
                    ReturnUrl = _configuration["VnPay:PaymentBackReturnUrl"]
                };

                Console.WriteLine("VNPay Config:");
                Console.WriteLine($"TmnCode: {vnpayConfig.TmnCode}");
                Console.WriteLine($"BaseUrl: {vnpayConfig.BaseUrl}");
                Console.WriteLine($"ReturnUrl: {vnpayConfig.ReturnUrl}");

                if (string.IsNullOrEmpty(vnpayConfig.TmnCode) || 
                    string.IsNullOrEmpty(vnpayConfig.HashSecret) || 
                    string.IsNullOrEmpty(vnpayConfig.BaseUrl))
                {
                    throw new Exception("Thiếu thông tin cấu hình VNPay");
                }

                var vnPayUrl = await _vnPayService.CreatePaymentUrl(paymentInfo, HttpContext);
                
                if (string.IsNullOrEmpty(vnPayUrl))
                {
                    throw new Exception("Không thể tạo URL thanh toán VNPay");
                }

                Console.WriteLine($"Generated VNPay URL: {vnPayUrl}");
                return Redirect(vnPayUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessVnPay: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["Error"] = "Lỗi khi tạo URL thanh toán: " + ex.Message;
                return RedirectToAction("Checkout", "Cart");
            }
        }

        public async Task<IActionResult> PaymentCallback([FromQuery] string vnp_ResponseCode, [FromQuery] string vnp_TxnRef)
        {
            try
            {
                var vnpayResponse = _vnPayService.PaymentExecute(Request.Query);
                if (!vnpayResponse.Success)
                {
                    TempData["Error"] = "Xác thực thanh toán thất bại";
                    return RedirectToAction("PaymentFail");
                }

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderCode == vnpayResponse.OrderId);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("PaymentFail");
                }

                if (vnpayResponse.VnPayResponseCode == "00")
                {
                    order.Status = OrderStatus.Processing;
                    order.PaymentStatus = PaymentStatus.Completed;
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Thanh toán thành công";
                    return RedirectToAction("Thankyou", "Cart", new { orderId = order.OrderId });
                }

                order.PaymentStatus = PaymentStatus.Failed;
                await _context.SaveChangesAsync();
                TempData["Error"] = "Thanh toán thất bại";
                return RedirectToAction("PaymentFail");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("PaymentFail");
            }
        }

        public IActionResult PaymentFail()
        {
            return View();
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