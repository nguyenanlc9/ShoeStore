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
using Microsoft.Extensions.Configuration;

namespace ShoeStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly IMemberRankService _memberRankService;
        private readonly IConfiguration _configuration;

        public AccountController(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService,
            IMemberRankService memberRankService,
            IConfiguration configuration)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _memberRankService = memberRankService;
            _configuration = configuration;
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

                // Kiểm tra số điện thoại
                if (string.IsNullOrEmpty(model.Phone) || model.Phone == "Chưa cập nhật")
                {
                    ModelState.AddModelError("Phone", "Vui lòng nhập số điện thoại");
                    return View(model);
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Phone, @"^\d{10}$"))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại không hợp lệ. Vui lòng nhập đúng 10 chữ số.");
                    return View(model);
                }

                // Chỉ cập nhật các trường được phép
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Phone = model.Phone;

                await _context.SaveChangesAsync();

                // Cập nhật lại session
                HttpContext.Session.Set("userInfo", user);

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật thông tin: " + ex.Message);
                return View(model);
            }
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

                // Chỉ cập nhật TotalSpent khi đơn hàng đã thanh toán
                if (order.PaymentStatus == PaymentStatus.Completed)
                {
                    // Tính tổng tiền từ các đơn hàng đã hoàn thành và thanh toán
                    var completedOrders = await _context.Orders
                        .Where(o => o.UserId == userInfo.UserID 
                            && o.Status == OrderStatus.Completed 
                            && o.PaymentStatus == PaymentStatus.Completed)
                        .SumAsync(o => o.TotalAmount);

                    var user = await _context.Users.FindAsync(userInfo.UserID);
                    if (user != null)
                    {
                        user.TotalSpent = completedOrders;
                        _context.Users.Update(user);
                    }

                    await _context.SaveChangesAsync();

                    // Cập nhật rank của user sau khi đã cập nhật TotalSpent
                    await _memberRankService.UpdateUserRank(userInfo.UserID);
                }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra username đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username đã tồn tại");
                    return View(model);
                }

                // Kiểm tra email đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                    return View(model);
                }

                // Tạo token xác thực email
                model.EmailConfirmationToken = Guid.NewGuid().ToString();
                model.EmailConfirmationTokenExpiry = DateTime.Now.AddHours(24);
                model.Status = false; // Tài khoản chưa kích hoạt
                model.RoleID = 1; // Role mặc định là User

                // Hash mật khẩu
                model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

                // Lưu user vào database
                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                // Tạo link xác thực
                var confirmationLink = Url.Action("ConfirmEmail", "Account",
                    new { userId = model.UserID, token = model.EmailConfirmationToken },
                    protocol: HttpContext.Request.Scheme);

                // Gửi email xác thực
                await _emailService.SendEmailConfirmationAsync(model.Email, confirmationLink);

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác thực tài khoản.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(int userId, string token)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Kiểm tra token có hợp lệ không
            if (user.EmailConfirmationToken != token)
            {
                TempData["ErrorMessage"] = "Link xác thực không hợp lệ.";
                return RedirectToAction("Login");
            }

            // Kiểm tra token có hết hạn không
            if (user.EmailConfirmationTokenExpiry < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Link xác thực đã hết hạn.";
                return RedirectToAction("Login");
            }

            // Xác thực email thành công
            user.EmailConfirmed = true;
            user.Status = true; // Kích hoạt tài khoản
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiry = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xác thực email thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.EmailConfirmed)
            {
                return Json(new { success = false, message = "Không thể gửi lại email xác thực." });
            }

            // Tạo token mới
            user.EmailConfirmationToken = Guid.NewGuid().ToString();
            user.EmailConfirmationTokenExpiry = DateTime.Now.AddHours(24);

            await _context.SaveChangesAsync();

            // Tạo link xác thực mới
            var confirmationLink = Url.Action("ConfirmEmail", "Account",
                new { userId = user.UserID, token = user.EmailConfirmationToken },
                protocol: HttpContext.Request.Scheme);

            // Gửi lại email xác thực
            await _emailService.SendEmailConfirmationAsync(user.Email, confirmationLink);

            return Json(new { success = true, message = "Email xác thực đã được gửi lại. Vui lòng kiểm tra hộp thư của bạn." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserLoginDTO model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // Kiểm tra email đã xác thực chưa
                    if (!user.EmailConfirmed)
                    {
                        ModelState.AddModelError("", "Tài khoản chưa được xác thực. Vui lòng kiểm tra email để xác thực.");
                        ViewBag.Email = user.Email;
                        return View(model);
                    }

                    // Kiểm tra tài khoản có bị khóa không
                    if (!user.Status)
                    {
                        ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ admin.");
                        return View(model);
                    }

                    // Cập nhật thời gian đăng nhập
                    user.LastLogin = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Lưu thông tin user vào session
                    HttpContext.Session.Set("userInfo", user);

                    // Chuyển hướng dựa vào role
                    if (user.Role.RoleName == "Admin")
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Username hoặc mật khẩu không đúng");
            }

            return View(model);
        }
    }
} 