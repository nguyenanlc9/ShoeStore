using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ShoeStore.Filters;
using ShoeStore.Models;
using ShoeStore.Models.DTO.Request;
using ShoeStore.Utils;
using ShoeStore.Models.Enums;
using ShoeStore.Services.Email;
using ShoeStore.Services.MemberRanking;

namespace ShoeStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly IMemberRankService _memberRankService;

        public AccountController(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService,
            IMemberRankService memberRankService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _memberRankService = memberRankService;
        }

        [TypeFilter(typeof(AuthenticationFilter))]
        public async Task<IActionResult> Profile()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            
            // Cập nhật rank trước khi lấy thông tin user
            await _memberRankService.UpdateUserRank(userInfo.UserID);

            // Lấy thông tin user đã được cập nhật
            var user = await _context.Users
                .Include(u => u.MemberRank)
                .FirstOrDefaultAsync(u => u.UserID == userInfo.UserID);

            if (user == null)
            {
                return NotFound();
            }

            // Lấy tổng số đơn hàng và tổng tiền đã chi tiêu
            var orders = await _context.Orders
                .Where(o => o.UserId == user.UserID && o.Status != OrderStatus.Cancelled)
                .ToListAsync();

            ViewBag.TotalOrders = orders.Count();
            ViewBag.TotalSpent = orders.Sum(o => o.TotalAmount);

            return View(user);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View(userInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(User model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users.FindAsync(model.UserID);
                    if (user == null)
                    {
                        return NotFound();
                    }

                    // Kiểm tra xem email mới có bị trùng không (nếu email thay đổi)
                    if (model.Email != user.Email)
                    {
                        var existingUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == model.Email && u.UserID != model.UserID);
                        if (existingUser != null)
                        {
                            ModelState.AddModelError("Email", "Email đã được sử dụng");
                            return View(model);
                        }
                    }

                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.Phone = model.Phone;

                    await _context.SaveChangesAsync();

                    // Cập nhật lại session
                    HttpContext.Session.Set("userInfo", user);

                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction(nameof(Profile));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật thông tin");
                }
            }
            return View(model);
        }

        public IActionResult ChangePassword()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO model)
        {
            if (ModelState.IsValid)
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                if (userInfo == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var user = await _context.Users.FindAsync(userInfo.UserID);
                if (user == null)
                {
                    return NotFound();
                }

                // Kiểm tra mật khẩu cũ
                if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                    return View(model);
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction(nameof(Profile));
            }
            return View(model);
        }

        [TypeFilter(typeof(AuthenticationFilter))]
        public async Task<IActionResult> Orders()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userInfo.UserID)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(int orderId)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userInfo.UserID);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == OrderStatus.Delivered)
            {
                order.Status = OrderStatus.Completed;
                order.CreatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Cập nhật rank của user
                await _memberRankService.UpdateUserRank(userInfo.UserID);

                TempData["SuccessMessage"] = "Xác nhận đã nhận hàng thành công!";
            }

            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductSizeStocks)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userInfo.UserID);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Cancelled;
                order.CancelReason = "Hủy bởi người dùng";
                order.CreatedAt = DateTime.Now;

                // Hoàn trả số lượng sản phẩm vào kho
                foreach (var item in order.OrderDetails)
                {
                    var productSizeStock = item.Product.ProductSizeStocks
                        .FirstOrDefault(pss => pss.ProductID == item.Product.ProductId && pss.SizeID == item.SizeId);
                    
                    if (productSizeStock != null)
                    {
                        productSizeStock.StockQuantity += item.Quantity;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Hủy đơn hàng thành công!";
            }

            return RedirectToAction(nameof(Orders));
        }

        [HttpGet]
        [Route("Account/OrderDetails/{orderId}")]
        public async Task<IActionResult> OrderDetails(int orderId)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userInfo.UserID);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
} 