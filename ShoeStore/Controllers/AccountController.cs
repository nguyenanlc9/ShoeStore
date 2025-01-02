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
            if (ModelState.IsValid)
            {
                // Kiểm tra xem username đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ViewData["Message"] = "Tên đăng nhập đã tồn tại";
                    return View(model);
                }

                // Kiểm tra email đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ViewData["Message"] = "Email đã được sử dụng";
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    PasswordHash = PasswordHelper.HashPassword(model.Password),
                    RoleID = 2, // Role User
                    Status = true,
                    RegisterDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Thêm thông báo thành công vào TempData
                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                
                return RedirectToAction("Login");
            }
            return View(model);
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["Message"] = "Email không tồn tại trong hệ thống";
                return View();
            }

            try
            {
                // Tạo mật khẩu mới
                string newPassword = GenerateRandomPassword();
                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                await _context.SaveChangesAsync();

                // Gửi email
                string emailBody = EmailTemplates.GetResetPasswordEmail(user.FullName, newPassword);
                await _emailService.SendEmailAsync(user.Email, "Đặt lại mật khẩu - ShoeStore", emailBody);

                TempData["SuccessMessage"] = "Mật khẩu mới đã được gửi đến email của bạn";
                return RedirectToAction("Login");
            }
            catch
            {
                TempData["Message"] = "Có lỗi xảy ra khi gửi email";
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
            
            // Cập nhật rank trước khi lấy thông tin user
            await _memberRankService.UpdateUserRank(userInfo.UserID);

            // Lấy thông tin user đã được cập nhật
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

            // Cập nhật session với thông tin mới nhất
            HttpContext.Session.Set("userInfo", user);

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
        public async Task<IActionResult> EditProfile(User model)
        {
            try
            {
                var userInfo = HttpContext.Session.Get<User>("userInfo");
                if (userInfo == null)
                {
                    return RedirectToAction("Login");
                }

                // Lấy user hiện tại từ database
                var user = await _context.Users.FindAsync(userInfo.UserID);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin người dùng";
                    return View(model);
                }

                // Cập nhật thông tin
                user.FullName = model.FullName;
                user.Phone = model.Phone;
                user.Email = model.Email;

                // Lưu thay đổi vào database
                await _context.SaveChangesAsync();

                // Cập nhật lại session
                userInfo.FullName = model.FullName;
                userInfo.Phone = model.Phone;
                userInfo.Email = model.Email;
                HttpContext.Session.Set("userInfo", userInfo);

                TempData["Success"] = "Cập nhật thông tin thành công";
                return RedirectToAction("EditProfile");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
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
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập";
                return RedirectToAction("EditProfile");
            }

            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userInfo.UserID);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng";
                return RedirectToAction("EditProfile");
            }

            // Kiểm tra mật khẩu hiện tại sử dụng PasswordHelper
            if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                TempData["Error"] = "Mật khẩu hiện tại không đúng";
                return RedirectToAction("EditProfile");
            }

            // Cập nhật mật khẩu mới sử dụng PasswordHelper
            user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            // Cập nhật lại session
            HttpContext.Session.Set("userInfo", user);

            TempData["Success"] = "Đổi mật khẩu thành công";
            return RedirectToAction("EditProfile");
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