using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using Microsoft.AspNetCore.Http;
using ShoeStore.Utils;
using ShoeStore.Models.DTO.Request;
using ShoeStore.Filters;
using Microsoft.AspNetCore.Authorization;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string AdminSessionKey = "AdminUserInfo";

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            var adminInfo = HttpContext.Session.Get<User>(AdminSessionKey);
            if (adminInfo?.RoleID == 2)
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            var login = Request.Cookies.Get<AdminLoginDTO>("AdminCredential");
            if (login != null)
            {
                var result = _context.Users.AsNoTracking()
                    .FirstOrDefault(x => x.Username == login.UserName &&
                            PasswordHelper.VerifyPassword(login.Password, x.PasswordHash));
                if (result != null && result.RoleID == 2)
                {
                    HttpContext.Session.Set(AdminSessionKey, result);
                    return RedirectToAction("Index", "Home");
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginDTO login)
        {
            var result = await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username == login.UserName);

            if (result != null && PasswordHelper.VerifyPassword(login.Password, result.PasswordHash))
            {
                if (result.RoleID == 2)
                {
                    if (login.RememberMe)
                    {
                        Response.Cookies.Append("AdminCredential", login, new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddYears(7),
                            HttpOnly = true,
                            IsEssential = true
                        });
                    }

                    HttpContext.Session.Set(AdminSessionKey, result);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewData["Message"] = "Bạn không có quyền truy cập!";
                }
            }
            else
            {
                ViewData["Message"] = "Tài khoản hoặc mật khẩu không đúng!";
            }
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove(AdminSessionKey);
            Response.Cookies.Delete("AdminCredential");
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            var adminInfo = HttpContext.Session.Get<User>(AdminSessionKey);
            if (adminInfo?.RoleID == 2)
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            return View();
        }
    }
}
