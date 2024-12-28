using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Services.Payment;
using Microsoft.Extensions.Configuration;
using ShoeStore.Models.Payment;

namespace ShoeStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(
            IVnPayService vnPayService,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _vnPayService = vnPayService;
            _context = context;
            _configuration = configuration;
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

        public IActionResult PaymentFail()
        {
            return View();
        }
    }
}