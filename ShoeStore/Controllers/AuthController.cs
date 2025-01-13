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
using ShoeStore.Services.ReCaptcha;

namespace ShoeStore.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly IMemberRankService _memberRankService;
        private readonly IGoogleReCaptchaService _reCaptchaService;

        public AuthController(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService,
            IMemberRankService memberRankService,
            IGoogleReCaptchaService reCaptchaService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _memberRankService = memberRankService;
            _reCaptchaService = reCaptchaService;
        }

        // GET: /Auth/Login
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

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserLoginDTO login)
        {
            try
            {
                // Verify reCAPTCHA trước
                var reCaptchaToken = Request.Form["g-recaptcha-response"].ToString();
                var reCaptchaValid = await _reCaptchaService.VerifyToken(reCaptchaToken);
                if (!reCaptchaValid)
                {
                    ViewBag.ReCaptchaError = string.IsNullOrEmpty(_reCaptchaService.LastError)
                        ? "Vui lòng xác nhận bạn không phải là robot"
                        : _reCaptchaService.LastError;
                    return View(login);
                }

                // Sau đó mới kiểm tra ModelState
                if (!ModelState.IsValid)
                {
                    return View(login);
                }

                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Username == login.Username);

                if (user != null && PasswordHelper.VerifyPassword(login.Password, user.PasswordHash))
                {
                    // Kiểm tra email đã xác thực chưa
                    if (!user.EmailConfirmed)
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản chưa được xác thực. Vui lòng kiểm tra email để xác thực tài khoản.");
                        ViewBag.Email = user.Email; // Để hiển thị nút gửi lại email xác thực
                        return View(login);
                    }

                    // Kiểm tra tài khoản có bị khóa không
                    if (!user.Status)
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ admin.");
                        return View(login);
                    }

                    // Lưu thông tin user vào session
                    HttpContext.Session.Set<User>("userInfo", user);

                    // Nếu người dùng chọn "Ghi nhớ đăng nhập"
                    if (login.RememberMe)
                    {
                        // Tạo chuỗi thông tin đăng nhập được mã hóa
                        var loginInfo = $"{login.Username}|{user.PasswordHash}";
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
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi đăng nhập: " + ex.Message;
                return View(login);
            }
        }

        // GET: /Auth/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (HttpContext.Session.Get<User>("userInfo") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }



        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegisterDTO model)
        {
            try 
            {
                if (ModelState.IsValid)
                {
                    // Verify reCAPTCHA
                    var reCaptchaToken = Request.Form["g-recaptcha-response"].ToString();
                    var reCaptchaValid = await _reCaptchaService.VerifyToken(reCaptchaToken);
                    if (!reCaptchaValid)
                    {
                        ViewBag.ReCaptchaError = "Vui lòng xác nhận bạn không phải là robot";
                        return View(model);
                    }

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
                        RoleID = 1, // Mặc định là User
                        Status = false, // Tài khoản chưa kích hoạt
                        RegisterDate = DateTime.Now,
                        EmailConfirmationToken = Guid.NewGuid().ToString(),
                        EmailConfirmationTokenExpiry = DateTime.Now.AddHours(24),
                        EmailConfirmed = false
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Tạo link xác thực
                    var confirmationLink = Url.Action("ConfirmEmail", "Auth",
                        new { userId = user.UserID, token = user.EmailConfirmationToken },
                        protocol: HttpContext.Request.Scheme);

                    // Gửi email xác thực
                    await _emailService.SendEmailConfirmationAsync(user.Email, confirmationLink);

                    // Thêm thông báo thành công vào TempData
                    TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng kiểm tra email để xác thực tài khoản.";
                    
                    return RedirectToAction("Login");
                }
                return View(model);
            }
            catch (Exception ex)
            {
                ViewData["Message"] = "Có lỗi xảy ra khi đăng ký: " + ex.Message;
                return View(model);
            }
        }

        // GET: /Auth/ForgotPassword
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Auth/ForgotPassword
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
            var confirmationLink = Url.Action("ConfirmEmail", "Auth",
                new { userId = user.UserID, token = user.EmailConfirmationToken },
                protocol: HttpContext.Request.Scheme);

            // Gửi lại email xác thực
            await _emailService.SendEmailConfirmationAsync(user.Email, confirmationLink);

            return Json(new { success = true, message = "Email xác thực đã được gửi lại. Vui lòng kiểm tra hộp thư của bạn." });
        }
    }
} 