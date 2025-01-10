using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Payment;
using ShoeStore.Models.Enums;
using ShoeStore.Services.ZaloPay;
using ShoeStore.Services.Email;
using ShoeStore.Services.MemberRanking;

namespace ShoeStore.Controllers
{
    public class ZaloPayController : Controller
    {
        private readonly IZaloPayService _zaloPayService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IMemberRankService _memberRankService;

        public ZaloPayController(
            IZaloPayService zaloPayService,
            ApplicationDbContext context,
            IEmailService emailService,
            IMemberRankService memberRankService)
        {
            _zaloPayService = zaloPayService;
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

                if (order.PaymentStatus == PaymentStatus.Completed)
                {
                    TempData["Error"] = "Đơn hàng này đã được thanh toán trước đó";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                // Cập nhật trạng thái đơn hàng
                order.PaymentStatus = PaymentStatus.Pending;
                order.PaymentMethod = PaymentMethod.ZaloPay;
                order.Status = OrderStatus.Pending;
                await _context.SaveChangesAsync();

                var model = new OrderInfoModel
                {
                    OrderId = order.OrderCode,
                    FullName = order.OrderUsName,
                    OrderInfo = $"Thanh toán đơn hàng {order.OrderCode}",
                    Amount = (int)order.TotalAmount
                };

                var response = await _zaloPayService.CreatePaymentAsync(model);

                if (response.ReturnCode == 1 && !string.IsNullOrEmpty(response.OrderUrl))
                {
                    return Redirect(response.OrderUrl);
                }

                // Xử lý các mã lỗi cụ thể từ ZaloPay
                string errorMessage = GetZaloPayErrorMessage(response.ReturnCode, response.ReturnMessage);
                
                // Cập nhật trạng thái thất bại
                order.PaymentStatus = PaymentStatus.Failed;
                order.Status = OrderStatus.Pending;
                await _context.SaveChangesAsync();

                return RedirectToAction("PaymentError", "Error", new { message = errorMessage });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ZaloPay Payment Error: {ex.Message}");
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
                Console.WriteLine("=== ZaloPay Callback Started ===");
                Console.WriteLine($"Query string: {Request.QueryString}");

                var isValidCallback = await _zaloPayService.VerifyCallbackAsync(Request.Query);
                if (!isValidCallback)
                {
                    Console.WriteLine("Invalid callback signature");
                    TempData["Error"] = "Xác thực thanh toán thất bại";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                var data = Request.Query["data"].ToString();
                var dataObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(data);
                var appTransId = dataObj["apptransid"];
                var orderCode = appTransId.Split('_')[1];
                var status = int.Parse(dataObj["status"]);
                var amount = decimal.Parse(dataObj["amount"]);

                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

                if (order == null)
                {
                    Console.WriteLine($"Order not found: {orderCode}");
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                if (order.PaymentStatus == PaymentStatus.Completed)
                {
                    TempData["Error"] = "Đơn hàng này đã được thanh toán trước đó";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                // Kiểm tra số tiền thanh toán
                if (amount != order.TotalAmount)
                {
                    order.PaymentStatus = PaymentStatus.Failed;
                    order.Status = OrderStatus.Cancelled;
                    await _context.SaveChangesAsync();
                    TempData["Error"] = "Số tiền thanh toán không khớp";
                    return RedirectToAction("PaymentFail", "Payment");
                }

                switch (status)
                {
                    case 1: // Thanh toán thành công
                        Console.WriteLine("Payment successful, updating order status...");
                        order.Status = OrderStatus.Processing;
                        order.PaymentStatus = PaymentStatus.Completed;
                        await _context.SaveChangesAsync();

                        // Cập nhật rank user
                        if (order.User != null)
                        {
                            await _memberRankService.UpdateUserRank(order.User.UserID);
                        }

                        // Gửi email xác nhận
                        if (order.User?.Email != null)
                        {
                            await _emailService.SendEmailAsync(
                                order.User.Email,
                                $"Xác nhận đơn hàng #{order.OrderCode}",
                                EmailTemplates.GetOrderConfirmationEmail(order)
                            );
                        }

                        return RedirectToAction("Thankyou", "Cart", new { orderId = order.OrderId });

                    case -49: // Người dùng huỷ thanh toán
                        order.PaymentStatus = PaymentStatus.Failed;
                        order.Status = OrderStatus.Cancelled;
                        order.CancelReason = "Người dùng huỷ thanh toán";
                        await _context.SaveChangesAsync();
                        TempData["Error"] = "Bạn đã huỷ thanh toán";
                        break;

                    case -83: // Giao dịch đã tồn tại
                        TempData["Error"] = "Giao dịch đã được xử lý trước đó";
                        break;

                    case -84: // Timeout
                        order.PaymentStatus = PaymentStatus.Failed;
                        order.Status = OrderStatus.Pending;
                        await _context.SaveChangesAsync();
                        TempData["Error"] = "Giao dịch hết hạn";
                        break;

                    default:
                        order.PaymentStatus = PaymentStatus.Failed;
                        order.Status = OrderStatus.Pending;
                        await _context.SaveChangesAsync();
                        TempData["Error"] = "Thanh toán thất bại";
                        break;
                }

                return RedirectToAction("PaymentFail", "Payment");
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
        public async Task<IActionResult> ZaloPayNotify()
        {
            try
            {
                Console.WriteLine("=== ZaloPay Notify Started ===");
                var isValidCallback = await _zaloPayService.VerifyCallbackAsync(Request.Query);
                if (!isValidCallback)
                {
                    return Ok(new { return_code = -1, return_message = "mac not match" });
                }

                var data = Request.Query["data"].ToString();
                var dataObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(data);
                var appTransId = dataObj["apptransid"];
                var orderCode = appTransId.Split('_')[1];
                var status = int.Parse(dataObj["status"]);
                var amount = decimal.Parse(dataObj["amount"]);

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
                if (order != null)
                {
                    // Kiểm tra số tiền
                    if (amount != order.TotalAmount)
                    {
                        order.PaymentStatus = PaymentStatus.Failed;
                        order.Status = OrderStatus.Cancelled;
                        await _context.SaveChangesAsync();
                        return Ok(new { return_code = -1, return_message = "amount mismatch" });
                    }

                    switch (status)
                    {
                        case 1: // Thành công
                            if (order.PaymentStatus != PaymentStatus.Completed)
                            {
                                order.Status = OrderStatus.Processing;
                                order.PaymentStatus = PaymentStatus.Completed;
                                await _context.SaveChangesAsync();
                            }
                            break;

                        case -49: // Huỷ thanh toán
                            order.PaymentStatus = PaymentStatus.Failed;
                            order.Status = OrderStatus.Cancelled;
                            order.CancelReason = "Người dùng huỷ thanh toán";
                            await _context.SaveChangesAsync();
                            break;

                        default: // Các trường hợp thất bại khác
                            order.PaymentStatus = PaymentStatus.Failed;
                            await _context.SaveChangesAsync();
                            break;
                    }
                }

                return Ok(new { return_code = 1, return_message = "success" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ZaloPayNotify: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Ok(new { return_code = 0, return_message = "failed" });
            }
        }

        private string GetZaloPayErrorMessage(int returnCode, string returnMessage)
        {
            switch (returnCode)
            {
                case -49:
                    return "Người dùng huỷ thanh toán";
                case -83:
                    return "Giao dịch đã tồn tại";
                case -84:
                    return "Giao dịch đã hết hạn";
                case -86:
                    return "Số tiền không hợp lệ";
                case -87:
                    return "Số tiền vượt quá hạn mức cho phép";
                case -88:
                    return "Ví không đủ tiền";
                case -89:
                    return "Ví chưa được kích hoạt";
                case -90:
                    return "Ví đang bị tạm khoá";
                case -91:
                    return "Ví đã bị khoá";
                case -92:
                    return "Số điện thoại không hợp lệ";
                case -93:
                    return "Mã đơn hàng không hợp lệ";
                case -94:
                    return "Mã đơn hàng đã tồn tại";
                case -95:
                    return "Số tiền thanh toán không khớp với đơn hàng";
                case -96:
                    return "Đơn hàng đã được thanh toán";
                case -97:
                    return "Token không hợp lệ hoặc đã hết hạn";
                case -98:
                    return "Merchant không tồn tại hoặc chưa được kích hoạt";
                case -99:
                    return "Merchant đang bị khoá";
                case -100:
                    return "Dữ liệu request không hợp lệ";
                default:
                    return returnMessage ?? "Có lỗi xảy ra trong quá trình thanh toán";
            }
        }
    }
} 