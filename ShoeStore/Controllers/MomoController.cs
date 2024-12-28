using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Payment;
using ShoeStore.Models.Enums;
using ShoeStore.Services.Momo;

namespace ShoeStore.Controllers
{
    public class MomoController : Controller
    {
        private readonly IMomoService _momoService;
        private readonly ApplicationDbContext _context;

        public MomoController(IMomoService momoService, ApplicationDbContext context)
        {
            _momoService = momoService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ProcessPayment(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("Checkout", "Cart");
                }

                var model = new OrderInfoModel
                {
                    OrderId = order.OrderCode,
                    FullName = order.OrderUsName,
                    OrderInfo = $"Thanh toán đơn hàng {order.OrderCode}",
                    Amount = (int)order.TotalAmount
                };

                var response = await _momoService.CreatePaymentAsync(model);
                if (response.ErrorCode != 0)
                {
                    TempData["Error"] = $"Lỗi tạo thanh toán: {response.LocalMessage}";
                    return RedirectToAction("Checkout", "Cart");
                }

                // Cập nhật trạng thái đơn hàng
                order.PaymentMethod = PaymentMethod.Momo;
                order.PaymentStatus = PaymentStatus.Pending;
                await _context.SaveChangesAsync();

                return Redirect(response.PayUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi tạo thanh toán MOMO: " + ex.Message;
                return RedirectToAction("Checkout", "Cart");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            try
            {
                var response = _momoService.PaymentExecuteAsync(Request.Query);
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == response.OrderId);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                // Kiểm tra trạng thái thanh toán từ MOMO
                var errorCode = Request.Query["errorCode"].ToString();
                if (errorCode == "0")
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
                    TempData["Error"] = $"Thanh toán thất bại. Mã lỗi: {errorCode}";
                    return RedirectToAction("PaymentFail", "Payment");
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra trong quá trình xử lý thanh toán";
                return RedirectToAction("PaymentFail", "Payment");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MomoNotify()
        {
            try
            {
                var response = _momoService.PaymentExecuteAsync(Request.Query);
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == response.OrderId);

                if (order != null)
                {
                    var errorCode = Request.Query["errorCode"].ToString();
                    if (errorCode == "0")
                    {
                        order.Status = OrderStatus.Processing;
                        order.PaymentStatus = PaymentStatus.Completed;
                    }
                    else
                    {
                        order.PaymentStatus = PaymentStatus.Failed;
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new { ReturnCode = 1, ReturnMessage = "Thành công" });
            }
            catch
            {
                return Ok(new { ReturnCode = 0, ReturnMessage = "Thất bại" });
            }
        }
    }
}