﻿using System;
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
using ShoeStore.Services.GHN;
using ShoeStore.Models.GHN;
using System.Net.Http.Json;
using ShoeStore.Services.Excel;
using ShoeStore.Services;
using ClosedXML.Excel;
using System.Data;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGHNService _ghnService;
        private readonly IConfiguration _configuration;
        private readonly IExcelService _excelService;
        private readonly INotificationService _notificationService;

        public OrderController(ApplicationDbContext context, IServiceProvider serviceProvider, 
            IGHNService ghnService, IConfiguration configuration, IExcelService excelService, INotificationService notificationService)
        {
            _context = context;
            _serviceProvider = serviceProvider;
            _ghnService = ghnService;
            _configuration = configuration;
            _excelService = excelService;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(string searchString, DateTime? fromDate, DateTime? toDate, string status)
        {
            ViewBag.CurrentSearch = searchString;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentStatus = status;

            var orders = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o => 
                    o.OrderCode.Contains(searchString) || 
                    o.OrderUsName.Contains(searchString) || 
                    o.PhoneNumber.Contains(searchString));
            }

            // Lọc theo ngày
            if (fromDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= toDate.Value.AddDays(1));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                OrderStatus orderStatus = (OrderStatus)Enum.Parse(typeof(OrderStatus), status);
                orders = orders.Where(o => o.Status == orderStatus);
            }

            // Sắp xếp theo ngày đặt hàng mới nhất
            orders = orders.OrderByDescending(o => o.OrderDate);

            return View(await orders.ToListAsync());
        }

        // GET: Admin/Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Size)
                .Include(o => o.User)
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
        public async Task<IActionResult> Create([FromBody] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Gửi thông báo khi có đơn hàng mới
                await _notificationService.SendOrderNotification(
                    $"Đơn hàng mới #{order.OrderId} từ {order.OrderUsName}",
                    order.OrderId.ToString(),
                    "new"
                );

                return Ok(new { success = true, message = "Đơn hàng đã được tạo thành công" });
            }
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
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

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status, string reason = null)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                order.Status = status;
                if (status == OrderStatus.Cancelled)
                {
                    order.CancelReason = reason;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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

        // Thêm action GET cho Process
        [HttpGet]
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
        public async Task<IActionResult> Process(int id, string wardCode, int districtId)
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

            // Kiểm tra thông tin bắt buộc
            if (string.IsNullOrEmpty(order.ShippingAddress))
            {
                TempData["ErrorMessage"] = "Thiếu địa chỉ giao hàng";
                return View(order);
            }

            if (string.IsNullOrEmpty(order.PhoneNumber))
            {
                TempData["ErrorMessage"] = "Thiếu số điện thoại người nhận";
                return View(order);
            }

            if (string.IsNullOrEmpty(wardCode))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn Phường/Xã";
                return View(order);
            }

            if (districtId <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn Quận/Huyện";
                return View(order);
            }

            // Cập nhật thông tin địa chỉ
            order.WardCode = wardCode;
            order.DistrictId = districtId;
            await _context.SaveChangesAsync();

            try
            {
                var result = await _ghnService.CreateShippingOrder(order, wardCode, districtId);
                if (result.Success)
                {
                    // Lưu response từ GHN
                    order.ShippingOrderCode = result.OrderCode;
                    order.Status = OrderStatus.Shipped;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Đã tạo đơn vận chuyển thành công! Mã vận đơn: {result.OrderCode}";
                    return RedirectToAction(nameof(Process), new { id = order.OrderId });
                }

                TempData["ErrorMessage"] = $"Lỗi khi tạo đơn vận chuyển: {result.Message}";
                return View(order);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tạo đơn vận chuyển: {ex.Message}";
                return View(order);
            }
        }

        private int CalculateTotalWeight(ICollection<OrderDetail> orderDetails)
        {
            // Tính tổng trọng lượng (gram)
            return orderDetails.Sum(od => od.Product.Weight * od.Quantity);
        }

        private List<GHN_OrderItem> CreateGHNOrderItems(ICollection<OrderDetail> orderDetails)
        {
            return orderDetails.Select(od => new GHN_OrderItem
            {
                Name = od.Product.Name,
                Code = od.Product.Code,
                Quantity = od.Quantity,
                Price = (int)od.Price,
                Weight = od.Product.Weight
            }).ToList();
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

                // Cập nhật trạng thái đơn hàng
                order.Status = newStatus;

                // Cập nhật trạng thái thanh toán nếu cần
                if (order.PaymentMethod == PaymentMethod.COD)
                {
                    if (newStatus == OrderStatus.Completed)
                    {
                        order.PaymentStatus = PaymentStatus.Completed;
                    }
                    else if (newStatus == OrderStatus.Cancelled)
                    {
                        order.PaymentStatus = PaymentStatus.Failed;
                    }
                }

                await _context.SaveChangesAsync();

                // Cập nhật TotalSpent và rank khi đơn hàng hoàn thành và đã thanh toán
                if (newStatus == OrderStatus.Completed && order.PaymentStatus == PaymentStatus.Completed)
                {
                    var user = await _context.Users.FindAsync(order.UserId);
                    if (user != null)
                    {
                        // Tính tổng tiền từ các đơn hàng đã hoàn thành
                        var completedOrders = await _context.Orders
                            .Where(o => o.UserId == user.UserID 
                                && o.Status == OrderStatus.Completed 
                                && o.PaymentStatus == PaymentStatus.Completed)
                            .ToListAsync();

                        user.TotalSpent = completedOrders.Sum(o => o.TotalAmount);

                        // Cập nhật rank dựa trên tổng chi tiêu
                        if (user.TotalSpent >= 10000000) // 10 triệu
                        {
                            user.MemberRankId = 3; // Gold
                        }
                        else if (user.TotalSpent >= 5000000) // 5 triệu
                        {
                            user.MemberRankId = 2; // Silver
                        }
                        else
                        {
                            user.MemberRankId = 1; // Bronze
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
                    return newStatus == OrderStatus.Shipping;
                case OrderStatus.Shipping:
                    return newStatus == OrderStatus.Completed || newStatus == OrderStatus.Cancelled;
                case OrderStatus.Completed:
                    return false; // Không thể chuyển từ trạng thái hoàn thành
                case OrderStatus.Cancelled:
                    return false; // Không thể chuyển từ trạng thái hủy
                default:
                    return false;
            }
        }

        private string GetStatusText(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.Pending:
                    return "Chờ xử lý";
                case OrderStatus.Processing:
                    return "Đang xử lý";
                case OrderStatus.Shipped:
                    return "Đã giao cho đơn vị vận chuyển";
                case OrderStatus.Shipping:
                    return "Đang vận chuyển";
                case OrderStatus.Completed:
                    return "Hoàn thành";
                case OrderStatus.Cancelled:
                    return "Đã hủy";
                default:
                    return status.ToString();
            }
        }

        public class CreateShippingOrderRequest
        {
            public string Id { get; set; }
            public string WardCode { get; set; }
            public int DistrictId { get; set; }
            public int Length { get; set; } = 20;
            public int Width { get; set; } = 20;
            public int Height { get; set; } = 10;
        }

        [HttpPost]
        public async Task<IActionResult> CreateShippingOrder([FromBody] CreateShippingOrderRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderId.ToString() == request.Id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Tạo đơn hàng trên GHN
                var result = await _ghnService.CreateShippingOrder(
                    order, 
                    request.WardCode, 
                    request.DistrictId,
                    request.Length,
                    request.Width,
                    request.Height);

                if (result.Success)
                {
                    order.ShippingOrderCode = result.OrderCode;
                    order.Status = OrderStatus.Shipped;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAllUserRanks()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                foreach (var user in users)
                {
                    // Tính tổng tiền từ các đơn hàng đã hoàn thành
                    var totalSpent = await _context.Orders
                        .Where(o => o.UserId == user.UserID 
                            && o.Status == OrderStatus.Completed 
                            && o.PaymentStatus == PaymentStatus.Completed)
                        .SumAsync(o => o.TotalAmount);

                    user.TotalSpent = totalSpent;

                    // Cập nhật rank dựa trên tổng chi tiêu
                    if (totalSpent >= 10000000) // 10 triệu
                    {
                        user.MemberRankId = 3; // Gold
                    }
                    else if (totalSpent >= 5000000) // 5 triệu
                    {
                        user.MemberRankId = 2; // Silver
                    }
                    else
                    {
                        user.MemberRankId = 1; // Bronze
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã cập nhật rank cho tất cả người dùng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CalculateShippingFee([FromBody] CreateShippingOrderRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderId.ToString() == request.Id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Tính tổng trọng lượng từ các sản phẩm trong đơn hàng
                var totalWeight = CalculateTotalWeight(order.OrderDetails);

                // Tính phí vận chuyển
                var (success, total, message) = await _ghnService.CalculateShippingFeeDetail(
                    request.WardCode,
                    request.DistrictId,
                    totalWeight,
                    request.Length,
                    request.Width,
                    request.Height
                );

                if (success)
                {
                    return Json(new { success = true, fee = total });
                }

                return Json(new { success = false, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAvailableServices([FromBody] GetServicesRequest request)
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Token", _configuration["GHN:Token"]);
                
                var requestContent = new
                {
                    shop_id = int.Parse(_configuration["GHN:ShopId"]),
                    from_district = request.from_district,
                    to_district = request.to_district
                };

                var response = await client.PostAsJsonAsync(
                    "https://dev-online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/available-services",
                    requestContent
                );

                var result = await response.Content.ReadFromJsonAsync<GHNResponse<List<GHNServiceInfo>>>();
                return Json(new { success = true, data = result.Data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> ExportToExcel(string searchString, DateTime? fromDate, DateTime? toDate, string status)
        {
            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.User)
                .AsQueryable();

            // Áp dụng các điều kiện lọc tương tự như Index
            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o => 
                    o.OrderCode.Contains(searchString) || 
                    o.OrderUsName.Contains(searchString) || 
                    o.PhoneNumber.Contains(searchString));
            }

            if (fromDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= toDate.Value.AddDays(1));
            }

            if (!string.IsNullOrEmpty(status))
            {
                OrderStatus orderStatus = (OrderStatus)Enum.Parse(typeof(OrderStatus), status);
                orders = orders.Where(o => o.Status == orderStatus);
            }

            var orderList = await orders.OrderByDescending(o => o.OrderDate).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Orders");

                // Thiết lập header
                worksheet.Cell(1, 1).Value = "Mã đơn hàng";
                worksheet.Cell(1, 2).Value = "Ngày đặt";
                worksheet.Cell(1, 3).Value = "Khách hàng";
                worksheet.Cell(1, 4).Value = "Số điện thoại";
                worksheet.Cell(1, 5).Value = "Địa chỉ";
                worksheet.Cell(1, 6).Value = "Tổng tiền";
                worksheet.Cell(1, 7).Value = "Trạng thái";
                worksheet.Cell(1, 8).Value = "Thanh toán";
                worksheet.Cell(1, 9).Value = "Chi tiết đơn hàng";

                // Style cho header
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Điền dữ liệu
                int row = 2;
                foreach (var order in orderList)
                {
                    worksheet.Cell(row, 1).Value = order.OrderCode;
                    worksheet.Cell(row, 2).Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(row, 3).Value = order.OrderUsName;
                    worksheet.Cell(row, 4).Value = order.PhoneNumber;
                    worksheet.Cell(row, 5).Value = order.ShippingAddress;
                    worksheet.Cell(row, 6).Value = order.TotalAmount;
                    worksheet.Cell(row, 7).Value = order.Status.ToString();
                    worksheet.Cell(row, 8).Value = order.PaymentStatus.ToString();

                    // Tạo chi tiết đơn hàng
                    var orderDetails = order.OrderDetails.Select(od => 
                        $"{od.Product.Name} - SL: {od.Quantity} - Giá: {od.Price:N0}đ");
                    worksheet.Cell(row, 9).Value = string.Join("\n", orderDetails);

                    row++;
                }

                // Tự động điều chỉnh độ rộng cột
                worksheet.Columns().AdjustToContents();

                // Format cột tiền tệ
                worksheet.Column(6).Style.NumberFormat.Format = "#,##0";

                // Xuất file
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"Orders_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        // Action xuất báo cáo doanh thu
        [HttpPost]
        public async Task<IActionResult> ExportRevenueReport(DateTime fromDate, DateTime toDate)
        {
            var reportData = new List<RevenueReportData>();
            
            for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            {
                var nextDate = date.AddDays(1);
                
                var dailyOrders = await _context.Orders
                    .Where(o => o.OrderDate >= date && o.OrderDate < nextDate)
                    .ToListAsync();

                var totalOrders = dailyOrders.Count;
                var revenue = dailyOrders.Where(o => o.Status == OrderStatus.Completed)
                    .Sum(o => o.TotalAmount);
                var cancelledOrders = dailyOrders.Count(o => o.Status == OrderStatus.Cancelled);
                var completionRate = totalOrders > 0 
                    ? (decimal)(totalOrders - cancelledOrders) / totalOrders 
                    : 0;

                reportData.Add(new RevenueReportData
                {
                    Date = date,
                    TotalOrders = totalOrders,
                    Revenue = revenue,
                    CancelledOrders = cancelledOrders,
                    CompletionRate = completionRate,
                    Note = string.Empty
                });
            }

            var fileBytes = _excelService.ExportRevenueReport(fromDate, toDate, reportData);
            var fileName = $"revenue_report_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Trong phương thức hủy đơn hàng
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();

                // Gửi thông báo khi đơn hàng bị hủy
                await _notificationService.SendOrderNotification(
                    $"Đơn hàng #{id} đã bị hủy",
                    id.ToString(),
                    "cancel"
                );

                return Ok(new { success = true, message = "Đơn hàng đã được hủy" });
            }
            return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
        }

        // Trong phương thức xử lý yêu cầu đổi/trả
        [HttpPost]
        public async Task<IActionResult> ProcessReturnRequest(int id)
        {
            var returnRequest = await _context.ReturnRequests.FindAsync(id);
            if (returnRequest != null)
            {
                // Xử lý logic đổi/trả
                await _context.SaveChangesAsync();

                // Gửi thông báo khi có yêu cầu đổi/trả
                await _notificationService.SendOrderNotification(
                    $"Yêu cầu đổi/trả cho đơn hàng #{returnRequest.OrderId}",
                    returnRequest.OrderId.ToString(),
                    "return"
                );

                return Ok(new { success = true, message = "Yêu cầu đổi/trả đã được xử lý" });
            }
            return NotFound(new { success = false, message = "Không tìm thấy yêu cầu đổi/trả" });
        }
    }
}
