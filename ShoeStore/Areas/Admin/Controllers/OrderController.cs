using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Filters;
using ShoeStore.Models;
using ShoeStore.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using ShoeStore.Services.MemberRanking;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]

    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public OrderController(ApplicationDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        // GET: Admin/Order
        public async Task<IActionResult> Index()
        {
            return View(await _context.Orders.ToListAsync());
        }

        // GET: Admin/Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Admin/Order/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Order/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Order request)
        {
            if (ModelState.IsValid)
            {
                var order = new Order
                {
                    OrderUsName = request.OrderUsName,
                    OrderCode = request.OrderCode,
                    OrderDescription = request.OrderDescription,
                    OrderCoupon = request.OrderCoupon,
                    PaymentMethod = request.PaymentMethod,
                    OrderStatus = request.OrderStatus
                };

                // Thêm vào context và lưu vào cơ sở dữ liệu
                _context.Add(order);
                await _context.SaveChangesAsync();

                // Tạo thông báo cho đơn hàng mới

                // Chuyển hướng về trang Index hoặc danh sách đơn hàng
                return RedirectToAction(nameof(Index));
            }

            // Nếu không hợp lệ, trở lại view với dữ liệu request
            return View(request);
        }

        // GET: Admin/Order/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: Admin/Order/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,OrderUsName,OrderCode,OrderDescription,OrderCoupon,PaymentMethod,OrderStatus")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Admin/Order/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Admin/Order/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }

        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                // Cập nhật trạng thái thanh toán dựa trên phương thức thanh toán và trạng thái đơn hàng
                if (order.PaymentMethod == PaymentMethod.VNPay)
                {
                    // Nếu là VNPay, giữ nguyên trạng thái thanh toán vì đã được xử lý trong callback
                    // Chỉ cập nhật trạng thái đơn hàng
                    if (status == OrderStatus.Cancelled)
                    {
                        order.PaymentStatus = PaymentStatus.Failed;
                    }
                }
                else
                {
                    // Với các phương thức thanh toán khác
                    switch (status)
                    {
                        case OrderStatus.Completed:
                            order.PaymentStatus = PaymentStatus.Completed;
                            break;
                        case OrderStatus.Cancelled:
                            order.PaymentStatus = PaymentStatus.Failed;
                            break;
                        case OrderStatus.Processing:
                            // Giữ nguyên trạng thái thanh toán
                            break;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // Thêm phương thức để xem chi tiết đơn hàng bao gồm thông tin thanh toán
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Size)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Thêm action mới
        public async Task<IActionResult> Process(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Size)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Kiểm tra logic chuyển trạng thái
                if (!IsValidStatusTransition(order.Status, newStatus))
                {
                    return Json(new { success = false, message = "Không thể chuyển sang trạng thái này" });
                }

                order.Status = newStatus;
                
                // Cập nhật trạng thái thanh toán nếu cần
                if (newStatus == OrderStatus.Completed && order.PaymentMethod == PaymentMethod.Cash)
                {
                    order.PaymentStatus = PaymentStatus.Completed;
                }

                // Cập nhật TotalSpent và rank khi đơn hàng hoàn thành và đã thanh toán
                if (newStatus == OrderStatus.Completed && order.PaymentStatus == PaymentStatus.Completed)
                {
                    var user = await _context.Users.FindAsync(order.UserId);
                    if (user != null)
                    {
                        // Tính tổng tiền từ các đơn hàng đã hoàn thành
                        var completedOrders = await _context.Orders
                            .Where(o => o.UserId == order.UserId 
                                && o.Status == OrderStatus.Completed 
                                && o.PaymentStatus == PaymentStatus.Completed)
                            .SumAsync(o => o.TotalAmount);
                        
                        user.TotalSpent = completedOrders;
                        _context.Users.Update(user);

                        // Tạo scope để sử dụng IMemberRankService
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var memberRankService = scope.ServiceProvider.GetRequiredService<IMemberRankService>();
                            await memberRankService.UpdateUserRank(order.UserId);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Đã cập nhật trạng thái thành {GetStatusText(newStatus)}",
                    newStatus = newStatus.ToString(),
                    newStatusText = GetStatusText(newStatus)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái: " + ex.Message });
            }
        }

        private bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            switch (currentStatus)
            {
                case OrderStatus.Pending:
                    return newStatus == OrderStatus.Processing;
                case OrderStatus.Processing:
                    return newStatus == OrderStatus.Shipped;
                case OrderStatus.Shipped:
                    return newStatus == OrderStatus.Delivered || newStatus == OrderStatus.Cancelled;
                case OrderStatus.Delivered:
                    return newStatus == OrderStatus.Completed;
                default:
                    return false;
            }
        }

        private string GetStatusText(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xử lý",
                OrderStatus.Processing => "Đang xử lý",
                OrderStatus.Shipped => "Đã giao cho vận chuyển",
                OrderStatus.Delivered => "Đã giao hàng",
                OrderStatus.Completed => "Hoàn thành",
                OrderStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }
    }
}
