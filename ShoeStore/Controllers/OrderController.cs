using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Utils;
using ShoeStore.Services;
using Microsoft.AspNetCore.SignalR;

namespace ShoeStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public OrderController(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
                return RedirectToAction("Login", "Auth");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Size)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userInfo.UserID);

            if (order == null)
            {
                return NotFound();
            }

            return PartialView("_OrderDetails", order);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userInfo.UserID);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            // Cho phép hủy đơn hàng ở trạng thái Pending hoặc Processing
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
            {
                return Json(new { success = false, message = "Không thể hủy đơn hàng ở trạng thái này" });
            }

            // Cập nhật trạng thái đơn hàng
            order.Status = OrderStatus.Cancelled;
            order.CancelReason = "Khách hàng yêu cầu hủy đơn";

            // Hoàn lại số lượng sản phẩm
            var orderDetails = await _context.OrderDetails
                .Where(od => od.OrderId == orderId)
                .Include(od => od.Product)
                .ToListAsync();

            foreach (var detail in orderDetails)
            {
                var stock = await _context.ProductSizeStocks
                    .FirstOrDefaultAsync(pss => pss.ProductID == detail.ProductId && pss.SizeID == detail.SizeId);
                
                if (stock != null)
                {
                    stock.StockQuantity += detail.Quantity;
                }
            }

            // Tạo thông báo hủy đơn
            var notification = new Notification
            {
                Message = $"Đơn hàng #{orderId} đã bị hủy bởi khách hàng",
                Type = "cancel",
                ReferenceId = orderId.ToString(),
                CreatedAt = DateTime.Now,
                IsRead = false,
                Url = $"/Admin/Order/Details/{orderId}"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Gửi thông báo realtime
            await _notificationService.SendOrderNotification(
                notification.Message,
                notification.ReferenceId,
                notification.Type
            );

            return Json(new { success = true, message = "Hủy đơn hàng thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(Order order)
        {
            try 
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                if (userInfo == null)
                    return RedirectToAction("Login", "Auth");

                order.UserId = userInfo.UserID;
                order.OrderDate = DateTime.Now;
                order.Status = OrderStatus.Pending;

                // Thêm đơn hàng vào database
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Tạo thông báo mới
                var notification = new Notification
                {
                    Message = $"Đơn hàng mới #{order.OrderId} từ {order.OrderUsName}",
                    Type = "new",
                    ReferenceId = order.OrderId.ToString(),
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Url = $"/Admin/Order/Details/{order.OrderId}"
                };

                try 
                {
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    // Gửi thông báo realtime cho admin
                    await _notificationService.SendOrderNotification(
                        notification.Message,
                        notification.ReferenceId,
                        notification.Type
                    );
                    System.Diagnostics.Debug.WriteLine($"Notification sent: {notification.Message}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending notification: {ex.Message}");
                }

                return RedirectToAction("Thankyou", new { orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PlaceOrder: {ex.Message}");
                return RedirectToAction("Error", "Home");
            }
        }
    }
} 