using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Models.Payment;
using ShoeStore.Services.Email;
using ShoeStore.Services.Momo;
using ShoeStore.Utils;

namespace ShoeStore.Controllers
{
    public class MomoController : Controller
    {
        private readonly IMomoService _momoService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public MomoController(IMomoService momoService, ApplicationDbContext context, IEmailService emailService)
        {
            _momoService = momoService;
            _context = context;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] OrderInfoModel model)
        {
            try
            {
                var response = await _momoService.CreatePaymentAsync(model);
                if (response.ResultCode == 0)
                {
                    return Ok(new { PayUrl = response.PayUrl });
                }
                return BadRequest(new { Message = response.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            try
            {
                // Lấy tất cả các tham số từ query string
                var queryParams = Request.Query.ToDictionary(
                    x => x.Key,
                    x => x.Value.ToString()
                );

                // Log tất cả các tham số để debug
                foreach (var param in queryParams)
                {
                    Console.WriteLine($"{param.Key}: {param.Value}");
                }

                var isValidSignature = await _momoService.PaymentExecuteAsync(queryParams);
                if (!isValidSignature)
                {
                    return RedirectToAction("Error", "Home", new { message = "Invalid signature" });
                }

                var orderId = queryParams["orderId"];
                var resultCode = queryParams["resultCode"];

                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Size)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderId);

                if (order == null)
                {
                    return RedirectToAction("Error", "Home", new { message = "Order not found" });
                }

                if (resultCode == "0")
                {
                    order.PaymentStatus = PaymentStatus.Completed;
                    order.Status = OrderStatus.Processing;
                    order.PaymentMethod = PaymentMethod.Momo;
                    order.PaidAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // Send confirmation email
                    if (order.User?.Email != null)
                    {
                        var emailSubject = $"Xác nhận đơn hàng #{order.OrderCode}";
                        var emailBody = EmailTemplates.GetOrderConfirmationEmail(order);
                        await _emailService.SendEmailAsync(order.User.Email, emailSubject, emailBody);
                    }

                    return RedirectToAction("Thankyou", "Cart", new { orderId = order.OrderId });
                }
                else
                {
                    order.PaymentStatus = PaymentStatus.Failed;
                    order.Status = OrderStatus.Cancelled;
                    order.CancelReason = "Thanh toán thất bại";
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Error", "Home", new { message = "Payment failed" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PaymentCallback: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return RedirectToAction("Error", "Home", new { message = ex.Message });
            }
        }
    }
} 