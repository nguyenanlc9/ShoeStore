using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Utils;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace ShoeStore.Controllers
{
    public class ReturnController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ReturnController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Hiển thị form yêu cầu đổi trả
        public async Task<IActionResult> Create(int orderId)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
                return RedirectToAction("Login", "Auth");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userInfo.UserID);

            if (order == null)
                return NotFound();

            // Kiểm tra điều kiện đổi trả (ví dụ: trong 7 ngày)
            if ((DateTime.Now - order.OrderDate).TotalDays > 7)
            {
                TempData["Error"] = "Đơn hàng đã quá thời hạn đổi trả (7 ngày)";
                return RedirectToAction("Orders", "Auth");
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int orderId, string reason, List<IFormFile> images)
        {
            try
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                if (userInfo == null)
                    return RedirectToAction("Login", "Auth");

                var order = await _context.Orders.FindAsync(orderId);
                if (order == null || order.UserId != userInfo.UserID)
                    return NotFound();

                var imagesPaths = new List<string>();
                if (images != null && images.Count > 0)
                {
                    var returnsPath = Path.Combine(_environment.WebRootPath, "images", "returns");
                    if (!Directory.Exists(returnsPath))
                    {
                        Directory.CreateDirectory(returnsPath);
                    }

                    foreach (var image in images)
                    {
                        if (image.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            var filePath = Path.Combine(_environment.WebRootPath, "images", "returns", fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }
                            imagesPaths.Add("/images/returns/" + fileName);
                        }
                    }
                }

                var returnRequest = new ReturnRequest
                {
                    OrderId = orderId,
                    UserId = userInfo.UserID,
                    Reason = reason,
                    Images = string.Join(",", imagesPaths),
                    Status = ReturnStatus.Pending,
                    RequestDate = DateTime.Now
                };

                _context.ReturnRequests.Add(returnRequest);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Yêu cầu đổi trả đã được gửi thành công. Chúng tôi sẽ xử lý trong thời gian sớm nhất!";
                return RedirectToAction("Orders", "Account");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Orders", "Account");
            }
        }

        // Xem lịch sử đổi trả
        public async Task<IActionResult> History()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
                return RedirectToAction("Login", "Auth");

            var returns = await _context.ReturnRequests
                .Include(r => r.Order)
                .Where(r => r.UserId == userInfo.UserID)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(returns);
        }
    }
} 