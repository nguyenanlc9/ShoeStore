using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ShoeStore.Filters;
using ShoeStore.Models;
using ShoeStore.Models.DTO.Request;
using ShoeStore.Utils;
using ShoeStore.Models.Enums;
using ShoeStore.Services.Email;

namespace ShoeStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;

        public AccountController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IEmailService emailService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            // Kiểm tra nếu đã đăng nhập qua session
            if (HttpContext.Session.Get<User>("userInfo") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra cookie ghi nhớ đăng nhập
            var rememberMeCookie = Request.Cookies["RememberMe"];
            if (!string.IsNullOrEmpty(rememberMeCookie))
            {
                try
                {
                    var decryptedLoginInfo = CookieHelper.Decrypt(rememberMeCookie);
                    var loginInfo = decryptedLoginInfo.Split('|');
                    var username = loginInfo[0];
                    var passwordHash = loginInfo[1];

                    var user = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Username == username && x.PasswordHash == passwordHash);

                    if (user != null)
                    {
                        HttpContext.Session.Set<User>("userInfo", user);
                        return RedirectToAction("Index", "Home");
                    }
                }
                catch
                {
                    // Nếu có lỗi khi giải mã cookie, xóa cookie đó
                    Response.Cookies.Delete("RememberMe");
                }
            }

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserLoginDTO login)
        {
            if (!ModelState.IsValid)
            {
                return View(login);
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username == login.UserName);

            if (user != null && PasswordHelper.VerifyPassword(login.Password, user.PasswordHash))
            {
                // Lưu thông tin user vào session
                HttpContext.Session.Set<User>("userInfo", user);

                // Nếu người dùng chọn "Ghi nhớ đăng nhập"
                if (login.RememberMe)
                {
                    // Tạo chuỗi thông tin đăng nhập được mã hóa
                    var loginInfo = $"{login.UserName}|{user.PasswordHash}";
                    var encryptedLoginInfo = CookieHelper.Encrypt(loginInfo);

                    // Tạo cookie với thời hạn 30 ngày
                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30),
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    };

                    Response.Cookies.Append("RememberMe", encryptedLoginInfo, cookieOptions);
                }
                else
                {
                    // Xóa cookie nếu có
                    Response.Cookies.Delete("RememberMe");
                }

                // Cập nhật thời gian đăng nhập cuối
                user.LastLogin = DateTime.Now;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng!");
            return View(login);
        }

        // GET: /Account/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (HttpContext.Session.Get<User>("userInfo") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegisterDTO model)
        {
            try
            {
                // Log ModelState errors
                if (!ModelState.IsValid)
                {
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                        }
                    }
                    return View(model);
                }

                // Log registration attempt
                Console.WriteLine($"Attempting to register user: {model.Username}, Email: {model.Email}");

                // Kiểm tra username đã tồn tại
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã được sử dụng");
                    return View(model);
                }

                // Kiểm tra email đã tồn tại
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                    return View(model);
                }

                // Tạo user mới
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = PasswordHelper.HashPassword(model.Password),
                    FullName = model.FullName,
                    Phone = model.Phone,
                    RoleID = 1, // Sửa lại thành 1 vì 1 là User, 2 là Admin
                    RegisterDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    Status = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Log successful registration
                Console.WriteLine($"User registered successfully: {user.Username}");

                // Lưu thông tin user vào session
                HttpContext.Session.Set("userInfo", user);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Log detailed error
                Console.WriteLine($"Registration error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đăng ký: " + ex.Message);
                return View(model);
            }
        }

        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    TempData["Message"] = "Email không tồn tại trong hệ thống";
                    return View();
                }

                // Tạo mật khẩu mới ngẫu nhiên
                string newPassword = GenerateRandomPassword();
                
                // Cập nhật mật khẩu mới trong database
                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                await _context.SaveChangesAsync();

                // Gửi email chứa mật khẩu mới
                await _emailService.SendEmailAsync(
                    email,
                    "Đặt lại mật khẩu - ShoeStore",
                    EmailTemplates.GetResetPasswordEmail(user.FullName, newPassword)
                );

                TempData["Success"] = true;
                TempData["Message"] = "Mật khẩu mới đã được gửi đến email của bạn";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        private string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            // Xóa cookie ghi nhớ đăng nhập
            Response.Cookies.Delete("RememberMe");
            return RedirectToAction("Login");
        }

        [TypeFilter(typeof(AuthenticationFilter))]
        public async Task<IActionResult> Profile()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            
            var user = await _context.Users
                .Include(u => u.MemberRank)
                .FirstOrDefaultAsync(u => u.UserID == userInfo.UserID);

            // Thêm log
            Console.WriteLine($"User Profile - ID: {user.UserID}");
            Console.WriteLine($"Total Spent: {user.TotalSpent}");
            Console.WriteLine($"Current Rank: {user.MemberRank?.RankName ?? "None"}");

            // Tìm rank tiếp theo
            var nextRank = await _context.MemberRanks
                .Where(r => r.MinimumSpent > user.TotalSpent)
                .OrderBy(r => r.MinimumSpent)
                .FirstOrDefaultAsync();

            if (nextRank != null)
            {
                Console.WriteLine($"Next Rank: {nextRank.RankName}");
                Console.WriteLine($"Need to spend: {nextRank.MinimumSpent - user.TotalSpent} more");
            }

            ViewBag.NextRank = nextRank;
            return View(user);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(userInfo.UserID);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(User user)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserID == userInfo.UserID);

                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật thông tin cơ bản
                    user.Username = existingUser.Username; // Không cho phép đổi username
                    user.RoleID = existingUser.RoleID;
                    user.Status = existingUser.Status;
                    user.RegisterDate = existingUser.RegisterDate;
                    user.CreatedDate = existingUser.CreatedDate;
                    user.LastLogin = existingUser.LastLogin;

                    // Xử lý mật khẩu
                    if (!string.IsNullOrEmpty(user.NewPassword))
                    {
                        user.PasswordHash = PasswordHelper.HashPassword(user.NewPassword);
                    }
                    else
                    {
                        user.PasswordHash = existingUser.PasswordHash;
                    }

                    _context.Entry(existingUser).State = EntityState.Detached;
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    // Cập nhật lại session
                    HttpContext.Session.Set("userInfo", user);

                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thông tin: " + ex.Message);
                }
            }

            return View(user);
        }

        public IActionResult ChangePassword()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userInfo.UserID);
            if (user == null || !PasswordHelper.VerifyPassword(model.OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu cũ không đúng.");
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu mới và xác nhận mật khẩu không khớp.");
                return View(model);
            }

            user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            _context.Update(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.Set<User>("userInfo", user); // Cập nhật session
            ViewData["Message"] = "Mật khẩu đã được thay đổi thành công.";
            return RedirectToAction("Profile");
        }

        [TypeFilter(typeof(AuthenticationFilter))]
        public async Task<IActionResult> Orders()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Size)
                .Where(o => o.UserId == userInfo.UserID)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(int orderId)
        {
            try
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userInfo.UserID);

                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                if (order.Status != OrderStatus.Delivered)
                    return Json(new { success = false, message = "Trạng thái đơn hàng không hợp lệ" });

                order.Status = OrderStatus.Completed;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userInfo.UserID);

                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                if (order.Status != OrderStatus.Pending)
                    return Json(new { success = false, message = "Không thể hủy đơn hàng ở trạng thái này" });

                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpGet]
        [Route("Account/OrderDetails/{orderId}")]
        public async Task<IActionResult> OrderDetails(int orderId)
        {
            try 
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                if (userInfo == null)
                    return PartialView("_Error", "Vui lòng đăng nhập");

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Size)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userInfo.UserID);

                if (order == null)
                {
                    return PartialView("_Error", "Không tìm thấy đơn hàng");
                }

                return PartialView("_OrderDetails", order);
            }
            catch (Exception ex)
            {
                return PartialView("_Error", "Có lỗi xảy ra: " + ex.Message);
            }
        }
    }
} 