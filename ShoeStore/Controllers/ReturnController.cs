using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Utils;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace ShoeStore.Controllers
{
    [Route("Return")]
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
        [HttpGet("Create/{orderId}")]
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
                return RedirectToAction("Orders", "Account");
            }

            return View(order);
        }

        [HttpPost("Create/{orderId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int orderId, string reason, List<IFormFile> images)
        {
            try
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                if (userInfo == null)
                    return RedirectToAction("Login", "Auth");

                // Kiểm tra user có tồn tại trong database không
                var user = await _context.Users.FindAsync(userInfo.UserID);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin người dùng";
                    return RedirectToAction("Login", "Auth");
                }

                // Kiểm tra order và include relationship
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userInfo.UserID);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền thực hiện yêu cầu này";
                    return RedirectToAction("Orders", "Account");
                }

                if (string.IsNullOrEmpty(reason))
                {
                    TempData["Error"] = "Vui lòng nhập lý do đổi trả";
                    return RedirectToAction("Create", new { orderId = orderId });
                }

                var imagesPaths = new List<string>();
                if (images != null && images.Any())
                {
                    var returnsPath = Path.Combine(_environment.WebRootPath, "images", "returns");
                    Directory.CreateDirectory(returnsPath); // Tạo thư mục nếu chưa tồn tại

                    foreach (var image in images)
                    {
                        if (image != null && image.Length > 0)
                        {
                            // Kiểm tra định dạng file
                            var extension = Path.GetExtension(image.FileName).ToLower();
                            if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(extension))
                            {
                                TempData["Error"] = "Chỉ chấp nhận file ảnh có định dạng .jpg, .jpeg, .png, .gif";
                                return RedirectToAction("Create", new { orderId = orderId });
                            }

                            // Giới hạn kích thước file (ví dụ: 5MB)
                            if (image.Length > 5 * 1024 * 1024)
                            {
                                TempData["Error"] = "Kích thước file không được vượt quá 5MB";
                                return RedirectToAction("Create", new { orderId = orderId });
                            }

                            var fileName = $"{Guid.NewGuid()}{extension}";
                            var filePath = Path.Combine(returnsPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }
                            imagesPaths.Add($"/images/returns/{fileName}");
                        }
                    }
                }

                var returnRequest = new ReturnRequest
                {
                    OrderId = order.OrderId,
                    UserId = order.UserId,
                    Reason = reason,
                    Images = imagesPaths.Any() ? string.Join(",", imagesPaths) : null,
                    Status = ReturnStatus.Pending,
                    RequestDate = DateTime.Now,
                    Order = order
                };

                // Thêm và lưu trong một transaction
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _context.Entry(order).State = EntityState.Unchanged;
                        _context.ReturnRequests.Add(returnRequest);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["Success"] = "Yêu cầu đổi trả đã được gửi thành công";
                        return RedirectToAction("History", "Return");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        // Xóa các file ảnh đã upload nếu có lỗi
                        foreach (var imagePath in imagesPaths)
                        {
                            var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        // Log chi tiết lỗi
                        Console.WriteLine($"Error details: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi gửi yêu cầu: " + ex.Message;
                return RedirectToAction("Create", new { orderId = orderId });
            }
        }

        // Xem lịch sử đổi trả
        [HttpGet("History")]
        public async Task<IActionResult> History()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
                return RedirectToAction("Login", "Auth");

            var returns = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .Where(r => r.UserId == userInfo.UserID)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(returns);
        }
    }
} 