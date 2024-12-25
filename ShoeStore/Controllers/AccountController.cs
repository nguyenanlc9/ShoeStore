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
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = PasswordHelper.HashPassword(model.Password),
                    RegisterDate = DateTime.Now,
                    Address = model.Address ?? string.Empty,
                    Email = model.Email,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Status = true,
                    RoleID = 1
                };

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Login");
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
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Logic xử lý quên mật khẩu
                ViewData["Message"] = "Yêu cầu đặt lại mật khẩu đã được gửi.";
            }
            return View(model);
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
                    user.LastLogin = existingUser.LastLogin;
                    user.CreatedDate = existingUser.CreatedDate;

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