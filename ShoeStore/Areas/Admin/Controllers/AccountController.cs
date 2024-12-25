using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using Microsoft.AspNetCore.Http;
using ShoeStore.Utils;
using ShoeStore.Models.DTO.Request;
using ShoeStore.Filters;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            var login = Request.Cookies.Get<AdminLoginDTO>("UserCredential");
            if (login != null)
            {
                var result = _context.Users.AsNoTracking()
                    .FirstOrDefault(x => x.Username == login.UserName &&
                            PasswordHelper.VerifyPassword(login.Password, x.PasswordHash));
                if (result != null)
                {
                    HttpContext.Session.Set<User>("userInfo", result);
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
                .FirstOrDefaultAsync(x => x.Username == login.UserName &&
                    PasswordHelper.VerifyPassword(login.Password, x.PasswordHash));

            if (result != null)
            {
                if (result.Role.RoleName == "Admin")
                {
                    if (login.RememberMe)
                    {
                        Response.Cookies.Append("UserCredential", login, new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddYears(7),
                            HttpOnly = true,
                            IsEssential = true
                        });
                    }

                    HttpContext.Session.Set<User>("userInfo", result);
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
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserCredential");
            return RedirectToAction("Login");
        }
    }
}
