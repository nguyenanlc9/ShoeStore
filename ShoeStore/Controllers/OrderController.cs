using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using ShoeStore.Utils;

namespace ShoeStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
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

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hủy đơn hàng thành công" });
        }
    }
} 