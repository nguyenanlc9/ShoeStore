using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Payment;
using ShoeStore.Models.Enums;
using ShoeStore.Services.Momo;
using ShoeStore.Services.Email;
using ShoeStore.Services.MemberRanking;

namespace ShoeStore.Controllers
{
    public class MomoController : Controller
    {
        private readonly IMomoService _momoService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IMemberRankService _memberRankService;

        public MomoController(
            IMomoService momoService, 
            ApplicationDbContext context, 
            IEmailService emailService,
            IMemberRankService memberRankService)
        {
            _momoService = momoService;
            _context = context;
            _emailService = emailService;
            _memberRankService = memberRankService;
        }

        [HttpGet]
        public async Task<IActionResult> ProcessPayment(int orderId)
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
                    return RedirectToAction("PaymentError", "Error", new { message = "Không tìm thấy đơn hàng" });
                }

                // Kiểm tra nếu đơn hàng đã được thanh toán
                if (order.PaymentStatus == PaymentStatus.Completed)
                {
                    TempData["Error"] = "Đơn hàng này đã được thanh toán trước đó";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                // Cập nhật trạng thái đơn hàng sang Pending
                order.PaymentStatus = PaymentStatus.Pending;
                order.PaymentMethod = PaymentMethod.Momo;
                await _context.SaveChangesAsync();

                var model = new OrderInfoModel
                {
                    OrderId = order.OrderCode,
                    FullName = order.OrderUsName,
                    OrderInfo = $"Thanh toán đơn hàng {order.OrderCode}",
                    Amount = (int)order.TotalAmount
                };

                var response = await _momoService.CreatePaymentAsync(model);
                
                // Log response for debugging
                Console.WriteLine($"MOMO Response - ErrorCode: {response.ErrorCode}, Message: {response.LocalMessage}");

                if (response.ErrorCode == 0 && !string.IsNullOrEmpty(response.PayUrl))
                {
                    return Redirect(response.PayUrl);
                }

                // Xử lý các trường hợp lỗi cụ thể
                string errorMessage;
                switch (response.ErrorCode)
                {
                    case 0:
                        return Redirect(response.PayUrl);
                    case 4:
                        errorMessage = "Số tiền không hợp lệ";
                        break;
                    case 5:
                        errorMessage = "Số tiền vượt quá giới hạn cho phép";
                        break;
                    case 7:
                        errorMessage = "Giao dịch bị từ chối bởi ngân hàng";
                        break;
                    case 9:
                        errorMessage = "Thẻ/Tài khoản của khách hàng không đủ số dư để thực hiện giao dịch";
                        break;
                    case 10:
                        errorMessage = "Thông tin thẻ không đúng";
                        break;
                    case 11:
                        errorMessage = "Thẻ hết hạn/Thẻ bị khóa";
                        break;
                    case 29:
                        errorMessage = "Không tìm thấy giao dịch";
                        break;
                    case 32:
                        errorMessage = "Số tiền không đúng với số tiền thanh toán";
                        break;
                    case 36:
                        errorMessage = "Giao dịch đã được thanh toán";
                        break;
                    case 37:
                        errorMessage = "Mã đơn hàng đã tồn tại";
                        break;
                    case 38:
                        errorMessage = "Giao dịch đang được xử lý";
                        break;
                    case 39:
                        errorMessage = "Giao dịch đã hết hạn";
                        break;
                    case 40:
                        errorMessage = "Giao dịch bị hủy";
                        break;
                    case 41:
                        errorMessage = "Giao dịch đã hoàn thành";
                        break;
                    case 42:
                        errorMessage = "Giao dịch bị từ chối nhận tiền";
                        break;
                    case 43:
                        errorMessage = "Giao dịch bị từ chối thanh toán";
                        break;
                    case 44:
                        errorMessage = "Giao dịch đã được hoàn tiền";
                        break;
                    case 49:
                        errorMessage = "Giao dịch không thành công do: Không đúng số điện thoại";
                        break;
                    case 63:
                        errorMessage = "Giao dịch không thành công do: Tài khoản người dùng không đủ số dư";
                        break;
                    case 99:
                        errorMessage = "Hệ thống đang bảo trì, vui lòng thử lại sau";
                        break;
                    default:
                        errorMessage = $"Lỗi không xác định: {response.LocalMessage}";
                        break;
                }

                // Cập nhật trạng thái thất bại nếu có lỗi
                order.PaymentStatus = PaymentStatus.Failed;
                await _context.SaveChangesAsync();

                return RedirectToAction("PaymentError", "Error", new { message = errorMessage });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MOMO Payment Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                return RedirectToAction("PaymentError", "Error", 
                    new { message = "Có lỗi xảy ra trong quá trình xử lý thanh toán. Vui lòng thử lại sau." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            try
            {
                Console.WriteLine("=== Momo Callback Started ===");
                Console.WriteLine($"Query string: {Request.QueryString}");

                if (!Request.Query.ContainsKey("signature"))
                {
                    Console.WriteLine("Missing signature");
                    TempData["Error"] = "Yêu cầu không hợp lệ";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                MomoExecuteResponseModel response;
                try
                {
                    response = _momoService.PaymentExecuteAsync(Request.Query);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error validating signature: {ex.Message}");
                    TempData["Error"] = "Xác thực thanh toán thất bại";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                Console.WriteLine($"Momo Response - OrderId: {response.OrderId}");

                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderCode == response.OrderId);

                if (order == null)
                {
                    Console.WriteLine($"Order not found: {response.OrderId}");
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                Console.WriteLine($"Order found - ID: {order.OrderId}, Status: {order.Status}, PaymentStatus: {order.PaymentStatus}");

                if (order.PaymentStatus == PaymentStatus.Completed)
                {
                    TempData["Error"] = "Đơn hàng này đã được thanh toán trước đó";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                var errorCode = Request.Query["errorCode"].ToString();
                var amount = Request.Query["amount"].ToString();
                var orderAmount = ((long)order.TotalAmount).ToString();

                // Kiểm tra số tiền
                if (amount != orderAmount)
                {
                    Console.WriteLine($"Amount mismatch - Momo: {amount}, Order: {orderAmount}");
                    order.PaymentStatus = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    TempData["Error"] = "Số tiền thanh toán không khớp";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                if (errorCode == "0")
                {
                    Console.WriteLine("Payment successful, updating order status...");
                    order.Status = OrderStatus.Processing;
                    order.PaymentStatus = PaymentStatus.Completed;
                    await _context.SaveChangesAsync();

                    // Cập nhật rank user
                    if (order.User != null)
                    {
                        await _memberRankService.UpdateUserRank(order.User.UserID);
                    }

                    return RedirectToAction("Thankyou", "Cart", new { orderId = order.OrderId });
                }
                else
                {
                    Console.WriteLine($"Payment failed with error code: {errorCode}");
                    order.PaymentStatus = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    TempData["Error"] = $"Thanh toán thất bại. Mã lỗi: {errorCode}";
                    return RedirectToAction("PaymentFail", "Payment");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PaymentCallback: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["Error"] = "Có lỗi xảy ra trong quá trình xử lý thanh toán";
                return RedirectToAction("PaymentFail", "Payment");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MomoNotify()
        {
            try
            {
                Console.WriteLine("=== Momo Notify Started ===");
                var response = _momoService.PaymentExecuteAsync(Request.Query);
                Console.WriteLine($"Momo Notify - OrderId: {response.OrderId}");

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == response.OrderId);

                if (order != null)
                {
                    var errorCode = Request.Query["errorCode"].ToString();
                    if (errorCode == "0" && order.PaymentStatus != PaymentStatus.Completed)
                    {
                        order.Status = OrderStatus.Processing;
                        order.PaymentStatus = PaymentStatus.Completed;
                    }
                    else if (errorCode != "0")
                    {
                        order.PaymentStatus = PaymentStatus.Failed;
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new { ReturnCode = 1, ReturnMessage = "Thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MomoNotify: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Ok(new { ReturnCode = 0, ReturnMessage = "Thất bại" });
            }
        }
    }
}