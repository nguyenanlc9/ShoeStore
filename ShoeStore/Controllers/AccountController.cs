using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Filters;
using ShoeStore.Models;
using ShoeStore.Models.DTO.Request;
using ShoeStore.Utils;

namespace ShoeStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
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

            var result = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username == login.UserName);

            if (result != null && PasswordHelper.VerifyPassword(login.Password, result.PasswordHash))
            {
                HttpContext.Session.Set<User>("userInfo", result);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng!");
            }
            return View(login);
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegisterDTO model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ViewData["Message"] = "Tên đăng nhập đã được sử dụng!";
                    return View(model);
                }

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ViewData["Message"] = "Email đã được sử dụng!";
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = PasswordHelper.HashPassword(model.Password),
                    FullName = model.FullName,
                    Address = model.Address,
                    Phone = model.Phone,
                    RoleID = 1, // Role User
                    RegisterDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    Status = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                HttpContext.Session.Set("userInfo", user);
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    ViewData["Message"] = "Email không tồn tại trong hệ thống!";
                    return View(model);
                }

                // Tạo mật khẩu mới ngẫu nhiên
                var newPassword = GenerateRandomPassword();
                user.PasswordHash = PasswordHelper.HashPassword(newPassword);

                try
                {
                    // Gửi email mật khẩu mới
                    await SendPasswordResetEmail(user.Email, newPassword);
                    await _context.SaveChangesAsync();

                    ViewData["MessageType"] = "success";
                    ViewData["Message"] = "Mật khẩu mới đã được gửi đến email của bạn!";
                }
                catch
                {
                    ViewData["Message"] = "Có lỗi xảy ra khi gửi email!";
                }
            }

            return View(model);
        }

        private string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task SendPasswordResetEmail(string email, string newPassword)
        {
            // Implement email sending logic here
            // You might want to use a service like SendGrid or SMTP
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult Profile()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return RedirectToAction("Login");
            }
            ViewData["IsLoggedIn"] = true;
            ViewData["FullName"] = userInfo.FullName;
            return View(userInfo);
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
    }
} 